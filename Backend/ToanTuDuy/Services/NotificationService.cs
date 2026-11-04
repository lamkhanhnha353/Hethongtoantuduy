using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Notification;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Services
{
    public class NotificationService : INotificationService
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IUnreadCountStore _unreadCountStore;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger,
            IUnreadCountStore unreadCountStore)
        {
            _context = context;
            _logger = logger;
            _unreadCountStore = unreadCountStore;
        }

        public Task<bool> EnqueueAsync(
            NotificationMessage message,
            AudienceSpec audience,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message.Title))
            {
                return Task.FromResult(false);
            }

            var normalizedType = Normalize(message.Type);
            if (string.IsNullOrEmpty(normalizedType))
            {
                return Task.FromResult(false);
            }

            if (audience == null || audience.IsEmpty)
            {
                _logger.LogWarning("Bỏ qua thông báo {Type} vì không có người nhận.", normalizedType);
                return Task.FromResult(false);
            }

            var now = DateTime.UtcNow;
            var notification = new Notification
            {
                Type = normalizedType,
                Title = message.Title.Trim(),
                Body = string.IsNullOrWhiteSpace(message.Body) ? null : message.Body.Trim(),
                RefId = string.IsNullOrWhiteSpace(message.RefId) ? null : message.RefId.Trim(),
                CreatedByAccountId = message.CreatedByAccountId,
                CreatedAt = now,
            };

            var outbox = new OutboxMessage
            {
                Notification = notification,
                AudienceSpec = JsonSerializer.Serialize(audience, AudienceSpecJson.Options),
                Status = (int)OutboxStatus.Pending,
                Attempts = 0,
                AvailableAt = now,
                CreatedAt = now,
            };

            _context.Notifications.Add(notification);
            _context.OutboxMessages.Add(outbox);

            return Task.FromResult(true);
        }

        public async Task<bool> PublishAsync(
            NotificationMessage message,
            AudienceSpec audience,
            CancellationToken cancellationToken = default)
        {
            var queued = await EnqueueAsync(message, audience, cancellationToken);
            if (!queued)
            {
                return false;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<NotificationListResponse> GetInboxAsync(
            int accountId,
            int page,
            int pageSize,
            bool unreadOnly,
            string? type,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0
                ? DefaultPageSize
                : Math.Min(pageSize, MaxPageSize);

            var query = _context.UserInboxes
                .AsNoTracking()
                .Where(inbox => inbox.AccountId == accountId);

            if (unreadOnly)
            {
                query = query.Where(inbox => !inbox.IsRead);
            }

            var normalizedType = Normalize(type);
            if (!string.IsNullOrEmpty(normalizedType))
            {
                query = query.Where(inbox => inbox.Type == normalizedType);
            }

            var total = await query.CountAsync(cancellationToken);
            var unreadCount = await _unreadCountStore.GetAsync(accountId, cancellationToken);

            var items = await query
                .OrderByDescending(inbox => inbox.CreatedAt)
                .ThenByDescending(inbox => inbox.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(inbox => new NotificationResponse
                {
                    Id = inbox.Id,
                    Title = inbox.Notification.Title,
                    Body = inbox.Notification.Body,
                    Type = inbox.Type,
                    RefId = inbox.RefId,
                    IsRead = inbox.IsRead,
                    CreatedAt = inbox.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return new NotificationListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total,
                UnreadCount = unreadCount,
            };
        }

        public Task<int> GetUnreadCountAsync(int accountId, CancellationToken cancellationToken = default)
        {
            return _unreadCountStore.GetAsync(accountId, cancellationToken);
        }

        public async Task<bool> MarkReadAsync(
            int accountId,
            long inboxId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var updated = await _context.UserInboxes
                .Where(inbox => inbox.AccountId == accountId
                    && inbox.Id == inboxId
                    && !inbox.IsRead)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(inbox => inbox.IsRead, true)
                        .SetProperty(inbox => inbox.ReadAt, now),
                    cancellationToken);

            if (updated > 0)
            {
                await _unreadCountStore.DecrementAsync(accountId, cancellationToken);
                return true;
            }

            // Đã đọc trước đó vẫn trả true để endpoint idempotent.
            return await _context.UserInboxes
                .AsNoTracking()
                .AnyAsync(inbox => inbox.AccountId == accountId && inbox.Id == inboxId, cancellationToken);
        }

        public async Task<int> MarkAllReadAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var updated = await _context.UserInboxes
                .Where(inbox => inbox.AccountId == accountId && !inbox.IsRead)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(inbox => inbox.IsRead, true)
                        .SetProperty(inbox => inbox.ReadAt, now),
                    cancellationToken);

            await _unreadCountStore.ResetAsync(accountId, cancellationToken);
            return updated;
        }

        public async Task<IReadOnlyList<NotificationSettingResponse>> GetSettingsAsync(
            int accountId,
            CancellationToken cancellationToken = default)
        {
            var rows = await _context.NotificationSettings
                .AsNoTracking()
                .Where(setting => setting.AccountId == accountId)
                .Select(setting => new { setting.Type, setting.Enabled })
                .ToListAsync(cancellationToken);

            var grouped = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.Type))
                .GroupBy(row => Normalize(row.Type))
                .ToDictionary(group => group.Key, group => group.Any(row => row.Enabled));

            var result = new List<NotificationSettingResponse>();
            foreach (var type in NotificationSettingTypes.All)
            {
                result.Add(new NotificationSettingResponse
                {
                    Type = type,
                    Enabled = grouped.TryGetValue(type, out var enabled) ? enabled : true,
                });
            }

            foreach (var type in grouped.Keys.Except(NotificationSettingTypes.All, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(new NotificationSettingResponse
                {
                    Type = type,
                    Enabled = grouped[type],
                });
            }

            return result;
        }

        public async Task<IReadOnlyList<NotificationSettingResponse>> UpdateSettingsAsync(
            int accountId,
            UpdateNotificationSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            var requested = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in request.Settings)
            {
                var type = Normalize(item.Type);
                if (string.IsNullOrEmpty(type))
                {
                    throw ApiException.BadRequest("Loại thông báo không được để trống.");
                }

                requested[type] = item.Enabled;
            }

            var existing = await _context.NotificationSettings
                .Where(setting => setting.AccountId == accountId)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var (type, enabled) in requested)
            {
                var setting = existing.FirstOrDefault(row =>
                    string.Equals(Normalize(row.Type), type, StringComparison.OrdinalIgnoreCase));

                if (setting == null)
                {
                    _context.NotificationSettings.Add(new NotificationSetting
                    {
                        AccountId = accountId,
                        Type = type,
                        Enabled = enabled,
                        UpdatedAt = now,
                    });
                    continue;
                }

                setting.Type = type;
                setting.Enabled = enabled;
                setting.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return await GetSettingsAsync(accountId, cancellationToken);
        }

        private static string Normalize(string? value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
