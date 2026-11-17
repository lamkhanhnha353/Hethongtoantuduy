using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs.Profile
{
    public sealed class UpdateEmailRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [MaxLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
        public required string Email { get; init; }
    }
}
