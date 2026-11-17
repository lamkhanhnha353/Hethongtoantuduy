using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public sealed class AdminReportsController(IAdminReportService reportService) : ControllerBase
{
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(AdminRevenueReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminRevenueReportResponse>> GetRevenue(
        [FromQuery, Range(1, 24)] int months = 6)
    {
        return Ok(await reportService.GetRevenueAsync(months));
    }

    [HttpGet("payroll")]
    [ProducesResponseType(typeof(AdminPayrollTrendResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminPayrollTrendResponse>> GetPayrollTrend(
        [FromQuery, Range(1, 24)] int months = 6)
    {
        return Ok(await reportService.GetPayrollTrendAsync(months));
    }

    [HttpGet("student-growth")]
    [ProducesResponseType(typeof(AdminStudentGrowthReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminStudentGrowthReportResponse>> GetStudentGrowth(
        [FromQuery, Range(1, 24)] int months = 6)
    {
        return Ok(await reportService.GetStudentGrowthAsync(months));
    }

    [HttpGet("attendance")]
    [ProducesResponseType(typeof(AdminAttendanceReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminAttendanceReportResponse>> GetAttendance(
        [FromQuery, Range(1, 365)] int days = 90,
        [FromQuery, Range(1, int.MaxValue)] int? classId = null)
    {
        return Ok(await reportService.GetAttendanceAsync(days, classId));
    }
}
