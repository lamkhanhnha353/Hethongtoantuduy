using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToanTuDuy.Models
{
    // Token xác minh email qua liên kết (magic link) — chỉ lưu SHA-256 hash,
    // token thô chỉ được gửi qua email, không bao giờ lưu trong DB.
    [Table("EMAIL_VERIFICATION_TOKEN")]
    public class EmailVerificationToken
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("token_hash")]
        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        // Email tại thời điểm cấp token — nếu user đổi email sau đó thì link cũ tự vô hiệu.
        [Column("email_at_issue")]
        [MaxLength(255)]
        public string EmailAtIssue { get; set; } = string.Empty;

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("used_at")]
        public DateTime? UsedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(AccountId))]
        public ApplicationUser Account { get; set; }
    }
}
