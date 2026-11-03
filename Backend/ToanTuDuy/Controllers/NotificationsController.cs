using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using ToanTuDuy.DTOs.Notification;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Parent")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IHostEnvironment _environment;

        public NotificationsController(
            INotificationService notificationService,
            IHostEnvironment environment)
        {
            _notificationService = notificationService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false,
            [FromQuery] string? type = null,
            CancellationToken cancellationToken = default)
        {
            var response = await _notificationService.GetForParentAsync(
                User.GetAccountId(),
                page,
                pageSize,
                unreadOnly,
                type,
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
        {
            var unreadCount = await _notificationService.GetUnreadCountAsync(
                User.GetAccountId(),
                cancellationToken);

            return Ok(new NotificationUnreadCountResponse { UnreadCount = unreadCount });
        }

        [HttpPost("test")]
        public async Task<IActionResult> CreateTestNotification(
            [FromBody] CreateTestNotificationRequest request,
            CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            var type = string.IsNullOrWhiteSpace(request.Type)
                ? NotificationTypes.System
                : request.Type.Trim();

            var title = string.IsNullOrWhiteSpace(request.Title)
                ? "Thông báo test"
                : request.Title.Trim();

            var body = string.IsNullOrWhiteSpace(request.Body)
                ? $"Thông báo test được tạo lúc {DateTime.UtcNow:HH:mm:ss dd/MM/yyyy} (UTC)."
                : request.Body.Trim();

            var refId = string.IsNullOrWhiteSpace(request.RefId)
                ? $"TEST:{Guid.NewGuid():N}"
                : request.RefId.Trim();

            var accountId = User.GetAccountId();

            var created = await _notificationService.CreateForParentAccountAsync(
                accountId,
                type,
                title,
                body,
                refId,
                cancellationToken);

            if (!created)
            {
                return BadRequest(new
                {
                    message = "Không tạo được thông báo. Kiểm tra cài đặt IN_APP_NOTIFICATION hoặc RefId đã tồn tại.",
                    type,
                    refId,
                });
            }

            var unreadCount = await _notificationService.GetUnreadCountAsync(
                accountId,
                cancellationToken);

            return Ok(new
            {
                created = true,
                type,
                title,
                body,
                refId,
                unreadCount,
            });
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
        {
            var updated = await _notificationService.MarkReadAsync(
                User.GetAccountId(),
                id,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new { message = "Không tìm thấy thông báo." });
            }

            return NoContent();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
        {
            var updated = await _notificationService.MarkAllReadAsync(
                User.GetAccountId(),
                cancellationToken);

            return Ok(new { updated });
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
        {
            var settings = await _notificationService.GetSettingsAsync(
                User.GetAccountId(),
                cancellationToken);

            return Ok(settings);
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings(
            [FromBody] UpdateNotificationSettingsRequest request,
            CancellationToken cancellationToken)
        {
            var settings = await _notificationService.UpdateSettingsAsync(
                User.GetAccountId(),
                request,
                cancellationToken);

            return Ok(settings);
        }
    }
}
