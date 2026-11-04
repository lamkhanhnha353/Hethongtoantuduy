using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ToanTuDuy.Data;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services.Notifications
{
    public sealed class NotificationFanoutWorker : BackgroundService
    {
        private const int BatchSize = 20;
        private const int MaxAttempts = 5;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationFanoutWorker> _logger;

        public NotificationFanoutWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationFanoutWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Lỗi khi xử lý outbox thông báo.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var resolver = scope.ServiceProvider.GetRequiredService<INotificationAudienceResolver>();

            var now = DateTime.UtcNow;
            var messages = await context.OutboxMessages
                .Where(message => message.Status == (int)OutboxStatus.Pending
                    && message.AvailableAt <= now)
                .OrderBy(message => message.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                return;
            }

            foreach (var message in messages)
            {
                message.Status = (int)OutboxStatus.Processing;
            }

            await context.SaveChangesAsync(cancellationToken);

            foreach (var message in messages)
            {
                try
                {
                    await FanoutAsync(context, resolver, message, cancellationToken);
                    message.Status = (int)OutboxStatus.Done;
                    message.ProcessedAt = DateTime.UtcNow;
                    message.LastError = null;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    message.Attempts++;
                    message.LastError = Truncate(exception.Message, 2000);

                    if (message.Attempts >= MaxAttempts)
                    {
                        message.Status = (int)OutboxStatus.Dead;
                        message.ProcessedAt = DateTime.UtcNow;
                        _logger.LogError(
                            exception,
                            "Outbox {OutboxId} thất bại quá {MaxAttempts} lần, chuyển sang Dead.",
                            message.Id,
                            MaxAttempts);
                    }
                    else
                    {
                        message.Status = (int)OutboxStatus.Pending;
                        message.AvailableAt = DateTime.UtcNow.AddSeconds(
                            Math.Min(300, Math.Pow(2, message.Attempts) * 5));
                        _logger.LogWarning(
                            exception,
                            "Outbox {OutboxId} thất bại lần {Attempts}, sẽ thử lại.",
                            message.Id,
                            message.Attempts);
                    }
                }

                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private static async Task FanoutAsync(
            ApplicationDbContext context,
            INotificationAudienceResolver resolver,
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var audience = JsonSerializer.Deserialize<AudienceSpec>(
                message.AudienceSpec,
                AudienceSpecJson.Options) ?? new AudienceSpec();

            var accountIds = await resolver.ResolveAccountIdsAsync(audience, cancellationToken);
            if (accountIds.Count == 0)
            {
                return;
            }

            var disabledAccountIds = await context.NotificationSettings
                .AsNoTracking()
                .Where(setting => accountIds.Contains(setting.AccountId)
                    && setting.Type == NotificationSettingTypes.InApp
                    && !setting.Enabled)
                .Select(setting => setting.AccountId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var accountId in disabledAccountIds)
            {
                accountIds.Remove(accountId);
            }

            var notification = await context.Notifications
                .AsNoTracking()
                .FirstAsync(item => item.Id == message.NotificationId, cancellationToken);

            if (accountIds.Count == 0)
            {
                return;
            }

            if (notification.RefId != null)
            {
                var alreadyDelivered = await context.UserInboxes
                    .AsNoTracking()
                    .Where(inbox => accountIds.Contains(inbox.AccountId)
                        && inbox.Type == notification.Type
                        && inbox.RefId == notification.RefId)
                    .Select(inbox => inbox.AccountId)
                    .ToListAsync(cancellationToken);

                foreach (var accountId in alreadyDelivered)
                {
                    accountIds.Remove(accountId);
                }
            }

            if (accountIds.Count == 0)
            {
                return;
            }

            var createdAt = DateTime.UtcNow;
            context.UserInboxes.AddRange(accountIds.Select(accountId => new UserInbox
            {
                NotificationId = notification.Id,
                AccountId = accountId,
                Type = notification.Type,
                RefId = notification.RefId,
                IsRead = false,
                CreatedAt = createdAt,
            }));

            await context.SaveChangesAsync(cancellationToken);
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
