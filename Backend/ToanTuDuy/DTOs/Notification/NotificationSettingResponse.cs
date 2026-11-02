using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs.Notification
{
    public class NotificationSettingResponse
    {
        public string Type { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }

    public class NotificationSettingItemRequest
    {
        [Required(ErrorMessage = "Loại thông báo không được để trống.")]
        [MaxLength(64, ErrorMessage = "Loại thông báo tối đa 64 ký tự.")]
        public string Type { get; set; } = string.Empty;

        public bool Enabled { get; set; }
    }

    public class UpdateNotificationSettingsRequest
    {
        [Required(ErrorMessage = "Danh sách cài đặt không được để trống.")]
        [MinLength(1, ErrorMessage = "Cần ít nhất một cài đặt thông báo.")]
        public List<NotificationSettingItemRequest> Settings { get; set; } = new();
    }
}
