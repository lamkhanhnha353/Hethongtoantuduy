
using System;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    // Hứng dữ liệu từ Mobile App (Phụ huynh gửi)
    public class LeaveRequestCreateRequest
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public string? Reason { get; set; }
    }

    // Trả dữ liệu ra cho Web Admin xem danh sách đơn
    public class LeaveRequestResponse
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } // Tên bé
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } // Chủ đề học
        public DateTime LessonDate { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}