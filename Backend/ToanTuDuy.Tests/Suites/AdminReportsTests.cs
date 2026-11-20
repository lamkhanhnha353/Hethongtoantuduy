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
public sealed class AdminReportsTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AdminReports_RevenuePayrollStudentGrowthAttendance_MatchDbAndEnforceValidation()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Report" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        // Non-Admin blocked 403
        Assert.Equal(HttpStatusCode.Forbidden,
            (await parent.Client.GetAsync("/api/admin/reports/revenue?months=6")).StatusCode);

        // Out-of-range query params -> 400
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.Client.GetAsync("/api/admin/reports/revenue?months=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.Client.GetAsync("/api/admin/reports/attendance?days=0")).StatusCode);

        var vnNow = DateTime.UtcNow.AddHours(7);
        int classId;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "RPT-C", Name = "Toán Report", MinAge = 6, MaxAge = 12, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var level = new CourseLevel { CourseId = course.Id, Code = "RPT-L", Name = "L1", Fee = 1800000m, TotalSessions = 4, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp Report", MaxCapacity = 10, StartDate = vnNow.Date.AddDays(-3), Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
            classId = cls.Id;

            db.ClassStudents.Add(new ClassStudent
            {
                ClassId = cls.Id,
                StudentId = student.Id,
                Status = "ACTIVE",
                JoinedAt = DateTime.UtcNow.AddDays(-1)
            });

            db.Invoices.AddRange(
                new Invoice
                {
                    ParentId = parent.ProfileId,
                    StudentId = student.Id,
                    ClassId = cls.Id,
                    Amount = 1800000,
                    Status = "PAID",
                    PaymentMethod = "VNPAY",
                    TxnRef = "RPT_PAID_1",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    PaidAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new Invoice
                {
                    ParentId = parent.ProfileId,
                    StudentId = student.Id,
                    ClassId = cls.Id,
                    Amount = 500000,
                    Status = "PENDING",
                    TxnRef = "RPT_PEND_1",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                });

            var lesson = new Lesson
            {
                ClassId = cls.Id,
                Title = "Buổi Report",
                LessonDate = vnNow.Date.AddDays(-1),
                Status = "COMPLETED"
            };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();

            db.Attendances.Add(new Attendance
            {
                LessonId = lesson.Id,
                StudentId = student.Id,
                Status = "PRESENT"
            });

            db.PayrollSummaries.Add(new PayrollSummary
            {
                TeacherId = teacher.ProfileId,
                Month = vnNow.Month,
                Year = vnNow.Year,
                TotalValidSessions = 1,
                BaseSalary = 250000m,
                TotalBonus = 50000m,
                TotalPenalty = 0m,
                FinalSalary = 300000m,
                EmailStatus = "NotSent"
            });

            await db.SaveChangesAsync();
        }

        // TC-PAY-006: Revenue report
        var revenue = (await admin.Client.GetFromJsonAsync<AdminRevenueReportResponse>("/api/admin/reports/revenue?months=6"))!;
        Assert.Equal(1800000m, revenue.TotalRevenue);
        Assert.Equal(1, revenue.PaidInvoiceCount);
        Assert.Equal(1, revenue.PendingInvoiceCount);
        Assert.Equal(6, revenue.MonthlyRevenue.Count);

        // Payroll trend
        var payroll = (await admin.Client.GetFromJsonAsync<AdminPayrollTrendResponse>("/api/admin/reports/payroll?months=6"))!;
        Assert.Equal(6, payroll.MonthlyPayroll.Count);
        Assert.Equal(300000m, payroll.MonthlyPayroll.Last().Amount);

        // Student growth
        var growth = (await admin.Client.GetFromJsonAsync<AdminStudentGrowthReportResponse>("/api/admin/reports/student-growth?months=6"))!;
        Assert.Equal(1, growth.ActiveStudentCount);
        Assert.Equal(1, growth.NewEnrollmentCount);
        Assert.Equal(1, growth.ClassCount);

        // Attendance report
        var attendance = (await admin.Client.GetFromJsonAsync<AdminAttendanceReportResponse>($"/api/admin/reports/attendance?days=90&classId={classId}"))!;
        Assert.Equal(1, attendance.LessonCount);
        Assert.Equal(1, attendance.PresentCount);
        Assert.Equal(0, attendance.AbsentCount);
    }
}
