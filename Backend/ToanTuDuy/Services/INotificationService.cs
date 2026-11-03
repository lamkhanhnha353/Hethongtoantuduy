using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.DTOs.Notification;

namespace ToanTuDuy.Services
{
    public interface INotificationService
    {
        Task<bool> CreateForParentAsync(
            int parentId,
            string type,
            string title,
            string? body,
            string? refId,
            CancellationToken cancellationToken = default);

        Task<bool> CreateForStudentAsync(
            int studentId,
            string type,
            string title,
            string? body,
            string? refId,
            CancellationToken cancellationToken = default);

        Task<int> CreateForClassParentsAsync(
            int classId,
            string type,
            string title,
            string? body,
            string? refId,
            CancellationToken cancellationToken = default);

        Task<NotificationListResponse> GetForParentAsync(
            int accountId,
            int page,
            int pageSize,
            bool unreadOnly,
            string? type,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(int accountId, CancellationToken cancellationToken = default);

        Task<bool> MarkReadAsync(int accountId, int notificationId, CancellationToken cancellationToken = default);

        Task<int> MarkAllReadAsync(int accountId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NotificationSettingResponse>> GetSettingsAsync(
            int accountId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NotificationSettingResponse>> UpdateSettingsAsync(
            int accountId,
            UpdateNotificationSettingsRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> CreateForParentAccountAsync(
            int accountId,
            string type,
            string title,
            string? body,
            string? refId,
            CancellationToken cancellationToken = default);
    }
}
