using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Models;

namespace ToanTuDuy.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- User DbSets ---
        public DbSet<ParentProfile> ParentProfiles { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        // ✅ THÊM MỚI
        public DbSet<Notification> Notifications { get; set; }
        // ✅ THÊM MỚI
        public DbSet<NotificationSetting> NotificationSettings { get; set; }

        // --- Learning DbSets ---
        public DbSet<Course> Courses { get; set; }
        // ✅ THÊM MỚI
        public DbSet<CourseLevel> CourseLevels { get; set; }
        public DbSet<Classes> Classes { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<ClassStudent> ClassStudents { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        // ✅ THÊM MỚI
        public DbSet<QuestionBank> QuestionBanks { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<StudentAssignment> StudentAssignments { get; set; }
        // ✅ THÊM MỚI
        public DbSet<QuestionAssignment> QuestionAssignments { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<StudentBadge> StudentBadges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Bắt buộc gọi base method của IdentityDbContext
            base.OnModelCreating(modelBuilder);

            // ✅ ĐÃ XÓA: decimal config cho Course.Fee (Fee chuyển sang CourseLevel)
            // ✅ THÊM MỚI: decimal config cho CourseLevel.Fee
            modelBuilder.Entity<CourseLevel>()
                .Property(c => c.Fee)
                .HasColumnType("decimal(10,2)");

            // ✅ ĐỔI KIỂU: Invoice.Amount nay là int, không cần decimal config
            // (đã xóa config decimal cho Invoice.Amount)

            // 1. Ánh xạ Identity User về bảng ACCOUNT
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("ACCOUNT");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PhoneNumber).HasColumnName("phone");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            });

            // 2. ✅ ĐÃ XÓA: ClassStudent composite key config (nay có Id riêng)
            // ✅ ĐÃ XÓA: StudentBadge composite key config (nay có Id riêng)

            // 3. Cấu hình 1-1 Relationship cho các Profiles (Mỗi AccountId là Unique)
            modelBuilder.Entity<ParentProfile>()
                .HasIndex(p => p.AccountId)
                .IsUnique();

            modelBuilder.Entity<TeacherProfile>()
                .HasIndex(t => t.AccountId)
                .IsUnique();

            // 4. Giải quyết triệt để lỗi vòng lặp xóa chuỗi (Multiple Cascade Paths)
            var cascadeFKs = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKs)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
