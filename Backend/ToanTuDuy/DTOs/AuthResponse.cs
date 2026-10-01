namespace ToanTuDuy.DTOs
{
    public class AuthResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Role { get; set; }
        public int ProfileId { get; set; }
    }
}
