namespace ToanTuDuy.Options
{
    /// <summary>
    /// Cấu hình xác minh email qua liên kết, đọc từ section "EmailVerification".
    /// </summary>
    public class EmailVerificationSettings
    {
        public const string SectionName = "EmailVerification";

        // Base URL của API — dùng để dựng link xác minh trong email.
        public string BaseUrl { get; set; } = string.Empty;

        // Sender đã verify trên Resend (vd: onboarding@resend.dev hoặc domain riêng).
        public string FromEmail { get; set; } = string.Empty;

        // Thời hạn hiệu lực của token (giờ).
        public int TokenExpiryHours { get; set; } = 24;

        // Khoảng cách tối thiểu giữa hai lần gửi lại link (giây).
        public int ResendCooldownSeconds { get; set; } = 60;
    }
}
