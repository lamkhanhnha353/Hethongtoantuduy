using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Models;
using ToanTuDuy.Services;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Data
{
    /// <summary>
    /// Service sinh dữ liệu mẫu Bogus chuẩn nghiệp vụ (~60.000 bản ghi trên 31 bảng,
    /// trong đó ATTENDANCE = 25.000 dòng), phân bổ trọng số thời gian tập trung cao điểm T8-T11/2026.
    /// Đảm bảo toàn vẹn khóa ngoại (FK), Unique Index và định dạng chuẩn của toàn bộ module API/Web.
    /// </summary>
    public class BogusDataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher;
        private readonly PasswordHasher<ParentProfile> _pinHasher;
        private readonly string _cachedPasswordHash;
        private readonly string _cachedPinHash;

        // Mật khẩu & PIN mặc định cho toàn bộ tài khoản demo được sinh ra
        public const string DefaultPassword = "Demo123@";
        public const string DefaultPin = "123456";

        // Mốc thời gian chuẩn của kịch bản dữ liệu năm 2026
        private static readonly DateTime AnchorUtc = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime HistoryStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime HistoryEnd = new(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc);
        private static readonly DateTime PeakStart = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime PeakEnd = new(2026, 11, 27, 11, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime FutureStart = new(2026, 11, 28, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime FutureEnd = new(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        private const int RandomSeedValue = 20261127;

        public BogusDataSeeder(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<ApplicationUser>();
            _pinHasher = new PasswordHasher<ParentProfile>();
            _cachedPasswordHash = _passwordHasher.HashPassword(new ApplicationUser(), DefaultPassword);
            _cachedPinHash = _pinHasher.HashPassword(new ParentProfile(), DefaultPin);
        }

        /// <summary>
        /// Nạp trực tiếp toàn bộ bộ dữ liệu chuẩn vào database thông qua EF Core (batch insert + IDENTITY_INSERT).
        /// </summary>
        public async Task<SeederResult> SeedAsync(int rowsPerTable = 1000, bool cleanExisting = true)
        {
            var stopwatch = Stopwatch.StartNew();
            var summary = new Dictionary<string, int>();

            Console.WriteLine("==================================================================");
            Console.WriteLine(" BẮT ĐẦU NẠP BỘ DỮ LIỆU BOGUS MỚI (31 BẢNG - CAO ĐIỂM T8-T11/2026)");
            Console.WriteLine($" Thời gian bắt đầu: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("==================================================================");

            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            if (cleanExisting)
            {
                await CleanExistingDataAsync();
            }

            var data = BuildDataset();

            await SaveBatchWithIdentityAsync(_context.Courses, data.Courses, "COURSE", summary);
            await SaveBatchWithIdentityAsync(_context.CourseLevels, data.CourseLevels, "COURSE_LEVEL", summary);
            await SaveBatchWithIdentityAsync(_context.Badges, data.Badges, "BADGE", summary);
            await SaveBatchWithIdentityAsync(_context.Users, data.Accounts, "ACCOUNT", summary);
            await SaveBatchWithIdentityAsync(_context.AdminProfiles, data.AdminProfiles, "ADMIN_PROFILE", summary);
            await SaveBatchWithIdentityAsync(_context.TeacherProfiles, data.TeacherProfiles, "TEACHER_PROFILE", summary);
            await SaveBatchWithIdentityAsync(_context.ParentProfiles, data.ParentProfiles, "PARENT_PROFILE", summary);
            await SaveBatchWithIdentityAsync(_context.EmailVerificationTokens, data.EmailVerificationTokens, "EMAIL_VERIFICATION_TOKEN", summary);
            await SaveBatchWithIdentityAsync(_context.TeacherSalaryConfigs, data.TeacherSalaryConfigs, "TEACHER_SALARY_CONFIG", summary);
            await SaveBatchWithIdentityAsync(_context.StudentProfiles, data.Students, "STUDENT_PROFILE", summary);
            await SaveBatchWithIdentityAsync(_context.StudentBadges, data.StudentBadges, "StudentBadge", summary);
            await SaveBatchWithIdentityAsync(_context.Classes, data.Classes, "CLASSES", summary);
            await SaveBatchWithIdentityAsync(_context.ClassSchedules, data.ClassSchedules, "CLASS_SCHEDULE", summary);
            await SaveBatchWithIdentityAsync(_context.Invoices, data.Invoices, "INVOICE", summary);
            await SaveBatchWithIdentityAsync(_context.ClassStudents, data.ClassStudents, "CLASS_STUDENT", summary);
            await SaveBatchWithIdentityAsync(_context.Lessons, data.Lessons, "LESSON", summary);
            await SaveBatchWithIdentityAsync(_context.Attendances, data.Attendances, "ATTENDANCE", summary);
            await SaveBatchWithIdentityAsync(_context.LeaveRequests, data.LeaveRequests, "LEAVE_REQUEST", summary);
            await SaveBatchWithIdentityAsync(_context.QuestionBanks, data.QuestionBanks, "QUESTION_BANK", summary);
            await SaveBatchWithIdentityAsync(_context.Questions, data.Questions, "QUESTION", summary);
            await SaveBatchWithIdentityAsync(_context.Assignments, data.Assignments, "ASSIGNMENT", summary);
            await SaveBatchWithIdentityAsync(_context.QuestionAssignments, data.QuestionAssignments, "QUESTION_ASSIGNMENT", summary);
            await SaveBatchWithIdentityAsync(_context.StudentAssignments, data.StudentAssignments, "STUDENT_ASSIGNMENT", summary);
            await SaveBatchWithIdentityAsync(_context.Conversations, data.Conversations, "CONVERSATION", summary);
            await SaveBatchWithIdentityAsync(_context.Messages, data.Messages, "MESSAGE", summary);
            await SaveBatchWithIdentityAsync(_context.Notifications, data.Notifications, "NOTIFICATION", summary);
            await SaveBatchWithIdentityAsync(_context.OutboxMessages, data.OutboxMessages, "OUTBOX", summary);
            await SaveBatchWithIdentityAsync(_context.UserInboxes, data.UserInboxes, "USER_INBOX", summary);
            await SaveBatchWithIdentityAsync(_context.NotificationSettings, data.NotificationSettings, "NOTIFICATION_SETTING", summary);
            await SaveBatchWithIdentityAsync(_context.SalaryAdjustments, data.SalaryAdjustments, "SALARY_ADJUSTMENT", summary);
            await SaveBatchWithIdentityAsync(_context.PayrollSummaries, data.PayrollSummaries, "PAYROLL_SUMMARY", summary);

            stopwatch.Stop();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;

            Console.WriteLine("==================================================================");
            Console.WriteLine($" HOÀN TẤT NẠP DỮ LIỆU BOGUS THÀNH CÔNG TRONG {stopwatch.Elapsed.TotalSeconds:F2} GIÂY!");
            Console.WriteLine($" Tổng số bảng đã xử lý: {summary.Count}");
            Console.WriteLine($" Tổng số dòng dữ liệu : {summary.Values.Sum():N0}");
            Console.WriteLine("==================================================================");

            return new SeederResult
            {
                Success = true,
                ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                TableCounts = summary
            };
        }

        /// <summary>
        /// Xóa sạch dữ liệu cũ theo đúng thứ tự ràng buộc khóa ngoại (FK) để chuẩn bị nạp mới.
        /// </summary>
        public async Task CleanExistingDataAsync()
        {
            Console.WriteLine(">>> Đang dọn dẹp dữ liệu cũ theo thứ tự khóa ngoại...");

            foreach (var table in TablesInDeleteOrder)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM [dbo].[{table}];");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [Lưu ý khi xóa bảng {table}]: {ex.Message}");
                }
            }

            Console.WriteLine(">>> Đã dọn dẹp xong dữ liệu cũ.");
        }

        private static readonly string[] TablesInDeleteOrder =
        {
            "MESSAGE", "CONVERSATION",
            "SALARY_ADJUSTMENT", "PAYROLL_SUMMARY", "TEACHER_SALARY_CONFIG",
            "USER_INBOX", "OUTBOX", "NOTIFICATION", "NOTIFICATION_SETTING",
            "QUESTION_ASSIGNMENT", "STUDENT_ASSIGNMENT", "ASSIGNMENT",
            "QUESTION", "QUESTION_BANK",
            "LEAVE_REQUEST", "ATTENDANCE", "LESSON",
            "CLASS_STUDENT", "INVOICE", "CLASS_SCHEDULE", "CLASSES",
            "StudentBadge", "BADGE",
            "STUDENT_PROFILE", "PARENT_PROFILE", "TEACHER_PROFILE", "ADMIN_PROFILE",
            "COURSE_LEVEL", "COURSE",
            "EMAIL_VERIFICATION_TOKEN",
            "AspNetUserRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens",
            "ACCOUNT"
        };

        // =========================================================================================
        // XÂY DỰNG TOÀN BỘ DATASET NHẤT QUÁN (DÙNG CHUNG CHO EF CORE SEED & SQL EXPORT)
        // =========================================================================================

        private SeedDataset BuildDataset()
        {
            Randomizer.Seed = new Random(RandomSeedValue);
            var rng = new Random(RandomSeedValue);
            var faker = new Faker("vi");

            var dataset = new SeedDataset();

            // ─── NHÓM 3 (Phần nền): COURSE (5), COURSE_LEVEL (20) ────────────────────────────
            dataset.Courses = GenerateCourses(faker);
            dataset.CourseLevels = GenerateCourseLevels(dataset.Courses);

            // ─── NHÓM 2 (Phần nền): BADGE (12) ───────────────────────────────────────────────
            dataset.Badges = GenerateBadges();

            // ─── NHÓM 1: ACCOUNT (420), PROFILES (10 + 40 + 350), TOKENS (45) ────────────────
            dataset.Accounts = GenerateAccounts(rng);
            var adminAccounts = dataset.Accounts.Where(a => a.Role == "Admin").ToList(); // 10
            var teacherAccounts = dataset.Accounts.Where(a => a.Role == "Teacher").ToList(); // 40
            var verifiedParentAccounts = dataset.Accounts.Where(a => a.Role == "Parent" && a.EmailConfirmed).ToList(); // 350
            var unverifiedAccounts = dataset.Accounts.Where(a => !a.EmailConfirmed).ToList(); // 20
            var verifiedAccounts = dataset.Accounts.Where(a => a.EmailConfirmed).ToList(); // 400

            dataset.AdminProfiles = GenerateAdminProfiles(adminAccounts, faker);
            dataset.TeacherProfiles = GenerateTeacherProfiles(teacherAccounts, faker);
            dataset.ParentProfiles = GenerateParentProfiles(verifiedParentAccounts, faker);
            dataset.EmailVerificationTokens = GenerateEmailVerificationTokens(unverifiedAccounts, verifiedParentAccounts, rng);

            // ─── NHÓM 2: STUDENT_PROFILE (500) ───────────────────────────────────────────────
            dataset.Students = GenerateStudents(dataset.ParentProfiles, faker, rng);

            // ─── NHÓM 3: CLASSES (200), CLASS_SCHEDULE (400) ─────────────────────────────────
            // Chỉ dùng 36 giáo viên đầu (Id 1..36), giữ 4 giáo viên mới (37..40) chưa gán lớp
            var activeTeachers = dataset.TeacherProfiles.Take(36).ToList();
            // Chỉ dùng 18 cấp độ đầu (Id 1..18), giữ 2 cấp độ mới (19..20) chưa mở lớp
            var activeCourseLevels = dataset.CourseLevels.Take(18).ToList();

            dataset.Classes = GenerateClasses(activeCourseLevels, activeTeachers, rng);
            dataset.ClassSchedules = GenerateClassSchedules(dataset.Classes);

            // ─── NHÓM 2: INVOICE (1.950) & CLASS_STUDENT (1.850) ─────────────────────────────
            var (invoices, classStudents, classEnrollments) = GenerateInvoicesAndClassStudents(
                dataset.Classes,
                dataset.CourseLevels,
                dataset.Students,
                dataset.ParentProfiles,
                rng);
            dataset.Invoices = invoices;
            dataset.ClassStudents = classStudents;

            // ─── NHÓM 2: StudentBadge (650) ──────────────────────────────────────────────────
            dataset.StudentBadges = GenerateStudentBadges(dataset.Students, dataset.Badges, rng);

            // ─── NHÓM 3: LESSON (3.200) ──────────────────────────────────────────────────────
            dataset.Lessons = GenerateLessons(dataset.Classes, dataset.ClassSchedules, rng);

            // ─── NHÓM 4: ATTENDANCE (25.000) & LEAVE_REQUEST (450) ───────────────────────────
            dataset.Attendances = GenerateAttendances(dataset.Lessons, classEnrollments, rng);
            dataset.LeaveRequests = GenerateLeaveRequests(dataset.Lessons, dataset.Attendances, classEnrollments, rng);

            // ─── NHÓM 4: QUESTION_BANK (15), QUESTION (600), ASSIGNMENT (800), QA (8.000), SA (7.500)
            dataset.QuestionBanks = GenerateQuestionBanks(dataset.Courses);
            dataset.Questions = GenerateQuestions(dataset.QuestionBanks, rng);
            dataset.Assignments = GenerateAssignments(dataset.Lessons, rng);
            dataset.QuestionAssignments = GenerateQuestionAssignments(dataset.Assignments, dataset.Lessons, dataset.Classes, dataset.CourseLevels, dataset.QuestionBanks, dataset.Questions, rng);
            dataset.StudentAssignments = GenerateStudentAssignments(dataset.Assignments, dataset.Lessons, classEnrollments, rng);

            // ─── NHÓM 5: LƯƠNG GIÁO VIÊN (36 + 180 + 216) ────────────────────────────────────
            dataset.TeacherSalaryConfigs = GenerateTeacherSalaryConfigs(activeTeachers, rng);
            dataset.SalaryAdjustments = GenerateSalaryAdjustments(activeTeachers, rng);
            dataset.PayrollSummaries = GeneratePayrollSummaries(
                activeTeachers,
                dataset.TeacherSalaryConfigs,
                dataset.Classes,
                dataset.Lessons,
                dataset.SalaryAdjustments);

            // ─── NHÓM 5: CONVERSATION (320) & MESSAGE (3.200) ────────────────────────────────
            dataset.Conversations = GenerateConversations(dataset.Students, dataset.Classes, classEnrollments, rng);
            dataset.Messages = GenerateMessages(dataset.Conversations, rng);

            // ─── NHÓM 1: NOTIFICATION_SETTING (400), NOTIFICATION (600), OUTBOX (600), USER_INBOX (2.400)
            dataset.NotificationSettings = GenerateNotificationSettings(verifiedAccounts, rng);
            dataset.Notifications = GenerateNotifications(adminAccounts, teacherAccounts, rng);
            dataset.OutboxMessages = GenerateOutboxMessages(dataset.Notifications, rng);
            dataset.UserInboxes = GenerateUserInboxes(dataset.Notifications, verifiedAccounts, rng);

            return dataset;
        }

        // =========================================================================================
        // HÀM TRỌNG SỐ THỜI GIAN (15% LỊCH SỬ T1-T7 | 78% CAO ĐIỂM T8-T11/27 | 7% T11/28-T12)
        // =========================================================================================

        private static DateTime RandomDateBetween(Random rng, DateTime start, DateTime end)
        {
            long totalSeconds = Math.Max(1, (long)(end - start).TotalSeconds);
            long offset = (long)(rng.NextDouble() * totalSeconds);
            return start.AddSeconds(offset);
        }

        private static DateTime WeightedRandomDate(Random rng, bool backToSchoolSurge = false)
        {
            double roll = rng.NextDouble();
            if (roll < 0.15)
            {
                return RandomDateBetween(rng, HistoryStart, HistoryEnd);
            }
            if (roll < 0.93)
            {
                if (backToSchoolSurge && rng.NextDouble() < 0.45)
                {
                    // Tăng nhẹ ở đầu T8 và đầu T9 mô phỏng mùa tựu trường
                    return RandomDateBetween(
                        rng,
                        new DateTime(2026, 8, 1, 7, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 9, 15, 22, 0, 0, DateTimeKind.Utc));
                }
                return RandomDateBetween(rng, PeakStart, PeakEnd);
            }
            return RandomDateBetween(rng, FutureStart, FutureEnd);
        }

        private static DateTime PeakDateWithBackToSchoolSurge(Random rng)
        {
            if (rng.NextDouble() < 0.45)
            {
                return RandomDateBetween(
                    rng,
                    new DateTime(2026, 8, 1, 7, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 9, 15, 22, 0, 0, DateTimeKind.Utc));
            }
            return RandomDateBetween(rng, PeakStart, PeakEnd);
        }

        private static string ComputeSha256Hex(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // =========================================================================================
        // NHÓM 3: COURSE (5) & COURSE_LEVEL (20)
        // =========================================================================================

        private List<Course> GenerateCourses(Faker f)
        {
            var specs = new[]
            {
                ("FGM", "Finger Math - Toán Bàn Tay Tư Duy", 4, 7,
                    "Phương pháp tính nhẩm nhanh từ 0 đến 99 bằng hai bàn tay, kích thích phản xạ và hứng thú toán học cho trẻ nhỏ.",
                    "https://images.unsplash.com/photo-1509228468518-180dd4864904?w=800"),
                ("SRB-BASIC", "Soroban Cơ Bản - Bàn Tính Số Học", 6, 10,
                    "Làm quen bàn tính Soroban chuẩn Nhật Bản, thành thạo thao tác hạt và công thức Anh bạn nhỏ, Anh bạn lớn.",
                    "https://images.unsplash.com/photo-1596495578065-6e0763fa1178?w=800"),
                ("SRB-ADV", "Soroban Nâng Cao - Ảo Tính Anzan", 7, 13,
                    "Luyện ảo tính Anzan không cần bàn tính, thực hiện phép nhân chia nhiều chữ số và chuỗi tính nhẩm tốc độ cao.",
                    "https://images.unsplash.com/photo-1580582932707-520aed937b7b?w=800"),
                ("LOGIC", "Toán Tư Duy Logic & Suy Luận", 7, 14,
                    "Phát triển tư duy phản biện, giải toán đố bằng sơ đồ Bar Model, quy luật dãy số và chuẩn bị kỳ thi SASMO/Kangaroo.",
                    "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=800"),
                ("PRE-PRI", "Tiền Tiểu Học - Hành Trang Vào Lớp 1", 4, 6,
                    "Xây dựng nền tảng nhận diện con số, so sánh, hình khối cơ bản và rèn thói quen tập trung trước khi vào lớp 1.",
                    "https://images.unsplash.com/photo-1577896851231-70ef18881754?w=800")
            };

            var courses = new List<Course>(5);
            for (int i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                courses.Add(new Course
                {
                    Id = i + 1,
                    Code = s.Item1,
                    Name = s.Item2,
                    MinAge = s.Item3,
                    MaxAge = s.Item4,
                    Description = s.Item5,
                    ImageUrl = s.Item6,
                    VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    Benefits = JsonSerializer.Serialize(new[]
                    {
                        "Phát triển cân bằng cả hai bán cầu não",
                        "Tăng khả năng tập trung và ghi nhớ hình ảnh",
                        "Tự tin tính nhẩm và giải quyết bài toán tư duy"
                    }),
                    FacilityImages = JsonSerializer.Serialize(new[]
                    {
                        "https://images.unsplash.com/photo-1580582932707-520aed937b7b?w=600",
                        "https://images.unsplash.com/photo-1427504494785-3a9ca7044f45?w=600"
                    }),
                    Rating = Math.Round(f.Random.Double(4.7, 5.0), 1),
                    Reviews = f.Random.Number(120, 480),
                    Status = "ACTIVE",
                    CreatedAt = new DateTime(2025, 10, 1, 8, 0, 0, DateTimeKind.Utc).AddDays(i * 5),
                    UpdatedAt = new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc)
                });
            }
            return courses;
        }

        private List<CourseLevel> GenerateCourseLevels(List<Course> courses)
        {
            var levelTitles = new[]
            {
                "Cấp độ 1 - Nhập môn",
                "Cấp độ 2 - Cơ bản",
                "Cấp độ 3 - Trung cấp",
                "Cấp độ 4 - Chuyên sâu"
            };
            var baseFees = new[] { 2200000m, 2600000m, 3000000m, 3500000m };

            var levels = new List<CourseLevel>(20);
            int id = 1;

            // Sắp xếp sao cho 2 cấp độ cuối (Id = 19, 20) là 2 cấp độ mới chưa mở lớp
            for (int order = 1; order <= 4; order++)
            {
                foreach (var course in courses)
                {
                    bool isNewUnopened = (id >= 19);
                    var syllabusItems = Enumerable.Range(1, 16)
                        .Select(s => new { session = s, title = $"Chủ đề {s}: {course.Code} cấp {order} - Thực hành & Ứng dụng" })
                        .ToList();

                    levels.Add(new CourseLevel
                    {
                        Id = id,
                        CourseId = course.Id,
                        Code = $"{course.Code}-LV{order:D2}",
                        Name = $"{course.Name} - {levelTitles[order - 1]}",
                        OrderIndex = order,
                        Status = "ACTIVE",
                        Fee = baseFees[order - 1] + (course.Id - 1) * 100000m,
                        TotalSessions = 16,
                        Syllabus = JsonSerializer.Serialize(syllabusItems),
                        Faqs = JsonSerializer.Serialize(new[]
                        {
                            new { q = "Bé cần chuẩn bị học cụ gì?", a = "Trung tâm trang bị đầy đủ bàn tính, giáo trình và tài khoản luyện tập trực tuyến." },
                            new { q = "Nếu bé nghỉ học có được học bù không?", a = "Phụ huynh gửi đơn xin nghỉ trên ứng dụng trước 2 giờ để giáo viên sắp xếp buổi bù." }
                        }),
                        CreatedAt = isNewUnopened
                            ? new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc)
                            : new DateTime(2025, 11, 1, 8, 0, 0, DateTimeKind.Utc).AddDays(id * 2),
                        UpdatedAt = isNewUnopened
                            ? new DateTime(2026, 11, 25, 8, 0, 0, DateTimeKind.Utc)
                            : new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc)
                    });
                    id++;
                }
            }

            return levels;
        }

        // =========================================================================================
        // NHÓM 2: BADGE (12)
        // =========================================================================================

        private List<Badge> GenerateBadges()
        {
            var specs = new[]
            {
                ("Chiến Binh Khởi Đầu", "Hoàn thành buổi học đầu tiên tại trung tâm", "FIRST_LESSON", 1),
                ("Thần Tốc Tính Nhẩm", "Hoàn thành bài tập tính nhẩm với tốc độ dưới 3 phút", "FAST_SOLVER", 180),
                ("Chuyên Cần Tháng", "Đi học đầy đủ 100% các buổi trong tháng không nghỉ", "ATTENDANCE_FULL", 8),
                ("Bảng Vàng Tư Duy", "Đạt điểm từ 90/100 trở lên trong 5 bài tập liên tiếp", "PERFECT_SCORE", 5),
                ("Hoàn Thành Xuất Sắc", "Hoàn thành trọn vẹn một cấp độ với kết quả Giỏi", "LEVEL_COMPLETED", 1),
                ("Ong Chăm Chỉ 7 Ngày", "Duy trì chuỗi học tập 7 ngày liên tiếp", "STREAK_DAYS", 7),
                ("Bậc Thầy Bàn Tính", "Tích lũy đạt mốc 1.000 điểm tư duy Soroban", "TOTAL_POINTS", 1000),
                ("Vua Phản Xạ Ngón Tay", "Thực hiện chuỗi phép tính Finger Math chuẩn xác 100%", "FINGER_SPEED", 20),
                ("Chuyên Gia Suy Luận", "Giải đúng 25 bài toán tư duy logic cấp độ khó", "LOGIC_SOLVE", 25),
                ("Chinh Phục 100 Sao", "Tích lũy đủ 100 ngôi sao thưởng từ bài tập về nhà", "STARS_COLLECTED", 100),
                // 2 danh hiệu giữ trạng thái khóa (chưa cấp cho học sinh nào)
                ("Đại Kiện Tướng Anzan", "Hoàn thành thử thách Flash Anzan 3 chữ số không sai câu nào", "ANZAN_MASTER", 50),
                ("Huyền Thoại Olympic Toán", "Đạt giải Vàng kỳ thi học sinh giỏi Toán Tư duy cấp toàn quốc", "OLYMPIC_GOLD", 1)
            };

            var badges = new List<Badge>(12);
            for (int i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                badges.Add(new Badge
                {
                    Id = i + 1,
                    Name = s.Item1,
                    Description = s.Item2,
                    ConditionType = s.Item3,
                    ConditionValue = s.Item4,
                    IconUrl = $"https://api.dicebear.com/7.x/shapes/svg?seed=math_badge_{i + 1}"
                });
            }
            return badges;
        }

        // =========================================================================================
        // NHÓM 1: ACCOUNT (420), PROFILES (10 + 40 + 350), EMAIL_VERIFICATION_TOKEN (45)
        // =========================================================================================

        private List<ApplicationUser> GenerateAccounts(Random rng)
        {
            var accounts = new List<ApplicationUser>(420);
            int id = 1;

            // --- 10 Admin (id 1..10): 2 Super Admin, 5 Giáo vụ/Tuyển sinh, 3 Kế toán ---
            accounts.Add(CreateUser(id++, "0901999999", "admin123@toantuduy.edu.vn", "Admin123", "Admin", true, null,
                new DateTime(2025, 11, 1, 8, 0, 0, DateTimeKind.Utc)));
            accounts.Add(CreateUser(id++, "0901000000", "admin@toantuduy.edu.vn", "0901000000", "Admin", true, null,
                new DateTime(2025, 11, 2, 8, 0, 0, DateTimeKind.Utc)));

            for (int i = 3; i <= 10; i++)
            {
                string phone = $"0901{i:D6}";
                string email = i <= 7 ? $"giaovu{i - 2:D2}@toantuduy.edu.vn" : $"ketoan{i - 7:D2}@toantuduy.edu.vn";
                accounts.Add(CreateUser(id++, phone, email, phone, "Admin", true, null,
                    new DateTime(2025, 11, 10, 8, 0, 0, DateTimeKind.Utc).AddDays(i * 3)));
            }

            // --- 40 Teacher (id 11..50): 36 đang đứng lớp (11..46) + 4 mới onboard (47..50) ---
            accounts.Add(CreateUser(id++, "0902999999", "teacher123@toantuduy.edu.vn", "Teacher123", "Teacher", true, null,
                new DateTime(2025, 12, 1, 8, 0, 0, DateTimeKind.Utc)));
            accounts.Add(CreateUser(id++, "0902000001", "teacher001@toantuduy.edu.vn", "0902000001", "Teacher", true, null,
                new DateTime(2025, 12, 5, 8, 0, 0, DateTimeKind.Utc)));

            for (int i = 2; i <= 35; i++)
            {
                string phone = $"0902{i:D6}";
                DateTime created = i <= 20
                    ? new DateTime(2025, 12, 10, 8, 0, 0, DateTimeKind.Utc).AddDays(i)
                    : new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc).AddDays(i * 2);
                accounts.Add(CreateUser(id++, phone, $"gv.toantuduy{i:D3}@toantuduy.edu.vn", phone, "Teacher", true, null, created));
            }

            // 4 giáo viên mới onboard (T11/2026, chưa gán lớp & chưa cấu hình lương)
            for (int i = 36; i <= 39; i++)
            {
                string phone = $"0902{i:D6}";
                accounts.Add(CreateUser(id++, phone, $"gv.onboard{i - 35:D2}@toantuduy.edu.vn", phone, "Teacher", true, null,
                    new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc).AddDays(i - 36)));
            }

            // --- 350 Phụ huynh đã xác thực (id 51..400) ---
            // 140 phụ huynh của 200 HS active (chủ yếu tạo từ T7-T10/2026)
            // 190 phụ huynh của HS cũ/bảo lưu (T1-T7/2026)
            // 20 phụ huynh mới chưa thêm hồ sơ con (T11/2026)
            for (int i = 1; i <= 350; i++)
            {
                string phone = $"0903{i:D6}";
                DateTime created;
                if (i <= 140)
                {
                    created = RandomDateBetween(rng, new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc));
                }
                else if (i <= 330)
                {
                    created = RandomDateBetween(rng, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc));
                }
                else
                {
                    created = RandomDateBetween(rng, new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 26, 18, 0, 0, DateTimeKind.Utc));
                }

                accounts.Add(CreateUser(id++, phone, $"phuhuynh{i:D4}@gmail.com", phone, "Parent", true, null, created));
            }

            // --- 20 Tài khoản mới đăng ký chưa xác thực email (id 401..420) ---
            for (int i = 1; i <= 20; i++)
            {
                string phone = $"0904{i:D6}";
                string email = $"newuser{i:D3}@gmail.com";
                DateTime created = RandomDateBetween(rng, new DateTime(2026, 11, 26, 14, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 27, 11, 30, 0, DateTimeKind.Utc));
                accounts.Add(CreateUser(id++, phone, email, phone, "Parent", false, email, created));
            }

            return accounts;
        }

        private ApplicationUser CreateUser(
            int id,
            string phone,
            string email,
            string userName,
            string role,
            bool emailConfirmed,
            string? pendingEmail,
            DateTime createdAt)
        {
            return new ApplicationUser
            {
                Id = id,
                PhoneNumber = phone,
                PhoneNumberConfirmed = emailConfirmed,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                PendingEmail = pendingEmail,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                EmailConfirmed = emailConfirmed,
                Role = role,
                IsActive = true,
                CreatedAt = createdAt,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0,
                SecurityStamp = ComputeSha256Hex($"sec-stamp-{id}-{userName}")[..32],
                ConcurrencyStamp = ComputeSha256Hex($"conc-stamp-{id}-{userName}")[..32],
                PasswordHash = _cachedPasswordHash
            };
        }

        private List<AdminProfile> GenerateAdminProfiles(List<ApplicationUser> adminAccounts, Faker f)
        {
            var titles = new[]
            {
                "Nguyễn Minh Quang (Super Admin)",
                "Trần Thị Hạnh (Super Admin)",
                "Lê Thu Thảo (Giáo vụ)",
                "Phạm Hoàng Nam (Giáo vụ)",
                "Đỗ Khánh Linh (Tư vấn tuyển sinh)",
                "Vũ Gia Huy (Tư vấn tuyển sinh)",
                "Bùi Ngọc Ánh (Giáo vụ)",
                "Hoàng Đức Mạnh (Kế toán trưởng)",
                "Đặng Mỹ Duyên (Kế toán công nợ)",
                "Ngô Thanh Tùng (Kế toán lương)"
            };

            var profiles = new List<AdminProfile>(adminAccounts.Count);
            for (int i = 0; i < adminAccounts.Count; i++)
            {
                var acc = adminAccounts[i];
                profiles.Add(new AdminProfile
                {
                    Id = i + 1,
                    AccountId = acc.Id,
                    FullName = i < titles.Length ? titles[i] : f.Name.FullName(),
                    Avatar = $"https://api.dicebear.com/7.x/avataaars/svg?seed=admin_{acc.Id}",
                    CreatedAt = acc.CreatedAt
                });
            }
            return profiles;
        }

        private List<TeacherProfile> GenerateTeacherProfiles(List<ApplicationUser> teacherAccounts, Faker f)
        {
            var specializations = new[]
            {
                "Finger Math & Toán Bàn Tay Mầm Non",
                "Soroban Cơ Bản & Kỹ Năng Thao Tác Hạt",
                "Soroban Nâng Cao & Ảo Tính Anzan",
                "Toán Tư Duy Logic & Sơ Đồ Bar Model",
                "Tiền Tiểu Học & Hành Trang Lớp 1"
            };

            var profiles = new List<TeacherProfile>(teacherAccounts.Count);
            for (int i = 0; i < teacherAccounts.Count; i++)
            {
                var acc = teacherAccounts[i];
                string fullName = i switch
                {
                    0 => "Thầy Huỳnh Nhật Hào (GV Demo)",
                    1 => "Cô Nguyễn Thị Mai Anh",
                    _ => f.Name.FullName()
                };

                profiles.Add(new TeacherProfile
                {
                    Id = i + 1,
                    AccountId = acc.Id,
                    FullName = fullName,
                    Email = acc.Email,
                    Specialize = specializations[i % specializations.Length],
                    Avatar = $"https://api.dicebear.com/7.x/avataaars/svg?seed=teacher_{acc.Id}",
                    CreatedAt = acc.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                });
            }
            return profiles;
        }

        private List<ParentProfile> GenerateParentProfiles(List<ApplicationUser> verifiedParentAccounts, Faker f)
        {
            var profiles = new List<ParentProfile>(verifiedParentAccounts.Count);
            for (int i = 0; i < verifiedParentAccounts.Count; i++)
            {
                var acc = verifiedParentAccounts[i];
                profiles.Add(new ParentProfile
                {
                    Id = i + 1,
                    AccountId = acc.Id,
                    FullName = f.Name.FullName(),
                    Pin = _cachedPinHash,
                    Avatar = $"https://api.dicebear.com/7.x/avataaars/svg?seed=parent_{acc.Id}",
                    CreatedAt = acc.CreatedAt
                });
            }
            return profiles;
        }

        private List<EmailVerificationToken> GenerateEmailVerificationTokens(
            List<ApplicationUser> unverifiedAccounts,
            List<ApplicationUser> verifiedParentAccounts,
            Random rng)
        {
            var tokens = new List<EmailVerificationToken>(45);
            int id = 1;

            // 1) 20 token còn hạn khớp 1-1 với 20 account chưa xác thực (ExpiresAt > AnchorUtc)
            foreach (var acc in unverifiedAccounts)
            {
                tokens.Add(new EmailVerificationToken
                {
                    Id = id,
                    AccountId = acc.Id,
                    TokenHash = ComputeSha256Hex($"valid-token-{id}-{acc.Email}"),
                    EmailAtIssue = acc.Email!,
                    CreatedAt = acc.CreatedAt,
                    ExpiresAt = acc.CreatedAt.AddHours(48),
                    UsedAt = null
                });
                id++;
            }

            // 2) 15 token đã sử dụng (UsedAt != null)
            for (int i = 0; i < 15; i++)
            {
                var acc = verifiedParentAccounts[i];
                DateTime issued = acc.CreatedAt;
                tokens.Add(new EmailVerificationToken
                {
                    Id = id,
                    AccountId = acc.Id,
                    TokenHash = ComputeSha256Hex($"used-token-{id}-{acc.Email}"),
                    EmailAtIssue = acc.Email!,
                    CreatedAt = issued,
                    ExpiresAt = issued.AddHours(24),
                    UsedAt = issued.AddMinutes(rng.Next(5, 180))
                });
                id++;
            }

            // 3) 10 token đã hết hạn chưa sử dụng (ExpiresAt < AnchorUtc, UsedAt = null)
            for (int i = 15; i < 25; i++)
            {
                var acc = verifiedParentAccounts[i];
                DateTime issued = RandomDateBetween(rng, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc));
                tokens.Add(new EmailVerificationToken
                {
                    Id = id,
                    AccountId = acc.Id,
                    TokenHash = ComputeSha256Hex($"expired-token-{id}-{acc.Email}"),
                    EmailAtIssue = acc.Email!,
                    CreatedAt = issued,
                    ExpiresAt = issued.AddHours(24),
                    UsedAt = null
                });
                id++;
            }

            return tokens;
        }

        // =========================================================================================
        // NHÓM 2: STUDENT_PROFILE (500)
        // =========================================================================================

        private List<StudentProfile> GenerateStudents(List<ParentProfile> parents, Faker f, Random rng)
        {
            var students = new List<StudentProfile>(500);
            int id = 1;

            // 1) 200 học sinh đang theo học tích cực (Active) trong T8-T11/2026 thuộc 140 phụ huynh đầu (Id 1..140)
            //    60 phụ huynh đầu có 2 con (120 HS), 80 phụ huynh tiếp theo có 1 con (80 HS) => 200 HS
            for (int pIdx = 0; pIdx < 140; pIdx++)
            {
                var parent = parents[pIdx];
                int childCount = pIdx < 60 ? 2 : 1;
                for (int c = 0; c < childCount; c++)
                {
                    string gender = rng.Next(2) == 0 ? "Nam" : "Nữ";
                    students.Add(new StudentProfile
                    {
                        Id = id,
                        ParentId = parent.Id,
                        FullName = gender == "Nam"
                            ? f.Name.FullName(Bogus.DataSets.Name.Gender.Male)
                            : f.Name.FullName(Bogus.DataSets.Name.Gender.Female),
                        Gender = gender,
                        Avatar = $"https://api.dicebear.com/7.x/bottts/svg?seed=student_active_{id}",
                        TotalPoints = rng.Next(650, 4800),
                        Stars = rng.Next(25, 140),
                        StreakDays = rng.Next(3, 45),
                        IsActive = true,
                        CreatedAt = parent.CreatedAt.AddHours(rng.Next(1, 48))
                    });
                    id++;
                }
            }

            // 2) 250 học viên đã hoàn thành khóa lịch sử (Id 201..450) thuộc 160 phụ huynh (Id 141..300)
            //    90 phụ huynh có 2 con (180 HS) + 70 phụ huynh có 1 con (70 HS) => 250 HS
            for (int pIdx = 140; pIdx < 300; pIdx++)
            {
                var parent = parents[pIdx];
                int childCount = pIdx < 230 ? 2 : 1;
                for (int c = 0; c < childCount; c++)
                {
                    string gender = rng.Next(2) == 0 ? "Nam" : "Nữ";
                    students.Add(new StudentProfile
                    {
                        Id = id,
                        ParentId = parent.Id,
                        FullName = gender == "Nam"
                            ? f.Name.FullName(Bogus.DataSets.Name.Gender.Male)
                            : f.Name.FullName(Bogus.DataSets.Name.Gender.Female),
                        Gender = gender,
                        Avatar = $"https://api.dicebear.com/7.x/bottts/svg?seed=student_alumni_{id}",
                        TotalPoints = rng.Next(300, 2600),
                        Stars = rng.Next(10, 85),
                        StreakDays = 0,
                        IsActive = false,
                        CreatedAt = parent.CreatedAt.AddHours(rng.Next(1, 24))
                    });
                    id++;
                }
            }

            // 3) 50 học viên bảo lưu / chờ lớp (Id 451..500) thuộc 30 phụ huynh (Id 301..330)
            //    20 phụ huynh có 2 con (40 HS) + 10 phụ huynh có 1 con (10 HS) => 50 HS
            //    (20 phụ huynh cuối Id 331..350 không có hồ sơ con)
            for (int pIdx = 300; pIdx < 330; pIdx++)
            {
                var parent = parents[pIdx];
                int childCount = pIdx < 320 ? 2 : 1;
                for (int c = 0; c < childCount; c++)
                {
                    string gender = rng.Next(2) == 0 ? "Nam" : "Nữ";
                    students.Add(new StudentProfile
                    {
                        Id = id,
                        ParentId = parent.Id,
                        FullName = gender == "Nam"
                            ? f.Name.FullName(Bogus.DataSets.Name.Gender.Male)
                            : f.Name.FullName(Bogus.DataSets.Name.Gender.Female),
                        Gender = gender,
                        Avatar = $"https://api.dicebear.com/7.x/bottts/svg?seed=student_hold_{id}",
                        TotalPoints = rng.Next(0, 400),
                        Stars = rng.Next(0, 15),
                        StreakDays = 0,
                        IsActive = false,
                        CreatedAt = parent.CreatedAt.AddHours(rng.Next(1, 48))
                    });
                    id++;
                }
            }

            return students;
        }

        // =========================================================================================
        // NHÓM 3: CLASSES (200) & CLASS_SCHEDULE (400)
        // =========================================================================================

        private List<Classes> GenerateClasses(List<CourseLevel> activeLevels, List<TeacherProfile> activeTeachers, Random rng)
        {
            var classes = new List<Classes>(200);

            // 1) 45 lớp lịch sử T1-T7/2026 (Id 1..45) -> COMPLETED
            for (int i = 1; i <= 45; i++)
            {
                var level = activeLevels[(i - 1) % activeLevels.Count];
                var teacher = activeTeachers[(i - 1) % activeTeachers.Count];
                var startDate = RandomDateBetween(rng, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc)).Date;
                var endDate = startDate.AddDays(56); // ~8 tuần

                classes.Add(new Classes
                {
                    Id = i,
                    CourseLevelId = level.Id,
                    TeacherId = teacher.Id,
                    Name = $"Lớp {level.Code}-K{i:D3}",
                    MaxCapacity = 12,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "COMPLETED",
                    CreatedAt = startDate.AddDays(-14),
                    UpdatedAt = endDate
                });
            }

            // 2) 60 lớp ngắn hạn vừa kết thúc trong T8-T11/2026 (Id 46..105) -> COMPLETED
            for (int i = 46; i <= 105; i++)
            {
                var level = activeLevels[(i - 1) % activeLevels.Count];
                var teacher = activeTeachers[(i - 1) % activeTeachers.Count];
                var startDate = RandomDateBetween(rng, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc)).Date;
                var endDate = startDate.AddDays(60);
                if (endDate > new DateTime(2026, 11, 24)) endDate = new DateTime(2026, 11, 24);

                classes.Add(new Classes
                {
                    Id = i,
                    CourseLevelId = level.Id,
                    TeacherId = teacher.Id,
                    Name = $"Lớp {level.Code}-T{startDate.Month:D2}-{i:D3}",
                    MaxCapacity = 12,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "COMPLETED",
                    CreatedAt = startDate.AddDays(-14),
                    UpdatedAt = endDate
                });
            }

            // 3) 80 lớp đang học trong T8-T11/2026 (Id 106..185) -> ACTIVE
            for (int i = 106; i <= 185; i++)
            {
                var level = activeLevels[(i - 1) % activeLevels.Count];
                var teacher = activeTeachers[(i - 1) % activeTeachers.Count];
                var startDate = RandomDateBetween(rng, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc)).Date;
                var endDate = new DateTime(2026, 12, 15).AddDays((i % 14));

                classes.Add(new Classes
                {
                    Id = i,
                    CourseLevelId = level.Id,
                    TeacherId = teacher.Id,
                    Name = $"Lớp {level.Code}-A{i:D3}",
                    MaxCapacity = 12,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "ACTIVE",
                    CreatedAt = startDate.AddDays(-12),
                    UpdatedAt = new DateTime(2026, 11, 25, 9, 0, 0, DateTimeKind.Utc)
                });
            }

            // 4) 15 lớp sắp khai giảng T12/2026 (Id 186..200) -> UPCOMING (5 lớp cuối teacher_id = NULL)
            for (int i = 186; i <= 200; i++)
            {
                var level = activeLevels[(i - 1) % activeLevels.Count];
                int? teacherId = i <= 195 ? activeTeachers[(i - 1) % activeTeachers.Count].Id : null;
                var startDate = new DateTime(2026, 12, 1).AddDays((i - 186));
                var endDate = startDate.AddDays(56);

                classes.Add(new Classes
                {
                    Id = i,
                    CourseLevelId = level.Id,
                    TeacherId = teacherId,
                    Name = $"Lớp {level.Code}-T12-{i:D3}",
                    MaxCapacity = 12,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "UPCOMING",
                    CreatedAt = new DateTime(2026, 11, 15, 8, 0, 0, DateTimeKind.Utc).AddDays(i - 186),
                    UpdatedAt = new DateTime(2026, 11, 26, 8, 0, 0, DateTimeKind.Utc)
                });
            }

            return classes;
        }

        private List<ClassSchedule> GenerateClassSchedules(List<Classes> classes)
        {
            var schedules = new List<ClassSchedule>(400);

            // Cặp thứ cố định (DayOfWeek chuẩn 0..6 theo ClassScheduleService: 0=CN, 1=T2..6=T7)
            var dayPairs = new[]
            {
                (1, 3), // T2 - T4
                (2, 4), // T3 - T5
                (3, 5), // T4 - T6
                (4, 6), // T5 - T7
                (5, 0), // T6 - CN
                (6, 0), // T7 - CN
                (1, 5)  // T2 - T6
            };

            var timeSlots = new[]
            {
                (new TimeSpan(8, 0, 0), new TimeSpan(9, 30, 0)),
                (new TimeSpan(9, 45, 0), new TimeSpan(11, 15, 0)),
                (new TimeSpan(14, 0, 0), new TimeSpan(15, 30, 0)),
                (new TimeSpan(16, 0, 0), new TimeSpan(17, 30, 0)),
                (new TimeSpan(18, 0, 0), new TimeSpan(19, 30, 0))
            };

            var rooms = Enumerable.Range(1, 12)
                .Select(r => $"Phòng P{((r - 1) / 4) + 1}0{((r - 1) % 4) + 1}")
                .ToArray();

            // Theo dõi tránh trùng giáo viên/phòng trên nhóm lớp đang học ACTIVE (106..185) và UPCOMING (186..200)
            var activeTeacherBusy = new HashSet<(int TeacherId, int Day, int Slot)>();
            var activeRoomBusy = new HashSet<(string Room, int Day, int Slot)>();

            int id = 1;
            for (int idx = 0; idx < classes.Count; idx++)
            {
                var c = classes[idx];
                bool checkActiveConflicts = c.Status is "ACTIVE" or "UPCOMING";

                int chosenPairIdx = idx % dayPairs.Length;
                int chosenSlotIdx = (idx / dayPairs.Length) % timeSlots.Length;
                int chosenRoomIdx = idx % rooms.Length;

                if (checkActiveConflicts)
                {
                    bool found = false;
                    for (int pTry = 0; pTry < dayPairs.Length && !found; pTry++)
                    {
                        int pIdx = (chosenPairIdx + pTry) % dayPairs.Length;
                        var (d1, d2) = dayPairs[pIdx];
                        for (int sTry = 0; sTry < timeSlots.Length && !found; sTry++)
                        {
                            int sIdx = (chosenSlotIdx + sTry) % timeSlots.Length;
                            if (c.TeacherId.HasValue &&
                                (activeTeacherBusy.Contains((c.TeacherId.Value, d1, sIdx)) ||
                                 activeTeacherBusy.Contains((c.TeacherId.Value, d2, sIdx))))
                            {
                                continue;
                            }

                            for (int rTry = 0; rTry < rooms.Length; rTry++)
                            {
                                int rIdx = (chosenRoomIdx + rTry) % rooms.Length;
                                string rm = rooms[rIdx];
                                if (!activeRoomBusy.Contains((rm, d1, sIdx)) &&
                                    !activeRoomBusy.Contains((rm, d2, sIdx)))
                                {
                                    chosenPairIdx = pIdx;
                                    chosenSlotIdx = sIdx;
                                    chosenRoomIdx = rIdx;
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }

                    var (fd1, fd2) = dayPairs[chosenPairIdx];
                    string chosenRoom = rooms[chosenRoomIdx];
                    activeRoomBusy.Add((chosenRoom, fd1, chosenSlotIdx));
                    activeRoomBusy.Add((chosenRoom, fd2, chosenSlotIdx));
                    if (c.TeacherId.HasValue)
                    {
                        activeTeacherBusy.Add((c.TeacherId.Value, fd1, chosenSlotIdx));
                        activeTeacherBusy.Add((c.TeacherId.Value, fd2, chosenSlotIdx));
                    }
                }

                var (day1, day2) = dayPairs[chosenPairIdx];
                var slot = timeSlots[chosenSlotIdx];
                string room = rooms[chosenRoomIdx];

                schedules.Add(new ClassSchedule
                {
                    Id = id++,
                    ClassId = c.Id,
                    DayOfWeek = day1,
                    StartTime = slot.Item1,
                    EndTime = slot.Item2,
                    Room = room,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                });

                schedules.Add(new ClassSchedule
                {
                    Id = id++,
                    ClassId = c.Id,
                    DayOfWeek = day2,
                    StartTime = slot.Item1,
                    EndTime = slot.Item2,
                    Room = room,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                });
            }

            return schedules;
        }

        // =========================================================================================
        // NHÓM 2: INVOICE (1.950) & CLASS_STUDENT (1.850)
        // =========================================================================================

        private (List<Invoice> Invoices, List<ClassStudent> ClassStudents, Dictionary<int, List<int>> ClassEnrollments)
            GenerateInvoicesAndClassStudents(
                List<Classes> classes,
                List<CourseLevel> courseLevels,
                List<StudentProfile> students,
                List<ParentProfile> parents,
                Random rng)
        {
            var invoices = new List<Invoice>(1950);
            var classStudents = new List<ClassStudent>(1850);
            var classEnrollments = new Dictionary<int, List<int>>(185);

            var levelById = courseLevels.ToDictionary(cl => cl.Id);
            var activeStudents = students.Take(200).ToList();       // Id 1..200
            var alumniStudents = students.Skip(200).Take(250).ToList(); // Id 201..450
            var waitingStudents = students.Skip(450).Take(50).ToList(); // Id 451..500

            int invoiceId = 1;
            int classStudentId = 1;

            // 1) 1.800 hóa đơn PAID gắn 1-1 với 1.800 suất học (10 HV/lớp cho 180 lớp Id 1..180)
            //    - Lớp 1..45 (45 lớp lịch sử T1-T7): 450 hóa đơn PAID (học viên cũ Id 201..450)
            //    - Lớp 46..105 (60 lớp ngắn hạn T8-T11): 600 hóa đơn PAID (học viên cũ + học sinh active)
            //    - Lớp 106..180 (75 lớp đang học T8-T11): 750 hóa đơn PAID (200 học sinh active)
            //    => Tổng Paid T8-T11 = 600 + 750 = 1.350 hóa đơn (75% của 1.800 PAID)!
            for (int classIdx = 0; classIdx < 180; classIdx++)
            {
                var c = classes[classIdx];
                var level = levelById[c.CourseLevelId];
                int amount = (int)level.Fee;

                List<StudentProfile> pool;
                if (c.Id <= 45)
                {
                    pool = alumniStudents;
                }
                else if (c.Id <= 105)
                {
                    // Lớp 46..65 dùng học sinh active (200 suất -> mỗi em ~1 lớp trước đó); 66..105 dùng học viên cũ (400 suất)
                    pool = c.Id <= 65 ? activeStudents : alumniStudents;
                }
                else
                {
                    pool = activeStudents;
                }

                // Chọn đúng 10 học sinh khác nhau cho lớp này
                var selected = new List<StudentProfile>(10);
                int startOffset = (classIdx * 10) % pool.Count;
                for (int k = 0; k < 10; k++)
                {
                    selected.Add(pool[(startOffset + k) % pool.Count]);
                }

                classEnrollments[c.Id] = selected.Select(s => s.Id).ToList();

                foreach (var student in selected)
                {
                    DateTime paidAt;
                    if (c.Id <= 45)
                    {
                        // Nằm gọn trong T1-T7/2026
                        DateTime minDate = c.StartDate.AddDays(-12);
                        if (minDate < HistoryStart) minDate = HistoryStart.AddHours(8);
                        DateTime maxDate = c.StartDate.AddDays(3);
                        if (maxDate > HistoryEnd) maxDate = HistoryEnd.AddHours(-2);
                        paidAt = RandomDateBetween(rng, minDate, maxDate);
                    }
                    else
                    {
                        // Tập trung vào T8-T11/2026, tăng nhẹ đầu T8 & đầu T9 mùa tựu trường
                        paidAt = PeakDateWithBackToSchoolSurge(rng);
                        // Ưu tiên gần ngày khai giảng nhưng vẫn đảm bảo trong [2026-08-01, 2026-11-27]
                        DateTime nearStart = c.StartDate.AddDays(rng.Next(-10, 5)).AddHours(rng.Next(8, 20));
                        if (nearStart >= PeakStart && nearStart <= PeakEnd)
                        {
                            paidAt = rng.NextDouble() < 0.7 ? nearStart : paidAt;
                        }
                    }

                    DateTime createdAt = paidAt.AddMinutes(-rng.Next(2, 14));
                    bool isVnPay = rng.NextDouble() < 0.75;

                    invoices.Add(new Invoice
                    {
                        Id = invoiceId,
                        ParentId = student.ParentId,
                        StudentId = student.Id,
                        ClassId = c.Id,
                        TxnRef = $"TTD2026{invoiceId:D6}",
                        Amount = amount,
                        Status = "PAID",
                        PaymentMethod = isVnPay ? "VNPAY" : "CASH",
                        TransactionId = isVnPay ? $"VNP2026{invoiceId:D8}" : null,
                        CheckoutUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?order=TTD2026{invoiceId:D6}",
                        QrCode = $"data:image/png;base64,qr_toantuduy_{invoiceId}",
                        CreatedAt = createdAt,
                        ExpiredAt = createdAt.AddMinutes(15),
                        PaidAt = paidAt
                    });

                    classStudents.Add(new ClassStudent
                    {
                        Id = classStudentId++,
                        ClassId = c.Id,
                        StudentId = student.Id,
                        InvoiceId = invoiceId,
                        JoinedAt = paidAt,
                        Status = "ACTIVE"
                    });

                    invoiceId++;
                }
            }

            // 2) 50 suất học thử / học bổng (invoice_id = NULL) cho 5 lớp đang học (Id 181..185, 10 HV/lớp)
            for (int classIdx = 180; classIdx < 185; classIdx++)
            {
                var c = classes[classIdx];
                var selected = new List<StudentProfile>(10);
                int startOffset = ((classIdx - 180) * 10 + 15) % activeStudents.Count;
                for (int k = 0; k < 10; k++)
                {
                    selected.Add(activeStudents[(startOffset + k) % activeStudents.Count]);
                }

                classEnrollments[c.Id] = selected.Select(s => s.Id).ToList();

                foreach (var student in selected)
                {
                    DateTime joinedAt = c.StartDate.AddDays(-rng.Next(1, 7)).AddHours(rng.Next(8, 17));
                    if (joinedAt < PeakStart) joinedAt = PeakStart.AddHours(9);
                    if (joinedAt > PeakEnd) joinedAt = PeakEnd.AddHours(-2);

                    classStudents.Add(new ClassStudent
                    {
                        Id = classStudentId++,
                        ClassId = c.Id,
                        StudentId = student.Id,
                        InvoiceId = null,
                        JoinedAt = joinedAt,
                        Status = "ACTIVE"
                    });
                }
            }

            // 3) 150 hóa đơn chưa thanh toán / hủy (KHÔNG tạo CLASS_STUDENT):
            //    - 80 PENDING (chờ thanh toán cho lớp sắp khai giảng T12 hoặc học viên chờ lớp)
            //    - 40 OVERDUE (quá hạn thanh toán)
            //    - 30 CANCELLED / REFUNDED (18 CANCELLED + 12 REFUNDED)
            var upcomingClasses = classes.Skip(185).Take(15).ToList(); // Id 186..200

            for (int i = 0; i < 80; i++)
            {
                var c = upcomingClasses[i % upcomingClasses.Count];
                var level = levelById[c.CourseLevelId];
                var student = i < 50 ? waitingStudents[i] : activeStudents[i % activeStudents.Count];
                DateTime createdAt = RandomDateBetween(rng, new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc), PeakEnd);

                invoices.Add(new Invoice
                {
                    Id = invoiceId,
                    ParentId = student.ParentId,
                    StudentId = student.Id,
                    ClassId = c.Id,
                    TxnRef = $"TTD2026{invoiceId:D6}",
                    Amount = (int)level.Fee,
                    Status = "PENDING",
                    PaymentMethod = null,
                    TransactionId = null,
                    CheckoutUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?order=TTD2026{invoiceId:D6}",
                    QrCode = $"data:image/png;base64,qr_toantuduy_{invoiceId}",
                    CreatedAt = createdAt,
                    ExpiredAt = RandomDateBetween(rng, FutureStart, new DateTime(2026, 12, 10, 23, 0, 0, DateTimeKind.Utc)),
                    PaidAt = null
                });
                invoiceId++;
            }

            for (int i = 0; i < 40; i++)
            {
                var c = classes[45 + (i % 140)];
                var level = levelById[c.CourseLevelId];
                var student = waitingStudents[i % waitingStudents.Count];
                DateTime createdAt = RandomDateBetween(rng, new DateTime(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 15, 18, 0, 0, DateTimeKind.Utc));

                invoices.Add(new Invoice
                {
                    Id = invoiceId,
                    ParentId = student.ParentId,
                    StudentId = student.Id,
                    ClassId = c.Id,
                    TxnRef = $"TTD2026{invoiceId:D6}",
                    Amount = (int)level.Fee,
                    Status = "OVERDUE",
                    PaymentMethod = null,
                    TransactionId = null,
                    CheckoutUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?order=TTD2026{invoiceId:D6}",
                    QrCode = $"data:image/png;base64,qr_toantuduy_{invoiceId}",
                    CreatedAt = createdAt,
                    ExpiredAt = createdAt.AddDays(5),
                    PaidAt = null
                });
                invoiceId++;
            }

            for (int i = 0; i < 30; i++)
            {
                bool isRefunded = i >= 18; // 18 CANCELLED + 12 REFUNDED
                var c = classes[45 + (i % 140)];
                var level = levelById[c.CourseLevelId];
                var student = alumniStudents[i % alumniStudents.Count];
                DateTime createdAt = WeightedRandomDate(rng);
                if (createdAt > PeakEnd) createdAt = RandomDateBetween(rng, PeakStart, PeakEnd);

                invoices.Add(new Invoice
                {
                    Id = invoiceId,
                    ParentId = student.ParentId,
                    StudentId = student.Id,
                    ClassId = c.Id,
                    TxnRef = $"TTD2026{invoiceId:D6}",
                    Amount = (int)level.Fee,
                    Status = isRefunded ? "REFUNDED" : "CANCELLED",
                    PaymentMethod = isRefunded ? "VNPAY" : null,
                    TransactionId = null,
                    CheckoutUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?order=TTD2026{invoiceId:D6}",
                    QrCode = $"data:image/png;base64,qr_toantuduy_{invoiceId}",
                    CreatedAt = createdAt,
                    ExpiredAt = createdAt.AddMinutes(15),
                    PaidAt = isRefunded ? createdAt.AddMinutes(5) : null
                });
                invoiceId++;
            }

            return (invoices, classStudents, classEnrollments);
        }

        // =========================================================================================
        // NHÓM 2: StudentBadge (650) — giữ 2 danh hiệu cuối (Id 11, 12) chưa ai đạt
        // =========================================================================================

        private List<StudentBadge> GenerateStudentBadges(List<StudentProfile> students, List<Badge> badges, Random rng)
        {
            var result = new List<StudentBadge>(650);
            var unlockedBadges = badges.Take(10).ToList(); // Id 1..10 (giữ 11, 12 khóa)
            var activeStudents = students.Take(200).ToList();
            var alumniStudents = students.Skip(200).Take(250).ToList();
            var usedPairs = new HashSet<(int StudentId, int BadgeId)>();

            int id = 1;

            // 500 lượt cấp trong T8-T11/2026 (chủ yếu cho 200 học sinh active)
            while (result.Count < 500)
            {
                var st = activeStudents[rng.Next(activeStudents.Count)];
                var bg = unlockedBadges[rng.Next(unlockedBadges.Count)];
                if (usedPairs.Add((st.Id, bg.Id)))
                {
                    result.Add(new StudentBadge
                    {
                        Id = id++,
                        StudentId = st.Id,
                        BadgeId = bg.Id,
                        EarnedAt = RandomDateBetween(rng, PeakStart, PeakEnd)
                    });
                }
            }

            // 150 lượt cấp trong T1-T7/2026 cho học viên cũ
            while (result.Count < 650)
            {
                var st = alumniStudents[rng.Next(alumniStudents.Count)];
                var bg = unlockedBadges[rng.Next(unlockedBadges.Count)];
                if (usedPairs.Add((st.Id, bg.Id)))
                {
                    result.Add(new StudentBadge
                    {
                        Id = id++,
                        StudentId = st.Id,
                        BadgeId = bg.Id,
                        EarnedAt = RandomDateBetween(rng, HistoryStart.AddDays(15), HistoryEnd)
                    });
                }
            }

            return result;
        }

        // =========================================================================================
        // NHÓM 3: LESSON (3.200)
        //   - 650 buổi lịch sử T1-T7/2026 (550 COMPLETED + 100 HOLIDAY/CANCELED)
        //   - 2.400 buổi cao điểm T8-T11/2026 (1.950 COMPLETED + 150 HOLIDAY/CANCELED + 300 SCHEDULED cuối T11)
        //   - 150 buổi T12/2026 (150 SCHEDULED cho 15 lớp khai giảng T12)
        //   => Tổng 2.500 buổi COMPLETED (550 T1-T7 + 1.950 T8-T11) x 10 HV = 25.000 ATTENDANCE (78% T8-T11)
        // =========================================================================================

        private List<Lesson> GenerateLessons(List<Classes> classes, List<ClassSchedule> schedules, Random rng)
        {
            var lessons = new List<Lesson>(3200);
            var schedulesByClass = schedules.GroupBy(s => s.ClassId).ToDictionary(g => g.Key, g => g.ToList());

            var topics = new[]
            {
                "Làm quen bàn tính & cấu trúc số tròn chục",
                "Quy tắc cộng trừ trực tiếp không nhớ",
                "Công thức Anh bạn nhỏ cộng (+1..+4)",
                "Công thức Anh bạn nhỏ trừ (-1..-4)",
                "Công thức Anh bạn lớn cộng qua cầu 10",
                "Công thức Anh bạn lớn trừ qua cầu 10",
                "Luyện phản xạ Finger Math phạm vi 0-99",
                "Ảo tính Anzan: Hình dung hạt tính trong đầu",
                "Bài toán suy luận sơ đồ khối Bar Model",
                "Tìm quy luật dãy số & tư duy không gian",
                "Phép nhân 2 chữ số trên bàn tính Soroban",
                "Luyện tốc độ chuỗi phép tính nhiều số hạng",
                "Tổng ôn chuyên đề giữa khóa",
                "Kiểm tra đánh giá năng lực định kỳ",
                "Chữa bài tập nâng cao & thi đấu đồng đội",
                "Tổng kết cấp độ & trao huy hiệu thành tích"
            };

            int id = 1;

            // 1) 45 lớp lịch sử (Id 1..45) => 650 buổi trong T1-T7/2026:
            //    550 COMPLETED (20 lớp đầu x 13 + 25 lớp sau x 12? Không: 10 lớp x 13 + 35 lớp x 12 = 550)
            //    + 100 HOLIDAY/CANCELED (10 lớp x 3 + 35 lớp x 2 = 100) => tổng 650 buổi
            for (int i = 0; i < 45; i++)
            {
                var c = classes[i];
                var classScheds = schedulesByClass[c.Id];
                int completedCount = i < 10 ? 13 : 12;
                int skippedCount = i < 10 ? 3 : 2;
                int totalForClass = completedCount + skippedCount;

                var dates = GenerateDatesFromSchedule(c.StartDate, classScheds, totalForClass, new DateTime(2026, 7, 30));
                for (int s = 0; s < totalForClass; s++)
                {
                    var (date, startSpan, endSpan) = dates[s];
                    string status = s < completedCount ? "COMPLETED" : (s % 2 == 0 ? "HOLIDAY" : "CANCELED");
                    lessons.Add(new Lesson
                    {
                        Id = id++,
                        ClassId = c.Id,
                        Title = $"Buổi {s + 1}: {topics[s % topics.Length]}",
                        LessonDate = date,
                        StartTime = date.Add(startSpan),
                        EndTime = date.Add(endSpan),
                        Status = status,
                        IsImportan = ((s + 1) % 4 == 0)
                    });
                }
            }

            // 2) 60 lớp ngắn hạn vừa kết thúc T8-T11/2026 (Id 46..105):
            //    Mỗi lớp 18 buổi COMPLETED = 1.080 COMPLETED + 120 HOLIDAY/CANCELED (2 buổi/lớp) = 1.200 buổi
            for (int i = 45; i < 105; i++)
            {
                var c = classes[i];
                var classScheds = schedulesByClass[c.Id];
                int completedCount = 18;
                int skippedCount = 2;
                int totalForClass = completedCount + skippedCount;

                var dates = GenerateDatesFromSchedule(c.StartDate, classScheds, totalForClass, new DateTime(2026, 11, 24));
                for (int s = 0; s < totalForClass; s++)
                {
                    var (date, startSpan, endSpan) = dates[s];
                    string status = s < completedCount ? "COMPLETED" : (s % 2 == 0 ? "HOLIDAY" : "CANCELED");
                    lessons.Add(new Lesson
                    {
                        Id = id++,
                        ClassId = c.Id,
                        Title = $"Buổi {s + 1}: {topics[s % topics.Length]}",
                        LessonDate = date,
                        StartTime = date.Add(startSpan),
                        EndTime = date.Add(endSpan),
                        Status = status,
                        IsImportan = ((s + 1) % 4 == 0)
                    });
                }
            }

            // 3) 80 lớp đang học T8-T11/2026 (Id 106..185) => 1.200 buổi trong T8-T11/2026:
            //    - 870 COMPLETED (70 lớp x 11 + 10 lớp x 10 = 870) trong T8-26/11/2026
            //      => Tổng COMPLETED T8-T11 = 1.080 + 870 = 1.950 buổi (x 10 HV = 19.500 ATTENDANCE = 78%!)
            //    - 30 HOLIDAY/CANCELED (30 lớp đầu x 1 = 30)
            //    - 300 SCHEDULED cuối T11 (27/11 - 30/11/2026: 60 lớp x 4 + 20 lớp x 3 = 300)
            //    => Tổng T8-T11 = 1.200 + 870 + 30 + 300 = 2.400 buổi (đúng 75% của 3.200!)
            for (int i = 105; i < 185; i++)
            {
                int rel = i - 105;
                var c = classes[i];
                var classScheds = schedulesByClass[c.Id];

                int completedCount = rel < 70 ? 11 : 10;
                int skippedCount = rel < 30 ? 1 : 0;
                int scheduledCount = rel < 60 ? 4 : 3;

                var pastDates = GenerateDatesFromSchedule(c.StartDate, classScheds, completedCount + skippedCount, new DateTime(2026, 11, 26));
                int sessionNum = 1;

                for (int s = 0; s < completedCount + skippedCount; s++)
                {
                    var (date, startSpan, endSpan) = pastDates[s];
                    string status = s < completedCount ? "COMPLETED" : "HOLIDAY";
                    lessons.Add(new Lesson
                    {
                        Id = id++,
                        ClassId = c.Id,
                        Title = $"Buổi {sessionNum}: {topics[(sessionNum - 1) % topics.Length]}",
                        LessonDate = date,
                        StartTime = date.Add(startSpan),
                        EndTime = date.Add(endSpan),
                        Status = status,
                        IsImportan = (sessionNum % 4 == 0)
                    });
                    sessionNum++;
                }

                // Các buổi cuối T11 sắp diễn ra (27/11 .. 30/11/2026, mỗi buổi 1 ngày khác nhau)
                var baseSlot = classScheds[0];
                for (int k = 0; k < scheduledCount; k++)
                {
                    var date = new DateTime(2026, 11, 27 + k, 0, 0, 0, DateTimeKind.Utc);
                    lessons.Add(new Lesson
                    {
                        Id = id++,
                        ClassId = c.Id,
                        Title = $"Buổi {sessionNum}: {topics[(sessionNum - 1) % topics.Length]}",
                        LessonDate = date,
                        StartTime = date.Add(baseSlot.StartTime),
                        EndTime = date.Add(baseSlot.EndTime),
                        Status = "SCHEDULED",
                        IsImportan = (k == scheduledCount - 1)
                    });
                    sessionNum++;
                }
            }

            // 4) 15 lớp sắp khai giảng T12/2026 (Id 186..200) => 15 x 10 = 150 buổi SCHEDULED trong T12/2026
            for (int i = 185; i < 200; i++)
            {
                var c = classes[i];
                var classScheds = schedulesByClass[c.Id];
                var decDates = GenerateDatesFromSchedule(c.StartDate, classScheds, 10, new DateTime(2026, 12, 31));

                for (int s = 0; s < 10; s++)
                {
                    var (date, startSpan, endSpan) = decDates[s];
                    lessons.Add(new Lesson
                    {
                        Id = id++,
                        ClassId = c.Id,
                        Title = $"Buổi {s + 1}: {topics[s % topics.Length]}",
                        LessonDate = date,
                        StartTime = date.Add(startSpan),
                        EndTime = date.Add(endSpan),
                        Status = "SCHEDULED",
                        IsImportan = ((s + 1) % 5 == 0)
                    });
                }
            }

            return lessons;
        }

        private static List<(DateTime Date, TimeSpan Start, TimeSpan End)> GenerateDatesFromSchedule(
            DateTime startDate,
            List<ClassSchedule> classSchedules,
            int count,
            DateTime maxDate)
        {
            var result = new List<(DateTime, TimeSpan, TimeSpan)>(count);
            var scheduleMap = classSchedules.ToDictionary(s => s.DayOfWeek);

            // Nếu khoảng [startDate, maxDate] quá ngắn để khớp ngày theo lịch tuần, lùi startDate nhẹ hoặc trải đều
            var cursor = startDate.Date;
            int safety = 0;
            while (result.Count < count && cursor <= maxDate.Date && safety < 400)
            {
                int dow = (int)cursor.DayOfWeek;
                if (scheduleMap.TryGetValue(dow, out var sched))
                {
                    result.Add((cursor, sched.StartTime, sched.EndTime));
                }
                cursor = cursor.AddDays(1);
                safety++;
            }

            // Nếu chưa đủ buổi trước maxDate, bổ sung các ngày xen kẽ trong khoảng [startDate, maxDate]
            if (result.Count < count)
            {
                var usedDates = result.Select(r => r.Item1).ToHashSet();
                var fallbackSlot = classSchedules[0];
                var fillDate = startDate.Date;
                while (result.Count < count && fillDate <= maxDate.Date)
                {
                    if (usedDates.Add(fillDate))
                    {
                        result.Add((fillDate, fallbackSlot.StartTime, fallbackSlot.EndTime));
                    }
                    fillDate = fillDate.AddDays(1);
                }

                // Trường hợp hi hữu: lùi ngược từ startDate nếu khoảng thời gian cực ngắn
                fillDate = startDate.Date.AddDays(-1);
                while (result.Count < count)
                {
                    if (usedDates.Add(fillDate))
                    {
                        result.Add((fillDate, fallbackSlot.StartTime, fallbackSlot.EndTime));
                    }
                    fillDate = fillDate.AddDays(-1);
                }

                result.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            }

            return result;
        }

        // =========================================================================================
        // NHÓM 4: ATTENDANCE (25.000) & LEAVE_REQUEST (450)
        // =========================================================================================

        private List<Attendance> GenerateAttendances(
            List<Lesson> lessons,
            Dictionary<int, List<int>> classEnrollments,
            Random rng)
        {
            var attendances = new List<Attendance>(25000);
            var completedLessons = lessons.Where(l => l.Status == "COMPLETED").ToList(); // Đúng 2.500 buổi

            // Phân bổ tỷ lệ chính xác: 88% PRESENT (22.000), 5% LATE (1.250), 5% EXCUSED (1.250), 2% ABSENT (500)
            var statusPool = new List<string>(25000);
            statusPool.AddRange(Enumerable.Repeat("PRESENT", 22000));
            statusPool.AddRange(Enumerable.Repeat("LATE", 1250));
            statusPool.AddRange(Enumerable.Repeat("EXCUSED", 1250));
            statusPool.AddRange(Enumerable.Repeat("ABSENT", 500));
            Shuffle(statusPool, rng);

            var presentNotes = new[]
            {
                "Bé tập trung tốt, thao tác tính nhẩm nhanh và chính xác.",
                "Hoàn thành xuất sắc bài thực hành tại lớp.",
                "Tích cực phát biểu xây dựng bài, tiến bộ rõ rệt.",
                "Hợp tác tốt với bạn cùng nhóm, nắm chắc công thức."
            };
            var lateNotes = new[]
            {
                "Vào lớp muộn 10 phút do tắc đường.",
                "Đến trễ 15 phút, đã theo kịp phần luyện tập.",
                "Phụ huynh đưa bé tới muộn 5 phút đầu giờ."
            };
            var excusedNotes = new[]
            {
                "Nghỉ có phép từ phụ huynh (bé bị sốt nhẹ).",
                "Nghỉ có phép, đã đăng ký lịch học bù cuối tuần.",
                "Gia đình xin phép nghỉ do trùng lịch thi ở trường."
            };
            var absentNotes = new[]
            {
                "Vắng không phép, giáo vụ đã liên hệ phụ huynh.",
                "Không có mặt tại lớp và chưa gửi đơn xin nghỉ."
            };

            int id = 1;
            int poolIdx = 0;

            foreach (var lesson in completedLessons)
            {
                var studentIds = classEnrollments[lesson.ClassId]; // Đúng 10 học sinh
                foreach (var studentId in studentIds)
                {
                    string status = statusPool[poolIdx++];
                    string note = status switch
                    {
                        "PRESENT" => presentNotes[id % presentNotes.Length],
                        "LATE" => lateNotes[id % lateNotes.Length],
                        "EXCUSED" => excusedNotes[id % excusedNotes.Length],
                        _ => absentNotes[id % absentNotes.Length]
                    };

                    attendances.Add(new Attendance
                    {
                        Id = id++,
                        LessonId = lesson.Id,
                        StudentId = studentId,
                        Status = status,
                        TeacherNotes = note
                    });
                }
            }

            return attendances;
        }

        private List<LeaveRequest> GenerateLeaveRequests(
            List<Lesson> lessons,
            List<Attendance> attendances,
            Dictionary<int, List<int>> classEnrollments,
            Random rng)
        {
            var requests = new List<LeaveRequest>(450);
            var lessonById = lessons.ToDictionary(l => l.Id);

            var reasons = new[]
            {
                "Bé bị sốt nhẹ, gia đình xin phép cho bé nghỉ dưỡng bệnh 1 buổi.",
                "Gia đình có việc bận đột xuất, xin nghỉ và đăng ký học bù tuần sau.",
                "Trùng lịch kiểm tra học kỳ tại trường tiểu học của bé.",
                "Bé đi khám răng định kỳ cùng phụ huynh, xin phép vắng mặt.",
                "Gia đình đi công tác cuối tuần, xin nghỉ phép có bài tập về nhà."
            };

            // 1) 380 APPROVED: gắn khớp với các bản ghi ATTENDANCE = EXCUSED (300 trong T8-T11, 80 trong T1-T7)
            var excusedPeak = attendances
                .Where(a => a.Status == "EXCUSED" && lessonById[a.LessonId].LessonDate >= PeakStart)
                .ToList();
            var excusedHistory = attendances
                .Where(a => a.Status == "EXCUSED" && lessonById[a.LessonId].LessonDate < PeakStart)
                .ToList();
            Shuffle(excusedPeak, rng);
            Shuffle(excusedHistory, rng);

            int id = 1;
            foreach (var att in excusedPeak.Take(300))
            {
                var lesson = lessonById[att.LessonId];
                requests.Add(new LeaveRequest
                {
                    Id = id,
                    StudentId = att.StudentId,
                    LessonId = att.LessonId,
                    Reason = reasons[id % reasons.Length],
                    Status = "APPROVED",
                    CreatedAt = lesson.LessonDate.AddDays(-rng.Next(1, 3)).AddHours(rng.Next(8, 20))
                });
                id++;
            }

            foreach (var att in excusedHistory.Take(80))
            {
                var lesson = lessonById[att.LessonId];
                requests.Add(new LeaveRequest
                {
                    Id = id,
                    StudentId = att.StudentId,
                    LessonId = att.LessonId,
                    Reason = reasons[id % reasons.Length],
                    Status = "APPROVED",
                    CreatedAt = lesson.LessonDate.AddDays(-rng.Next(1, 3)).AddHours(rng.Next(8, 20))
                });
                id++;
            }

            // 2) 30 PENDING: đơn xin nghỉ cho các buổi SCHEDULED cuối T11 (chờ giáo vụ/giáo viên duyệt)
            var upcomingNovLessons = lessons
                .Where(l => l.Status == "SCHEDULED" && l.LessonDate >= new DateTime(2026, 11, 27) && l.LessonDate <= new DateTime(2026, 11, 30) && classEnrollments.ContainsKey(l.ClassId))
                .ToList();
            Shuffle(upcomingNovLessons, rng);

            for (int i = 0; i < 30; i++)
            {
                var lesson = upcomingNovLessons[i % upcomingNovLessons.Count];
                var studentIds = classEnrollments[lesson.ClassId];
                int studentId = studentIds[i % studentIds.Count];

                requests.Add(new LeaveRequest
                {
                    Id = id,
                    StudentId = studentId,
                    LessonId = lesson.Id,
                    Reason = reasons[id % reasons.Length],
                    Status = "PENDING",
                    CreatedAt = RandomDateBetween(rng, new DateTime(2026, 11, 25, 8, 0, 0, DateTimeKind.Utc), PeakEnd)
                });
                id++;
            }

            // 3) 40 REJECTED: 20 trong T8-T11 + 20 trong T1-T7 (lấy từ bản ghi PRESENT/ABSENT)
            var nonExcusedPeak = attendances
                .Where(a => a.Status is "PRESENT" or "ABSENT" && lessonById[a.LessonId].LessonDate >= PeakStart)
                .ToList();
            var nonExcusedHistory = attendances
                .Where(a => a.Status is "PRESENT" or "ABSENT" && lessonById[a.LessonId].LessonDate < PeakStart)
                .ToList();
            Shuffle(nonExcusedPeak, rng);
            Shuffle(nonExcusedHistory, rng);

            foreach (var att in nonExcusedPeak.Take(20).Concat(nonExcusedHistory.Take(20)))
            {
                var lesson = lessonById[att.LessonId];
                requests.Add(new LeaveRequest
                {
                    Id = id,
                    StudentId = att.StudentId,
                    LessonId = att.LessonId,
                    Reason = "Gửi đơn sát giờ học (< 30 phút) hoặc buổi kiểm tra bắt buộc.",
                    Status = "REJECTED",
                    CreatedAt = lesson.LessonDate.AddHours(7)
                });
                id++;
            }

            return requests;
        }

        // =========================================================================================
        // NHÓM 4: QUESTION_BANK (15), QUESTION (600), ASSIGNMENT (800), QA (8.000), SA (7.500)
        // =========================================================================================

        private List<QuestionBank> GenerateQuestionBanks(List<Course> courses)
        {
            var tiers = new[] { "Cơ bản", "Trung bình", "Nâng cao" };
            var banks = new List<QuestionBank>(15);
            int id = 1;

            foreach (var course in courses)
            {
                foreach (var tier in tiers)
                {
                    banks.Add(new QuestionBank
                    {
                        Id = id++,
                        CourseId = course.Id,
                        Name = $"Ngân hàng {course.Code} - {tier}",
                        Description = $"Bộ 40 câu hỏi mức độ {tier.ToLowerInvariant()} cho chương trình {course.Name} (trắc nghiệm, điền đáp số và chuỗi phép tính)."
                    });
                }
            }

            return banks;
        }

        private List<Question> GenerateQuestions(List<QuestionBank> banks, Random rng)
        {
            var questions = new List<Question>(600);
            int id = 1;

            foreach (var bank in banks)
            {
                int tierIndex = (bank.Id - 1) % 3; // 0: EASY, 1: MEDIUM, 2: HARD
                string defaultDiff = tierIndex switch { 0 => "EASY", 1 => "MEDIUM", _ => "HARD" };

                // 40 câu/ngân hàng: 16 MULTIPLE_CHOICE, 14 FILL_IN, 10 TIMED_SEQUENCE
                for (int q = 1; q <= 40; q++)
                {
                    string difficulty = (q % 5 == 0 && tierIndex < 2)
                        ? (tierIndex == 0 ? "MEDIUM" : "HARD")
                        : defaultDiff;

                    if (q <= 16)
                    {
                        // MULTIPLE_CHOICE chuẩn parseMultipleChoice (A., B., C., D.)
                        int maxVal = tierIndex == 0 ? 15 : (tierIndex == 1 ? 45 : 95);
                        int a = rng.Next(4, maxVal);
                        int b = rng.Next(3, maxVal);
                        bool isAdd = (q % 2 == 0) || a <= b;
                        int correct = isAdd ? a + b : a - b;
                        string op = isAdd ? "+" : "-";

                        var optionVals = new[] { correct, correct + 2, Math.Max(0, correct - 1), correct + 5 };
                        int correctPos = (id - 1) % 4;
                        if (correctPos != 0)
                        {
                            (optionVals[0], optionVals[correctPos]) = (optionVals[correctPos], optionVals[0]);
                        }
                        char correctLetter = (char)('A' + correctPos);

                        questions.Add(new Question
                        {
                            Id = id,
                            BankId = bank.Id,
                            Type = "MULTIPLE_CHOICE",
                            Content = $"[{bank.Name} - Câu {q}] Thực hiện phép tính tư duy: {a} {op} {b} = ?\n" +
                                      $"A. {optionVals[0]}\n" +
                                      $"B. {optionVals[1]}\n" +
                                      $"C. {optionVals[2]}\n" +
                                      $"D. {optionVals[3]}",
                            ImageUrl = q % 4 == 0 ? "https://images.unsplash.com/photo-1596495578065-6e0763fa1178?w=600" : null,
                            Difficulty = difficulty,
                            Answer = $"{correctLetter}. {correct}\n- Lời giải: Kết quả của phép tính {a} {op} {b} là {correct}."
                        });
                    }
                    else if (q <= 30)
                    {
                        // FILL_IN (điền đáp số)
                        int a = rng.Next(12, tierIndex == 0 ? 35 : (tierIndex == 1 ? 80 : 150));
                        int b = rng.Next(5, tierIndex == 0 ? 25 : (tierIndex == 1 ? 50 : 90));
                        int c = rng.Next(2, 20);
                        int ans = a + b - c;

                        questions.Add(new Question
                        {
                            Id = id,
                            BankId = bank.Id,
                            Type = "FILL_IN",
                            Content = $"[{bank.Name} - Câu {q}] Điền đáp số chính xác cho biểu thức: {a} + {b} - {c} = ?",
                            ImageUrl = null,
                            Difficulty = difficulty,
                            Answer = $"{ans}\n- Hướng dẫn giải: Tính lần lượt {a} + {b} = {a + b}, sau đó trừ {c} được {ans}."
                        });
                    }
                    else
                    {
                        // TIMED_SEQUENCE chuẩn AssignmentService.ParseTimedSequence ("Tính nhẩm theo nhịp: n1, +n2, -n3, ...")
                        int termCount = tierIndex == 0 ? 3 : (tierIndex == 1 ? 4 : 5);
                        int running = rng.Next(8, tierIndex == 0 ? 25 : 60);
                        var parts = new List<string> { running.ToString(CultureInfo.InvariantCulture) };

                        for (int t = 1; t < termCount; t++)
                        {
                            int delta = rng.Next(2, tierIndex == 0 ? 12 : 25);
                            if (running > delta + 2 && rng.Next(2) == 0)
                            {
                                running -= delta;
                                parts.Add($"-{delta}");
                            }
                            else
                            {
                                running += delta;
                                parts.Add($"+{delta}");
                            }
                        }

                        string seqText = string.Join(", ", parts);
                        questions.Add(new Question
                        {
                            Id = id,
                            BankId = bank.Id,
                            Type = "TIMED_SEQUENCE",
                            Content = $"Tính nhẩm theo nhịp: {seqText}.",
                            ImageUrl = null,
                            Difficulty = difficulty,
                            Answer = $"{running}\n- Hướng dẫn giải: Cộng trừ liên tiếp chuỗi {seqText} cho kết quả bằng {running}."
                        });
                    }

                    id++;
                }
            }

            return questions;
        }

        private List<Assignment> GenerateAssignments(List<Lesson> lessons, Random rng)
        {
            var assignments = new List<Assignment>(800);

            // 1) 200 bài tập lịch sử T1-T7/2026 (đều đã phát hành, CLOSED)
            var histCompleted = lessons
                .Where(l => l.Status == "COMPLETED" && l.LessonDate < PeakStart)
                .ToList();
            Shuffle(histCompleted, rng);

            int id = 1;
            for (int i = 0; i < 200; i++)
            {
                var lesson = histCompleted[i % histCompleted.Count];
                DateTime start = lesson.LessonDate.AddHours(17);
                DateTime deadline = start.AddDays(5);

                assignments.Add(new Assignment
                {
                    Id = id,
                    LessonId = lesson.Id,
                    StageId = $"STAGE-H{(i % 8) + 1:D2}",
                    Title = $"Bài tập về nhà #{id:D3} - {lesson.Title}",
                    StartTime = start,
                    Deadline = deadline,
                    TimeLimitMinutes = rng.Next(1, 4) * 15,
                    MaxAttempt = 1,
                    Status = "CLOSED"
                });
                id++;
            }

            // 2) 550 bài tập đã phát hành trong T8-T11/2026:
            //    - 470 bài trên buổi COMPLETED trước 22/11 (Deadline đã qua hoặc vừa hết)
            //    - 80 bài trên buổi gần nhất 22/11-26/11 (Deadline >= 29/11 để chứa 500 SA đang làm ASSIGNED)
            var peakCompletedEarlier = lessons
                .Where(l => l.Status == "COMPLETED" && l.LessonDate >= PeakStart && l.LessonDate < new DateTime(2026, 11, 20))
                .ToList();
            var peakCompletedRecent = lessons
                .Where(l => l.Status == "COMPLETED" && l.LessonDate >= new DateTime(2026, 11, 20) && l.LessonDate <= new DateTime(2026, 11, 26))
                .ToList();
            Shuffle(peakCompletedEarlier, rng);
            Shuffle(peakCompletedRecent, rng);

            for (int i = 0; i < 470; i++)
            {
                var lesson = peakCompletedEarlier[i % peakCompletedEarlier.Count];
                DateTime start = lesson.LessonDate.AddHours(18);
                DateTime deadline = start.AddDays(4);
                string status = deadline < AnchorUtc ? "CLOSED" : "OPEN";

                assignments.Add(new Assignment
                {
                    Id = id,
                    LessonId = lesson.Id,
                    StageId = $"STAGE-P{(i % 12) + 1:D2}",
                    Title = $"Bài tập rèn luyện #{id:D3} - {lesson.Title}",
                    StartTime = start,
                    Deadline = deadline,
                    TimeLimitMinutes = rng.Next(1, 4) * 15,
                    MaxAttempt = 1,
                    Status = status
                });
                id++;
            }

            for (int i = 0; i < 80; i++)
            {
                var lesson = peakCompletedRecent[i % peakCompletedRecent.Count];
                DateTime start = lesson.LessonDate.AddHours(18);
                DateTime deadline = new DateTime(2026, 12, 1, 23, 59, 0, DateTimeKind.Utc).AddDays(i % 5);

                assignments.Add(new Assignment
                {
                    Id = id,
                    LessonId = lesson.Id,
                    StageId = $"STAGE-A{(i % 10) + 1:D2}",
                    Title = $"Bài tập tuần cuối T11 #{id:D3} - {lesson.Title}",
                    StartTime = start,
                    Deadline = deadline,
                    TimeLimitMinutes = 20,
                    MaxAttempt = 1,
                    Status = "OPEN"
                });
                id++;
            }

            // 3) 50 bài tập chưa phát hành / chuẩn bị cho buổi SCHEDULED cuối T11 (27/11 - 30/11)
            //    => 750 bài đã phát hành (Id 1..750) x 10 HV = 7.500 STUDENT_ASSIGNMENT!
            var lateNovScheduled = lessons
                .Where(l => l.Status == "SCHEDULED" && l.LessonDate >= new DateTime(2026, 11, 27) && l.LessonDate <= new DateTime(2026, 11, 30))
                .ToList();
            Shuffle(lateNovScheduled, rng);

            for (int i = 0; i < 50; i++)
            {
                var lesson = lateNovScheduled[i % lateNovScheduled.Count];
                DateTime start = lesson.LessonDate.AddHours(19);
                DateTime deadline = start.AddDays(5);

                assignments.Add(new Assignment
                {
                    Id = id,
                    LessonId = lesson.Id,
                    StageId = $"STAGE-D{(i % 6) + 1:D2}",
                    Title = $"Bài tập dự kiến #{id:D3} - {lesson.Title}",
                    StartTime = start,
                    Deadline = deadline,
                    TimeLimitMinutes = 30,
                    MaxAttempt = 1,
                    Status = "DRAFT"
                });
                id++;
            }

            return assignments;
        }

        private List<QuestionAssignment> GenerateQuestionAssignments(
            List<Assignment> assignments,
            List<Lesson> lessons,
            List<Classes> classes,
            List<CourseLevel> courseLevels,
            List<QuestionBank> questionBanks,
            List<Question> questions,
            Random rng)
        {
            var result = new List<QuestionAssignment>(8000);

            var lessonById = lessons.ToDictionary(l => l.Id);
            var classById = classes.ToDictionary(c => c.Id);
            var levelById = courseLevels.ToDictionary(cl => cl.Id);
            var bankToCourse = questionBanks.ToDictionary(b => b.Id, b => b.CourseId);
            var questionsByCourse = questions
                .GroupBy(q => bankToCourse[q.BankId])
                .ToDictionary(g => g.Key, g => g.ToList()); // 120 câu/khóa

            int id = 1;
            foreach (var asm in assignments)
            {
                var lesson = lessonById[asm.LessonId];
                var cls = classById[lesson.ClassId];
                var level = levelById[cls.CourseLevelId];
                var coursePool = questionsByCourse[level.CourseId];

                // Chọn 10 câu hỏi khác nhau thuộc đúng khóa học (tái sử dụng chéo giữa các lớp cùng cấp độ)
                int offset = ((asm.Id - 1) * 7 + rng.Next(0, 3)) % coursePool.Count;
                for (int order = 1; order <= 10; order++)
                {
                    var q = coursePool[(offset + order - 1) % coursePool.Count];
                    result.Add(new QuestionAssignment
                    {
                        Id = id++,
                        AssignmentId = asm.Id,
                        QuestionId = q.Id,
                        OrderIndex = order,
                        PointValue = 10.0f // 10 câu x 10đ = 100đ
                    });
                }
            }

            return result;
        }

        private List<StudentAssignment> GenerateStudentAssignments(
            List<Assignment> assignments,
            List<Lesson> lessons,
            Dictionary<int, List<int>> classEnrollments,
            Random rng)
        {
            // 750 bài tập đã phát hành (Id 1..750) x 10 HV/lớp = 7.500 dòng
            // Phân bổ chính xác: 5.800 GRADED, 900 SUBMITTED, 500 ASSIGNED (chưa làm), 300 OVERDUE (quá hạn)
            var result = new List<StudentAssignment>(7500);
            var lessonById = lessons.ToDictionary(l => l.Id);

            // Bài Id 1..670 (670 bài, hạn đã qua => 6.700 lượt):
            //   - 5.800 GRADED
            //   - 600 SUBMITTED (mới nộp gần đây chờ chấm)
            //   - 300 OVERDUE (quá hạn chưa nộp)
            // Bài Id 671..750 (80 bài tuần cuối T11, còn hạn => 800 lượt):
            //   - 300 SUBMITTED (đã nộp sớm chờ chấm)
            //   - 500 ASSIGNED (chưa làm, còn hạn)
            // => Tổng: 5.800 GRADED + (600+300)=900 SUBMITTED + 500 ASSIGNED + 300 OVERDUE = 7.500!

            var closedStatuses = new List<string>(6700);
            closedStatuses.AddRange(Enumerable.Repeat("GRADED", 5800));
            closedStatuses.AddRange(Enumerable.Repeat("SUBMITTED", 600));
            closedStatuses.AddRange(Enumerable.Repeat("OVERDUE", 300));
            Shuffle(closedStatuses, rng);

            var openStatuses = new List<string>(800);
            openStatuses.AddRange(Enumerable.Repeat("SUBMITTED", 300));
            openStatuses.AddRange(Enumerable.Repeat("ASSIGNED", 500));
            Shuffle(openStatuses, rng);

            var highFeedback = new[]
            {
                "Xuất sắc! Con tính nhẩm rất nhanh và trình bày rõ ràng.",
                "Rất tốt! Nắm chắc quy tắc bàn tính và không sai câu nào ở phần chuỗi nhịp.",
                "Phong độ ổn định, tiếp tục phát huy nhé con!"
            };
            var midFeedback = new[]
            {
                "Bài làm khá tốt, con chú ý cẩn thận hơn ở phép trừ có nhớ nhé.",
                "Tiến bộ so với tuần trước, cần luyện thêm tốc độ ảo tính Anzan."
            };

            int id = 1;
            int closedIdx = 0;
            int openIdx = 0;

            for (int aIdx = 0; aIdx < 750; aIdx++)
            {
                var asm = assignments[aIdx];
                var lesson = lessonById[asm.LessonId];
                var studentIds = classEnrollments[lesson.ClassId]; // 10 học sinh
                bool isOpenRecent = aIdx >= 670;

                foreach (var studentId in studentIds)
                {
                    string status = isOpenRecent ? openStatuses[openIdx++] : closedStatuses[closedIdx++];
                    float? score = null;
                    string? feedback = null;
                    DateTime? completedAt = null;

                    if (status == "GRADED")
                    {
                        int pts = rng.Next(12, 21) * 5; // 60, 65, ..., 100
                        score = pts;
                        feedback = pts >= 85
                            ? highFeedback[id % highFeedback.Length]
                            : midFeedback[id % midFeedback.Length];
                        DateTime maxSubmit = asm.Deadline < AnchorUtc ? asm.Deadline : AnchorUtc.AddHours(-1);
                        if (maxSubmit <= asm.StartTime) maxSubmit = asm.StartTime.AddHours(6);
                        completedAt = RandomDateBetween(rng, asm.StartTime, maxSubmit);
                    }
                    else if (status == "SUBMITTED")
                    {
                        DateTime maxSubmit = asm.Deadline < AnchorUtc ? asm.Deadline : AnchorUtc.AddMinutes(-15);
                        if (maxSubmit <= asm.StartTime) maxSubmit = asm.StartTime.AddHours(3);
                        completedAt = RandomDateBetween(rng, asm.StartTime, maxSubmit);
                    }

                    result.Add(new StudentAssignment
                    {
                        Id = id++,
                        AssignmentId = asm.Id,
                        StudentId = studentId,
                        Score = score,
                        Status = status,
                        TeacherFeedback = feedback,
                        CompletedAt = completedAt
                    });
                }
            }

            return result;
        }

        // =========================================================================================
        // NHÓM 5: TEACHER_SALARY_CONFIG (36), SALARY_ADJUSTMENT (180), PAYROLL_SUMMARY (216)
        // =========================================================================================

        private List<TeacherSalaryConfig> GenerateTeacherSalaryConfigs(List<TeacherProfile> activeTeachers, Random rng)
        {
            var configs = new List<TeacherSalaryConfig>(36);
            for (int i = 0; i < activeTeachers.Count; i++)
            {
                configs.Add(new TeacherSalaryConfig
                {
                    Id = i + 1,
                    TeacherId = activeTeachers[i].Id,
                    BaseRatePerSession = rng.Next(22, 43) * 10000m // 220k - 420k/buổi
                });
            }
            return configs;
        }

        private List<SalaryAdjustment> GenerateSalaryAdjustments(List<TeacherProfile> activeTeachers, Random rng)
        {
            var adjustments = new List<SalaryAdjustment>(180);

            var bonusSpecs = new[]
            {
                (300000m, "Thưởng chuyên cần giảng dạy đầy đủ không vắng buổi"),
                (400000m, "Phụ cấp dạy thay lớp tăng cường"),
                (500000m, "Thưởng duy trì sĩ số lớp đạt 100%"),
                (350000m, "Thưởng học viên đạt điểm cao bài kiểm tra định kỳ")
            };
            var penaltySpecs = new[]
            {
                (100000m, "Khấu trừ đi muộn buổi học không báo trước"),
                (150000m, "Khấu trừ chậm hoàn tất điểm danh quá 24 giờ")
            };

            int id = 1;

            // 1) 140 khoản thưởng/phạt rải đều trong 4 kỳ lương T8, T9, T10, T11/2026 (35 khoản/tháng)
            for (int month = 8; month <= 11; month++)
            {
                int maxDay = month == 11 ? 26 : 28;
                for (int k = 0; k < 35; k++)
                {
                    var teacher = activeTeachers[(month * 7 + k) % activeTeachers.Count];
                    bool isBonus = (k % 4 != 3); // 75% Bonus, 25% Penalty
                    var appliedDate = new DateOnly(2026, month, rng.Next(3, maxDay + 1));

                    if (isBonus)
                    {
                        var spec = bonusSpecs[id % bonusSpecs.Length];
                        adjustments.Add(new SalaryAdjustment
                        {
                            Id = id++,
                            TeacherId = teacher.Id,
                            Type = "Bonus",
                            Amount = spec.Item1,
                            Reason = $"{spec.Item2} (Tháng {month:D2}/2026)",
                            AppliedDate = appliedDate
                        });
                    }
                    else
                    {
                        var spec = penaltySpecs[id % penaltySpecs.Length];
                        adjustments.Add(new SalaryAdjustment
                        {
                            Id = id++,
                            TeacherId = teacher.Id,
                            Type = "Penalty",
                            Amount = spec.Item1,
                            Reason = $"{spec.Item2} (Tháng {month:D2}/2026)",
                            AppliedDate = appliedDate
                        });
                    }
                }
            }

            // 2) 40 khoản thưởng/phạt trong T6 & T7/2026 (20 khoản/tháng)
            for (int month = 6; month <= 7; month++)
            {
                for (int k = 0; k < 20; k++)
                {
                    var teacher = activeTeachers[(month * 5 + k) % activeTeachers.Count];
                    bool isBonus = (k % 4 != 3);
                    var appliedDate = new DateOnly(2026, month, rng.Next(3, 28));

                    if (isBonus)
                    {
                        var spec = bonusSpecs[id % bonusSpecs.Length];
                        adjustments.Add(new SalaryAdjustment
                        {
                            Id = id++,
                            TeacherId = teacher.Id,
                            Type = "Bonus",
                            Amount = spec.Item1,
                            Reason = $"{spec.Item2} (Tháng {month:D2}/2026)",
                            AppliedDate = appliedDate
                        });
                    }
                    else
                    {
                        var spec = penaltySpecs[id % penaltySpecs.Length];
                        adjustments.Add(new SalaryAdjustment
                        {
                            Id = id++,
                            TeacherId = teacher.Id,
                            Type = "Penalty",
                            Amount = spec.Item1,
                            Reason = $"{spec.Item2} (Tháng {month:D2}/2026)",
                            AppliedDate = appliedDate
                        });
                    }
                }
            }

            return adjustments;
        }

        private List<PayrollSummary> GeneratePayrollSummaries(
            List<TeacherProfile> activeTeachers,
            List<TeacherSalaryConfig> configs,
            List<Classes> classes,
            List<Lesson> lessons,
            List<SalaryAdjustment> adjustments)
        {
            // Tính chính xác theo logic PayrollService.GetMonthlyPayrollsAsync cho 6 tháng T6-T11/2026 x 36 GV = 216 dòng
            var summaries = new List<PayrollSummary>(216);
            var rateByTeacher = configs.ToDictionary(c => c.TeacherId, c => c.BaseRatePerSession);
            var classById = classes.ToDictionary(c => c.Id);

            // Đếm số buổi COMPLETED (có điểm danh) theo (TeacherId, Month)
            var sessionCounts = lessons
                .Where(l => l.Status == "COMPLETED" && l.LessonDate.Year == 2026 && classById[l.ClassId].TeacherId.HasValue)
                .GroupBy(l => (TeacherId: classById[l.ClassId].TeacherId!.Value, l.LessonDate.Month))
                .ToDictionary(g => g.Key, g => g.Count());

            var bonusByTeacherMonth = adjustments
                .Where(a => a.Type == "Bonus" && a.AppliedDate.Year == 2026)
                .GroupBy(a => (a.TeacherId, a.AppliedDate.Month))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var penaltyByTeacherMonth = adjustments
                .Where(a => a.Type == "Penalty" && a.AppliedDate.Year == 2026)
                .GroupBy(a => (a.TeacherId, a.AppliedDate.Month))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            int id = 1;
            for (int month = 6; month <= 11; month++)
            {
                bool isDraftNov = (month == 11);
                foreach (var teacher in activeTeachers)
                {
                    int validSessions = sessionCounts.GetValueOrDefault((teacher.Id, month), 0);
                    decimal rate = rateByTeacher[teacher.Id];
                    decimal baseSalary = validSessions * rate;
                    decimal totalBonus = bonusByTeacherMonth.GetValueOrDefault((teacher.Id, month), 0m);
                    decimal totalPenalty = penaltyByTeacherMonth.GetValueOrDefault((teacher.Id, month), 0m);
                    decimal finalSalary = baseSalary + totalBonus - totalPenalty;

                    summaries.Add(new PayrollSummary
                    {
                        Id = id++,
                        TeacherId = teacher.Id,
                        Month = month,
                        Year = 2026,
                        TotalValidSessions = validSessions,
                        BaseSalary = baseSalary,
                        TotalBonus = totalBonus,
                        TotalPenalty = totalPenalty,
                        FinalSalary = finalSalary,
                        EmailStatus = isDraftNov ? "NotSent" : "Sent",
                        EmailSentAt = isDraftNov ? null : new DateTime(2026, month, 28, 10, 0, 0, DateTimeKind.Utc),
                        EmailErrorMessage = null
                    });
                }
            }

            return summaries;
        }

        // =========================================================================================
        // NHÓM 5: CONVERSATION (320) & MESSAGE (3.200)
        // =========================================================================================

        private List<Conversation> GenerateConversations(
            List<StudentProfile> students,
            List<Classes> classes,
            Dictionary<int, List<int>> classEnrollments,
            Random rng)
        {
            var conversations = new List<Conversation>(320);
            var studentById = students.ToDictionary(s => s.Id);
            var classById = classes.ToDictionary(c => c.Id);

            // Thu thập các cặp (ParentId, TeacherId) thực tế của 200 học sinh active (Id 1..200)
            var activePairs = new HashSet<(int ParentId, int TeacherId)>();
            foreach (var (classId, enrolledStudentIds) in classEnrollments)
            {
                var cls = classById[classId];
                if (!cls.TeacherId.HasValue) continue;

                foreach (var stId in enrolledStudentIds)
                {
                    if (stId <= 200)
                    {
                        int parentId = studentById[stId].ParentId;
                        activePairs.Add((parentId, cls.TeacherId.Value));
                    }
                }
            }

            var pairList = activePairs.ToList();
            Shuffle(pairList, rng);

            // Bổ sung nếu cặp thực tế ít hơn 320 (từ 140 phụ huynh active x 36 GV đang dạy)
            if (pairList.Count < 320)
            {
                for (int pId = 1; pId <= 140 && pairList.Count < 320; pId++)
                {
                    for (int tId = 1; tId <= 36 && pairList.Count < 320; tId++)
                    {
                        if (activePairs.Add((pId, tId)))
                        {
                            pairList.Add((pId, tId));
                        }
                    }
                }
            }

            // 250 hội thoại chính trong T8-T11/2026 (x 10 tin = 2.500 tin) + 70 hội thoại T1-T7/2026 (700 tin)
            for (int i = 0; i < 320; i++)
            {
                var (parentId, teacherId) = pairList[i];
                bool isPeak = i < 250;
                DateTime createdAt = isPeak
                    ? RandomDateBetween(rng, new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 10, 18, 0, 0, DateTimeKind.Utc))
                    : RandomDateBetween(rng, new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc));

                conversations.Add(new Conversation
                {
                    Id = i + 1,
                    ParentId = parentId,
                    TeacherId = teacherId,
                    ChatType = "HUMAN",
                    LastMessage = string.Empty,
                    LastMessageAt = createdAt,
                    CreatedAt = createdAt
                });
            }

            return conversations;
        }

        private List<Message> GenerateMessages(List<Conversation> conversations, Random rng)
        {
            var messages = new List<Message>(3200);
            var parentTexts = new[]
            {
                "Dạ chào thầy/cô, hôm nay bé đi học về rất vui và làm hết bài tập bàn tính rồi ạ.",
                "Thầy/cô cho em hỏi tuần này bé học công thức Anh bạn lớn có theo kịp lớp không ạ?",
                "Chiều thứ 5 nhà em có việc bận nên bé xin vào muộn 10 phút ạ.",
                "Em cảm ơn thầy/cô đã gửi nhận xét chi tiết tiến độ học tập của con nhé!",
                "Thầy/cô ơi cho em hỏi lịch ôn tập kiểm tra cuối cấp độ vào ngày nào ạ?"
            };
            var teacherTexts = new[]
            {
                "Chào phụ huynh, tuần này bé tiến bộ rõ rệt ở tốc độ tính nhẩm không bàn tính ạ!",
                "Phụ huynh yên tâm, bé nắm bài nhanh và rất hăng hái giơ tay phát biểu trên lớp.",
                "Dạ vâng em đã ghi nhận ạ, phụ huynh nhớ nhắc bé mang theo bàn tính và phiếu bài tập nhé.",
                "Em vừa giao thêm bộ bài tập chuỗi phép tính trên ứng dụng, bé luyện 10 phút mỗi tối là rất tốt ạ.",
                "Lịch đánh giá cuối cấp độ sẽ diễn ra vào buổi thứ 16, trung tâm sẽ gửi thông báo nhắc trước 3 ngày ạ."
            };

            int id = 1;
            for (int cIdx = 0; cIdx < conversations.Count; cIdx++)
            {
                var conv = conversations[cIdx];
                bool hasUnreadTail = (cIdx % 3 == 0);
                DateTime cursor = conv.CreatedAt;
                string lastContent = string.Empty;

                for (int m = 0; m < 10; m++)
                {
                    bool fromParent = (m % 2 == 0);
                    string content = fromParent
                        ? parentTexts[(cIdx + m) % parentTexts.Length]
                        : teacherTexts[(cIdx + m) % teacherTexts.Length];

                    cursor = cursor.AddHours(rng.Next(2, 26));
                    if (cursor > PeakEnd) cursor = PeakEnd.AddMinutes(-(10 - m) * 5);

                    bool isRead = !(hasUnreadTail && m >= 8);

                    messages.Add(new Message
                    {
                        Id = id++,
                        ConversationId = conv.Id,
                        SenderType = fromParent ? "PARENT" : "TEACHER",
                        SenderId = fromParent ? conv.ParentId : conv.TeacherId,
                        MessageType = "TEXT",
                        Content = content,
                        AttachmentUrl = null,
                        IsRead = isRead,
                        CreatedAt = cursor
                    });

                    lastContent = content;
                }

                conv.LastMessage = lastContent;
                conv.LastMessageAt = cursor;
            }

            return messages;
        }

        // =========================================================================================
        // NHÓM 1: NOTIFICATION_SETTING (400), NOTIFICATION (600), OUTBOX (600), USER_INBOX (2.400)
        // =========================================================================================

        private List<NotificationSetting> GenerateNotificationSettings(List<ApplicationUser> verifiedAccounts, Random rng)
        {
            var settings = new List<NotificationSetting>(400);
            var channels = new[]
            {
                NotificationSettingTypes.InApp,
                NotificationSettingTypes.Email,
                NotificationSettingTypes.Push
            };

            for (int i = 0; i < verifiedAccounts.Count; i++)
            {
                var acc = verifiedAccounts[i];
                settings.Add(new NotificationSetting
                {
                    Id = i + 1,
                    AccountId = acc.Id,
                    Type = channels[i % channels.Length],
                    Enabled = rng.NextDouble() < 0.78, // Đa dạng bật/tắt Email, Push, In-app
                    UpdatedAt = RandomDateBetween(rng, acc.CreatedAt, PeakEnd)
                });
            }

            return settings;
        }

        private List<Notification> GenerateNotifications(
            List<ApplicationUser> adminAccounts,
            List<ApplicationUser> teacherAccounts,
            Random rng)
        {
            var notifications = new List<Notification>(600);
            var templates = new[]
            {
                (NotificationTypes.Tuition, "Nhắc học phí đầu kỳ học mới", "Quý phụ huynh vui lòng kiểm tra hóa đơn học phí và hoàn tất trước ngày khai giảng."),
                (NotificationTypes.Attendance, "Thông báo điểm danh buổi học", "Giáo viên đã cập nhật kết quả điểm danh và nhận xét buổi học hôm nay của bé."),
                (NotificationTypes.Academic, "Bài tập tư duy mới đã được giao", "Con vừa nhận bài tập luyện tính mới trên ứng dụng, hạn nộp trong vòng 4 ngày."),
                (NotificationTypes.Payment, "Xác nhận thanh toán học phí thành công", "Hệ thống đã ghi nhận giao dịch học phí thành công và kích hoạt suất học cho bé."),
                (NotificationTypes.LeaveRequest, "Cập nhật trạng thái đơn xin nghỉ phép", "Đơn xin nghỉ phép buổi học của học viên đã được giáo vụ/giáo viên xử lý."),
                (NotificationTypes.System, "Thông báo chốt bảng lương tháng", "Bảng lương giảng dạy định kỳ đã được tổng hợp, giáo viên vui lòng kiểm tra chi tiết.")
            };

            // 450 thông báo (75%) trong T8-T11/2026 + 150 thông báo (25%) trong T1-T7/2026
            for (int i = 1; i <= 600; i++)
            {
                bool isPeak = i <= 450;
                var tpl = templates[(i - 1) % templates.Length];
                var creator = (tpl.Item1 == NotificationTypes.Attendance || tpl.Item1 == NotificationTypes.Academic)
                    ? teacherAccounts[(i - 1) % 36]
                    : adminAccounts[(i - 1) % adminAccounts.Count];

                DateTime createdUtc = isPeak
                    ? RandomDateBetween(rng, PeakStart, PeakEnd)
                    : RandomDateBetween(rng, HistoryStart.AddDays(5), HistoryEnd);

                notifications.Add(new Notification
                {
                    Id = i,
                    Type = tpl.Item1,
                    Title = $"{tpl.Item2} #{i:D3}",
                    Body = tpl.Item3,
                    RefId = $"NOTIF-2026-{i:D5}",
                    CreatedByAccountId = creator.Id,
                    CreatedAt = new DateTimeOffset(createdUtc, TimeSpan.Zero)
                });
            }

            return notifications;
        }

        private List<OutboxMessage> GenerateOutboxMessages(List<Notification> notifications, Random rng)
        {
            // Khớp 1-1 với NOTIFICATION (600 dòng): 585 Sent (Done=2), 10 Pending (Pending=0), 5 Failed (Dead=3)
            var outbox = new List<OutboxMessage>(600);

            string parentsAudience = JsonSerializer.Serialize(AudienceSpec.AllParents(), AudienceSpecJson.Options);
            string teachersAudience = JsonSerializer.Serialize(AudienceSpec.AllTeachers(), AudienceSpecJson.Options);
            string adminsAudience = JsonSerializer.Serialize(AudienceSpec.AllAdmins(), AudienceSpecJson.Options);

            for (int i = 0; i < notifications.Count; i++)
            {
                var n = notifications[i];
                string audience = n.Type == NotificationTypes.System
                    ? teachersAudience
                    : (n.Type == NotificationTypes.LeaveRequest ? adminsAudience : parentsAudience);

                int status;
                int attempts;
                DateTime? processedAt;
                DateTimeOffset availableAt;
                string? lastError;

                if (i < 585)
                {
                    status = (int)OutboxStatus.Done;
                    attempts = 1;
                    availableAt = n.CreatedAt;
                    processedAt = n.CreatedAt.AddSeconds(rng.Next(2, 20)).UtcDateTime;
                    lastError = null;
                }
                else if (i < 595)
                {
                    status = (int)OutboxStatus.Pending;
                    attempts = i % 2;
                    availableAt = new DateTimeOffset(AnchorUtc.AddMinutes(rng.Next(1, 30)), TimeSpan.Zero);
                    processedAt = null;
                    lastError = null;
                }
                else
                {
                    status = (int)OutboxStatus.Dead;
                    attempts = 5;
                    availableAt = n.CreatedAt.AddMinutes(30);
                    processedAt = null;
                    lastError = "SMTP gateway timeout (503) sau 5 lần thử gửi lại.";
                }

                outbox.Add(new OutboxMessage
                {
                    Id = i + 1,
                    NotificationId = n.Id,
                    AudienceSpec = audience,
                    Status = status,
                    Attempts = attempts,
                    AvailableAt = availableAt,
                    CreatedAt = n.CreatedAt,
                    ProcessedAt = processedAt,
                    LastError = lastError
                });
            }

            return outbox;
        }

        private List<UserInbox> GenerateUserInboxes(
            List<Notification> notifications,
            List<ApplicationUser> verifiedAccounts,
            Random rng)
        {
            // 2.400 dòng: 600 thông báo x 4 người nhận = 2.400 dòng (TB đúng 6 tin / 400 user đã xác thực)
            // Trong đó 450 thông báo T8-T11 x 4 = 1.800 tin nằm trong T8-T11/2026!
            var inboxes = new List<UserInbox>(2400);
            var usedPairs = new HashSet<(long NotificationId, int AccountId)>();

            long id = 1;
            int cursor = 0;

            for (int nIdx = 0; nIdx < notifications.Count; nIdx++)
            {
                var notif = notifications[nIdx];
                bool isPeak = nIdx < 450;

                int added = 0;
                while (added < 4)
                {
                    var acc = verifiedAccounts[cursor % verifiedAccounts.Count];
                    cursor++;

                    if (usedPairs.Add((notif.Id, acc.Id)))
                    {
                        // Tin lịch sử hầu hết đã đọc (90%), tin cao điểm trộn ~60% đã đọc / 40% chưa đọc
                        bool isRead = isPeak ? rng.NextDouble() < 0.60 : rng.NextDouble() < 0.90;
                        DateTimeOffset? readAt = isRead
                            ? notif.CreatedAt.AddMinutes(rng.Next(5, 1440))
                            : null;
                        if (readAt.HasValue && readAt.Value > new DateTimeOffset(AnchorUtc, TimeSpan.Zero))
                        {
                            readAt = new DateTimeOffset(AnchorUtc.AddMinutes(-rng.Next(1, 60)), TimeSpan.Zero);
                        }

                        inboxes.Add(new UserInbox
                        {
                            Id = id++,
                            NotificationId = notif.Id,
                            AccountId = acc.Id,
                            Type = notif.Type,
                            RefId = notif.RefId,
                            IsRead = isRead,
                            CreatedAt = notif.CreatedAt,
                            ReadAt = readAt
                        });
                        added++;
                    }
                }
            }

            return inboxes;
        }

        // =========================================================================================
        // TIỆN ÍCH BATCH INSERT QUA EF CORE (HỖ TRỢ IDENTITY_INSERT)
        // =========================================================================================

        private async Task SaveBatchWithIdentityAsync<TEntity>(
            DbSet<TEntity> dbSet,
            List<TEntity> items,
            string tableName,
            Dictionary<string, int> summary) where TEntity : class
        {
            if (items.Count == 0)
            {
                summary[tableName] = 0;
                return;
            }

            const int batchSize = 1000;
            int totalInserted = 0;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{tableName}] ON;");

                for (int i = 0; i < items.Count; i += batchSize)
                {
                    var batch = items.Skip(i).Take(batchSize).ToList();
                    await dbSet.AddRangeAsync(batch);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    totalInserted += batch.Count;
                }

                await _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{tableName}] OFF;");
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            summary[tableName] = totalInserted;
            Console.WriteLine($"  [✔ Đã lưu] Bảng {tableName,-25}: {totalInserted,6:N0} dòng");
        }

        // =========================================================================================
        // XUẤT SCRIPT SQL HOÀN CHỈNH (UTF-8 WITH BOM, BATCH 500 DÒNG/INSERT)
        // =========================================================================================

        public async Task<string> GenerateSqlScriptAsync(string outputPath, int rowsPerTable = 1000)
        {
            Console.WriteLine($">>> Đang sinh bộ dữ liệu Bogus mới và xuất script SQL: {outputPath}");

            var data = BuildDataset();
            var previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                await writer.WriteLineAsync("-- =============================================================================");
                await writer.WriteLineAsync("-- SCRIPT SEED DATA BOGUS QUY MÔ CAO ĐIỂM T8-T11/2026 - HỆ THỐNG TOÁN TƯ DUY");
                await writer.WriteLineAsync($"-- Tạo tự động bằng BogusDataSeeder lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync($"-- Tổng số bảng: 31 | Tổng số bản ghi: {data.TotalRowCount:N0}");
                await writer.WriteLineAsync("-- Tài khoản kiểm thử: Admin123 / Teacher123 (Mật khẩu: Demo123@, PIN: 123456)");
                await writer.WriteLineAsync("-- =============================================================================");
                await writer.WriteLineAsync("SET NOCOUNT ON;");
                await writer.WriteLineAsync("SET XACT_ABORT ON;");
                await writer.WriteLineAsync("SET QUOTED_IDENTIFIER ON;");
                await writer.WriteLineAsync("SET ANSI_NULLS ON;");
                await writer.WriteLineAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("BEGIN TRY");
                await writer.WriteLineAsync("    BEGIN TRANSACTION;");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("    PRINT N'>>> Đang xóa dữ liệu cũ theo thứ tự khóa ngoại...';");

                foreach (var t in TablesInDeleteOrder)
                {
                    await writer.WriteLineAsync($"    DELETE FROM [dbo].[{t}];");
                }
                await writer.WriteLineAsync();

                await WriteSqlInsertsAsync(writer, "COURSE", data.Courses,
                    c => $"({c.Id}, '{Escape(c.Code)}', N'{Escape(c.Name)}', N'{Escape(c.Description)}', {c.MinAge}, {c.MaxAge}, {SqlStr(c.ImageUrl)}, {SqlStr(c.VideoUrl)}, {SqlNStr(c.Benefits)}, {SqlNStr(c.FacilityImages)}, {c.Rating:F1}, {c.Reviews}, '{c.Status}', '{FmtDt(c.CreatedAt)}', {SqlDt(c.UpdatedAt)})",
                    "id, code, name, description, min_age, max_age, image_url, video_url, benefits, facility_images, rating, reviews, status, created_at, updated_at");

                await WriteSqlInsertsAsync(writer, "COURSE_LEVEL", data.CourseLevels,
                    cl => $"({cl.Id}, {cl.CourseId}, '{Escape(cl.Code)}', N'{Escape(cl.Name)}', {cl.OrderIndex}, '{cl.Status}', {cl.Fee:F2}, {cl.TotalSessions}, {SqlNStr(cl.Syllabus)}, {SqlNStr(cl.Faqs)}, '{FmtDt(cl.CreatedAt)}', {SqlDt(cl.UpdatedAt)})",
                    "id, course_id, code, name, order_index, status, fee, total_sessions, syllabus, faqs, created_at, updated_at");

                await WriteSqlInsertsAsync(writer, "BADGE", data.Badges,
                    b => $"({b.Id}, N'{Escape(b.Name)}', {SqlNStr(b.Description)}, {SqlStr(b.IconUrl)}, '{b.ConditionType}', {b.ConditionValue})",
                    "id, name, description, icon_url, condition_type, condition_value");

                await WriteSqlInsertsAsync(writer, "ACCOUNT", data.Accounts,
                    a => $"({a.Id}, '{Escape(a.PhoneNumber)}', '{Escape(a.PasswordHash)}', '{a.Role}', NULL, {(a.IsActive ? 1 : 0)}, '{FmtDt(a.CreatedAt)}', '{Escape(a.UserName)}', '{Escape(a.NormalizedUserName)}', '{Escape(a.Email)}', '{Escape(a.NormalizedEmail)}', {SqlStr(a.PendingEmail)}, {(a.EmailConfirmed ? 1 : 0)}, {(a.PhoneNumberConfirmed ? 1 : 0)}, 0, 0, 0, '{Escape(a.SecurityStamp)}', '{Escape(a.ConcurrencyStamp)}')",
                    "id, phone, password_hash, role, refresh_token, is_active, created_at, UserName, NormalizedUserName, Email, NormalizedEmail, pending_email, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, SecurityStamp, ConcurrencyStamp");

                await WriteSqlInsertsAsync(writer, "ADMIN_PROFILE", data.AdminProfiles,
                    ap => $"({ap.Id}, {ap.AccountId}, N'{Escape(ap.FullName)}', {SqlStr(ap.Avatar)}, '{FmtDt(ap.CreatedAt)}')",
                    "id, account_id, full_name, avatar, created_at");

                await WriteSqlInsertsAsync(writer, "TEACHER_PROFILE", data.TeacherProfiles,
                    tp => $"({tp.Id}, {tp.AccountId}, N'{Escape(tp.FullName)}', {SqlStr(tp.Email)}, {SqlStr(tp.Avatar)}, {SqlNStr(tp.Specialize)}, {SqlStr(tp.CreatedAt)})",
                    "id, account_id, full_name, email, avatar, specialize, created_at");

                await WriteSqlInsertsAsync(writer, "PARENT_PROFILE", data.ParentProfiles,
                    pp => $"({pp.Id}, {pp.AccountId}, N'{Escape(pp.FullName)}', {SqlStr(pp.Pin)}, {SqlStr(pp.Avatar)}, '{FmtDt(pp.CreatedAt)}')",
                    "id, account_id, full_name, pin, avatar, created_at");

                await WriteSqlInsertsAsync(writer, "EMAIL_VERIFICATION_TOKEN", data.EmailVerificationTokens,
                    tk => $"({tk.Id}, {tk.AccountId}, '{Escape(tk.TokenHash)}', '{Escape(tk.EmailAtIssue)}', '{FmtDt(tk.ExpiresAt)}', {SqlDt(tk.UsedAt)}, '{FmtDt(tk.CreatedAt)}')",
                    "id, account_id, token_hash, email_at_issue, expires_at, used_at, created_at");

                await WriteSqlInsertsAsync(writer, "TEACHER_SALARY_CONFIG", data.TeacherSalaryConfigs,
                    sc => $"({sc.Id}, {sc.TeacherId}, {sc.BaseRatePerSession:F2})",
                    "id, teacher_id, base_rate_per_session");

                await WriteSqlInsertsAsync(writer, "STUDENT_PROFILE", data.Students,
                    sp => $"({sp.Id}, {sp.ParentId}, N'{Escape(sp.FullName)}', {SqlStr(sp.Avatar)}, {sp.TotalPoints}, N'{Escape(sp.Gender)}', {sp.Stars}, {sp.StreakDays}, {(sp.IsActive ? 1 : 0)}, '{FmtDt(sp.CreatedAt)}')",
                    "id, parent_id, full_name, avatar, total_points, gender, stars, streak_days, is_active, created_at");

                await WriteSqlInsertsAsync(writer, "StudentBadge", data.StudentBadges,
                    sb => $"({sb.Id}, {sb.StudentId}, {sb.BadgeId}, '{FmtDt(sb.EarnedAt)}')",
                    "id, student_id, badge_id, earned_at");

                await WriteSqlInsertsAsync(writer, "CLASSES", data.Classes,
                    c => $"({c.Id}, {c.CourseLevelId}, {SqlInt(c.TeacherId)}, N'{Escape(c.Name)}', {c.MaxCapacity}, '{FmtDt(c.StartDate)}', {SqlDt(c.EndDate)}, '{c.Status}', '{FmtDt(c.CreatedAt)}', {SqlDt(c.UpdatedAt)})",
                    "id, course_level_id, teacher_id, name, max_capacity, start_date, end_date, status, created_at, updated_at");

                await WriteSqlInsertsAsync(writer, "CLASS_SCHEDULE", data.ClassSchedules,
                    cs => $"({cs.Id}, {cs.ClassId}, {cs.DayOfWeek}, '{cs.StartTime:hh\\:mm\\:ss}', '{cs.EndTime:hh\\:mm\\:ss}', {SqlNStr(cs.Room)}, '{FmtDt(cs.CreatedAt)}', {SqlDt(cs.UpdatedAt)})",
                    "id, class_id, day_of_week, start_time, end_time, room, created_at, updated_at");

                await WriteSqlInsertsAsync(writer, "INVOICE", data.Invoices,
                    inv => $"({inv.Id}, {inv.ParentId}, {inv.StudentId}, {inv.ClassId}, '{Escape(inv.TxnRef)}', {inv.Amount}, '{inv.Status}', {SqlStr(inv.PaymentMethod)}, {SqlStr(inv.TransactionId)}, {SqlStr(inv.CheckoutUrl)}, {SqlStr(inv.QrCode)}, '{FmtDt(inv.CreatedAt)}', {SqlDt(inv.ExpiredAt)}, {SqlDt(inv.PaidAt)})",
                    "id, parent_id, student_id, class_id, txn_ref, amount, status, payment_method, transaction_id, checkout_url, qr_code, created_at, expired_at, paid_at");

                await WriteSqlInsertsAsync(writer, "CLASS_STUDENT", data.ClassStudents,
                    cs => $"({cs.Id}, {cs.ClassId}, {cs.StudentId}, {SqlInt(cs.InvoiceId)}, '{FmtDt(cs.JoinedAt)}', '{cs.Status}')",
                    "id, class_id, student_id, invoice_id, joined_at, status");

                await WriteSqlInsertsAsync(writer, "LESSON", data.Lessons,
                    l => $"({l.Id}, {l.ClassId}, {SqlNStr(l.Title)}, '{l.LessonDate:yyyy-MM-dd}', {SqlDt(l.StartTime)}, {SqlDt(l.EndTime)}, '{l.Status}', {(l.IsImportan ? 1 : 0)})",
                    "id, class_id, title, lesson_date, start_time, end_time, status, is_importan");

                await WriteSqlInsertsAsync(writer, "ATTENDANCE", data.Attendances,
                    att => $"({att.Id}, {att.LessonId}, {att.StudentId}, '{att.Status}', {SqlNStr(att.TeacherNotes)})",
                    "id, lesson_id, student_id, status, teacher_notes");

                await WriteSqlInsertsAsync(writer, "LEAVE_REQUEST", data.LeaveRequests,
                    lr => $"({lr.Id}, {lr.StudentId}, {lr.LessonId}, {SqlNStr(lr.Reason)}, '{lr.Status}', {SqlDt(lr.CreatedAt)})",
                    "id, student_id, lesson_id, reason, status, created_at");

                await WriteSqlInsertsAsync(writer, "QUESTION_BANK", data.QuestionBanks,
                    qb => $"({qb.Id}, {qb.CourseId}, N'{Escape(qb.Name)}', {SqlNStr(qb.Description)})",
                    "id, course_id, name, description");

                await WriteSqlInsertsAsync(writer, "QUESTION", data.Questions,
                    q => $"({q.Id}, {q.BankId}, '{q.Type}', N'{Escape(q.Content)}', {SqlStr(q.ImageUrl)}, '{q.Difficulty}', N'{Escape(q.Answer)}')",
                    "id, bank_id, type, content, image_url, difficulty, answer");

                await WriteSqlInsertsAsync(writer, "ASSIGNMENT", data.Assignments,
                    asm => $"({asm.Id}, {asm.LessonId}, {SqlStr(asm.StageId)}, N'{Escape(asm.Title)}', '{FmtDt(asm.StartTime)}', '{FmtDt(asm.Deadline)}', {asm.TimeLimitMinutes}, {SqlInt(asm.MaxAttempt)}, '{asm.Status}')",
                    "id, lesson_id, stage_id, title, start_time, deadline, time_limit_minutes, max_attempt, status");

                await WriteSqlInsertsAsync(writer, "QUESTION_ASSIGNMENT", data.QuestionAssignments,
                    qa => $"({qa.Id}, {qa.AssignmentId}, {qa.QuestionId}, {qa.OrderIndex}, {qa.PointValue:F1})",
                    "id, assignment_id, question_id, order_index, point_value");

                await WriteSqlInsertsAsync(writer, "STUDENT_ASSIGNMENT", data.StudentAssignments,
                    sa => $"({sa.Id}, {sa.AssignmentId}, {sa.StudentId}, {SqlFloat(sa.Score)}, '{sa.Status}', {SqlNStr(sa.TeacherFeedback)}, {SqlDt(sa.CompletedAt)})",
                    "id, assignment_id, student_id, score, status, teacher_feedback, completed_at");

                await WriteSqlInsertsAsync(writer, "CONVERSATION", data.Conversations,
                    c => $"({c.Id}, {c.ParentId}, {SqlInt(c.TeacherId)}, '{c.ChatType}', {SqlNStr(c.LastMessage)}, '{FmtDt(c.LastMessageAt)}', '{FmtDt(c.CreatedAt)}')",
                    "id, parent_id, teacher_id, chat_type, last_message, last_message_at, created_at");

                await WriteSqlInsertsAsync(writer, "MESSAGE", data.Messages,
                    m => $"({m.Id}, {m.ConversationId}, '{m.SenderType}', {SqlInt(m.SenderId)}, '{m.MessageType}', {SqlNStr(m.Content)}, {SqlStr(m.AttachmentUrl)}, {(m.IsRead ? 1 : 0)}, '{FmtDt(m.CreatedAt)}')",
                    "id, conversation_id, sender_type, sender_id, message_type, content, attachment_url, is_read, created_at");

                await WriteSqlInsertsAsync(writer, "NOTIFICATION", data.Notifications,
                    n => $"({n.Id}, '{n.Type}', N'{Escape(n.Title)}', {SqlNStr(n.Body)}, {SqlStr(n.RefId)}, {SqlInt(n.CreatedByAccountId)}, '{FmtDto(n.CreatedAt)}')",
                    "id, type, title, body, ref_id, created_by_account_id, created_at");

                await WriteSqlInsertsAsync(writer, "OUTBOX", data.OutboxMessages,
                    o => $"({o.Id}, {o.NotificationId}, N'{Escape(o.AudienceSpec)}', {o.Status}, {o.Attempts}, '{FmtDto(o.AvailableAt)}', '{FmtDto(o.CreatedAt)}', {SqlDt(o.ProcessedAt)}, {SqlNStr(o.LastError)})",
                    "id, notification_id, audience_spec, status, attempts, available_at, created_at, processed_at, last_error");

                await WriteSqlInsertsAsync(writer, "USER_INBOX", data.UserInboxes,
                    u => $"({u.Id}, {u.NotificationId}, {u.AccountId}, '{u.Type}', {SqlStr(u.RefId)}, {(u.IsRead ? 1 : 0)}, '{FmtDto(u.CreatedAt)}', {SqlDto(u.ReadAt)})",
                    "id, notification_id, account_id, type, ref_id, is_read, created_at, read_at");

                await WriteSqlInsertsAsync(writer, "NOTIFICATION_SETTING", data.NotificationSettings,
                    ns => $"({ns.Id}, {ns.AccountId}, '{ns.Type}', {(ns.Enabled ? 1 : 0)}, '{FmtDt(ns.UpdatedAt)}')",
                    "id, account_id, type, enabled, updated_at");

                await WriteSqlInsertsAsync(writer, "SALARY_ADJUSTMENT", data.SalaryAdjustments,
                    sa => $"({sa.Id}, {sa.TeacherId}, '{sa.Type}', {sa.Amount:F2}, N'{Escape(sa.Reason)}', '{sa.AppliedDate:yyyy-MM-dd}')",
                    "id, teacher_id, type, amount, reason, applied_date");

                await WriteSqlInsertsAsync(writer, "PAYROLL_SUMMARY", data.PayrollSummaries,
                    ps => $"({ps.Id}, {ps.TeacherId}, {ps.Month}, {ps.Year}, {ps.TotalValidSessions}, {ps.BaseSalary:F2}, {ps.TotalPenalty:F2}, {ps.TotalBonus:F2}, {ps.FinalSalary:F2}, '{ps.EmailStatus}', {SqlDt(ps.EmailSentAt)}, {SqlNStr(ps.EmailErrorMessage)})",
                    "id, teacher_id, month, year, total_valid_sessions, base_salary, total_penalty, total_bonus, final_salary, email_status, email_sent_at, email_error_message");

                await writer.WriteLineAsync("    COMMIT TRANSACTION;");
                await writer.WriteLineAsync($"    PRINT N'>>> Nạp dữ liệu Bogus mới thành công 100%! ({data.TotalRowCount:N0} bản ghi / 31 bảng)';");
                await writer.WriteLineAsync("END TRY");
                await writer.WriteLineAsync("BEGIN CATCH");
                await writer.WriteLineAsync("    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;");
                await writer.WriteLineAsync("    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();");
                await writer.WriteLineAsync("    DECLARE @ErrSeverity INT = ERROR_SEVERITY();");
                await writer.WriteLineAsync("    DECLARE @ErrState INT = ERROR_STATE();");
                await writer.WriteLineAsync("    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);");
                await writer.WriteLineAsync("END CATCH;");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }

            Console.WriteLine($"[✔] Đã tạo file SQL thành công tại: {outputPath} ({data.TotalRowCount:N0} bản ghi / 31 bảng)");
            return outputPath;
        }

        private static async Task WriteSqlInsertsAsync<T>(
            StreamWriter writer,
            string tableName,
            List<T> items,
            Func<T, string> formatter,
            string columns)
        {
            if (items == null || items.Count == 0) return;

            await writer.WriteLineAsync($"    -- ==================== {tableName} ({items.Count:N0} dòng) ====================");
            await writer.WriteLineAsync($"    SET IDENTITY_INSERT [dbo].[{tableName}] ON;");

            const int chunkSize = 500;
            for (int i = 0; i < items.Count; i += chunkSize)
            {
                int end = Math.Min(i + chunkSize, items.Count);
                await writer.WriteLineAsync($"    INSERT INTO [dbo].[{tableName}] ({columns}) VALUES");
                for (int j = i; j < end; j++)
                {
                    string delimiter = (j == end - 1) ? ";" : ",";
                    await writer.WriteLineAsync($"    {formatter(items[j])}{delimiter}");
                }
            }

            await writer.WriteLineAsync($"    SET IDENTITY_INSERT [dbo].[{tableName}] OFF;");
            await writer.WriteLineAsync();
        }

        private static string Escape(string? input) =>
            string.IsNullOrEmpty(input) ? string.Empty : input.Replace("'", "''");

        private static string SqlStr(string? value) =>
            value is null ? "NULL" : $"'{Escape(value)}'";

        private static string SqlNStr(string? value) =>
            value is null ? "NULL" : $"N'{Escape(value)}'";

        private static string SqlInt(int? value) =>
            value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";

        private static string SqlFloat(float? value) =>
            value.HasValue ? value.Value.ToString("F1", CultureInfo.InvariantCulture) : "NULL";

        private static string FmtDt(DateTime value) =>
            value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        private static string SqlDt(DateTime? value) =>
            value.HasValue ? $"'{FmtDt(value.Value)}'" : "NULL";

        private static string FmtDto(DateTimeOffset value) =>
            value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss+00:00", CultureInfo.InvariantCulture);

        private static string SqlDto(DateTimeOffset? value) =>
            value.HasValue ? $"'{FmtDto(value.Value)}'" : "NULL";

        private sealed class SeedDataset
        {
            public List<Course> Courses { get; set; } = new();
            public List<CourseLevel> CourseLevels { get; set; } = new();
            public List<Badge> Badges { get; set; } = new();
            public List<ApplicationUser> Accounts { get; set; } = new();
            public List<AdminProfile> AdminProfiles { get; set; } = new();
            public List<TeacherProfile> TeacherProfiles { get; set; } = new();
            public List<ParentProfile> ParentProfiles { get; set; } = new();
            public List<EmailVerificationToken> EmailVerificationTokens { get; set; } = new();
            public List<TeacherSalaryConfig> TeacherSalaryConfigs { get; set; } = new();
            public List<StudentProfile> Students { get; set; } = new();
            public List<StudentBadge> StudentBadges { get; set; } = new();
            public List<Classes> Classes { get; set; } = new();
            public List<ClassSchedule> ClassSchedules { get; set; } = new();
            public List<Invoice> Invoices { get; set; } = new();
            public List<ClassStudent> ClassStudents { get; set; } = new();
            public List<Lesson> Lessons { get; set; } = new();
            public List<Attendance> Attendances { get; set; } = new();
            public List<LeaveRequest> LeaveRequests { get; set; } = new();
            public List<QuestionBank> QuestionBanks { get; set; } = new();
            public List<Question> Questions { get; set; } = new();
            public List<Assignment> Assignments { get; set; } = new();
            public List<QuestionAssignment> QuestionAssignments { get; set; } = new();
            public List<StudentAssignment> StudentAssignments { get; set; } = new();
            public List<Conversation> Conversations { get; set; } = new();
            public List<Message> Messages { get; set; } = new();
            public List<Notification> Notifications { get; set; } = new();
            public List<OutboxMessage> OutboxMessages { get; set; } = new();
            public List<UserInbox> UserInboxes { get; set; } = new();
            public List<NotificationSetting> NotificationSettings { get; set; } = new();
            public List<SalaryAdjustment> SalaryAdjustments { get; set; } = new();
            public List<PayrollSummary> PayrollSummaries { get; set; } = new();

            public int TotalRowCount =>
                Courses.Count + CourseLevels.Count + Badges.Count + Accounts.Count +
                AdminProfiles.Count + TeacherProfiles.Count + ParentProfiles.Count +
                EmailVerificationTokens.Count + TeacherSalaryConfigs.Count + Students.Count +
                StudentBadges.Count + Classes.Count + ClassSchedules.Count + Invoices.Count +
                ClassStudents.Count + Lessons.Count + Attendances.Count + LeaveRequests.Count +
                QuestionBanks.Count + Questions.Count + Assignments.Count +
                QuestionAssignments.Count + StudentAssignments.Count + Conversations.Count +
                Messages.Count + Notifications.Count + OutboxMessages.Count + UserInboxes.Count +
                NotificationSettings.Count + SalaryAdjustments.Count + PayrollSummaries.Count;
        }
    }

    public class SeederResult
    {
        public bool Success { get; set; }
        public double ElapsedSeconds { get; set; }
        public Dictionary<string, int> TableCounts { get; set; } = new();
    }
}
