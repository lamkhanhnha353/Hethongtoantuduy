namespace ToanTuDuy.Options
{
    /// <summary>
    /// Cấu hình gửi email qua SMTP (ví dụ Gmail App Password hoặc SMTP nội bộ), đọc từ section "Smtp".
    /// </summary>
    public sealed class SmtpSettings
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Hệ thống Toán Tư Duy";
    }
}
