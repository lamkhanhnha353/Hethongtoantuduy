using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        public string Password { get; set; }
    }
}
