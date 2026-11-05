using System.Data;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services;

public sealed class PayrollService(ApplicationDbContext context) : IPayrollService
{
    public async Task<IReadOnlyList<PayslipDto>> GetMonthlyPayrollsAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(month, year);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var teachers = await context.TeacherProfiles
            .AsNoTracking()
            .Select(teacher => new
            {
                teacher.Id,
                teacher.FullName,
                TeacherEmail = teacher.Email ?? teacher.Account.Email
            })
            .OrderBy(teacher => teacher.FullName)
            .ToListAsync(cancellationToken);

        var teacherIds = teachers.Select(teacher => teacher.Id).ToArray();
        var (startDate, endDate) = GetPeriod(month, year);

        var rates = await context.TeacherSalaryConfigs
            .AsNoTracking()
            .Where(config => teacherIds.Contains(config.TeacherId))
            .ToDictionaryAsync(config => config.TeacherId, config => config.BaseRatePerSession, cancellationToken);

        var sessionCounts = await context.Lessons
            .AsNoTracking()
            .Where(lesson =>
                lesson.Class.TeacherId.HasValue
                && teacherIds.Contains(lesson.Class.TeacherId.Value)
                && lesson.LessonDate >= startDate
                && lesson.LessonDate < endDate
                && lesson.Status == "COMPLETED"
                && lesson.Attendances.Any())
            .GroupBy(lesson => lesson.Class.TeacherId!.Value)
            .Select(group => new { TeacherId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.TeacherId, group => group.Count, cancellationToken);

        var adjustments = await context.SalaryAdjustments
            .AsNoTracking()
            .Where(adjustment =>
                teacherIds.Contains(adjustment.TeacherId)
                && adjustment.AppliedDate >= DateOnly.FromDateTime(startDate)
                && adjustment.AppliedDate < DateOnly.FromDateTime(endDate))
            .ToListAsync(cancellationToken);

        var summaries = await context.PayrollSummaries
            .Where(payroll => payroll.Month == month && payroll.Year == year)
            .ToDictionaryAsync(payroll => payroll.TeacherId, cancellationToken);

        var resultItems = new List<(PayrollSummary Summary, string Name, string? Email, decimal Rate)>(teachers.Count);
        foreach (var teacher in teachers)
        {
            var sessionCount = sessionCounts.GetValueOrDefault(teacher.Id);
            var rate = rates.GetValueOrDefault(teacher.Id);
            var teacherAdjustments = adjustments.Where(adjustment => adjustment.TeacherId == teacher.Id);
            var totalPenalty = teacherAdjustments
                .Where(adjustment => adjustment.Type == "Penalty")
                .Sum(adjustment => adjustment.Amount);
            var totalBonus = teacherAdjustments
                .Where(adjustment => adjustment.Type == "Bonus")
                .Sum(adjustment => adjustment.Amount);
            var baseSalary = sessionCount * rate;
            var finalSalary = baseSalary + totalBonus - totalPenalty;

            if (!summaries.TryGetValue(teacher.Id, out var summary))
            {
                summary = new PayrollSummary
                {
                    TeacherId = teacher.Id,
                    Month = month,
                    Year = year,
                    EmailStatus = "NotSent"
                };
                context.PayrollSummaries.Add(summary);
                summaries.Add(teacher.Id, summary);
            }

            var financialsChanged = summary.TotalValidSessions != sessionCount
                || summary.BaseSalary != baseSalary
                || summary.TotalPenalty != totalPenalty
                || summary.TotalBonus != totalBonus
                || summary.FinalSalary != finalSalary;

            summary.TotalValidSessions = sessionCount;
            summary.BaseSalary = baseSalary;
            summary.TotalPenalty = totalPenalty;
            summary.TotalBonus = totalBonus;
            summary.FinalSalary = finalSalary;

            if (financialsChanged && summary.EmailStatus is "Sent" or "Failed")
            {
                summary.EmailStatus = "NotSent";
                summary.EmailSentAt = null;
                summary.EmailErrorMessage = null;
            }

            resultItems.Add((summary, teacher.FullName, teacher.TeacherEmail, rate));
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return resultItems
            .Select(item => ToPayslipDto(item.Summary, item.Name, item.Email, item.Rate))
            .ToList();
    }

    public async Task<PayslipDetailDto> GetTeacherPayslipDetailAsync(
        int teacherId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(month, year);
        if (teacherId <= 0)
        {
            throw ApiException.BadRequest("Mã giáo viên không hợp lệ.");
        }

        var teacher = await context.TeacherProfiles
            .AsNoTracking()
            .Where(item => item.Id == teacherId)
            .Select(item => new { item.Id, item.FullName, TeacherEmail = item.Email ?? item.Account.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (teacher is null)
        {
            throw ApiException.NotFound("Không tìm thấy giáo viên.");
        }

        var (startDate, endDate) = GetPeriod(month, year);
        var lessons = await context.Lessons
            .AsNoTracking()
            .Where(lesson =>
                lesson.Class.TeacherId == teacherId
                && lesson.LessonDate >= startDate
                && lesson.LessonDate < endDate)
            .OrderBy(lesson => lesson.LessonDate)
            .ThenBy(lesson => lesson.StartTime)
            .Select(lesson => new
            {
                lesson.Id,
                lesson.Title,
                lesson.LessonDate,
                lesson.Status,
                HasAttendance = lesson.Attendances.Any()
            })
            .ToListAsync(cancellationToken);

        var adjustments = await context.SalaryAdjustments
            .AsNoTracking()
            .Where(adjustment =>
                adjustment.TeacherId == teacherId
                && adjustment.AppliedDate >= DateOnly.FromDateTime(startDate)
                && adjustment.AppliedDate < DateOnly.FromDateTime(endDate))
            .OrderBy(adjustment => adjustment.AppliedDate)
            .ThenBy(adjustment => adjustment.Id)
            .Select(adjustment => new SalaryAdjustmentDto(
                adjustment.Id,
                adjustment.TeacherId,
                adjustment.Type,
                adjustment.Amount,
                adjustment.Reason,
                adjustment.AppliedDate))
            .ToListAsync(cancellationToken);

        var rate = await context.TeacherSalaryConfigs
            .AsNoTracking()
            .Where(config => config.TeacherId == teacherId)
            .Select(config => (decimal?)config.BaseRatePerSession)
            .SingleOrDefaultAsync(cancellationToken) ?? 0m;

        var eligibleSessions = lessons.Count(lesson =>
            lesson.Status == "COMPLETED" && lesson.HasAttendance);
        var totalPenalty = adjustments
            .Where(adjustment => adjustment.Type == "Penalty")
            .Sum(adjustment => adjustment.Amount);
        var totalBonus = adjustments
            .Where(adjustment => adjustment.Type == "Bonus")
            .Sum(adjustment => adjustment.Amount);
        var baseSalary = eligibleSessions * rate;
        var summary = await context.PayrollSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                payroll => payroll.TeacherId == teacherId && payroll.Month == month && payroll.Year == year,
                cancellationToken);

        var payslip = new PayslipDto(
            summary?.Id ?? 0,
            teacher.Id,
            teacher.FullName,
            teacher.TeacherEmail,
            month,
            year,
            eligibleSessions,
            rate,
            baseSalary,
            totalPenalty,
            totalBonus,
            baseSalary + totalBonus - totalPenalty,
            summary?.EmailStatus ?? "NotSent",
            summary?.EmailSentAt is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(summary.EmailSentAt.Value, DateTimeKind.Utc)),
            summary?.EmailErrorMessage);

        var sessionDetails = lessons
            .Select(lesson =>
            {
                var eligible = lesson.Status == "COMPLETED" && lesson.HasAttendance;
                return new SessionDetailDto(
                    lesson.Id,
                    lesson.Title,
                    new DateTimeOffset(DateTime.SpecifyKind(lesson.LessonDate, DateTimeKind.Utc)),
                    lesson.Status,
                    lesson.HasAttendance,
                    eligible);
            })
            .ToList();

        return new PayslipDetailDto(payslip, sessionDetails, adjustments);
    }

    public async Task<SalaryAdjustmentDto> AddSalaryAdjustmentAsync(
        CreateAdjustmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TeacherId <= 0)
        {
            throw ApiException.BadRequest("Mã giáo viên không hợp lệ.");
        }

        var type = request.Type.Trim();
        if (!type.Equals("Penalty", StringComparison.OrdinalIgnoreCase)
            && !type.Equals("Bonus", StringComparison.OrdinalIgnoreCase))
        {
            throw ApiException.BadRequest("Loại điều chỉnh chỉ nhận Penalty hoặc Bonus.");
        }

        if (request.Amount <= 0)
        {
            throw ApiException.BadRequest("Số tiền điều chỉnh phải lớn hơn 0.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
        {
            throw ApiException.BadRequest("Lý do điều chỉnh không được để trống và tối đa 500 ký tự.");
        }

        if (request.AppliedDate == default)
        {
            throw ApiException.BadRequest("Ngày áp dụng không hợp lệ.");
        }

        var teacherExists = await context.TeacherProfiles
            .AnyAsync(teacher => teacher.Id == request.TeacherId, cancellationToken);
        if (!teacherExists)
        {
            throw ApiException.NotFound("Không tìm thấy giáo viên.");
        }

        var adjustment = new SalaryAdjustment
        {
            TeacherId = request.TeacherId,
            Type = type.Equals("Penalty", StringComparison.OrdinalIgnoreCase) ? "Penalty" : "Bonus",
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Reason = request.Reason.Trim(),
            AppliedDate = request.AppliedDate
        };

        context.SalaryAdjustments.Add(adjustment);
        await context.SaveChangesAsync(cancellationToken);

        return new SalaryAdjustmentDto(
            adjustment.Id,
            adjustment.TeacherId,
            adjustment.Type,
            adjustment.Amount,
            adjustment.Reason,
            adjustment.AppliedDate);
    }

    public Task<int?> GetTeacherIdForAccountAsync(
        int accountId,
        CancellationToken cancellationToken = default) =>
        context.TeacherProfiles
            .AsNoTracking()
            .Where(teacher => teacher.AccountId == accountId)
            .Select(teacher => (int?)teacher.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private static PayslipDto ToPayslipDto(
        PayrollSummary summary,
        string teacherName,
        string? teacherEmail,
        decimal baseRatePerSession) =>
        new(
            summary.Id,
            summary.TeacherId,
            teacherName,
            teacherEmail,
            summary.Month,
            summary.Year,
            summary.TotalValidSessions,
            baseRatePerSession,
            summary.BaseSalary,
            summary.TotalPenalty,
            summary.TotalBonus,
            summary.FinalSalary,
            summary.EmailStatus,
            summary.EmailSentAt is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(summary.EmailSentAt.Value, DateTimeKind.Utc)),
            summary.EmailErrorMessage);

    private static (DateTime StartDate, DateTime EndDate) GetPeriod(int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (startDate, startDate.AddMonths(1));
    }

    private static void ValidatePeriod(int month, int year)
    {
        if (month is < 1 or > 12 || year is < 1 or > 9998)
        {
            throw ApiException.BadRequest("Tháng hoặc năm không hợp lệ.");
        }
    }
}
