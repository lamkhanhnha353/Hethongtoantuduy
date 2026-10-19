namespace ToanTuDuy.DTOs.Profile
{
    public class ProfileResponse
    {
        public int AccountId { get; set; }
        public string Phone { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public bool EmailConfirm { get; set; }
        public string? Avatar { get; set; }

        // Phụ huynh đã thiết lập PIN hay chưa (không lộ giá trị PIN).
        public bool HasPin { get; set; }
    }
}
