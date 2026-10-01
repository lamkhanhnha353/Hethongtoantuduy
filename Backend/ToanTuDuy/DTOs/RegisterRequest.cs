using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vai trò (Role) là bắt buộc.")]
        public string Role { get; set; } // "Parent" hoặc "Teacher"

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        public string FullName { get; set; }

        // Dành riêng cho Teacher
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string? Email { get; set; }
    }
}
