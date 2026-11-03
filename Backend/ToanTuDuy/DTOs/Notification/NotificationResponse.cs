using System;
using System.Collections.Generic;

namespace ToanTuDuy.DTOs.Notification
{
    public class NotificationResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public string? Type { get; set; }
        public string? RefId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationListResponse
    {
        public IReadOnlyList<NotificationResponse> Items { get; set; } = Array.Empty<NotificationResponse>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int UnreadCount { get; set; }
    }

    public class NotificationUnreadCountResponse
    {
        public int UnreadCount { get; set; }
    }
}
