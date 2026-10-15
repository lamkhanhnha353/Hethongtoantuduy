
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
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Các trường bổ sung để Frontend dễ hiển thị
        public string CourseLevelName { get; set; }
        public string? TeacherName { get; set; }
    }
}