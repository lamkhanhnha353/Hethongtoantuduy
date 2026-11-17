using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services;

public sealed class AdminReportService(ApplicationDbContext context) : IAdminReportService
{
    public async Task<AdminRevenueReportResponse> GetRevenueAsync(int months)
    {
        var now = DateTime.UtcNow;
        var localNow = now.AddHours(7);
        var firstMonthLocal = new DateTime(localNow.Year, localNow.Month, 1)
            .AddMonths(1 - months);
        var firstMonthUtc = firstMonthLocal.AddHours(-7);

        var paidInvoices = await context.Invoices
            .AsNoTracking()
            .Where(invoice =>
                invoice.Status == "PAID"
                && (invoice.PaidAt ?? invoice.CreatedAt) >= firstMonthUtc
                && (invoice.PaidAt ?? invoice.CreatedAt) <= now)
            .OrderByDescending(invoice => invoice.PaidAt ?? invoice.CreatedAt)
            .Select(invoice => new AdminInvoiceResponse
            {
                InvoiceId = invoice.Id,
                TxnRef = invoice.TxnRef,
                Amount = invoice.Amount,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt,
                PaidAt = invoice.PaidAt,
                PaymentMethod = invoice.PaymentMethod,
                TransactionId = invoice.TransactionId,
                ParentName = invoice.Parent.FullName,
                ParentEmail = invoice.Parent.Account.Email,
                ParentPhone = invoice.Parent.Account.PhoneNumber,
                StudentName = invoice.Student.FullName,
                ClassName = invoice.Class.Name,
                CourseLevelName = invoice.Class.CourseLevel.Name
            })
            .ToListAsync();

        var pendingInvoiceCount = await context.Invoices
            .AsNoTracking()
            .CountAsync(invoice => invoice.Status == "PENDING");

        var monthlyRevenue = Enumerable.Range(0, months)
            .Select(index => firstMonthLocal.AddMonths(index))
            .Select(month => new AdminMonthlyRevenue
            {
                Month = month,
                Amount = paidInvoices
                    .Where(invoice =>
                    {
                        var paidAt = (invoice.PaidAt ?? invoice.CreatedAt).AddHours(7);
                        return paidAt.Year == month.Year && paidAt.Month == month.Month;
                    })
                    .Sum(invoice => (decimal)invoice.Amount)
            })
            .ToList();

        var topClasses = paidInvoices
            .GroupBy(invoice => invoice.ClassName)
            .Select(group => new AdminClassRevenue
            {
                Name = string.IsNullOrWhiteSpace(group.Key) ? "Chưa phân lớp" : group.Key,
                Amount = group.Sum(invoice => (decimal)invoice.Amount)
            })
            .OrderByDescending(item => item.Amount)
            .Take(5)
            .ToList();

        return new AdminRevenueReportResponse
        {
            TotalRevenue = paidInvoices.Sum(invoice => (decimal)invoice.Amount),
            PaidInvoiceCount = paidInvoices.Count,
            PendingInvoiceCount = pendingInvoiceCount,
            MonthlyRevenue = monthlyRevenue,
            TopClasses = topClasses,
            PaidInvoices = paidInvoices
        };
    }

    public async Task<AdminPayrollTrendResponse> GetPayrollTrendAsync(int months)
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        var firstMonth = new DateTime(localNow.Year, localNow.Month, 1)
            .AddMonths(1 - months);

        var persistedTotals = await context.PayrollSummaries
            .AsNoTracking()
            .Where(summary =>
                (summary.Year > firstMonth.Year
                    || (summary.Year == firstMonth.Year && summary.Month >= firstMonth.Month))
                && (summary.Year < localNow.Year
                    || (summary.Year == localNow.Year && summary.Month <= localNow.Month)))
            .GroupBy(summary => new { summary.Year, summary.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(summary => summary.FinalSalary)
            })
            .ToDictionaryAsync(
                item => (item.Year, item.Month),
                item => item.Amount);

        var monthlyPayroll = Enumerable.Range(0, months)
            .Select(index => firstMonth.AddMonths(index))
            .Select(month => new AdminMonthlyPayroll
            {
                Month = month,
                Amount = persistedTotals.GetValueOrDefault((month.Year, month.Month))
            })
            .ToList();

