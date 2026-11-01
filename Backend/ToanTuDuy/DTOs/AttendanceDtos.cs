using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    /// <summary>
    /// Thông tin điểm danh cho một học sinh trong buổi học.
    /// </summary>
    public class AttendanceItemDto
    {
        [Required(ErrorMessage = "StudentId không được để trống")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Trạng thái điểm danh không được để trống")]
        [RegularExpression("^(PRESENT|ABSENT|LATE|EXCUSED)$", ErrorMessage = "Trạng thái phải là: PRESENT, ABSENT, LATE, hoặc EXCUSED")]
        public string Status { get; set; } = "PRESENT";

        [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? TeacherNotes { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu điểm danh danh sách nhiều học sinh trong một buổi học.
    /// </summary>
    public class BatchAttendanceRequest
    {
        [Required(ErrorMessage = "LessonId không được để trống")]
        public int LessonId { get; set; }

        [Required(ErrorMessage = "Danh sách điểm danh không được để trống")]
        [MinLength(1, ErrorMessage = "Cần ít nhất một học sinh trong danh sách điểm danh")]
        public List<AttendanceItemDto> Attendances { get; set; } = new List<AttendanceItemDto>();
    }

    /// <summary>
    /// Thông tin điểm danh của một học sinh trong buổi học (trả về cho giao diện điểm danh).
    /// </summary>
    public class StudentAttendanceItemResponse
    {
        public int? AttendanceId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Gender { get; set; }
        public string Status { get; set; } = "UNMARKED"; // PRESENT, ABSENT, LATE, EXCUSED, UNMARKED
        public string? TeacherNotes { get; set; }

        // Thông tin đơn xin nghỉ phép (nếu có)
        public bool HasLeaveRequest { get; set; }
        public string? LeaveReason { get; set; }
        public string? LeaveRequestStatus { get; set; }
    }

    /// <summary>
    /// Chi tiết buổi học và danh sách điểm danh cả lớp.
    /// </summary>
    public class LessonAttendanceDetailResponse
    {
        public int LessonId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public DateTime LessonDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string LessonStatus { get; set; } = string.Empty;

        // Thống kê sĩ số và điểm danh
        public int TotalStudents { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int ExcusedCount { get; set; }
        public int UnmarkedCount { get; set; }

        public List<StudentAttendanceItemResponse> Students { get; set; } = new List<StudentAttendanceItemResponse>();
    }

    /// <summary>
    /// Lịch sử điểm danh của học sinh.
    /// </summary>
    public class StudentAttendanceHistoryResponse
    {
        public int AttendanceId { get; set; }
        public int LessonId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public DateTime LessonDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TeacherNotes { get; set; }
    }
}
