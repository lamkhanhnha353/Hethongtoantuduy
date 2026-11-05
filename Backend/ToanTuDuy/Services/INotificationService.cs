using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.DTOs.Notification;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Services
{
    public interface INotificationService
    {
        Task<bool> EnqueueAsync(
            NotificationMessage message,
            AudienceSpec audience,
            CancellationToken cancellationToken = default);

        Task<bool> PublishAsync(
            NotificationMessage message,
            AudienceSpec audience,
            CancellationToken cancellationToken = default);

        Task<NotificationListResponse> GetInboxAsync(
            int accountId,
            int page,
            int pageSize,
            bool unreadOnly,
            string? type,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(int accountId, CancellationToken cancellationToken = default);

        Task<bool> MarkReadAsync(int accountId, long inboxId, CancellationToken cancellationToken = default);

        Task<int> MarkAllReadAsync(int accountId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NotificationSettingResponse>> GetSettingsAsync(
            int accountId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NotificationSettingResponse>> UpdateSettingsAsync(
            int accountId,
            UpdateNotificationSettingsRequest request,
            CancellationToken cancellationToken = default);
    }
}
