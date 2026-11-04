using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class SendChatAttachmentRequest
    {
        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReceiverType { get; set; } = string.Empty;

        [Required]
        public IFormFile? File { get; set; }
    }
}
