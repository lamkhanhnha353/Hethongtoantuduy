using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services;

public interface IAdminReportService
{
    Task<AdminRevenueReportResponse> GetRevenueAsync(int months);
    Task<AdminStudentGrowthReportResponse> GetStudentGrowthAsync(int months);
    Task<AdminAttendanceReportResponse> GetAttendanceAsync(int days, int? classId);
}
