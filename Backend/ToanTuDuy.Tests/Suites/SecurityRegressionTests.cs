using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Models;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class SecurityRegressionTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private const string KnownIssueSkipReason =
        "Known issue - xem Document/Review/Backend/05-Security-Gaps-And-Code-Findings.md";

    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(Skip = KnownIssueSkipReason)]
    public async Task TC_SEC_001_Login_RejectsLockedAccountWith401()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);

        Assert.Equal(HttpStatusCode.OK, (await admin.Client.PutAsJsonAsync(
            $"/api/admin/accounts/{parent.AccountId}/status",
            new UpdateAccountStatusRequest { IsActive = false })).StatusCode);

        using var anon = Factory.CreateClient();
        var loginResp = await anon.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Phone = parent.Phone,
            Password = parent.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResp.StatusCode);
    }

    [Fact(Skip = KnownIssueSkipReason)]
    public async Task TC_SEC_002_ParentsController_RequiresAuth_AndNeverExposesPin()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        await parent.Client.PutAsJsonAsync("/api/profile/pin", new { newPin = "123456" });

        using var anon = Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/parents")).StatusCode);

        var parents = (await admin.Client.GetFromJsonAsync<List<ParentResponseDto>>("/api/parents"))!;
        Assert.All(parents, p => Assert.Null(p.Pin));
    }

    [Fact(Skip = KnownIssueSkipReason)]
    public async Task TC_SEC_003_ClassesLessonsAndTeachers_EnforceRoleAuthorization()
    {
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        using var anon = Factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = 1,
            Name = "Unauthorized Class",
            MaxCapacity = 10,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            Status = "OPEN"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await parent.Client.PostAsJsonAsync("/api/lessons", new LessonRequest
        {
            ClassId = 1,
            LessonDate = DateTime.UtcNow.Date.AddDays(1),
            Status = "SCHEDULED"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await parent.Client.PostAsJsonAsync("/api/teachers", new CreateTeacherDto
        {
            Phone = AuthTestHelper.NextPhone("092"),
            Password = "Password@123",
            FullName = "Unauthorized Teacher"
        })).StatusCode);
    }

    [Fact(Skip = KnownIssueSkipReason)]
    public async Task TC_SEC_004_DevSeedController_IsDisabledOutsideDevelopment()
    {
        using var anon = Factory.CreateClient();
        var resp = await anon.PostAsync("/api/dev/seed-data/bogus?rowsPerTable=1&cleanExisting=false", null);
        Assert.True(resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    [Fact(Skip = KnownIssueSkipReason)]
    public async Task TC_SEC_005_Assignments_PreventParentIdorAndAnswerLeak()
    {
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parentA = await AuthTestHelper.RegisterParentAsync(Factory);
        var parentB = await AuthTestHelper.RegisterParentAsync(Factory);

        var studentB = (await (await parentB.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé B" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        int courseId, lessonId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "SEC-C", Name = "Toán SEC", MinAge = 6, MaxAge = 10, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            courseId = course.Id;

            var level = new CourseLevel { CourseId = course.Id, Code = "SEC-L", Name = "L1", Fee = 1000000m, TotalSessions = 1, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp SEC", MaxCapacity = 10, StartDate = DateTime.UtcNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = studentB.Id, Status = "ACTIVE" });

            var lesson = new Lesson { ClassId = cls.Id, Title = "Buổi SEC", LessonDate = DateTime.UtcNow.Date, Status = "SCHEDULED" };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            lessonId = lesson.Id;
        }

        var bank = (await (await teacher.Client.PostAsJsonAsync("/api/question-banks", new QuestionBankRequest
        {
            CourseId = courseId,
            Name = "Bank SEC"
        })).Content.ReadFromJsonAsync<QuestionBankResponse>())!;

        var q = (await (await teacher.Client.PostAsJsonAsync("/api/question-banks/questions", new QuestionRequest
        {
            BankId = bank.Id,
            Type = "FILL_IN",
            Content = "2 + 2 = ?",
            Difficulty = "EASY",
            Answer = "4"
        })).Content.ReadFromJsonAsync<QuestionResponse>())!;

        var asgn = (await (await teacher.Client.PostAsJsonAsync("/api/assignments", new CreateAssignmentRequest
        {
            LessonId = lessonId,
            Title = "Bài SEC",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            Deadline = DateTime.UtcNow.AddHours(2),
            MaxAttempt = 1,
            AssignAllStudentsInClass = true,
            Questions = [new QuestionAssignmentItemDto { QuestionId = q.Id, OrderIndex = 1, PointValue = 10f }]
        })).Content.ReadFromJsonAsync<AssignmentDetailResponse>())!;

        // Parent A must not access Parent B's student submission
        var crossSubmission = await parentA.Client.GetAsync($"/api/assignments/{asgn.Id}/submission/{studentB.Id}");
        Assert.True(crossSubmission.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    [Fact(Skip = KnownIssueSkipReason)]
    public async Task TC_SEC_006_AttendanceByLesson_HandlesCancelledPlusNewLeaveRequestForSameStudent()
    {
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé LeaveDup" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        int lessonId;
        var vnNow = DateTime.UtcNow.AddHours(7);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "SECLR-C", Name = "Toán SECLR", MinAge = 6, MaxAge = 10, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var level = new CourseLevel { CourseId = course.Id, Code = "SECLR-L", Name = "L1", Fee = 1000000m, TotalSessions = 1, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp SECLR", MaxCapacity = 10, StartDate = vnNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = student.Id, Status = "ACTIVE" });

            var lesson = new Lesson { ClassId = cls.Id, Title = "Buổi SECLR", LessonDate = vnNow.AddDays(1).Date, StartTime = vnNow.AddDays(1), EndTime = vnNow.AddDays(1).AddHours(1.5), Status = "SCHEDULED" };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            lessonId = lesson.Id;
        }

        var lr1 = (await (await parent.Client.PostAsJsonAsync("/api/leaverequests", new LeaveRequestCreateRequest
        {
            StudentId = student.Id,
            LessonId = lessonId,
            Reason = "Lần 1"
        })).Content.ReadFromJsonAsync<LeaveRequestResponse>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await parent.Client.DeleteAsync($"/api/leaverequests/{lr1.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await parent.Client.PostAsJsonAsync("/api/leaverequests", new LeaveRequestCreateRequest
        {
            StudentId = student.Id,
            LessonId = lessonId,
            Reason = "Lần 2"
        })).StatusCode);

        var resp = await teacher.Client.GetAsync($"/api/attendances/lesson/{lessonId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
