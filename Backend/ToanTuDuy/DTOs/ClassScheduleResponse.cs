
using System;

namespace ToanTuDuy.DTOs
{
    public class ClassScheduleResponse
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } // Trả thêm tên lớp cho dễ nhìn
        public int DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Room { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}