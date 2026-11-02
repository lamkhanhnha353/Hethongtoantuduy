using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
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
