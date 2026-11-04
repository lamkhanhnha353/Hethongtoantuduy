using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    /// <summary>Google ID token submitted to authenticate an account.</summary>
    public sealed record GoogleLoginDto
    {
        [Required]
        public required string IdToken { get; init; }
    }
}
