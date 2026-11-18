using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ToanTuDuy.Data;
using ToanTuDuy.Hubs;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services.Notifications
{
    public sealed class NotificationFanoutWorker : BackgroundService
    {
        private const int BatchSize = 20;
        private const int MaxAttempts = 5;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationFanoutWorker> _logger;

        public NotificationFanoutWorker(
            IServiceScopeFactory scopeFactory,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationFanoutWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
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
            var unreadCountStore = scope.ServiceProvider.GetRequiredService<IUnreadCountStore>();
            var businessEmailService = scope.ServiceProvider.GetRequiredService<IBusinessNotificationEmailService>();

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
                    var result = await FanoutAsync(context, resolver, message, cancellationToken);

                    if (result != null)
                    {
                        if (result.Inboxes.Count > 0)
                        {
                            await UpdateUnreadCountsAsync(unreadCountStore, result, cancellationToken);
                            await PublishAsync(result, cancellationToken);
                        }

                        if (result.EmailAccountIds.Count > 0)
                        {
                            await SendBusinessEmailsAsync(businessEmailService, result, cancellationToken);
                        }
                    }

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

        private static async Task<FanoutResult?> FanoutAsync(
            ApplicationDbContext context,
            INotificationAudienceResolver resolver,
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var audience = JsonSerializer.Deserialize<AudienceSpec>(
                message.AudienceSpec,
                AudienceSpecJson.Options) ?? new AudienceSpec();

            var resolvedAccountIds = await resolver.ResolveAccountIdsAsync(audience, cancellationToken);
            if (resolvedAccountIds.Count == 0)
            {
                return null;
            }

            var notification = await context.Notifications
                .AsNoTracking()
                .FirstAsync(item => item.Id == message.NotificationId, cancellationToken);

            var shouldSendEmail = audience.SendEmail
                || string.Equals(notification.Type, NotificationTypes.Special, StringComparison.OrdinalIgnoreCase);

            var categorySettingKey = ResolveCategorySettingKey(notification.Type);
            var blockedSettingTypes = categorySettingKey == null
                ? new[] { NotificationSettingTypes.InApp }
                : new[] { NotificationSettingTypes.InApp, categorySettingKey };

            var disabledAccountIds = await context.NotificationSettings
                .AsNoTracking()
                .Where(setting => resolvedAccountIds.Contains(setting.AccountId)
                    && blockedSettingTypes.Contains(setting.Type)
                    && !setting.Enabled)
                .Select(setting => setting.AccountId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var inAppAccountIds = new HashSet<int>(resolvedAccountIds);
            foreach (var accountId in disabledAccountIds)
            {
                inAppAccountIds.Remove(accountId);
            }

            var emailAccountIds = shouldSendEmail
                ? new HashSet<int>(resolvedAccountIds)
                : new HashSet<int>();

            if (notification.RefId != null)
            {
                var alreadyDelivered = await context.UserInboxes
                    .AsNoTracking()
                    .Where(inbox => resolvedAccountIds.Contains(inbox.AccountId)
                        && inbox.Type == notification.Type
                        && inbox.RefId == notification.RefId)
                    .Select(inbox => inbox.AccountId)
                    .ToListAsync(cancellationToken);

                foreach (var accountId in alreadyDelivered)
                {
                    inAppAccountIds.Remove(accountId);
                    emailAccountIds.Remove(accountId);
                }
            }

            if (inAppAccountIds.Count == 0 && emailAccountIds.Count == 0)
            {
                return null;
            }

            var createdAt = DateTime.UtcNow;
            var inboxes = inAppAccountIds.Select(accountId => new UserInbox
            {
                NotificationId = notification.Id,
                AccountId = accountId,
                Type = notification.Type,
                RefId = notification.RefId,
                IsRead = false,
                CreatedAt = createdAt,
            }).ToList();

            if (inboxes.Count > 0)
            {
                context.UserInboxes.AddRange(inboxes);
                await context.SaveChangesAsync(cancellationToken);
            }

            return new FanoutResult(notification, inboxes, emailAccountIds.ToList());
        }

        private static string? ResolveCategorySettingKey(string? notificationType)
        {
            var normalized = notificationType?.Trim().ToUpperInvariant();
            return normalized switch
            {
                NotificationTypes.Tuition or NotificationTypes.Enrollment or NotificationTypes.Payment
                    => NotificationTypes.Tuition,
                NotificationTypes.Attendance or NotificationTypes.LeaveRequest
                    => NotificationTypes.Attendance,
                NotificationTypes.Academic
                    => NotificationTypes.Academic,
                NotificationTypes.Announcement or NotificationTypes.System
                    => NotificationTypes.Announcement,
                NotificationTypes.Account
                    => NotificationTypes.Account,
                _ => null,
            };
        }

        private static async Task UpdateUnreadCountsAsync(
            IUnreadCountStore unreadCountStore,
            FanoutResult result,
            CancellationToken cancellationToken)
        {
            foreach (var group in result.Inboxes.GroupBy(inbox => inbox.AccountId))
            {
                await unreadCountStore.IncrementAsync(group.Key, group.Count(), cancellationToken);
            }
        }

        private async Task PublishAsync(FanoutResult result, CancellationToken cancellationToken)
        {
            foreach (var inbox in result.Inboxes)
            {
                try
                {
                    await _hubContext.Clients
                        .User(inbox.AccountId.ToString())
                        .SendAsync(
                            "ReceiveNotification",
                            new
                            {
                                id = inbox.Id,
                                type = result.Notification.Type,
                                title = result.Notification.Title,
                                body = result.Notification.Body,
                                createdAt = inbox.CreatedAt,
                            },
                            cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // Realtime là best-effort; client sẽ resync bằng API nếu lỡ mất event.
                    _logger.LogWarning(
                        exception,
                        "Không đẩy được thông báo {InboxId} tới account {AccountId}.",
                        inbox.Id,
                        inbox.AccountId);
                }
            }
        }

        private static async Task SendBusinessEmailsAsync(
            IBusinessNotificationEmailService businessEmailService,
            FanoutResult result,
            CancellationToken cancellationToken)
        {
            var isSpecial = string.Equals(
                result.Notification.Type,
                NotificationTypes.Special,
                StringComparison.OrdinalIgnoreCase);

            var subjectPrefix = isSpecial ? "[Thông báo đặc biệt] " : string.Empty;
            var typeLabel = result.Notification.Type switch
            {
                NotificationTypes.Special => "Thông báo đặc biệt",
                NotificationTypes.Announcement => "Thông báo chung",
                NotificationTypes.System => "Thông báo hệ thống",
                _ => result.Notification.Type
            };

            var bodyText = string.IsNullOrWhiteSpace(result.Notification.Body)
                ? result.Notification.Title
                : result.Notification.Body!;

            var details = new List<(string Label, string Value)>
            {
                ("Loại thông báo", typeLabel),
                ("Thời gian gửi", $"{result.Notification.CreatedAt:HH:mm dd/MM/yyyy}")
            };

            await businessEmailService.SendToAccountsAsync(
                result.EmailAccountIds,
                new BusinessEmailPayload(
                    $"{subjectPrefix}{result.Notification.Title} — Toán Tư Duy",
                    result.Notification.Title,
                    bodyText,
                    details),
                cancellationToken);
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];

        private sealed record FanoutResult(
            Notification Notification,
            IReadOnlyList<UserInbox> Inboxes,
            IReadOnlyList<int> EmailAccountIds);
    }
}
