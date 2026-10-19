using System.Collections.Generic;

namespace ToanTuDuy.DTOs
{
    // --- DTOs Phụ huynh ---
    public class ParentResponseDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Pin { get; set; }
        public string? Avatar { get; set; }
        public bool IsActive { get; set; }
        public List<StudentResponseDto> Students { get; set; } = new();
    }

    public class CreateParentDto
    {
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Pin { get; set; }
        public string? Avatar { get; set; }
    }

    public class UpdateParentDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Pin { get; set; }
        public string? Avatar { get; set; }
    }

    public class UpdateParentEmailDto
    {
        public string Email { get; set; } = string.Empty;
    }

    // --- DTOs Học sinh ---
    public class StudentResponseDto
    {
        public int Id { get; set; }
        public int ParentAccountId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public int TotalPoints { get; set; }
        public int Stars { get; set; }
        public int StreakDays { get; set; }
    }

    public class CreateStudentDto
    {
        public int ParentAccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }

    public class UpdateStudentDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }
}