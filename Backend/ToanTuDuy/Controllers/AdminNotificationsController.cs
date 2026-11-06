using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Notification;
using ToanTuDuy.Extensions;
using ToanTuDuy.Models;
using ToanTuDuy.Services;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Controllers
{
    [Route("api/admin/notifications")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminNotificationsController : ControllerBase
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminNotificationsController(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<AdminNotificationHistoryResponse>> GetHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize,
            [FromQuery] string? keyword = null,
            [FromQuery] string? type = null,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

            var query = _context.Notifications.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var search = keyword.Trim();
                query = query.Where(notification =>
                    notification.Title.Contains(search) ||
                    (notification.Body != null && notification.Body.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                var normalizedType = type.Trim().ToUpperInvariant();
                query = query.Where(notification => notification.Type == normalizedType);
            }

            var total = await query.CountAsync(cancellationToken);
            var rows = await query
                .OrderByDescending(notification => notification.CreatedAt)
                .ThenByDescending(notification => notification.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(notification => new
                {
                    notification.Id,
                    notification.Type,
                    notification.Title,
                    notification.Body,
                    notification.CreatedAt,
                    AudienceSpec = _context.OutboxMessages
                        .Where(message => message.NotificationId == notification.Id)
                        .Select(message => message.AudienceSpec)
                        .FirstOrDefault(),
                    DeliveryStatus = _context.OutboxMessages
                        .Where(message => message.NotificationId == notification.Id)
                        .Select(message => (int?)message.Status)
                        .FirstOrDefault(),
                    RecipientCount = _context.UserInboxes.Count(inbox => inbox.NotificationId == notification.Id),
                    ReadCount = _context.UserInboxes.Count(inbox =>
                        inbox.NotificationId == notification.Id && inbox.IsRead),
                })
                .ToListAsync(cancellationToken);

            return Ok(new AdminNotificationHistoryResponse
            {
                Items = rows.Select(row => new AdminNotificationHistoryItem
                {
                    Id = row.Id,
                    Type = row.Type,
                    Title = row.Title,
                    Body = row.Body,
                    CreatedAt = row.CreatedAt,
                    DeliveryStatus = row.DeliveryStatus,
                    RecipientGroup = DescribeAudience(row.AudienceSpec),
                    RecipientCount = row.RecipientCount,
                    ReadCount = row.ReadCount,
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                Total = total,
                PendingCount = await _context.OutboxMessages.CountAsync(
                    message => message.Status == (int)OutboxStatus.Pending ||
                        message.Status == (int)OutboxStatus.Processing,
                    cancellationToken),
                FailedCount = await _context.OutboxMessages.CountAsync(
                    message => message.Status == (int)OutboxStatus.Dead,
                    cancellationToken),
                DeliveredCount = await _context.UserInboxes.CountAsync(cancellationToken),
                ReadCount = await _context.UserInboxes.CountAsync(inbox => inbox.IsRead, cancellationToken),
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateAdminNotificationRequest request,
            CancellationToken cancellationToken)
        {
            var recipientGroup = request.RecipientGroup.Trim().ToUpperInvariant();
            var audience = recipientGroup switch
            {
                "PARENT" => AudienceSpec.AllParents(),
                "TEACHER" => AudienceSpec.AllTeachers(),
                "PARENT_AND_TEACHER" => AudienceSpec.Union(
                    AudienceSpec.AllParents(),
                    AudienceSpec.AllTeachers()),
                _ => null,
            };

            if (audience == null)
            {
                return BadRequest(new { message = "Đối tượng nhận thông báo không hợp lệ." });
            }

            var type = request.Type.Trim().ToUpperInvariant();
            if (type != NotificationTypes.System && type != NotificationTypes.Announcement)
            {
                return BadRequest(new { message = "Loại thông báo không hợp lệ." });
            }

            var title = request.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { message = "Tiêu đề thông báo không được để trống." });
            }

            var activeRecipients = recipientGroup switch
            {
                "PARENT" => await _context.ParentProfiles
                    .AnyAsync(parent => parent.Account.IsActive, cancellationToken),
                "TEACHER" => await _context.TeacherProfiles
                    .AnyAsync(teacher => teacher.Account.IsActive, cancellationToken),
                _ => await _context.ParentProfiles
                    .AnyAsync(parent => parent.Account.IsActive, cancellationToken) ||
                    await _context.TeacherProfiles
                        .AnyAsync(teacher => teacher.Account.IsActive, cancellationToken),
            };

            if (!activeRecipients)
            {
                return BadRequest(new { message = "Không có tài khoản hoạt động trong nhóm người nhận này." });
            }

            var queued = await _notificationService.PublishAsync(
                new NotificationMessage(
                    type,
                    title,
                    string.IsNullOrWhiteSpace(request.Body) ? null : request.Body.Trim(),
                    CreatedByAccountId: User.GetAccountId()),
                audience,
                cancellationToken);

            if (!queued)
            {
                return BadRequest(new { message = "Không thể đưa thông báo vào hàng đợi." });
            }

            return Accepted(new { queued = true, message = "Thông báo đã được đưa vào hàng đợi gửi." });
        }

        private static string DescribeAudience(string? audienceJson)
        {
            if (string.IsNullOrWhiteSpace(audienceJson))
            {
                return "Thông báo hệ thống";
            }

            var audience = JsonSerializer.Deserialize<AudienceSpec>(audienceJson, AudienceSpecJson.Options);
            var kinds = audience?.Targets
                .Select(target => target.Kind)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (kinds.Contains(AudienceKinds.AllParents, StringComparer.OrdinalIgnoreCase) &&
                kinds.Contains(AudienceKinds.AllTeachers, StringComparer.OrdinalIgnoreCase))
            {
                return "Phụ huynh và giáo viên";
            }

            if (kinds.Contains(AudienceKinds.AllParents, StringComparer.OrdinalIgnoreCase))
            {
                return "Phụ huynh";
            }

            if (kinds.Contains(AudienceKinds.AllTeachers, StringComparer.OrdinalIgnoreCase))
            {
                return "Giáo viên";
            }

            return "Thông báo hệ thống";
        }
    }
}
