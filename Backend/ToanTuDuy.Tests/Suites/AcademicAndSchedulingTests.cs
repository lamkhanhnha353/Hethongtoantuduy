using System.Net;
using System.Net.Http.Json;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class AcademicAndSchedulingTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CoursesAndLevels_EnforceAdminRbac_And_DeleteConstraints()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);

        var courseReq = new CourseRequest
        {
            Code = "SRB-01",
            Name = "Soroban Cơ Bản",
            MinAge = 5,
            MaxAge = 12,
            Status = "ACTIVE"
        };

        Assert.Equal(HttpStatusCode.Forbidden, (await parent.Client.PostAsJsonAsync("/api/courses", courseReq)).StatusCode);

        var createCourse = await admin.Client.PostAsJsonAsync("/api/courses", courseReq);
        Assert.Equal(HttpStatusCode.Created, createCourse.StatusCode);
        var course = (await createCourse.Content.ReadFromJsonAsync<CourseResponse>())!;

        var createLevel = await admin.Client.PostAsJsonAsync("/api/courselevels", new CourseLevelRequest
        {
            CourseId = course.Id,
            Code = "SRB-L1",
            Name = "Cấp độ 1",
            Fee = 1200000m,
            TotalSessions = 4,
            OrderIndex = 1,
            Status = "ACTIVE",
            Syllabus = new[] { new { title = "Làm quen bàn tính" }, new { title = "Cộng trừ cơ bản" } }
        });
        Assert.Equal(HttpStatusCode.Created, createLevel.StatusCode);

        // Cannot delete Course while it still has CourseLevels
        var delCourse = await admin.Client.DeleteAsync($"/api/courses/{course.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, delCourse.StatusCode);
    }

    [Fact]
    public async Task ClassSchedules_EnforceRbacInProduction_DetectConflicts_AndAutoGenerateLessons()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Minh" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        var course = (await (await admin.Client.PostAsJsonAsync("/api/courses", new CourseRequest
        {
            Code = "FGM-01",
            Name = "Finger Math",
            MinAge = 4,
            MaxAge = 8,
            Status = "ACTIVE"
        })).Content.ReadFromJsonAsync<CourseResponse>())!;

        var level = (await (await admin.Client.PostAsJsonAsync("/api/courselevels", new CourseLevelRequest
        {
            CourseId = course.Id,
            Code = "FGM-L1",
            Name = "Finger Math Level 1",
            Fee = 1500000m,
            TotalSessions = 4,
            OrderIndex = 1,
            Status = "ACTIVE",
            Syllabus = new[] { new { title = "Chủ đề 1" }, new { title = "Chủ đề 2" } }
        })).Content.ReadFromJsonAsync<CourseLevelResponse>())!;

        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var class1 = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher.ProfileId,
            Name = "Lớp FGM-A",
            MaxCapacity = 5,
            StartDate = startDate,
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        var class2 = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher.ProfileId,
            Name = "Lớp FGM-B",
            MaxCapacity = 5,
            StartDate = startDate,
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        // Non-Admin blocked by AdminOrDevelopmentTesting in Production factory
        var scheduleReq = new ClassScheduleRequest
        {
            ClassId = class1.Id,
            DayOfWeek = (int)startDate.DayOfWeek,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Room = "P101"
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.Client.PostAsJsonAsync("/api/classschedules", scheduleReq)).StatusCode);

        // Admin creates schedule -> auto generates 4 lessons and updates EndDate
        var createSched = await admin.Client.PostAsJsonAsync("/api/classschedules", scheduleReq);
        Assert.Equal(HttpStatusCode.Created, createSched.StatusCode);

        var lessons = (await admin.Client.GetFromJsonAsync<List<LessonResponse>>($"/api/lessons/class/{class1.Id}"))!;
        Assert.Equal(4, lessons.Count);
        Assert.All(lessons, l => Assert.Equal("SCHEDULED", l.Status));

        var updatedClass1 = (await admin.Client.GetFromJsonAsync<ClassResponse>($"/api/classes/{class1.Id}"))!;
        Assert.Equal(lessons.Max(l => l.LessonDate.Date), updatedClass1.EndDate!.Value.Date);

        // Teacher schedule conflict on Class 2 -> 409 Conflict
        var teacherConflict = await admin.Client.PostAsJsonAsync("/api/classschedules", new ClassScheduleRequest
        {
            ClassId = class2.Id,
            DayOfWeek = (int)startDate.DayOfWeek,
            StartTime = new TimeSpan(9, 30, 0),
            EndTime = new TimeSpan(11, 0, 0),
            Room = "P202"
        });
        Assert.Equal(HttpStatusCode.Conflict, teacherConflict.StatusCode);

        // Enroll student & check roadmap (1st IN_PROGRESS, 2..4 LOCKED)
        var addStudent = await admin.Client.PostAsJsonAsync($"/api/classes/{class1.Id}/students", new AddClassStudentRequest
        {
            StudentId = student.Id
        });
        Assert.Equal(HttpStatusCode.OK, addStudent.StatusCode);

        var roadmaps = (await parent.Client.GetFromJsonAsync<List<StudentRoadmapResponse>>($"/api/lessons/student/{student.Id}/roadmap"))!;
        var roadmap = Assert.Single(roadmaps);
        Assert.Equal(4, roadmap.TotalSessions);
        Assert.Equal("IN_PROGRESS", roadmap.Lessons[0].Status);
        Assert.Equal("Chủ đề 1", roadmap.Lessons[0].Title);
        Assert.Equal("LOCKED", roadmap.Lessons[1].Status);
        Assert.Equal("Chủ đề 2", roadmap.Lessons[1].Title);
        Assert.Equal("LOCKED", roadmap.Lessons[2].Status);
    }
}
