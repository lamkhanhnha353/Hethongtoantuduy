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
public sealed class LeaveAttendanceAndPayrollTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task LeaveRequest_EnforcesTwoHourRule_AndApproveAutoMarksExcused()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var otherTeacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Nam" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        int classId, soonLessonId, futureLessonId;
        var vnNow = DateTime.UtcNow.AddHours(7);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "LR-C1", Name = "Toán LR", MinAge = 6, MaxAge = 10, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var level = new CourseLevel { CourseId = course.Id, Code = "LR-L1", Name = "Level LR", Fee = 1000000m, TotalSessions = 2, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp LR", MaxCapacity = 10, StartDate = vnNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
            classId = cls.Id;

            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = student.Id, Status = "ACTIVE" });

            var soonLesson = new Lesson
            {
                ClassId = cls.Id,
                Title = "Buổi gần",
                LessonDate = vnNow.AddHours(1).Date,
                StartTime = vnNow.AddHours(1),
                EndTime = vnNow.AddHours(2.5),
                Status = "SCHEDULED"
            };
            var futureLesson = new Lesson
            {
                ClassId = cls.Id,
                Title = "Buổi xa",
                LessonDate = vnNow.AddHours(5).Date,
                StartTime = vnNow.AddHours(5),
                EndTime = vnNow.AddHours(6.5),
                Status = "SCHEDULED"
            };
            db.Lessons.AddRange(soonLesson, futureLesson);
            await db.SaveChangesAsync();

            soonLessonId = soonLesson.Id;
            futureLessonId = futureLesson.Id;
        }

        // < 2 hours before start -> 400
        var tooLate = await parent.Client.PostAsJsonAsync("/api/leaverequests", new LeaveRequestCreateRequest
        {
            StudentId = student.Id,
            LessonId = soonLessonId,
            Reason = "Xin gấp"
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooLate.StatusCode);

        // >= 2 hours -> 200 PENDING
        var okCreate = await parent.Client.PostAsJsonAsync("/api/leaverequests", new LeaveRequestCreateRequest
        {
            StudentId = student.Id,
            LessonId = futureLessonId,
            Reason = "Bận việc gia đình"
        });
        Assert.Equal(HttpStatusCode.OK, okCreate.StatusCode);
        var created = (await okCreate.Content.ReadFromJsonAsync<LeaveRequestResponse>())!;
        Assert.Equal("PENDING", created.Status);

        // Duplicate leave request -> 409
        var dupCreate = await parent.Client.PostAsJsonAsync("/api/leaverequests", new LeaveRequestCreateRequest
        {
            StudentId = student.Id,
            LessonId = futureLessonId,
            Reason = "Gửi lại"
        });
        Assert.Equal(HttpStatusCode.Conflict, dupCreate.StatusCode);

        // Other teacher cannot approve -> 404
        Assert.Equal(HttpStatusCode.NotFound,
            (await otherTeacher.Client.PutAsync($"/api/leaverequests/{created.Id}/process?isApproved=true", null)).StatusCode);

        // Assigned teacher approves -> auto marks EXCUSED
        var approveResp = await teacher.Client.PutAsync($"/api/leaverequests/{created.Id}/process?isApproved=true", null);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);

        var attendanceDetail = (await teacher.Client.GetFromJsonAsync<LessonAttendanceDetailResponse>(
            $"/api/attendances/lesson/{futureLessonId}"))!;
        var item = Assert.Single(attendanceDetail.Students);
        Assert.Equal("EXCUSED", item.Status);
        Assert.Equal("APPROVED", item.LeaveRequestStatus);
    }

    [Fact]
    public async Task Payroll_CalculatesEligibleSessions_EnforcesRbac_AndQueuesEmail()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher1 = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var teacher2 = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Lương" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        var periodMonth = 10;
        var periodYear = 2026;
        var lessonDate = new DateTime(periodYear, periodMonth, 15, 0, 0, 0, DateTimeKind.Utc);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "PAY-LR", Name = "Toán Lương", MinAge = 6, MaxAge = 12, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var level = new CourseLevel { CourseId = course.Id, Code = "PAY-LR-1", Name = "L1", Fee = 1000000m, TotalSessions = 3, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher1.ProfileId, Name = "Lớp Lương", MaxCapacity = 10, StartDate = lessonDate.AddDays(-5), Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = student.Id, Status = "ACTIVE" });

            // 1. Eligible: COMPLETED + has Attendance
            var l1 = new Lesson { ClassId = cls.Id, Title = "Buổi 1", LessonDate = lessonDate, Status = "COMPLETED" };
            // 2. Not eligible: COMPLETED without Attendance
            var l2 = new Lesson { ClassId = cls.Id, Title = "Buổi 2", LessonDate = lessonDate.AddDays(1), Status = "COMPLETED" };
            // 3. Not eligible: SCHEDULED with Attendance
            var l3 = new Lesson { ClassId = cls.Id, Title = "Buổi 3", LessonDate = lessonDate.AddDays(2), Status = "SCHEDULED" };

            db.Lessons.AddRange(l1, l2, l3);
            await db.SaveChangesAsync();

            db.Attendances.AddRange(
                new Attendance { LessonId = l1.Id, StudentId = student.Id, Status = "PRESENT" },
                new Attendance { LessonId = l3.Id, StudentId = student.Id, Status = "PRESENT" });
            await db.SaveChangesAsync();
        }

        // Base rate & adjustments
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.PutAsJsonAsync(
            $"/api/payrolls/teachers/{teacher1.ProfileId}/base-rate",
            new UpdateTeacherBaseRateRequestDto { BaseRatePerSession = 250000m })).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await admin.Client.PostAsJsonAsync(
            "/api/payrolls/adjustments",
            new CreateAdjustmentRequestDto
            {
                TeacherId = teacher1.ProfileId,
                Type = "Bonus",
                Amount = 100000m,
                Reason = "Thưởng chuyên cần",
                AppliedDate = new DateOnly(periodYear, periodMonth, 20)
            })).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await admin.Client.PostAsJsonAsync(
            "/api/payrolls/adjustments",
            new CreateAdjustmentRequestDto
            {
                TeacherId = teacher1.ProfileId,
                Type = "Penalty",
                Amount = 50000m,
                Reason = "Đi trễ",
                AppliedDate = new DateOnly(periodYear, periodMonth, 21)
            })).StatusCode);

        // Monthly summary calculation
        var summaries = (await admin.Client.GetFromJsonAsync<List<PayslipDto>>(
            $"/api/payrolls/admin/summary?month={periodMonth}&year={periodYear}"))!;
        var t1Payslip = summaries.Single(p => p.TeacherId == teacher1.ProfileId);
        Assert.Equal(1, t1Payslip.TotalValidSessions);
        Assert.Equal(250000m, t1Payslip.BaseSalary);
        Assert.Equal(100000m, t1Payslip.TotalBonus);
        Assert.Equal(50000m, t1Payslip.TotalPenalty);
        Assert.Equal(300000m, t1Payslip.FinalSalary);

        // RBAC: Teacher 2 cannot view Teacher 1's payslip detail -> 403
        var forbiddenDetail = await teacher2.Client.GetAsync(
            $"/api/payrolls/detail?teacherId={teacher1.ProfileId}&month={periodMonth}&year={periodYear}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDetail.StatusCode);

        // Teacher 1 can view own payslip detail -> 200
        var ownDetail = await teacher1.Client.GetFromJsonAsync<PayslipDetailDto>(
            $"/api/payrolls/detail?teacherId={teacher1.ProfileId}&month={periodMonth}&year={periodYear}");
        Assert.Equal(300000m, ownDetail!.Payslip.FinalSalary);

        // Preview HTML & Queue email
        var htmlPreview = await admin.Client.GetStringAsync($"/api/payrolls/{t1Payslip.PayrollId}/preview-email");
        Assert.Contains(teacher1.Email, htmlPreview);

        var sendResp = await admin.Client.PostAsync($"/api/payrolls/{t1Payslip.PayrollId}/send-email", null);
        Assert.Equal(HttpStatusCode.Accepted, sendResp.StatusCode);

        await ApiAssertHelper.EventuallyAsync(() =>
            Task.FromResult(Factory.FakeEmail.SentMessages.Any(m =>
                m.ToEmail == teacher1.Email && m.Subject.Contains("Phiếu lương"))));
    }
}
