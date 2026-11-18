namespace ToanTuDuy.DTOs;

public sealed class AdminRevenueReportResponse
{
    public decimal TotalRevenue { get; set; }
    public int PaidInvoiceCount { get; set; }
    public int PendingInvoiceCount { get; set; }
    public List<AdminMonthlyRevenue> MonthlyRevenue { get; set; } = [];
    public List<AdminClassRevenue> TopClasses { get; set; } = [];
    public List<AdminInvoiceResponse> PaidInvoices { get; set; } = [];
}

public sealed class AdminMonthlyRevenue
{
    public DateTime Month { get; set; }
    public decimal Amount { get; set; }
}

public sealed class AdminPayrollTrendResponse
{
    public List<AdminMonthlyPayroll> MonthlyPayroll { get; set; } = [];
}

public sealed class AdminMonthlyPayroll
{
    public DateTime Month { get; set; }
    public decimal Amount { get; set; }
}

public sealed class AdminClassRevenue
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class AdminStudentGrowthReportResponse
{
    public int ActiveStudentCount { get; set; }
    public int NewEnrollmentCount { get; set; }
    public int ClassCount { get; set; }
    public List<AdminMonthlyEnrollment> MonthlyEnrollments { get; set; } = [];
    public List<AdminClassEnrollmentSummary> ClassSummaries { get; set; } = [];
    public List<AdminRecentEnrollment> RecentEnrollments { get; set; } = [];
}

public sealed class AdminMonthlyEnrollment
{
    public DateTime Month { get; set; }
    public int Count { get; set; }
}

public sealed class AdminClassEnrollmentSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class AdminRecentEnrollment
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class AdminAttendanceReportResponse
{
    public List<AdminReportClassOption> Classes { get; set; } = [];
    public int LessonCount { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int LateCount { get; set; }
    public int UnmarkedCount { get; set; }
    public List<AdminLessonAttendanceSummary> Lessons { get; set; } = [];
}

public sealed class AdminReportClassOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class AdminLessonAttendanceSummary
{
    public int LessonId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int LateCount { get; set; }
    public int UnmarkedCount { get; set; }
    public int Rate { get; set; }
}
