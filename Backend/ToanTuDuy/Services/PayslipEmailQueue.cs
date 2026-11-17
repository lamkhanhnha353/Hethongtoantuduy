using System.Threading.Channels;
using ToanTuDuy.Exceptions;

namespace ToanTuDuy.Services;

public sealed class PayslipEmailQueue
{
    private readonly Channel<QueuedPayslipEmail> _channel = Channel.CreateBounded<QueuedPayslipEmail>(
        new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    private readonly HashSet<int> _queuedPayrollIds = [];
    private readonly Lock _sync = new();

    public bool TryQueue(int payrollId, out QueuedPayslipEmail? job)
    {
        lock (_sync)
        {
            if (!_queuedPayrollIds.Add(payrollId))
            {
                job = null;
                return false;
            }

            var persistenceGate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            job = new QueuedPayslipEmail(payrollId, persistenceGate);
            if (_channel.Writer.TryWrite(job))
            {
                return true;
            }

            _queuedPayrollIds.Remove(payrollId);
            job = null;
            throw new ApiException(503, "Hàng đợi email phiếu lương đang quá tải. Vui lòng thử lại sau.");
        }
    }

    public IAsyncEnumerable<QueuedPayslipEmail> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    public void CompletePersistence(QueuedPayslipEmail job, bool succeeded) =>
        job.PersistenceGate.TrySetResult(succeeded);

    public void MarkProcessed(int payrollId)
    {
        lock (_sync)
        {
            _queuedPayrollIds.Remove(payrollId);
        }
    }
}

public sealed record QueuedPayslipEmail(
    int PayrollId,
    TaskCompletionSource<bool> PersistenceGate);
