using System.Text.Encodings.Web;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Resend;
using ToanTuDuy.Data;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;
using ToanTuDuy.Options;

namespace ToanTuDuy.Services;

public sealed class PayslipMailService(
    ApplicationDbContext context,
    IPayrollService payrollService,
    PayslipEmailQueue queue,
    IResend resend,
    IOptions<EmailVerificationSettings> emailSettings,
    ILogger<PayslipMailService> logger) : IPayslipMailService
{
    private const string TemplateFileName = "Payslip.html";

    public async Task<string> GeneratePayslipHtmlAsync(
        int payrollId,
        CancellationToken cancellationToken = default)
    {
        var payroll = await GetPayrollOrThrowAsync(payrollId, cancellationToken);
        await payrollService.GetMonthlyPayrollsAsync(payroll.Month, payroll.Year, cancellationToken);

        var payslip = await payrollService.GetTeacherPayslipDetailAsync(
            payroll.TeacherId,
            payroll.Month,
            payroll.Year,
            cancellationToken);
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", TemplateFileName);
        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException($"Không tìm thấy mẫu phiếu lương: {templatePath}");
        }

        var template = await File.ReadAllTextAsync(templatePath, cancellationToken);
        var sessionRows = string.Join(
            Environment.NewLine,
            payslip.Sessions
                .Where(session => session.IsEligible)
                .Select(session =>
                    $"<tr><td>{HtmlEncoder.Default.Encode(session.LessonDate.ToString("dd/MM/yyyy"))}</td>" +
                    $"<td>{HtmlEncoder.Default.Encode(session.Title ?? "Buổi học")}</td>" +
                    $"<td>Đủ điều kiện</td></tr>"));

        if (string.IsNullOrEmpty(sessionRows))
        {
            sessionRows = "<tr><td colspan=\"3\">Không có buổi học đủ điều kiện trong tháng.</td></tr>";
        }

        var adjustmentRows = string.Join(
            Environment.NewLine,
            payslip.Adjustments.Select(adjustment =>
                $"<tr><td>{HtmlEncoder.Default.Encode(adjustment.AppliedDate.ToString("dd/MM/yyyy"))}</td>" +
                $"<td>{HtmlEncoder.Default.Encode(adjustment.Type)}</td>" +
                $"<td>{HtmlEncoder.Default.Encode(adjustment.Reason)}</td>" +
                $"<td>{FormatCurrency(adjustment.Amount)}</td></tr>"));

        if (string.IsNullOrEmpty(adjustmentRows))
        {
            adjustmentRows = "<tr><td colspan=\"4\">Không có khoản điều chỉnh.</td></tr>";
        }

        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{TEACHER_NAME}}"] = HtmlEncoder.Default.Encode(payslip.Payslip.TeacherName),
            ["{{TEACHER_EMAIL}}"] = HtmlEncoder.Default.Encode(payslip.Payslip.TeacherEmail ?? "—"),
            ["{{PAYROLL_PERIOD}}"] = $"{payroll.Month:D2}/{payroll.Year}",
            ["{{SESSION_COUNT}}"] = payslip.Payslip.TotalValidSessions.ToString(),
            ["{{BASE_RATE}}"] = FormatCurrency(payslip.Payslip.BaseRatePerSession),
            ["{{BASE_SALARY}}"] = FormatCurrency(payslip.Payslip.BaseSalary),
            ["{{TOTAL_BONUS}}"] = FormatCurrency(payslip.Payslip.TotalBonus),
            ["{{TOTAL_PENALTY}}"] = FormatCurrency(payslip.Payslip.TotalPenalty),
            ["{{FINAL_SALARY}}"] = FormatCurrency(payslip.Payslip.FinalSalary),
            ["{{SESSION_ROWS}}"] = sessionRows,
            ["{{ADJUSTMENT_ROWS}}"] = adjustmentRows
        };

        foreach (var (placeholder, value) in replacements)
        {
            template = template.Replace(placeholder, value, StringComparison.Ordinal);
        }

        return template;
    }

    public async Task<bool> QueueSingleEmailAsync(
        int payrollId,
        CancellationToken cancellationToken = default)
    {
        var payroll = await GetPayrollOrThrowAsync(payrollId, cancellationToken);
        if (!queue.TryQueue(payrollId, out var job) || job is null)
        {
            return false;
        }

        try
        {
            payroll.EmailStatus = "Queued";
            payroll.EmailErrorMessage = null;
            await context.SaveChangesAsync(cancellationToken);
            queue.CompletePersistence(job, succeeded: true);
            return true;
        }
        catch
        {
            queue.CompletePersistence(job, succeeded: false);
            throw;
        }
    }

    public async Task<QueueBatchEmailsResult> QueueBatchEmailsAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var payrolls = await payrollService.GetMonthlyPayrollsAsync(month, year, cancellationToken);
        var queuedCount = 0;
        foreach (var payroll in payrolls)
        {
            if (await QueueSingleEmailAsync(payroll.PayrollId, cancellationToken))
            {
                queuedCount++;
            }
        }

        return new QueueBatchEmailsResult(queuedCount, payrolls.Count - queuedCount);
    }

    public async Task<QueueBatchEmailsResult> QueueScheduledBatchEmailsAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var payrolls = await payrollService.GetMonthlyPayrollsAsync(month, year, cancellationToken);
        var queuedCount = 0;
        foreach (var payroll in payrolls)
        {
            if (payroll.EmailStatus is "Sent" or "Queued" or "Processing")
            {
                continue;
            }

            if (await QueueSingleEmailAsync(payroll.PayrollId, cancellationToken))
            {
                queuedCount++;
            }
        }

        return new QueueBatchEmailsResult(queuedCount, payrolls.Count - queuedCount);
    }

    public async Task<AdminPayrollReviewReminderResult> SendAdminPayrollReviewReminderAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var adminAccounts = await context.Users
            .AsNoTracking()
            .Where(user => user.Role == "Admin" && user.IsActive)
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(cancellationToken);

        var recipients = adminAccounts
            .Where(admin => !string.IsNullOrWhiteSpace(admin.Email))
            .GroupBy(admin => admin.Email!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var skippedCount = adminAccounts.Count - recipients.Count;
        var failedCount = 0;

        foreach (var admin in recipients)
        {
            try
            {
                var message = new EmailMessage
                {
                    From = emailSettings.Value.FromEmail,
                    Subject = $"Nhắc kiểm tra bảng lương tháng {month:D2}/{year} — Toán Tư Duy",
                    TextBody =
                        $"Đây là lời nhắc kiểm tra bảng lương giáo viên tháng {month:D2}/{year}. " +
                        "Vui lòng đăng nhập hệ thống, mở mục Quản lý tài chính > Bảng lương giáo viên " +
                        "và rà soát các khoản lương, thưởng, phạt trước khi hệ thống gửi phiếu lương " +
                        "tới giáo viên vào 08:00 ngày mai."
                };
                message.To.Add(admin.Email!);
                await resend.EmailSendAsync(message);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failedCount++;
                logger.LogError(
                    exception,
                    "Không thể gửi email nhắc kiểm tra lương tháng {Month:D2}/{Year} tới tài khoản Admin {AdminId}.",
                    month,
                    year,
                    admin.Id);
            }
        }

        if (recipients.Count == 0)
        {
            logger.LogWarning(
                "Không tìm thấy tài khoản Admin đang hoạt động có email để nhắc kiểm tra lương tháng {Month:D2}/{Year}.",
                month,
                year);
        }

        return new AdminPayrollReviewReminderResult(
            recipients.Count - failedCount,
            skippedCount,
            failedCount);
    }

    public async Task SendQueuedEmailAsync(int payrollId, CancellationToken cancellationToken = default)
    {
        var payroll = await GetPayrollOrThrowAsync(payrollId, cancellationToken);
        try
        {
            var teacher = await context.TeacherProfiles
                .AsNoTracking()
                .Where(profile => profile.Id == payroll.TeacherId)
                .Select(profile => new
                {
                    profile.FullName,
                    Email = profile.Email ?? profile.Account.Email
                })
                .SingleAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(teacher.Email))
            {
                throw ApiException.BadRequest("Giáo viên chưa có địa chỉ email.");
            }

            var html = await GeneratePayslipHtmlAsync(payrollId, cancellationToken);
            var message = new EmailMessage
            {
                From = emailSettings.Value.FromEmail,
                Subject = $"Phiếu lương tháng {payroll.Month:D2}/{payroll.Year} — Toán Tư Duy",
                HtmlBody = html,
                TextBody =
                    $"Xin chào {teacher.FullName}, lương tháng {payroll.Month:D2}/{payroll.Year} " +
                    $"của bạn là {FormatCurrency(payroll.FinalSalary)}."
            };
            message.To.Add(teacher.Email);
            await resend.EmailSendAsync(message);

            payroll.EmailStatus = "Sent";
            payroll.EmailSentAt = DateTime.UtcNow;
            payroll.EmailErrorMessage = null;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Đã gửi phiếu lương {PayrollId} tới giáo viên {TeacherId}.", payrollId, payroll.TeacherId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            payroll.EmailStatus = "Failed";
            payroll.EmailErrorMessage = Truncate(exception.Message, 2000);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogError(exception, "Gửi email phiếu lương {PayrollId} thất bại.", payrollId);
        }
    }

    private async Task<PayrollSummary> GetPayrollOrThrowAsync(
        int payrollId,
        CancellationToken cancellationToken)
    {
        if (payrollId <= 0)
        {
            throw ApiException.BadRequest("Mã phiếu lương không hợp lệ.");
        }

        var payroll = await context.PayrollSummaries
            .FirstOrDefaultAsync(item => item.Id == payrollId, cancellationToken);
        return payroll ?? throw ApiException.NotFound("Không tìm thấy phiếu lương.");
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string FormatCurrency(decimal amount) =>
        string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", amount);
}
