using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Notification;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class NotificationService : INotificationService
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CreateForParentAsync(
            int parentId,
            string type,
            string title,
            string? body,
            string? refId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (parentId <= 0 || string.IsNullOrWhiteSpace(title))
                {
                    return false;
                }

                var normalizedType = Normalize(type);
                if (string.IsNullOrEmpty(normalizedType))
                {
                    return false;
                }

                if (!await IsInAppEnabledAsync(parentId, cancellationToken))
                {
                    return false;
                }

                var normalizedRefId = string.IsNullOrWhiteSpace(refId) ? null : refId.Trim();
                if (normalizedRefId != null)
                {
                    var duplicated = await _context.Notifications
                        .AsNoTracking()
                        .AnyAsync(
                            notification => notification.ParentId == parentId
                                && notification.Type == normalizedType
                                && notification.RefId == normalizedRefId,
                            cancellationToken);

                    if (duplicated)
                    {
                        return false;
                    }
                }

                _context.Notifications.Add(new Notification
                {
                    ParentId = parentId,
                    Title = title.Trim(),
                    Body = string.IsNullOrWhiteSpace(body) ? null : body.Trim(),
                    Type = normalizedType,
                    RefId = normalizedRefId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                });

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không tạo được thông báo {Type} cho phụ huynh {ParentId}.",
                    type,
                    parentId);
                return false;
            }
        }

        public async Task<bool> CreateForParentAccountAsync(
            int accountId,
            string type,
            string title,
            string? body,
            string? refId,
            CancellationToken cancellationToken = default)
        {
            var parentId = await GetParentIdAsync(accountId, cancellationToken);

            return await CreateForParentAsync(
                parentId,
                type,
                title,
                body,
                refId,
                cancellationToken);
        }

        public async Task<bool> CreateForStudentAsync(
            int studentId,
            string type,
            string title,
            string? body,
            string? refId,
            CancellationToken cancellationToken = default)
        {
            var parentId = await _context.StudentProfiles
                .AsNoTracking()
                .Where(student => student.Id == studentId)
                .Select(student => (int?)student.ParentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!parentId.HasValue)
            {
                return false;
            }

            return await CreateForParentAsync(
                parentId.Value,
                type,
                title,
                body,
                refId,
                cancellationToken);
        }

        public async Task<int> CreateForClassParentsAsync(
            int classId,
            string type,
            string title,
            string? body,
            string? refId,
            CancellationToken cancellationToken = default)
        {
            var parentIds = await _context.ClassStudents
                .AsNoTracking()
                .Where(classStudent => classStudent.ClassId == classId
                    && classStudent.Status == "ACTIVE")
                .Select(classStudent => classStudent.Student.ParentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var created = 0;
            foreach (var parentId in parentIds)
            {
                if (await CreateForParentAsync(
                    parentId,
                    type,
                    title,
                    body,
                    refId,
                    cancellationToken))
                {
                    created++;
                }
            }

            return created;
        }

        public async Task<NotificationListResponse> GetForParentAsync(
            int accountId,
            int page,
            int pageSize,
            bool unreadOnly,
            string? type,
            CancellationToken cancellationToken = default)
        {
            var parentId = await GetParentIdAsync(accountId, cancellationToken);

            page = Math.Max(1, page);
            pageSize = pageSize <= 0
                ? DefaultPageSize
                : Math.Min(pageSize, MaxPageSize);

            var query = _context.Notifications
                .AsNoTracking()
                .Where(notification => notification.ParentId == parentId);

            if (unreadOnly)
            {
                query = query.Where(notification => !notification.IsRead);
            }

            var normalizedType = Normalize(type);
            if (!string.IsNullOrEmpty(normalizedType))
            {
                query = query.Where(notification => notification.Type == normalizedType);
            }

            var total = await query.CountAsync(cancellationToken);
            var unreadCount = await _context.Notifications
                .AsNoTracking()
                .CountAsync(
                    notification => notification.ParentId == parentId && !notification.IsRead,
                    cancellationToken);

            var items = await query
                .OrderByDescending(notification => notification.CreatedAt)
                .ThenByDescending(notification => notification.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(notification => new NotificationResponse
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Body = notification.Body,
                    Type = notification.Type,
                    RefId = notification.RefId,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt,
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

        public async Task<int> GetUnreadCountAsync(
            int accountId,
            CancellationToken cancellationToken = default)
        {
            var parentId = await GetParentIdAsync(accountId, cancellationToken);

            return await _context.Notifications
                .AsNoTracking()
                .CountAsync(
                    notification => notification.ParentId == parentId && !notification.IsRead,
                    cancellationToken);
        }

        public async Task<bool> MarkReadAsync(
            int accountId,
            int notificationId,
            CancellationToken cancellationToken = default)
        {
            var parentId = await GetParentIdAsync(accountId, cancellationToken);

            var updated = await _context.Notifications
                .Where(notification => notification.Id == notificationId
                    && notification.ParentId == parentId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(notification => notification.IsRead, true),
                    cancellationToken);

            return updated > 0;
        }

        public async Task<int> MarkAllReadAsync(
            int accountId,
            CancellationToken cancellationToken = default)
        {
            var parentId = await GetParentIdAsync(accountId, cancellationToken);

            return await _context.Notifications
                .Where(notification => notification.ParentId == parentId && !notification.IsRead)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(notification => notification.IsRead, true),
                    cancellationToken);
        }

        public async Task<IReadOnlyList<NotificationSettingResponse>> GetSettingsAsync(
            int accountId,
            CancellationToken cancellationToken = default)
        {
            var parentId = await GetParentIdAsync(accountId, cancellationToken);

            var rows = await _context.NotificationSettings
                .AsNoTracking()
                .Where(setting => setting.ParentId == parentId)
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
            var parentId = await GetParentIdAsync(accountId, cancellationToken);

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
                .Where(setting => setting.ParentId == parentId)
                .ToListAsync(cancellationToken);

            foreach (var (type, enabled) in requested)
            {
                var matches = existing
                    .Where(setting => string.Equals(
                        Normalize(setting.Type),
                        type,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count == 0)
                {
                    _context.NotificationSettings.Add(new NotificationSetting
                    {
                        ParentId = parentId,
                        Type = type,
                        Enabled = enabled,
                    });
                    continue;
                }

                foreach (var setting in matches)
                {
                    setting.Enabled = enabled;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return await GetSettingsAsync(accountId, cancellationToken);
        }

        private async Task<int> GetParentIdAsync(
            int accountId,
            CancellationToken cancellationToken)
        {
            var parentId = await _context.ParentProfiles
                .AsNoTracking()
                .Where(parent => parent.AccountId == accountId)
                .Select(parent => (int?)parent.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!parentId.HasValue)
            {
                throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");
            }

            return parentId.Value;
        }

        private async Task<bool> IsInAppEnabledAsync(
            int parentId,
            CancellationToken cancellationToken)
        {
            var settings = await _context.NotificationSettings
                .AsNoTracking()
                .Where(setting => setting.ParentId == parentId
                    && setting.Type == NotificationSettingTypes.InApp)
                .Select(setting => setting.Enabled)
                .ToListAsync(cancellationToken);

            return settings.Count == 0 || settings.Any(enabled => enabled);
        }

        private static string Normalize(string? value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
