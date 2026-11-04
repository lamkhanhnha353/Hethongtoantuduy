using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToanTuDuy.Models
{
    [Table("NOTIFICATION")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("type")]
        [MaxLength(64)]
        public string Type { get; set; } = string.Empty;

        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("body")]
        [MaxLength(2000)]
        public string? Body { get; set; }

        [Column("ref_id")]
        [MaxLength(128)]
        public string? RefId { get; set; }

        [Column("created_by_account_id")]
        public int? CreatedByAccountId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public ICollection<UserInbox> Inboxes { get; set; } = new List<UserInbox>();
    }

    [Table("OUTBOX")]
    public class OutboxMessage
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("notification_id")]
        public long NotificationId { get; set; }

        [Column("audience_spec")]
        public string AudienceSpec { get; set; } = "{}";

        [Column("status")]
        public int Status { get; set; } = (int)OutboxStatus.Pending;

        [Column("attempts")]
        public int Attempts { get; set; }

        [Column("available_at")]
        public DateTime AvailableAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        [Column("last_error")]
        [MaxLength(2000)]
        public string? LastError { get; set; }

        [ForeignKey(nameof(NotificationId))]
        public Notification Notification { get; set; } = null!;
    }

    [Table("USER_INBOX")]
    public class UserInbox
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("notification_id")]
        public long NotificationId { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("type")]
        [MaxLength(64)]
        public string Type { get; set; } = string.Empty;

        [Column("ref_id")]
        [MaxLength(128)]
        public string? RefId { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }

        [ForeignKey(nameof(NotificationId))]
        public Notification Notification { get; set; } = null!;
    }

    [Table("NOTIFICATION_SETTING")]
    public class NotificationSetting
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("type")]
        [MaxLength(64)]
        public string Type { get; set; } = string.Empty;

        [Column("enabled")]
        public bool Enabled { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public enum OutboxStatus
    {
        Pending = 0,
        Processing = 1,
        Done = 2,
        Dead = 3,
        Relayed = 4,
    }
}
