namespace ToanTuDuy.Services;

public interface IPayslipMailService
{
    Task<string> GeneratePayslipHtmlAsync(int payrollId, CancellationToken cancellationToken = default);
    Task<bool> QueueSingleEmailAsync(int payrollId, CancellationToken cancellationToken = default);
    Task<QueueBatchEmailsResult> QueueBatchEmailsAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default);
    Task<QueueBatchEmailsResult> QueueScheduledBatchEmailsAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default);
    Task<AdminPayrollReviewReminderResult> SendAdminPayrollReviewReminderAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default);
    Task SendQueuedEmailAsync(int payrollId, CancellationToken cancellationToken = default);
}

public sealed record QueueBatchEmailsResult(int QueuedCount, int SkippedCount);
public sealed record AdminPayrollReviewReminderResult(int SentCount, int SkippedCount, int FailedCount);
