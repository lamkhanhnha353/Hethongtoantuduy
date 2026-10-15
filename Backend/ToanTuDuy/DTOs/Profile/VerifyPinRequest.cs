using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs.Profile
{
    public class VerifyPinRequest
    {
        [Required(ErrorMessage = "PIN là bắt buộc.")]
        [RegularExpression("^[0-9]{4,6}$", ErrorMessage = "PIN phải gồm 4 đến 6 chữ số.")]
        public string Pin { get; set; } = null!;
    }
}
