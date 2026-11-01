using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs.Profile
{
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = null!;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string? Phone { get; set; }
    }
}
