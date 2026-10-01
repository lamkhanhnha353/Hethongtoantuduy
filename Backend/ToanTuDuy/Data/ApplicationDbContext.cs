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

        // Khai báo các DbSets
        public DbSet<ParentProfile> ParentProfiles { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Classes> Classes { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<ClassStudent> ClassStudents { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<StudentAssignment> StudentAssignments { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<StudentBadge> StudentBadges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Bắt buộc gọi base method của IdentityDbContext
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .Property(c => c.Fee)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Amount)
                .HasColumnType("decimal(18,2)");

            // 1. Ánh xạ cấu trúc Identity User về bảng ACCOUNT như yêu cầu
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("ACCOUNT");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PhoneNumber).HasColumnName("phone");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                // (Role và RefreshToken đã được config bằng Data Annotations trong Class)
            });

            // 2. Cấu hình Composite Primary Keys (Khóa chính kép)
            modelBuilder.Entity<ClassStudent>()
                .HasKey(cs => new { cs.ClassId, cs.StudentId });

            modelBuilder.Entity<StudentBadge>()
                .HasKey(sb => new { sb.StudentId, sb.BadgeId });

            // 3. Cấu hình 1-1 Relationship cho các Profiles (Mỗi AccountId là Unique)
            modelBuilder.Entity<ParentProfile>()
                .HasIndex(p => p.AccountId)
                .IsUnique();

            modelBuilder.Entity<TeacherProfile>()
                .HasIndex(t => t.AccountId)
                .IsUnique();

            // 4. Giải quyết triệt để lỗi vòng lặp xóa chuỗi (Multiple Cascade Paths)
            // Thay vì cấu hình tay từng Entity, ta disable toàn bộ Cascade Delete thành Restrict 
            // cho tất cả các Foreign Keys. Nếu muốn xóa cha, bạn phải code logic xóa con (Soft Delete / Hard delete) thủ công.
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
