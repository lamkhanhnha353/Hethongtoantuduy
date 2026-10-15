
using System;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class ClassRequest
    {
        [Required(ErrorMessage = "ID Cấp độ khóa học không được để trống")]
        public int CourseLevelId { get; set; }

        // Cho phép null, khi nào bạn của bạn làm xong bảng Giáo viên thì truyền sau
        public int? TeacherId { get; set; }

        [Required(ErrorMessage = "Tên lớp không được để trống")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Sĩ số tối đa không được để trống")]
        public int MaxCapacity { get; set; }

        [Required(ErrorMessage = "Ngày khai giảng không được để trống")]
        public DateTime StartDate { get; set; }

        // Admin có thể truyền null, sau này Backend tự tính
        public DateTime? EndDate { get; set; }

        [Required]
        public string Status { get; set; }
    }
}