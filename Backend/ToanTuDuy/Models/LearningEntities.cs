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

        [Column("fee")]
        public decimal Fee { get; set; }

        [Column("total_sessions")]
        public int TotalSessions { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // Navigation Properties
        public ICollection<Classes> Classes { get; set; } = new List<Classes>();
        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }

    [Table("CLASSES")]
    public class Classes
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("teacher_id")]
        public int TeacherId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("max_capacity")]
        public int MaxCapacity { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public TeacherProfile Teacher { get; set; }

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
        public string DayOfWeek { get; set; }

        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ClassId))]
        public Classes Class { get; set; }
    }

    [Table("CLASS_STUDENT")]
    public class ClassStudent
    {
        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; }

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

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("payment_method")]
        public string PaymentMethod { get; set; }

        [Column("transaction_id")]
        public string TransactionId { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ParentId))]
        public ParentProfile Parent { get; set; }

        [ForeignKey(nameof(StudentId))]
        public StudentProfile Student { get; set; }

        [ForeignKey(nameof(ClassId))]
        public Classes Class { get; set; }
    }

    [Table("QUESTION")]
    public class Question
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [Column("difficulty")]
        public string Difficulty { get; set; }

        [Column("answer")]
        public string Answer { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; }
    }

    [Table("ASSIGNMENT")]
    public class Assignment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("teacher_id")]
        public int TeacherId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("deadline")]
        public DateTime Deadline { get; set; }

        [Column("time_limit_minutes")]
        public int TimeLimitMinutes { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ClassId))]
        public Classes Class { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public TeacherProfile Teacher { get; set; }

        public ICollection<StudentAssignment> StudentAssignments { get; set; } = new List<StudentAssignment>();
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
        public string TeacherFeedback { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(AssignmentId))]
        public Assignment Assignment { get; set; }

        [ForeignKey(nameof(StudentId))]
        public StudentProfile Student { get; set; }
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

        [Column("status")]
        public string Status { get; set; }

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

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("reason")]
        public string Reason { get; set; }

        [Column("status")]
        public string Status { get; set; }

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

        [Column("condition_type")]
        public string ConditionType { get; set; }

        [Column("condition_value")]
        public int ConditionValue { get; set; }

        // Navigation Properties
        public ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();
    }

    [Table("STUDENT_BADGE")]
    public class StudentBadge
    {
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
