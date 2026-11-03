using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToanTuDuy.Models;

[Table("TEACHER_SALARY_CONFIG")]
public sealed class TeacherSalaryConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Column("base_rate_per_session", TypeName = "decimal(18,2)")]
    public decimal BaseRatePerSession { get; set; }

    [ForeignKey(nameof(TeacherId))]
    public TeacherProfile Teacher { get; set; } = null!;
}

[Table("SALARY_ADJUSTMENT")]
public sealed class SalaryAdjustment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Column("type")]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("reason")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Column("applied_date", TypeName = "date")]
    public DateOnly AppliedDate { get; set; }

    [ForeignKey(nameof(TeacherId))]
    public TeacherProfile Teacher { get; set; } = null!;
}

[Table("PAYROLL_SUMMARY")]
public sealed class PayrollSummary
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("total_valid_sessions")]
    public int TotalValidSessions { get; set; }

    [Column("base_salary", TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; }

    [Column("total_penalty", TypeName = "decimal(18,2)")]
    public decimal TotalPenalty { get; set; }

    [Column("total_bonus", TypeName = "decimal(18,2)")]
    public decimal TotalBonus { get; set; }

    [Column("final_salary", TypeName = "decimal(18,2)")]
    public decimal FinalSalary { get; set; }

    [Column("email_status")]
    [MaxLength(20)]
    public string EmailStatus { get; set; } = "NotSent";

    [Column("email_sent_at")]
    public DateTime? EmailSentAt { get; set; }

    [Column("email_error_message")]
    [MaxLength(2000)]
    public string? EmailErrorMessage { get; set; }

    [ForeignKey(nameof(TeacherId))]
    public TeacherProfile Teacher { get; set; } = null!;
}
