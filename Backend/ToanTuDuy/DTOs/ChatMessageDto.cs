
using System;
//Dùng để trả dữ liệu tin nhắn về cho App/Postman
namespace ToanTuDuy.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string SenderType { get; set; } = null!;
        public int? SenderId { get; set; }
        public string MessageType { get; set; } = null!;
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}