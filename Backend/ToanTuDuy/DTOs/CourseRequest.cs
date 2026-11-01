
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class CourseRequest
    {
        [Required(ErrorMessage = "Mã khóa học không được để trống")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        public string Name { get; set; }

        public string? Description { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public string? ImageUrl { get; set; }

        [Required]
        public string Status { get; set; }

        public string? VideoUrl { get; set; }
        public List<string>? Benefits { get; set; }
        public List<string>? FacilityImages { get; set; }
    }
}