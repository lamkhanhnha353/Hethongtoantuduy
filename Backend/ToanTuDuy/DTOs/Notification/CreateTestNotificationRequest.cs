using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs.Notification
{
    public class CreateTestNotificationRequest
    {
        [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
        public string? Title { get; set; }

        [MaxLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự.")]
        public string? Body { get; set; }

        [MaxLength(32, ErrorMessage = "Loại thông báo tối đa 32 ký tự.")]
        public string? Type { get; set; }

        [MaxLength(128, ErrorMessage = "RefId tối đa 128 ký tự.")]
        public string? RefId { get; set; }
    }
}
