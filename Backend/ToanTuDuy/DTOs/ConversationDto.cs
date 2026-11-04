
using System;
//Dùng để load danh sách người nhắn tin
namespace ToanTuDuy.DTOs
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public string ChatType { get; set; } = null!;

        // Thông tin người đang chat cùng (Partner)
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string? PartnerAvatar { get; set; }

        public string? LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }

        // Số tin nhắn chưa đọc (nếu có làm)
        public int UnreadCount { get; set; }
    }
}