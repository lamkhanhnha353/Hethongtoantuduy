using System;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class LessonRequest
    {
        [Required(ErrorMessage = "ID Lớp học không được để trống")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Ngày diễn ra buổi học không được để trống")]
        public DateTime LessonDate { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required(ErrorMessage = "Trạng thái buổi học không được để trống")]
        public string Status { get; set; } = "SCHEDULED";

        public bool IsImportan { get; set; } = false;
    }
}
