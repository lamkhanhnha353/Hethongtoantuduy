namespace ToanTuDuy.DTOs
{
    public class AdminAccountResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsCurrentUser { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateAccountStatusRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public bool? IsActive { get; set; }
    }
}
