using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ToanTuDuy.Models
{
    // Bảng ACCOUNT được ánh xạ thành ApplicationUser
    public class ApplicationUser : IdentityUser<int>
    {
        [Column("role")]
        public string Role { get; set; }

        [Column("refresh_token")]
        public string? RefreshToken { get; set; }

        // ✅ THÊM MỚI: is_active (từ script SQL)
        [Column("is_active")]
        public bool IsActive { get; set; }

        // ✅ THÊM MỚI: created_at (từ script SQL)
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public ParentProfile ParentProfile { get; set; }
        public TeacherProfile TeacherProfile { get; set; }
        // ✅ ĐÃ XÓA: StudentProfiles (StudentProfile nay FK vào PARENT_PROFILE.id, không còn trực tiếp vào ACCOUNT)
    }

    [Table("PARENT_PROFILE")]
    public class ParentProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        // Lưu hash của PIN (không bao giờ lưu plaintext).
        [Column("pin")]
        [MaxLength(255)]
        public string? Pin { get; set; }

        [Column("avatar")]
        public string? Avatar { get; set; }

        // ✅ THÊM MỚI: created_at (từ script SQL)
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(AccountId))]
        public ApplicationUser Account { get; set; }

        // ✅ THÊM MỚI: StudentProfiles (StudentProfile nay FK vào PARENT_PROFILE.id)
        public ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        // ✅ THÊM MỚI: Navigations đến Notification và NotificationSetting
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<NotificationSetting> NotificationSettings { get; set; } = new List<NotificationSetting>();
    }

    [Table("STUDENT_PROFILE")]
    public class StudentProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // ✅ ĐỔI TÊN + ĐỔI FK: parent_account_id (→ACCOUNT) thành parent_id (→PARENT_PROFILE)
        [Column("parent_id")]
        public int ParentId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("avatar")]
        public string? Avatar { get; set; }

        [Column("total_points")]
        public int TotalPoints { get; set; }

        // ✅ THÊM MỚI: gender (từ script SQL)
        [Column("gender")]
        public string? Gender { get; set; }

        [Column("stars")]
        public int Stars { get; set; }

        [Column("streak_days")]
        public int StreakDays { get; set; }

        // ✅ THÊM MỚI: is_active (từ script SQL)
        [Column("is_active")]
        public bool IsActive { get; set; }

        // ✅ THÊM MỚI: created_at (từ script SQL)
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        // ✅ ĐỔI FK TARGET: trỏ vào ParentProfile thay vì ApplicationUser
        [ForeignKey(nameof(ParentId))]
        public ParentProfile Parent { get; set; }

        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<StudentAssignment> StudentAssignments { get; set; } = new List<StudentAssignment>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
        public ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();
    }

    [Table("TEACHER_PROFILE")]
    public class TeacherProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        // ✅ THÊM MỚI: specialize (từ script SQL)
        [Column("specialize")]
        public string? Specialize { get; set; }

        // ✅ THÊM MỚI: created_at — giữ kiểu string theo UML gốc (VARCHAR trong SQL)
        [Column("created_at")]
        public string? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(AccountId))]
        public ApplicationUser Account { get; set; }

        public ICollection<Classes> Classes { get; set; } = new List<Classes>();
        // ✅ ĐÃ XÓA: Assignments (ASSIGNMENT không còn cột teacher_id trong script SQL mới)
    }

    // ✅ ENTITY MỚI HOÀN TOÀN: Ánh xạ bảng NOTIFICATION
    [Table("NOTIFICATION")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("parent_id")]
        public int ParentId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("body")]
        public string? Body { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("ref_id")]
        public string? RefId { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ParentId))]
        public ParentProfile Parent { get; set; }
    }

    // ✅ ENTITY MỚI HOÀN TOÀN: Ánh xạ bảng NOTIFICATION_SETTING
    [Table("NOTIFICATION_SETTING")]
    public class NotificationSetting
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("parent_id")]
        public int ParentId { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ParentId))]
        public ParentProfile Parent { get; set; }
    }
}
