using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs.Student
{
    public class UpdateStudentRequest
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = null!;
    }
}
