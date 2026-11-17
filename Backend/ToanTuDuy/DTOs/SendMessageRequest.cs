
using System.ComponentModel.DataAnnotations;
//Dùng khi người dùng gửi tin nhắn
namespace ToanTuDuy.DTOs
{
    public class SendMessageRequest
    {
        [Required]
        public int ReceiverId { get; set; } // ID người nhận (Phụ huynh hoặc Giáo viên)

        [Required]
        [MaxLength(20)]
        public string ReceiverType { get; set; } = string.Empty; // "PARENT" hoặc "TEACHER"

        [Required]
        [MaxLength(20)]
        public string MessageType { get; set; } = "TEXT";

        [MaxLength(5000)]
        public string? Content { get; set; }
    }
}