        return new AdminPayrollTrendResponse { MonthlyPayroll = monthlyPayroll };
    }

    public async Task<AdminStudentGrowthReportResponse> GetStudentGrowthAsync(int months)
    {
        var now = DateTime.UtcNow;
        var localNow = now.AddHours(7);
        var firstMonthLocal = new DateTime(localNow.Year, localNow.Month, 1)
            .AddMonths(1 - months);
        var firstMonthUtc = firstMonthLocal.AddHours(-7);

        var classSummaries = await context.Classes
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new AdminClassEnrollmentSummary
            {
                Id = item.Id,
                Name = item.Name,
                Count = item.ClassStudents.Count(student =>
                    student.Status == "ACTIVE" || student.Status == "PENDING")
            })
            .ToListAsync();

        var activeStudentCount = await context.ClassStudents
            .AsNoTracking()
            .Where(item => item.Status == "ACTIVE")
            .Select(item => item.StudentId)
            .Distinct()
            .CountAsync();

        var enrollments = await context.ClassStudents
            .AsNoTracking()
            .Where(item =>
                item.Status == "ACTIVE"
                && item.JoinedAt >= firstMonthUtc
                && item.JoinedAt <= now)
            .OrderByDescending(item => item.JoinedAt)
            .Select(item => new AdminRecentEnrollment
            {
                ClassId = item.ClassId,
                StudentId = item.StudentId,
                StudentName = item.Student.FullName,
                ClassName = item.Class.Name,
                JoinedAt = item.JoinedAt,
                Status = item.Status
            })
            .ToListAsync();

        var monthlyEnrollments = Enumerable.Range(0, months)
            .Select(index => firstMonthLocal.AddMonths(index))
            .Select(month => new AdminMonthlyEnrollment
            {
                Month = month,
                Count = enrollments.Count(item =>
                {
                    var joinedAt = item.JoinedAt.AddHours(7);
                    return joinedAt.Year == month.Year && joinedAt.Month == month.Month;
                })
            })
            .ToList();

        return new AdminStudentGrowthReportResponse
        {
            ActiveStudentCount = activeStudentCount,
            NewEnrollmentCount = enrollments.Count,
            ClassCount = classSummaries.Count,
            MonthlyEnrollments = monthlyEnrollments,
            ClassSummaries = classSummaries,
            RecentEnrollments = enrollments.Take(50).ToList()
        };
    }

    public async Task<AdminAttendanceReportResponse> GetAttendanceAsync(int days, int? classId)
    {
        var now = DateTime.UtcNow;
        var from = now.AddDays(-days);

        var classes = await context.Classes
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new AdminReportClassOption
            {
                Id = item.Id,
                Name = item.Name
            })
            .ToListAsync();

        var lessons = await context.Lessons
            .AsNoTracking()
            .Where(lesson =>
                lesson.LessonDate >= from
                && lesson.LessonDate <= now
                && (!classId.HasValue || lesson.ClassId == classId.Value)
                && lesson.Attendances.Any())
            .OrderByDescending(lesson => lesson.LessonDate)
            .Select(lesson => new AdminLessonAttendanceSummary
            {
                LessonId = lesson.Id,
                ClassId = lesson.ClassId,
                ClassName = lesson.Class.Name,
                LessonDate = lesson.LessonDate,
                TotalStudents = lesson.Class.ClassStudents.Count(student => student.Status == "ACTIVE"),
                PresentCount = lesson.Attendances.Count(attendance => attendance.Status == "PRESENT"),
                AbsentCount = lesson.Attendances.Count(attendance => attendance.Status == "ABSENT"),
                ExcusedCount = lesson.Attendances.Count(attendance => attendance.Status == "EXCUSED"),
                LateCount = lesson.Attendances.Count(attendance => attendance.Status == "LATE")
            })
            .ToListAsync();

        foreach (var lesson in lessons)
        {
            lesson.UnmarkedCount = Math.Max(
                0,
                lesson.TotalStudents
                    - lesson.PresentCount
                    - lesson.AbsentCount
                    - lesson.ExcusedCount
                    - lesson.LateCount);
            lesson.Rate = lesson.TotalStudents == 0
                ? 0
                : (int)Math.Round(lesson.PresentCount * 100d / lesson.TotalStudents);
        }

        return new AdminAttendanceReportResponse
        {
            Classes = classes,
            LessonCount = lessons.Count,
            TotalStudents = lessons.Sum(item => item.TotalStudents),
            PresentCount = lessons.Sum(item => item.PresentCount),
            AbsentCount = lessons.Sum(item => item.AbsentCount),
            ExcusedCount = lessons.Sum(item => item.ExcusedCount),
            LateCount = lessons.Sum(item => item.LateCount),
            UnmarkedCount = lessons.Sum(item => item.UnmarkedCount),
            Lessons = lessons
        };
    }
}
