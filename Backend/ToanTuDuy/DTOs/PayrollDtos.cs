using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs;

/// <summary>Monthly payroll totals for one teacher.</summary>
public sealed record PayslipDto(
    int PayrollId,
    int TeacherId,
    string TeacherName,
    string? TeacherEmail,
    int Month,
    int Year,
    int TotalValidSessions,
    decimal BaseRatePerSession,
    decimal BaseSalary,
    decimal TotalPenalty,
    decimal TotalBonus,
    decimal FinalSalary,
    string EmailStatus,
    DateTimeOffset? EmailSentAt,
    string? EmailErrorMessage);

/// <summary>Lesson detail included in a teacher's monthly payslip.</summary>
public sealed record SessionDetailDto(
    int LessonId,
    string? Title,
    DateTimeOffset LessonDate,
    string Status,
    bool HasAttendance,
    bool IsEligible);

/// <summary>Salary adjustment applied to a teacher.</summary>
public sealed record SalaryAdjustmentDto(
    int Id,
    int TeacherId,
    string Type,
    decimal Amount,
    string Reason,
    DateOnly AppliedDate);

/// <summary>Detailed payslip with the sessions and adjustments used in its calculation.</summary>
public sealed record PayslipDetailDto(
    PayslipDto Payslip,
    IReadOnlyList<SessionDetailDto> Sessions,
    IReadOnlyList<SalaryAdjustmentDto> Adjustments);

/// <summary>Request to add a penalty or bonus to a teacher's payroll.</summary>
public sealed record CreateAdjustmentRequestDto
{
    [Range(1, int.MaxValue)]
    public int TeacherId { get; init; }

    [Required]
    [RegularExpression("^(Penalty|Bonus)$", ErrorMessage = "Type chỉ nhận Penalty hoặc Bonus.")]
    public string Type { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Amount { get; init; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;

    public DateOnly AppliedDate { get; init; }
}

/// <summary>Request to set a teacher's salary rate for each eligible session.</summary>
public sealed record UpdateTeacherBaseRateRequestDto
{
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal BaseRatePerSession { get; init; }
}

/// <summary>Configured salary rate for one teacher.</summary>
public sealed record TeacherBaseRateDto(int TeacherId, decimal BaseRatePerSession);

/// <summary>Number of payslip emails accepted or skipped by the background queue.</summary>
public sealed record QueueBatchEmailsResponse(int QueuedCount, int SkippedCount);
