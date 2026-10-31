using System;
using System.Collections.Generic;

namespace ToanTuDuy.DTOs
{
    public class StudentRoadmapResponse
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseLevelName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public List<StudentRoadmapLessonResponse> Lessons { get; set; } = new();
    }

    public class StudentRoadmapLessonResponse
    {
        public int SessionNumber { get; set; }
        public int? LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LessonDate { get; set; }
    }
}
