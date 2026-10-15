namespace ToanTuDuy.Options
{
    /// <summary>
    /// Cấu hình Cloudinary, đọc từ section "Cloudinary" trong appsettings/User Secrets.
    /// KHÔNG commit ApiSecret lên git — dùng User Secrets hoặc biến môi trường ở production.
    /// </summary>
    public class CloudinarySettings
    {
        public const string SectionName = "Cloudinary";

        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
    }
}
