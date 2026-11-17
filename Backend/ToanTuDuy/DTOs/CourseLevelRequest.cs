
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class CourseLevelRequest
    {
        [Required(ErrorMessage = "ID Khóa học không được để trống")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Mã cấp độ không được để trống")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Tên cấp độ không được để trống")]
        public string Name { get; set; }

        public decimal Fee { get; set; }
        public int TotalSessions { get; set; }
        public int OrderIndex { get; set; }

        [Required]
        public string Status { get; set; }

        public object? Syllabus { get; set; }
        public object? Faqs { get; set; }
    }
}