using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs.Profile
{
    public class SetPinRequest
    {
        [Required(ErrorMessage = "PIN là bắt buộc.")]
        [RegularExpression("^[0-9]{4,6}$", ErrorMessage = "PIN phải gồm 4 đến 6 chữ số.")]
        public string NewPin { get; set; } = null!;

        // Bắt buộc khi đã có PIN (đổi PIN); bỏ trống khi đặt PIN lần đầu.
        public string? CurrentPin { get; set; }
    }
}
