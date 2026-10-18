
using System;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class ClassScheduleRequest
    {
        [Required(ErrorMessage = "ID Lớp học không được để trống")]
        public int ClassId { get; set; }

        [Required]
        [Range(0, 6, ErrorMessage = "Ngày trong tuần phải từ 0 (Chủ nhật) đến 6 (Thứ bảy)")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "Giờ bắt đầu không được để trống")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc không được để trống")]
        public TimeSpan EndTime { get; set; }

        public string? Room { get; set; }
    }
}