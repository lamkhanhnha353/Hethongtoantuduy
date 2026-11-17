using System;

namespace ToanTuDuy.DTOs
{
    public class StudentLessonResponse
    {
        public int LessonId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public DateTime LessonDate { get; set; }
        public DateTime? StartTime { get; set; } // Đã đổi thành DateTime?
        public DateTime? EndTime { get; set; }   // Đã đổi thành DateTime?
        public string Title { get; set; }
        public string Status { get; set; }
        public bool IsImportant { get; set; }
        public string? TeacherName { get; set; }
        public string? Room { get; set; }
    }
}