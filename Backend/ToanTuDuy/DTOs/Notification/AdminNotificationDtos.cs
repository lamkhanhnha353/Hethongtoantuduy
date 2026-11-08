using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs.Notification
{
    public class CreateAdminNotificationRequest
    {
        [Required]
        [StringLength(20)]
        public string RecipientGroup { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Body { get; set; }
    }

    public class AdminNotificationHistoryItem
    {
        public long Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public string RecipientGroup { get; set; } = string.Empty;
        public int? DeliveryStatus { get; set; }
        public int RecipientCount { get; set; }
        public int ReadCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class AdminNotificationHistoryResponse
    {
        public IReadOnlyList<AdminNotificationHistoryItem> Items { get; set; } =
            Array.Empty<AdminNotificationHistoryItem>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int DeliveredCount { get; set; }
        public int ReadCount { get; set; }
    }
}
