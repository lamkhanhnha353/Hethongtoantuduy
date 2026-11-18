using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;
using ToanTuDuy.Services.Email;

namespace ToanTuDuy.Controllers;

[ApiController]
[Route("api/payrolls")]
[Authorize]
public sealed class PayrollsController(
    IPayrollService payrollService,
    IPayslipMailService payslipMailService,
    IEmailDeliveryService emailDeliveryService) : ControllerBase
{
    /// <summary>Returns and refreshes monthly payroll summaries for all teachers.</summary>
    [HttpGet("admin/summary")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IReadOnlyList<PayslipDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PayslipDto>>> GetMonthlySummary(
        [FromQuery, Range(1, 12)] int month,
        [FromQuery, Range(1, 9998)] int year,
        CancellationToken cancellationToken)
    {
        var payrolls = await payrollService.GetMonthlyPayrollsAsync(month, year, cancellationToken);
        return Ok(payrolls);
    }

    /// <summary>Returns paginated monthly payroll summaries for all teachers.</summary>
    [HttpGet("admin/summary/paged")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<PagedResult<PayslipDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PayslipDto>>> GetMonthlySummaryPaged(
        [FromQuery, Range(1, 12)] int month,
        [FromQuery, Range(1, 9998)] int year,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? keyword = null,
        [FromQuery] string? emailStatus = null,
        CancellationToken cancellationToken = default)
    {
        var result = await payrollService.GetMonthlyPayrollsPagedAsync(
            month,
            year,
            page,
            pageSize,
            keyword,
            emailStatus,
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Sets the salary rate paid to a teacher for each eligible session.</summary>
    [HttpPut("teachers/{teacherId:int}/base-rate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<TeacherBaseRateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherBaseRateDto>> UpdateTeacherBaseRate(
        int teacherId,
        [FromBody] UpdateTeacherBaseRateRequestDto request,
        CancellationToken cancellationToken)
    {
        var rate = await payrollService.UpdateTeacherBaseRateAsync(
            teacherId,
            request,
            cancellationToken);
        return Ok(rate);
    }

    /// <summary>Returns one teacher's session and adjustment details for a month.</summary>
    [HttpGet("detail")]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType<PayslipDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayslipDetailDto>> GetTeacherPayslipDetail(
        [FromQuery, Range(1, int.MaxValue)] int teacherId,
        [FromQuery, Range(1, 12)] int month,
        [FromQuery, Range(1, 9998)] int year,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin"))
        {
            var currentTeacherId = await payrollService.GetTeacherIdForAccountAsync(
                User.GetAccountId(),
                cancellationToken);
            if (currentTeacherId != teacherId)
            {
                throw ApiException.Forbidden("Bạn chỉ có thể xem phiếu lương của chính mình.");
            }
        }

        var detail = await payrollService.GetTeacherPayslipDetailAsync(
            teacherId,
            month,
            year,
            cancellationToken);
        return Ok(detail);
    }

    /// <summary>Adds a penalty or bonus to a teacher's payroll.</summary>
    [HttpPost("adjustments")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<SalaryAdjustmentDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SalaryAdjustmentDto>> CreateSalaryAdjustment(
        [FromBody] CreateAdjustmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var adjustment = await payrollService.AddSalaryAdjustmentAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetTeacherPayslipDetail),
            new
            {
                teacherId = adjustment.TeacherId,
                month = adjustment.AppliedDate.Month,
                year = adjustment.AppliedDate.Year
            },
            adjustment);
    }

    [HttpDelete("adjustments/{adjustmentId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSalaryAdjustment(
    int adjustmentId,
    CancellationToken cancellationToken)
    {
        await payrollService.DeleteSalaryAdjustmentAsync(adjustmentId, cancellationToken);
        return NoContent();
    }

    /// <summary>Renders an email preview for a saved payroll summary.</summary>
    [HttpGet("{id:int}/preview-email")]
    [Authorize(Roles = "Admin")]
    [Produces("text/html")]
    [ProducesResponseType<string>(StatusCodes.Status200OK, "text/html")]
    public async Task<IActionResult> PreviewPayslipEmail(
        int id,
        CancellationToken cancellationToken)
    {
        var html = await payslipMailService.GeneratePayslipHtmlAsync(id, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>Queues one payslip email for background delivery.</summary>
    [HttpPost("{id:int}/send-email")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SendPayslipEmail(
        int id,
        CancellationToken cancellationToken)
    {
        if (!emailDeliveryService.IsConfigured(out var reason))
        {
            throw ApiException.BadRequest(reason!);
        }

        var queued = await payslipMailService.QueueSingleEmailAsync(id, cancellationToken);
        return Accepted(new
        {
            payrollId = id,
            queued,
            message = queued
                ? "Email phiếu lương đã được đưa vào hàng đợi."
                : "Email phiếu lương này đang được xử lý."
        });
    }

    /// <summary>Queues all teacher payslip emails for a month.</summary>
    [HttpPost("send-batch-emails")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<QueueBatchEmailsResponse>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<QueueBatchEmailsResponse>> SendBatchPayslipEmails(
        [FromQuery, Range(1, 12)] int month,
        [FromQuery, Range(1, 9998)] int year,
        CancellationToken cancellationToken)
    {
        if (!emailDeliveryService.IsConfigured(out var reason))
        {
            throw ApiException.BadRequest(reason!);
        }

        var result = await payslipMailService.QueueBatchEmailsAsync(month, year, cancellationToken);
        return Accepted(new QueueBatchEmailsResponse(result.QueuedCount, result.SkippedCount));
    }
}
