namespace ToanTuDuy.DTOs
{
    public class TeacherResponseDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? Specialize { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateTeacherDto
    {
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class UpdateTeacherDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Specialize { get; set; }
    }
}