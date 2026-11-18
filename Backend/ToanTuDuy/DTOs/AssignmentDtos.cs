using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    // ==========================================
    // DTOs cho Bài kiểm tra / Bài tập (ASSIGNMENT)
    // ==========================================

    public class QuestionAssignmentItemDto
    {
        [Required(ErrorMessage = "QuestionId không được để trống")]
        public int QuestionId { get; set; }

        public int OrderIndex { get; set; } = 1;

        [Range(0.1, 100, ErrorMessage = "Điểm số mỗi câu hỏi phải từ 0.1 đến 100")]
        public float PointValue { get; set; } = 1.0f;
    }

    public class CreateAssignmentRequest
    {
        [Required(ErrorMessage = "Buổi học (LessonId) không được để trống")]
        public int LessonId { get; set; }

        public string? StageId { get; set; }

        [Required(ErrorMessage = "Tiêu đề bài kiểm tra không được để trống")]
        [MaxLength(255, ErrorMessage = "Tiêu đề tối đa 255 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thời gian bắt đầu không được để trống")]
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Hạn nộp không được để trống")]
        public DateTime Deadline { get; set; }

        [Range(1, 1440, ErrorMessage = "Thời gian làm bài phải từ 1 đến 1440 phút")]
        public int TimeLimitMinutes { get; set; } = 30;

        [Range(1, 100, ErrorMessage = "Số lần làm bài tối đa phải từ 1")]
        public int? MaxAttempt { get; set; } = 1;

        public string Status { get; set; } = "OPEN"; // DRAFT, OPEN, CLOSED, SCHEDULED

        // Danh sách câu hỏi chọn từ Ngân hàng câu hỏi
        public List<QuestionAssignmentItemDto> Questions { get; set; } = new List<QuestionAssignmentItemDto>();

        // Tùy chọn giao bài:
        // true: Giao tự động cho tất cả học sinh trong lớp của buổi học đó
        // false: Chỉ giao riêng lẻ cho các học sinh có ID trong SpecificStudentIds
        public bool AssignAllStudentsInClass { get; set; } = true;

        // Danh sách ID học sinh trong lớp muốn giao bài riêng lẻ (khi AssignAllStudentsInClass = false hoặc muốn chỉ định)
        public List<int>? SpecificStudentIds { get; set; }
    }

    public class UpdateAssignmentRequest
    {
        public string? StageId { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime Deadline { get; set; }

        [Range(1, 1440, ErrorMessage = "Thời gian làm bài phải từ 1 đến 1440 phút")]
        public int TimeLimitMinutes { get; set; }

        public int? MaxAttempt { get; set; }

        public string Status { get; set; } = "OPEN";

        // Cập nhật lại danh sách câu hỏi (nếu truyền lên)
        public List<QuestionAssignmentItemDto>? Questions { get; set; }
    }

    /// <summary>
    /// Đính kèm / Nhân bản bài kiểm tra có sẵn từ một bài khác sang buổi học (Lesson) mới
    /// </summary>
    public class CloneAssignmentRequest
    {
        [Required(ErrorMessage = "Bài kiểm tra nguồn không được để trống")]
        public int SourceAssignmentId { get; set; }

        [Required(ErrorMessage = "Buổi học đích không được để trống")]
        public int TargetLessonId { get; set; }

        public string? NewTitle { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? Deadline { get; set; }

        public bool AssignAllStudentsInClass { get; set; } = true;

        public List<int>? SpecificStudentIds { get; set; }
    }

    /// <summary>
    /// Giao bài kiểm tra cho danh sách học sinh riêng lẻ
    /// </summary>
    public class AssignStudentsRequest
    {
        [Required(ErrorMessage = "Danh sách học sinh không được để trống")]
        [MinLength(1, ErrorMessage = "Cần ít nhất một học sinh")]
        public List<int> StudentIds { get; set; } = new List<int>();
    }

    // ==========================================
    // Response DTOs
    // ==========================================

    public class AssignmentResponse
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string? LessonTitle { get; set; }
        public DateTime LessonDate { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string? StageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime Deadline { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int? MaxAttempt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public float TotalPoints { get; set; }
        public int AssignedStudentsCount { get; set; }
        public int CompletedStudentsCount { get; set; }
    }

    public class AssignmentDetailResponse : AssignmentResponse
    {
        public List<AssignmentQuestionDetailDto> Questions { get; set; } = new List<AssignmentQuestionDetailDto>();
        public List<StudentAssignmentResponse> AssignedStudents { get; set; } = new List<StudentAssignmentResponse>();
    }

    public class AssignmentQuestionDetailDto
    {
        public int QuestionAssignmentId { get; set; }
        public int QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public float PointValue { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class StudentPracticeAssignmentDto
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? LessonTitle { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime Deadline { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public float TotalPoints { get; set; }
        public string StudentStatus { get; set; } = string.Empty;
        public float? Score { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class StudentPracticeQuestionOptionDto
    {
        public string Key { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class StudentPracticeQuestionDto
    {
        public int QuestionAssignmentId { get; set; }
        public int OrderIndex { get; set; }
        public float PointValue { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public List<StudentPracticeQuestionOptionDto> Options { get; set; } = new List<StudentPracticeQuestionOptionDto>();
        public List<StudentPracticeSequenceTermDto> SequenceTerms { get; set; } = new List<StudentPracticeSequenceTermDto>();
    }

    public class StudentPracticeSequenceTermDto
    {
        public string? Operator { get; set; }
        public int Value { get; set; }
    }

    public class StudentPracticeAssignmentDetailDto
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? LessonTitle { get; set; }
        public DateTime Deadline { get; set; }
        public int TimeLimitMinutes { get; set; }
        public float TotalPoints { get; set; }
        public List<StudentPracticeQuestionDto> Questions { get; set; } = new List<StudentPracticeQuestionDto>();
    }

    public class SubmitStudentPracticeRequest
    {
        [Required]
        [MinLength(1)]
        public List<SubmitStudentPracticeAnswerDto> Answers { get; set; } = new List<SubmitStudentPracticeAnswerDto>();
    }

    public class SubmitStudentPracticeAnswerDto
    {
        [Range(1, int.MaxValue)]
        public int QuestionAssignmentId { get; set; }

        [Required]
        [MaxLength(4000)]
        public string Answer { get; set; } = string.Empty;
    }

    public class StudentPracticeSubmissionResponseDto
    {
        public int AssignmentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public float Score { get; set; }
        public float TotalPoints { get; set; }
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class StudentAssignmentResponse
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public float? Score { get; set; }
        public string Status { get; set; } = string.Empty; // ASSIGNED, SUBMITTED, GRADED
        public string? TeacherFeedback { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class GradeStudentRequest
    {
        public float? Score { get; set; }
        public string? TeacherFeedback { get; set; }
        public string? Status { get; set; } = "GRADED";
    }

    public class BatchGradeItemRequest
    {
        public int StudentId { get; set; }
        public float? Score { get; set; }
        public string? TeacherFeedback { get; set; }
        public string? Status { get; set; } = "GRADED";
    }

    public class BatchGradeRequest
    {
        public List<BatchGradeItemRequest> Grades { get; set; } = new List<BatchGradeItemRequest>();
    }

    public class StudentSubmissionDetailResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public int LessonId { get; set; }
        public string? LessonTitle { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int TimeLimitMinutes { get; set; }
        public float TotalPoints { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime Deadline { get; set; }

        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? StudentAvatar { get; set; }
        public float? Score { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TeacherFeedback { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<AssignmentQuestionDetailDto> Questions { get; set; } = new List<AssignmentQuestionDetailDto>();
    }
}
