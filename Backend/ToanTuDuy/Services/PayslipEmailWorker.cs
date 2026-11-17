using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;

namespace ToanTuDuy.Services;

public sealed class PayslipEmailWorker(
    PayslipEmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PayslipEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeueInterruptedEmailsAsync(stoppingToken);

        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                if (!await job.PersistenceGate.Task.WaitAsync(stoppingToken))
                {
                    continue;
                }

                await using var scope = scopeFactory.CreateAsyncScope();
                var mailService = scope.ServiceProvider.GetRequiredService<IPayslipMailService>();
                await mailService.SendQueuedEmailAsync(job.PayrollId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Không thể xử lý email phiếu lương {PayrollId}.", job.PayrollId);
            }
            finally
            {
                queue.MarkProcessed(job.PayrollId);
            }
        }
    }

    private async Task RequeueInterruptedEmailsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pendingPayrollIds = await context.PayrollSummaries
            .AsNoTracking()
            .Where(payroll => payroll.EmailStatus == "Queued")
            .Select(payroll => payroll.Id)
            .ToListAsync(cancellationToken);

        foreach (var payrollId in pendingPayrollIds)
        {
            if (queue.TryQueue(payrollId, out var job) && job is not null)
            {
                queue.CompletePersistence(job, succeeded: true);
            }
        }
    }
}
