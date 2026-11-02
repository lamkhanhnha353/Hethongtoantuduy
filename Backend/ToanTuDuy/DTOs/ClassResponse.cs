using System;

namespace ToanTuDuy.DTOs
{
    public class ClassResponse
    {
        public int Id { get; set; }
        public int CourseLevelId { get; set; }
        public int? TeacherId { get; set; }
        public string Name { get; set; }
        public int MaxCapacity { get; set; }
        public int ActiveStudentsCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Các trường bổ sung để Frontend dễ hiển thị
        public string CourseLevelName { get; set; }
        public string? TeacherName { get; set; }
        public int? CourseId { get; set; }
        public string? CourseName { get; set; }
    }

    public class ClassStudentResponse
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string? Avatar { get; set; }
        public int TotalPoints { get; set; }
        public int Stars { get; set; }
        public int StreakDays { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public string Status { get; set; }
        public DateTime JoinedAt { get; set; }
        public int? InvoiceId { get; set; }
    }

    public class AddClassStudentRequest
    {
        public int StudentId { get; set; }
    }
}
