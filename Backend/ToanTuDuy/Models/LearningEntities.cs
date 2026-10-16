using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToanTuDuy.Models
{
    [Table("COURSE")]
    public class Course
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        // ✅ THÊM MỚI: min_age, max_age (từ script SQL — thay thế logic TotalSessions)
        [Column("min_age")]
        public int MinAge { get; set; }

        [Column("max_age")]
        public int MaxAge { get; set; }

        // ✅ THÊM MỚI: image_url (từ script SQL)
        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<CourseLevel> CourseLevels { get; set; } = new List<CourseLevel>();
        public ICollection<QuestionBank> QuestionBanks { get; set; } = new List<QuestionBank>();
    }

  
    [Table("COURSE_LEVEL")]
    public class CourseLevel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // ✅ ĐỔI TÊN + ĐỔI FK: course_id (→COURSE) thành course_level_id (→COURSE_LEVEL)
        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("order_index")]
        public int OrderIndex { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("fee")]
        public decimal Fee { get; set; }

        [Column("total_sessions")]
        public int TotalSessions { get; set; }

       
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; }

        // Lớp học giờ sẽ thuộc về một Cấp độ cụ thể
        public ICollection<Classes> Classes { get; set; } = new List<Classes>();
    }



    [Table("CLASSES")]
    public class Classes
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_level_id")]
        public int CourseLevelId { get; set; }

        // 1. Thêm dấu ? để cho phép Null
        [Column("teacher_id")]
        public int? TeacherId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("max_capacity")]
        public int MaxCapacity { get; set; }

        // 2. Thêm trường Ngày khai giảng
        [Column("start_date")]
        public DateTime StartDate { get; set; }
       
        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // 4. Thêm CreatedAt và UpdatedAt cho đồng bộ
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        // ĐỔI FK TARGET: trỏ vào CourseLevel thay vì Course
        [ForeignKey(nameof(CourseLevelId))]
        public CourseLevel CourseLevel { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public TeacherProfile? Teacher { get; set; } // Thêm dấu ?

        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }


    [Table("CLASS_SCHEDULE")]
    public class ClassSchedule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("day_of_week")]
        public int DayOfWeek { get; set; }

        // ✅ ĐỔI KIỂU: TimeSpan → DateTime (script SQL dùng DATETIME cho start_time/end_time)
        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        // ✅ ĐỔI KIỂU: TimeSpan → DateTime
        [Column("end_time")]
        public TimeSpan EndTime { get; set; }
        [Column("room")]
        [StringLength(100)]
        public string? Room { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ClassId))]
        public Classes Class { get; set; }
    }
    

    [Table("CLASS_STUDENT")]
    public class ClassStudent
    {
        // ✅ THÊM MỚI: id riêng thay thế composite PK (ClassId, StudentId)
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; }

        // ✅ THÊM MỚI: status (từ script SQL)
        [Column("status")]
        public string? Status { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ClassId))]
        public Classes Class { get; set; }

        [ForeignKey(nameof(StudentId))]
        public StudentProfile Student { get; set; }
    }

    [Table("INVOICE")]
    public class Invoice
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("parent_id")]
        public int ParentId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        // ✅ ĐỔI KIỂU: decimal → int (script SQL dùng INT cho amount)
        [Column("amount")]
        public int Amount { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("payment_method")]
        public string? PaymentMethod { get; set; }

        [Column("transaction_id")]
        public string? TransactionId { get; set; }

        // ✅ THÊM MỚI: created_at (từ script SQL)
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ParentId))]
        public ParentProfile Parent { get; set; }

        [ForeignKey(nameof(StudentId))]
        public StudentProfile Student { get; set; }

        [ForeignKey(nameof(ClassId))]
        public Classes Class { get; set; }
    }

    // ✅ ENTITY MỚI HOÀN TOÀN: Ánh xạ bảng QUESTION_BANK
    [Table("QUESTION_BANK")]
    public class QuestionBank
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }

    [Table("QUESTION")]
    public class Question
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // ✅ ĐỔI TÊN + ĐỔI FK: course_id (→COURSE) thành bank_id (→QUESTION_BANK)
        [Column("bank_id")]
        public int BankId { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("content")]
        public string Content { get; set; }

        // ✅ THÊM MỚI: image_url (từ script SQL)
        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("difficulty")]
        public string Difficulty { get; set; }

        [Column("answer")]
        public string Answer { get; set; }

        // Navigation Properties
        // ✅ ĐỔI FK TARGET: trỏ vào QuestionBank thay vì Course
        [ForeignKey(nameof(BankId))]
        public QuestionBank Bank { get; set; }

        public ICollection<QuestionAssignment> QuestionAssignments { get; set; } = new List<QuestionAssignment>();
    }

    [Table("ASSIGNMENT")]
    public class Assignment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        // ✅ THÊM MỚI: stage_id (từ script SQL)
        [Column("stage_id")]
        public string? StageId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("deadline")]
        public DateTime Deadline { get; set; }

        [Column("time_limit_minutes")]
        public int TimeLimitMinutes { get; set; }

        // ✅ THÊM MỚI: max_attempt (từ script SQL)
        [Column("max_attempt")]
        public int? MaxAttempt { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // ✅ ĐÃ XÓA: TeacherId và Teacher navigation (ASSIGNMENT không còn teacher_id trong script SQL)

        // Navigation Properties
        [ForeignKey(nameof(ClassId))]
        public Classes Class { get; set; }

        public ICollection<StudentAssignment> StudentAssignments { get; set; } = new List<StudentAssignment>();
        public ICollection<QuestionAssignment> QuestionAssignments { get; set; } = new List<QuestionAssignment>();
    }

    [Table("STUDENT_ASSIGNMENT")]
    public class StudentAssignment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("assignment_id")]
        public int AssignmentId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("score")]
        public float? Score { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("teacher_feedback")]
        public string? TeacherFeedback { get; set; }

        // ✅ THÊM MỚI: completed_at (từ script SQL)
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(AssignmentId))]
        public Assignment Assignment { get; set; }

        [ForeignKey(nameof(StudentId))]
        public StudentProfile Student { get; set; }
    }

    // ✅ ENTITY MỚI HOÀN TOÀN: Ánh xạ bảng QUESTION_ASSIGNMENT
    [Table("QUESTION_ASSIGNMENT")]
    public class QuestionAssignment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("assignment_id")]
        public int AssignmentId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("order_index")]
        public int OrderIndex { get; set; }

        [Column("point_value")]
        public float PointValue { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(AssignmentId))]
        public Assignment Assignment { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; }
    }

    [Table("LESSON")]
    public class Lesson
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("lesson_date")]
        public DateTime LessonDate { get; set; }

        // ✅ THÊM MỚI: start_time, end_time (từ script SQL)
        [Column("start_time")]
        public DateTime? StartTime { get; set; }

        [Column("end_time")]
        public DateTime? EndTime { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // ✅ THÊM MỚI: is_importan — giữ nguyên typo từ UML/script SQL
        [Column("is_importan")]
        public bool IsImportan { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ClassId))]
        public Classes Class { get; set; }

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }

    [Table("ATTENDANCE")]
    public class Attendance
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // ✅ THÊM MỚI: teacher_notes (từ script SQL)
        [Column("teacher_notes")]
        public string? TeacherNotes { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(LessonId))]
        public Lesson Lesson { get; set; }

        [ForeignKey(nameof(StudentId))]
        public StudentProfile Student { get; set; }
    }

    [Table("LEAVE_REQUEST")]
    public class LeaveRequest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // ✅ THÊM MỚI: created_at (từ script SQL)
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(LessonId))]
        public Lesson Lesson { get; set; }

        [ForeignKey(nameof(StudentId))]
        public StudentProfile Student { get; set; }
    }

    [Table("BADGE")]
    public class Badge
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        // ✅ THÊM MỚI: description (từ script SQL)
        [Column("description")]
        public string? Description { get; set; }

        // ✅ THÊM MỚI: icon_url (từ script SQL)
        [Column("icon_url")]
        public string? IconUrl { get; set; }

        [Column("condition_type")]
        public string ConditionType { get; set; }

        [Column("condition_value")]
        public int ConditionValue { get; set; }

        // Navigation Properties
        public ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();
    }

    [Table("StudentBadge")]
    public class StudentBadge
    {
        // ✅ THÊM MỚI: id riêng thay thế composite PK (StudentId, BadgeId)
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("badge_id")]
        public int BadgeId { get; set; }

        [Column("earned_at")]
        public DateTime EarnedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(StudentId))]
        public StudentProfile Student { get; set; }

        [ForeignKey(nameof(BadgeId))]
        public Badge Badge { get; set; }
    }
}
