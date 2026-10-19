namespace ToanTuDuy.Options
{
    /// <summary>
    /// Cấu hình khôi phục mật khẩu khi quên, đọc từ section "PasswordReset".
    /// </summary>
    public class PasswordResetSettings
    {
        public const string SectionName = "PasswordReset";

        // Khoảng cách tối thiểu giữa hai lần yêu cầu mật khẩu tạm (giây).
        public int CooldownSeconds { get; set; } = 60;

        // Độ dài mật khẩu tạm sinh ra cho người dùng.
        public int TempPasswordLength { get; set; } = 12;

        // Sender đã verify trên Resend (giống section EmailVerification).
        public string FromEmail { get; set; } = string.Empty;
    }
}
