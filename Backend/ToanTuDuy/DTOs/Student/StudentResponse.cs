namespace ToanTuDuy.DTOs.Student
{
    public class StudentResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string? Avatar { get; set; }
        public int TotalPoints { get; set; }
        public int Stars { get; set; }
        public int StreakDays { get; set; }
    }
}
