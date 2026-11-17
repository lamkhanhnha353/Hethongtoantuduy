using System.Collections.Concurrent;
using ToanTuDuy.Services.Email;

namespace ToanTuDuy.Tests.Infrastructure.Fakes;

public sealed class FakeEmailDeliveryService : IEmailDeliveryService
{
    private readonly object _lock = new();
    private readonly List<EmailDeliveryMessage> _sentMessages = [];

    public bool IsConfiguredResult { get; set; } = true;
    public string? NotConfiguredReason { get; set; } = "Chưa cấu hình dịch vụ gửi email (test).";
    public bool ThrowOnSend { get; set; }

    public IReadOnlyList<EmailDeliveryMessage> SentMessages
    {
        get
        {
            lock (_lock)
            {
                return _sentMessages.ToList();
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _sentMessages.Clear();
            IsConfiguredResult = true;
            ThrowOnSend = false;
        }
    }

    public bool IsConfigured(out string? reason)
    {
        if (IsConfiguredResult)
        {
            reason = null;
            return true;
        }

        reason = NotConfiguredReason;
        return false;
    }

    public Task SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsConfiguredResult)
        {
            throw new InvalidOperationException(NotConfiguredReason ?? "Email not configured.");
        }

        if (ThrowOnSend)
        {
            throw new InvalidOperationException("Simulated email delivery failure.");
        }

        lock (_lock)
        {
            _sentMessages.Add(message);
        }

        return Task.CompletedTask;
    }
}
