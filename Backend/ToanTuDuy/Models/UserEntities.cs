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

        // Navigation Properties
        public ParentProfile ParentProfile { get; set; }
        public TeacherProfile TeacherProfile { get; set; }
        public ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();
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

        [Column("pin")]
        public string? Pin { get; set; }

        [Column("avatar")]
        public string? Avatar { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(AccountId))]
        public ApplicationUser Account { get; set; }

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

    [Table("STUDENT_PROFILE")]
    public class StudentProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("parent_account_id")]
        public int ParentAccountId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("avatar")]
        public string? Avatar { get; set; }

        [Column("total_points")]
        public int TotalPoints { get; set; }

        [Column("stars")]
        public int Stars { get; set; }

        [Column("streak_days")]
        public int StreakDays { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ParentAccountId))]
        public ApplicationUser ParentAccount { get; set; }

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

        // Navigation Properties
        [ForeignKey(nameof(AccountId))]
        public ApplicationUser Account { get; set; }

        public ICollection<Classes> Classes { get; set; } = new List<Classes>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
