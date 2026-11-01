using System;

namespace ToanTuDuy.DTOs
{
    public class LessonResponse
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public DateTime LessonDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsImportan { get; set; }
        public int AttendancesCount { get; set; }
        public int LeaveRequestsCount { get; set; }
    }
}
