using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; }
    }
}
