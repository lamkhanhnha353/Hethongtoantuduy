using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Models;

namespace ToanTuDuy.Data
{
    /// <summary>
    /// Service sinh dữ liệu mẫu quy mô lớn (~1000 dòng/bảng) sử dụng thư viện Bogus (locale 'vi').
    /// Đảm bảo tính toàn vẹn quan hệ (FK), các ràng buộc duy nhất (Unique Index),
    /// và định dạng câu hỏi/đáp án chuẩn theo module TeacherQuestionsView.
    /// </summary>
    public class BogusDataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher;
        private readonly PasswordHasher<ParentProfile> _pinHasher;
        private readonly string _cachedPasswordHash;
        private readonly string _cachedPinHash;

        // Mật khẩu mặc định cho tất cả các tài khoản demo được sinh ra
        public const string DefaultPassword = "Demo123@";
        public const string DefaultPin = "123456";

        public BogusDataSeeder(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<ApplicationUser>();
            _pinHasher = new PasswordHasher<ParentProfile>();
            _cachedPasswordHash = _passwordHasher.HashPassword(new ApplicationUser(), DefaultPassword);
            _cachedPinHash = _pinHasher.HashPassword(new ParentProfile(), DefaultPin);
        }

        /// <summary>
        /// Nạp trực tiếp dữ liệu mẫu vào database thông qua EF Core với cơ chế batch insert tối ưu.
        /// </summary>
        public async Task<SeederResult> SeedAsync(int rowsPerTable = 1000, bool cleanExisting = true)
        {
            var stopwatch = Stopwatch.StartNew();
            var summary = new Dictionary<string, int>();

            Console.WriteLine($"==================================================================");
            Console.WriteLine($" BẮT ĐẦU NẠP DỮ LIỆU MẪU BẰNG BOGUS (~{rowsPerTable} dòng/bảng)");
            Console.WriteLine($" Thời gian bắt đầu: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"==================================================================");

            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            if (cleanExisting)
            {
                await CleanExistingDataAsync();
            }

            // Đặt seed cố định để dữ liệu có tính tái tạo (deterministic) nếu cần
            Randomizer.Seed = new Random(20261115);
            var faker = new Faker("vi");

            // 1. COURSE (~25 môn học tư duy đa dạng)
            var courses = GenerateCourses();
            await SaveBatchAsync(_context.Courses, courses, "COURSE", summary);

            // 2. COURSE_LEVEL (~rowsPerTable)
            var courseLevels = GenerateCourseLevels(courses, rowsPerTable);
            await SaveBatchAsync(_context.CourseLevels, courseLevels, "COURSE_LEVEL", summary);

            // 3. BADGE (~50 danh hiệu huy hiệu)
            var badges = GenerateBadges();
            await SaveBatchAsync(_context.Badges, badges, "BADGE", summary);

            // 4. ACCOUNT (Tối thiểu rowsPerTable tài khoản: Admin, Teacher, Parent)
            var accounts = GenerateAccounts(rowsPerTable);
            await SaveBatchAsync(_context.Users, accounts, "ACCOUNT", summary);

            // Phân nhóm tài khoản theo Role để tạo Profile tương ứng
            var adminAccounts = accounts.Where(a => a.Role == "Admin").ToList();
            var teacherAccounts = accounts.Where(a => a.Role == "Teacher").ToList();
            var parentAccounts = accounts.Where(a => a.Role == "Parent").ToList();

            // 5. ADMIN_PROFILE
            var adminProfiles = GenerateAdminProfiles(adminAccounts);
            await SaveBatchAsync(_context.AdminProfiles, adminProfiles, "ADMIN_PROFILE", summary);

            // 6. TEACHER_PROFILE
            var teacherProfiles = GenerateTeacherProfiles(teacherAccounts);
            await SaveBatchAsync(_context.TeacherProfiles, teacherProfiles, "TEACHER_PROFILE", summary);

            // 7. PARENT_PROFILE (~rowsPerTable)
            var parentProfiles = GenerateParentProfiles(parentAccounts);
            await SaveBatchAsync(_context.ParentProfiles, parentProfiles, "PARENT_PROFILE", summary);

            // 8. TEACHER_SALARY_CONFIG
            var salaryConfigs = GenerateTeacherSalaryConfigs(teacherProfiles);
            await SaveBatchAsync(_context.TeacherSalaryConfigs, salaryConfigs, "TEACHER_SALARY_CONFIG", summary);

            // 9. STUDENT_PROFILE (~rowsPerTable)
            var students = GenerateStudents(parentProfiles, rowsPerTable);
            await SaveBatchAsync(_context.StudentProfiles, students, "STUDENT_PROFILE", summary);

            // 10. STUDENT_BADGE (~rowsPerTable)
            var studentBadges = GenerateStudentBadges(students, badges, rowsPerTable);
            await SaveBatchAsync(_context.StudentBadges, studentBadges, "StudentBadge", summary);

            // 11. CLASSES (~rowsPerTable)
            var classes = GenerateClasses(courseLevels, teacherProfiles, rowsPerTable);
            await SaveBatchAsync(_context.Classes, classes, "CLASSES", summary);

            // 12. CLASS_SCHEDULE (~rowsPerTable)
            var schedules = GenerateClassSchedules(classes, rowsPerTable);
            await SaveBatchAsync(_context.ClassSchedules, schedules, "CLASS_SCHEDULE", summary);

            // 13. INVOICE (~rowsPerTable)
            var invoices = GenerateInvoices(parentProfiles, students, classes, rowsPerTable);
            await SaveBatchAsync(_context.Invoices, invoices, "INVOICE", summary);

            // 14. CLASS_STUDENT (~rowsPerTable)
            var classStudents = GenerateClassStudents(classes, students, invoices, rowsPerTable);
            await SaveBatchAsync(_context.ClassStudents, classStudents, "CLASS_STUDENT", summary);

            // 15. LESSON (~rowsPerTable)
            var lessons = GenerateLessons(classes, rowsPerTable);
            await SaveBatchAsync(_context.Lessons, lessons, "LESSON", summary);

            // 16. ATTENDANCE (~rowsPerTable)
            var attendances = GenerateAttendances(lessons, students, rowsPerTable);
            await SaveBatchAsync(_context.Attendances, attendances, "ATTENDANCE", summary);

            // 17. LEAVE_REQUEST (~rowsPerTable)
            var leaveRequests = GenerateLeaveRequests(students, lessons, rowsPerTable);
            await SaveBatchAsync(_context.LeaveRequests, leaveRequests, "LEAVE_REQUEST", summary);

            // 18. QUESTION_BANK (~rowsPerTable)
            var questionBanks = GenerateQuestionBanks(courses, rowsPerTable);
            await SaveBatchAsync(_context.QuestionBanks, questionBanks, "QUESTION_BANK", summary);

            // 19. QUESTION (~rowsPerTable)
            var questions = GenerateQuestions(questionBanks, rowsPerTable);
            await SaveBatchAsync(_context.Questions, questions, "QUESTION", summary);

            // 20. ASSIGNMENT (~rowsPerTable)
            var assignments = GenerateAssignments(lessons, rowsPerTable);
            await SaveBatchAsync(_context.Assignments, assignments, "ASSIGNMENT", summary);

            // 21. QUESTION_ASSIGNMENT (~rowsPerTable)
            var questionAssignments = GenerateQuestionAssignments(assignments, questions, rowsPerTable);
            await SaveBatchAsync(_context.QuestionAssignments, questionAssignments, "QUESTION_ASSIGNMENT", summary);

            // 22. STUDENT_ASSIGNMENT (~rowsPerTable)
            var studentAssignments = GenerateStudentAssignments(assignments, students, rowsPerTable);
            await SaveBatchAsync(_context.StudentAssignments, studentAssignments, "STUDENT_ASSIGNMENT", summary);

            // 23. CONVERSATION (~rowsPerTable)
            var conversations = GenerateConversations(parentProfiles, teacherProfiles, rowsPerTable);
            await SaveBatchAsync(_context.Conversations, conversations, "CONVERSATION", summary);

            // 24. MESSAGE (~rowsPerTable)
            var messages = GenerateMessages(conversations, rowsPerTable);
            await SaveBatchAsync(_context.Messages, messages, "MESSAGE", summary);

            // 25. NOTIFICATION (~rowsPerTable)
            var notifications = GenerateNotifications(accounts, rowsPerTable);
            await SaveBatchAsync(_context.Notifications, notifications, "NOTIFICATION", summary);

            // 26. OUTBOX (~rowsPerTable)
            var outboxMessages = GenerateOutboxMessages(notifications, rowsPerTable);
            await SaveBatchAsync(_context.OutboxMessages, outboxMessages, "OUTBOX", summary);

            // 27. USER_INBOX (~rowsPerTable)
            var userInboxes = GenerateUserInboxes(notifications, accounts, rowsPerTable);
            await SaveBatchAsync(_context.UserInboxes, userInboxes, "USER_INBOX", summary);

            // 28. NOTIFICATION_SETTING (~rowsPerTable)
            var notificationSettings = GenerateNotificationSettings(accounts, rowsPerTable);
            await SaveBatchAsync(_context.NotificationSettings, notificationSettings, "NOTIFICATION_SETTING", summary);

            // 29. SALARY_ADJUSTMENT (~rowsPerTable)
            var salaryAdjustments = GenerateSalaryAdjustments(teacherProfiles, rowsPerTable);
            await SaveBatchAsync(_context.SalaryAdjustments, salaryAdjustments, "SALARY_ADJUSTMENT", summary);

            // 30. PAYROLL_SUMMARY (~rowsPerTable)
            var payrollSummaries = GeneratePayrollSummaries(teacherProfiles, rowsPerTable);
            await SaveBatchAsync(_context.PayrollSummaries, payrollSummaries, "PAYROLL_SUMMARY", summary);

            stopwatch.Stop();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;

            Console.WriteLine($"==================================================================");
            Console.WriteLine($" HOÀN TẤT NẠP DỮ LIỆU BOGUS THÀNH CÔNG TRONG {stopwatch.Elapsed.TotalSeconds:F2} GIÂY!");
            Console.WriteLine($" Tổng số bảng đã xử lý: {summary.Count}");
            Console.WriteLine($" Tổng số dòng dữ liệu: {summary.Values.Sum():N0}");
            Console.WriteLine($"==================================================================");

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

            var tablesInDeleteOrder = new[]
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
                "EMAIL_VERIFICATION_TOKEN", "ACCOUNT"
            };

            foreach (var table in tablesInDeleteOrder)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[" + table + "];");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [Lưu ý khi xóa bảng {table}]: {ex.Message}");
                }
            }

            Console.WriteLine(">>> Đã dọn dẹp xong dữ liệu cũ.");
        }

        // =========================================================================================
        // CÁC HÀM GENERATOR SỬ DỤNG BOGUS CHO TỪNG THỰC THỂ
        // =========================================================================================

        private List<Course> GenerateCourses()
        {
            var courseCatalog = new[]
            {
                ("SRB-BASIC", "Toán Bàn Tính Soroban Cơ Bản", 4, 8, "Rèn luyện khả năng quan sát và hình thành phản xạ thao tác hạt tính"),
                ("SRB-ADV", "Toán Bàn Tính Soroban Ảo Tính Anzan", 6, 12, "Luyện tập ảo tính không cần bàn tính, tăng cường hoạt động song não"),
                ("FGM-KID", "Finger Math - Toán Bàn Tay Mầm Non", 4, 6, "Đếm và tính nhẩm từ 0 đến 99 bằng hai bàn tay siêu tốc"),
                ("FGM-PRO", "Finger Math Phản Xạ Thần Tốc", 5, 8, "Tính chuỗi phép cộng trừ liên tục trên ngón tay kết hợp hình ảnh"),
                ("MTN-CORE", "Mathnasium Tư Duy Cấu Trúc Số", 6, 11, "Xây dựng nền tảng tư duy số học và trực quan hóa cấu trúc tròn chục"),
                ("MTN-LOGIC", "Mathnasium Suy Luận Bar Model", 7, 12, "Giải toán đố thực tế bằng sơ đồ khối đoạn thẳng trực quan Bar Model"),
                ("KMN-NUM", "Toán Kumon Tự Học & Luyện Tính", 5, 10, "Rèn tính kỷ luật, tự giác và sự chuẩn xác tuyệt đối trong từng phép tính"),
                ("IMT-CRIT", "I-Math Toán Tư Duy Đa Chiều & Phản Biện", 7, 13, "Kích thích tư duy phản biện, tìm kiếm nhiều giải pháp cho một vấn đề"),
                ("CPA-SG", "Toán Tư Duy Singapore Chuẩn CPA", 6, 12, "Phương pháp CPA (Concrete - Pictorial - Abstract) chuẩn quốc tế"),
                ("OLYMP-KID", "Toán Olympic SASMO & Kangaroo", 8, 14, "Bồi dưỡng toán tư duy chuẩn bị cho các kỳ thi học sinh giỏi quốc tế"),
                ("GEOM-3D", "Hình Học Trực Quan & Không Gian 3D", 6, 12, "Phát triển trí tưởng tượng hình học không gian và tư duy hình ảnh"),
                ("LOGIC-RIDDLE", "Logic Học & Phá Án Câu Đố Toán Học", 7, 14, "Thử thách trí thông minh với các câu đố logic và nghịch lý số học")
            };

            var f = new Faker("vi");
            var result = new List<Course>();
            int id = 1;

            foreach (var item in courseCatalog)
            {
                result.Add(new Course
                {
                    Id = id++,
                    Code = item.Item1,
                    Name = item.Item2,
                    MinAge = item.Item3,
                    MaxAge = item.Item4,
                    Description = item.Item5,
                    ImageUrl = $"https://images.unsplash.com/photo-{1596495578000 + id}?w=800",
                    VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    Benefits = JsonSerializer.Serialize(new[] { "Phát triển tư duy toàn diện", "Tăng khả năng tập trung", "Tự tin giải quyết vấn đề" }),
                    FacilityImages = JsonSerializer.Serialize(new[] { "https://images.unsplash.com/photo-1580582932707-520aed937b7b?w=600" }),
                    Rating = Math.Round(f.Random.Double(4.6, 5.0), 1),
                    Reviews = f.Random.Number(50, 500),
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6),
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return result;
        }

        private List<CourseLevel> GenerateCourseLevels(List<Course> courses, int count)
        {
            var f = new Faker("vi");
            var levels = new List<CourseLevel>();
            var levelNames = new[] { "Cơ bản - Nhập môn", "Khởi động & Làm quen", "Trung cấp 1", "Trung cấp 2", "Nâng cao - Ảo tính", "Chuyên sâu & Thực chiến", "Master Tốc độ" };

            for (int i = 1; i <= count; i++)
            {
                var course = courses[(i - 1) % courses.Count];
                int orderIndex = ((i - 1) / courses.Count) + 1;
                var baseName = levelNames[(orderIndex - 1) % levelNames.Length];

                levels.Add(new CourseLevel
                {
                    Id = i,
                    CourseId = course.Id,
                    Code = $"{course.Code}-LV{orderIndex:D2}",
                    Name = $"{course.Name} - {baseName} (Cấp độ {orderIndex})",
                    OrderIndex = orderIndex,
                    Status = "ACTIVE",
                    Fee = f.Random.Number(15, 45) * 100000m,
                    TotalSessions = f.Random.ListItem(new[] { 12, 16, 24, 32 }),
                    Syllabus = JsonSerializer.Serialize(new[]
                    {
                        $"Chủ đề 1: Nhập môn & Làm quen cấp độ {orderIndex}",
                        $"Chủ đề 2: Kỹ năng trọng tâm & Công thức nâng bậc",
                        $"Chủ đề 3: Luyện tập tốc độ & Bài toán thực tế",
                        $"Chủ đề 4: Đánh giá cuối kỳ & Thi lấy chứng chỉ"
                    }),
                    Faqs = JsonSerializer.Serialize(new[]
                    {
                        new { q = "Bé cần chuẩn bị dụng cụ gì?", a = "Trung tâm cung cấp đầy đủ giáo trình, bàn tính và ứng dụng hỗ trợ." },
                        new { q = "Có lớp học bù khi nghỉ không?", a = "Phụ huynh gửi đơn xin nghỉ trên ứng dụng để được sắp xếp buổi bù." }
                    }),
                    CreatedAt = DateTime.UtcNow.AddMonths(-5),
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return levels;
        }

        private List<Badge> GenerateBadges()
        {
            var badgeSpecs = new[]
            {
                ("Chiến Binh Bắt Đầu", "Hoàn thành buổi học đầu tiên", "FIRST_LESSON", 1),
                ("Bậc Thầy Bàn Tính", "Đạt 1000 điểm Soroban", "TOTAL_POINTS", 1000),
                ("Ong Chăm Chỉ", "Duy trì chuỗi học tập 7 ngày liên tiếp", "STREAK_DAYS", 7),
                ("Kỷ Lục Gia Ảo Tính", "Đạt điểm tuyệt đối 10 bài tập liên tiếp", "PERFECT_SCORE", 10),
                ("Đại Sứ Toàn Năng", "Hoàn thành trọn vẹn 3 khóa học tư duy", "COURSES_DONE", 3),
                ("Ngôi Sao Tốc Độ", "Hoàn thành bài tập trắc nghiệm dưới 3 phút", "FAST_SOLVER", 180),
                ("Chuyên Gia Phản Biện", "Đạt điểm 10 phần giải thích tự luận logic", "LOGIC_MASTER", 1),
                ("Học Giả Kumon", "Giải quyết 500 bài tập luyện tính", "TOTAL_ASSIGNMENTS", 500)
            };

            var list = new List<Badge>();
            int id = 1;
            foreach (var b in badgeSpecs)
            {
                list.Add(new Badge
                {
                    Id = id++,
                    Name = b.Item1,
                    Description = b.Item2,
                    ConditionType = b.Item3,
                    ConditionValue = b.Item4,
                    IconUrl = $"https://api.dicebear.com/7.x/shapes/svg?seed=badge_{id}"
                });
            }
            return list;
        }

        private List<ApplicationUser> GenerateAccounts(int targetCount)
        {
            var f = new Faker("vi");
            var accounts = new List<ApplicationUser>();

            // Phân bổ: 10 Admin, 200 Teacher, phần còn lại là Parent (đảm bảo tổng >= targetCount)
            int adminCount = Math.Max(10, (int)(targetCount * 0.02));
            int teacherCount = Math.Max(200, (int)(targetCount * 0.20));
            int parentCount = Math.Max(targetCount, targetCount + 100);

            int currentId = 1;

            // 1. Tài khoản Admin cố định để test (UserName = phone để tương thích LoginView + AuthService)
            accounts.Add(CreateUser(currentId++, "0901000000", "admin@toantuduy.edu.vn", "0901000000", "Admin"));

            // Các Admin tiếp theo
            for (int i = 2; i <= adminCount; i++)
            {
                var phone = $"0901{i:D6}";
                accounts.Add(CreateUser(currentId++, phone, $"admin{i:D2}@toantuduy.edu.vn", phone, "Admin"));
            }

            // Alias Admin123 dùng cho test trực tiếp qua API / Swagger (không qua regex SĐT trên Web UI)
            accounts.Add(CreateUser(currentId++, "0901999999", "admin123@toantuduy.edu.vn", "Admin123", "Admin"));

            // 2. Tài khoản Teacher cố định để test (TeacherProfile Id = 1, có sẵn lớp & học viên)
            accounts.Add(CreateUser(currentId++, "0902000001", "teacher001@toantuduy.edu.vn", "0902000001", "Teacher"));

            // Các Teacher tiếp theo
            for (int i = 2; i <= teacherCount; i++)
            {
                var phone = $"0902{i:D6}";
                accounts.Add(CreateUser(currentId++, phone, $"gv.toantuduy{i:D3}@gmail.com", phone, "Teacher"));
            }

            // Alias Teacher123 dùng cho test trực tiếp qua API / Swagger (không qua regex SĐT trên Web UI)
            accounts.Add(CreateUser(currentId++, "0902999999", "teacher123@toantuduy.edu.vn", "Teacher123", "Teacher"));

            // 3. Các tài khoản Parent (UserName = phone)
            for (int i = 1; i <= parentCount; i++)
            {
                var phone = $"0903{i:D6}";
                accounts.Add(CreateUser(currentId++, phone, $"phuhuynh{i:D4}@gmail.com", phone, "Parent"));
            }

            return accounts;
        }

        private ApplicationUser CreateUser(int id, string phone, string email, string userName, string role)
        {
            var user = new ApplicationUser
            {
                Id = id,
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                EmailConfirmed = true,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            user.PasswordHash = _cachedPasswordHash;
            return user;
        }

        private List<AdminProfile> GenerateAdminProfiles(List<ApplicationUser> admins)
        {
            var f = new Faker("vi");
            var profiles = new List<AdminProfile>();
            int id = 1;

            foreach (var a in admins)
            {
                profiles.Add(new AdminProfile
                {
                    Id = id++,
                    AccountId = a.Id,
                    FullName = id == 2 ? "Quản Trị Viên Hệ Thống" : f.Name.FullName(),
                    Avatar = $"https://api.dicebear.com/7.x/avataaars/svg?seed=admin_{a.Id}",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                });
            }
            return profiles;
        }

        private List<TeacherProfile> GenerateTeacherProfiles(List<ApplicationUser> teachers)
        {
            var f = new Faker("vi");
            var specializations = new[]
            {
                "Toán Bàn Tính Soroban & Ảo Tính Thần Tốc",
                "Finger Math Nhật Bản & Phản Xạ Ngón Tay",
                "Mathnasium & Tư Duy Trực Quan Bar Model",
                "Toán Kumon Tự Học & Luyện Tính Chuẩn Xác",
                "I-Math & Phát Triển Tư Duy Phản Biện",
                "Toán Tư Duy Singapore (CPA) & Olympic SASMO"
            };

            var profiles = new List<TeacherProfile>();
            int id = 1;

            foreach (var t in teachers)
            {
                profiles.Add(new TeacherProfile
                {
                    Id = id++,
                    AccountId = t.Id,
                    FullName = id == 2 ? "Thầy Giáo Test Toàn Năng" : f.Name.FullName(),
                    Email = t.Email,
                    Specialize = f.PickRandom(specializations),
                    Avatar = $"https://api.dicebear.com/7.x/avataaars/svg?seed=teacher_{t.Id}",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6).ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            return profiles;
        }

        private List<ParentProfile> GenerateParentProfiles(List<ApplicationUser> parents)
        {
            var f = new Faker("vi");
            var profiles = new List<ParentProfile>();
            int id = 1;

            foreach (var p in parents)
            {
                var profile = new ParentProfile
                {
                    Id = id++,
                    AccountId = p.Id,
                    FullName = f.Name.FullName(),
                    Avatar = $"https://api.dicebear.com/7.x/avataaars/svg?seed=parent_{p.Id}",
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                };
                profile.Pin = _cachedPinHash;
                profiles.Add(profile);
            }
            return profiles;
        }

        private List<TeacherSalaryConfig> GenerateTeacherSalaryConfigs(List<TeacherProfile> teachers)
        {
            var f = new Faker("vi");
            var configs = new List<TeacherSalaryConfig>();
            int id = 1;

            foreach (var t in teachers)
            {
                configs.Add(new TeacherSalaryConfig
                {
                    Id = id++,
                    TeacherId = t.Id,
                    BaseRatePerSession = f.Random.Number(18, 45) * 10000m
                });
            }
            return configs;
        }

        private List<StudentProfile> GenerateStudents(List<ParentProfile> parents, int count)
        {
            var f = new Faker("vi");
            var students = new List<StudentProfile>();

            for (int i = 1; i <= count; i++)
            {
                var parent = parents[(i - 1) % parents.Count];
                var gender = f.PickRandom("Nam", "Nữ");

                students.Add(new StudentProfile
                {
                    Id = i,
                    ParentId = parent.Id,
                    FullName = gender == "Nam" ? f.Name.FullName(Bogus.DataSets.Name.Gender.Male) : f.Name.FullName(Bogus.DataSets.Name.Gender.Female),
                    Gender = gender,
                    Avatar = $"https://api.dicebear.com/7.x/bottts/svg?seed=student_{i}",
                    TotalPoints = f.Random.Number(100, 4800),
                    Stars = f.Random.Number(5, 120),
                    StreakDays = f.Random.Number(1, 45),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                });
            }
            return students;
        }

        private List<StudentBadge> GenerateStudentBadges(List<StudentProfile> students, List<Badge> badges, int count)
        {
            var f = new Faker("vi");
            var list = new List<StudentBadge>();
            var usedPairs = new HashSet<(int, int)>();

            int id = 1;
            while (list.Count < count)
            {
                var student = f.PickRandom(students);
                var badge = f.PickRandom(badges);

                if (usedPairs.Add((student.Id, badge.Id)))
                {
                    list.Add(new StudentBadge
                    {
                        Id = id++,
                        StudentId = student.Id,
                        BadgeId = badge.Id,
                        EarnedAt = DateTime.UtcNow.AddDays(-f.Random.Number(1, 90))
                    });
                }
            }

            return list;
        }

        private List<Classes> GenerateClasses(List<CourseLevel> levels, List<TeacherProfile> teachers, int count)
        {
            var f = new Faker("vi");
            var classes = new List<Classes>();
            var classCodePrefix = new[] { "SRB", "FGM", "MTN", "KMN", "IMT", "CPA", "OLY" };

            for (int i = 1; i <= count; i++)
            {
                var level = levels[(i - 1) % levels.Count];
                var teacher = teachers[(i - 1) % teachers.Count];
                var prefix = classCodePrefix[(i - 1) % classCodePrefix.Length];
                var startDate = DateTime.UtcNow.AddMonths(-3).AddDays((i % 30));

                classes.Add(new Classes
                {
                    Id = i,
                    CourseLevelId = level.Id,
                    TeacherId = teacher.Id,
                    Name = $"Lớp {prefix}-{i:D3}: {level.Name}",
                    MaxCapacity = f.Random.Number(12, 20),
                    StartDate = startDate,
                    EndDate = startDate.AddMonths(3),
                    Status = f.PickRandom("ACTIVE", "ACTIVE", "ACTIVE", "UPCOMING", "COMPLETED"),
                    CreatedAt = startDate.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return classes;
        }

        private List<ClassSchedule> GenerateClassSchedules(List<Classes> classes, int count)
        {
            var f = new Faker("vi");
            var schedules = new List<ClassSchedule>();
            var timeSlots = new[]
            {
                (new TimeSpan(8, 0, 0), new TimeSpan(9, 30, 0)),
                (new TimeSpan(9, 45, 0), new TimeSpan(11, 15, 0)),
                (new TimeSpan(14, 0, 0), new TimeSpan(15, 30, 0)),
                (new TimeSpan(16, 0, 0), new TimeSpan(17, 30, 0)),
                (new TimeSpan(18, 0, 0), new TimeSpan(19, 30, 0))
            };

            for (int i = 1; i <= count; i++)
            {
                var c = classes[(i - 1) % classes.Count];
                var slot = timeSlots[(i - 1) % timeSlots.Length];
                int dayOfWeek = ((i * 2) % 6) + 2; // Thứ 2 đến Thứ 7

                schedules.Add(new ClassSchedule
                {
                    Id = i,
                    ClassId = c.Id,
                    DayOfWeek = dayOfWeek,
                    StartTime = slot.Item1,
                    EndTime = slot.Item2,
                    Room = $"Phòng Học Tư Duy {((i % 8) + 1):D2} (Tầng {(i % 3) + 1})",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3),
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return schedules;
        }

        private List<Invoice> GenerateInvoices(List<ParentProfile> parents, List<StudentProfile> students, List<Classes> classes, int count)
        {
            var f = new Faker("vi");
            var invoices = new List<Invoice>();
            var parentDict = parents.ToDictionary(p => p.Id);

            for (int i = 1; i <= count; i++)
            {
                var student = students[(i - 1) % students.Count];
                var parent = parentDict.TryGetValue(student.ParentId, out var p) ? p : parents[(i - 1) % parents.Count];
                var c = classes[(i - 1) % classes.Count];
                var status = f.PickRandom("PAID", "PAID", "PAID", "PENDING", "CANCELLED");

                invoices.Add(new Invoice
                {
                    Id = i,
                    ParentId = parent.Id,
                    StudentId = student.Id,
                    ClassId = c.Id,
                    TxnRef = $"TXN{i:D8}{f.Random.AlphaNumeric(4).ToUpperInvariant()}",
                    Amount = f.Random.Number(15, 35) * 100000,
                    Status = status,
                    PaymentMethod = status == "PAID" ? f.PickRandom("VNPAY", "CASH") : null,
                    TransactionId = status == "PAID" ? $"VNP{i:D8}{f.Random.Number(1000, 9999)}" : null,
                    CheckoutUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?order={i}",
                    QrCode = $"data:image/png;base64,demo_invoice_qr_{i}",
                    CreatedAt = DateTime.UtcNow.AddDays(-f.Random.Number(10, 80)),
                    PaidAt = status == "PAID" ? DateTime.UtcNow.AddDays(-f.Random.Number(1, 9)) : null
                });
            }

            return invoices;
        }

        private List<ClassStudent> GenerateClassStudents(List<Classes> classes, List<StudentProfile> students, List<Invoice> invoices, int count)
        {
            var f = new Faker("vi");
            var list = new List<ClassStudent>();

            int id = 1;
            int invoiceIndex = 0;

            for (int i = 1; i <= count; i++)
            {
                var c = classes[(i - 1) % classes.Count];
                var s = students[(i - 1) % students.Count];

                int? invId = null;
                if (invoiceIndex < invoices.Count && invoices[invoiceIndex].ClassId == c.Id && invoices[invoiceIndex].StudentId == s.Id)
                {
                    invId = invoices[invoiceIndex++].Id;
                }

                list.Add(new ClassStudent
                {
                    Id = id++,
                    ClassId = c.Id,
                    StudentId = s.Id,
                    InvoiceId = invId,
                    JoinedAt = c.StartDate.AddDays(-f.Random.Number(1, 10)),
                    Status = f.PickRandom("ACTIVE", "ACTIVE", "ACTIVE", "PENDING", "DROPPED")
                });
            }

            return list;
        }

        private List<Lesson> GenerateLessons(List<Classes> classes, int count)
        {
            var f = new Faker("vi");
            var lessons = new List<Lesson>();
            var topics = new[]
            {
                "Làm quen bàn tính Soroban và các thanh định vị",
                "Quy tắc cộng trừ không kết hợp hạt 5",
                "Công thức Anh bạn nhỏ cộng (+4 = +5 - 1)",
                "Công thức Anh bạn nhỏ trừ (-4 = -5 + 1)",
                "Công thức Anh bạn lớn cộng (+9 = +10 - 1)",
                "Công thức Anh bạn lớn trừ (-9 = -10 + 1)",
                "Phản xạ Finger Math hai bàn tay từ 0 đến 99",
                "Ảo tính Anzan: Hình dung bàn tính trong tâm trí",
                "Bài toán logic Bar Model: Tìm hai số khi biết tổng và hiệu",
                "Ôn tập chuyên đề & Thử thách giải đề bấm giờ"
            };

            for (int i = 1; i <= count; i++)
            {
                var c = classes[(i - 1) % classes.Count];
                int sessionNumber = ((i - 1) / classes.Count) + 1;
                var lessonDate = c.StartDate.AddDays((sessionNumber - 1) * 7);

                lessons.Add(new Lesson
                {
                    Id = i,
                    ClassId = c.Id,
                    Title = $"Buổi {sessionNumber}: {topics[(sessionNumber - 1) % topics.Length]}",
                    LessonDate = lessonDate,
                    StartTime = lessonDate.AddHours(8),
                    EndTime = lessonDate.AddHours(9).AddMinutes(30),
                    Status = lessonDate < DateTime.UtcNow ? "COMPLETED" : "SCHEDULED",
                    IsImportan = (sessionNumber % 5 == 0) // Các buổi tổng kết là quan trọng
                });
            }

            return lessons;
        }

        private List<Attendance> GenerateAttendances(List<Lesson> lessons, List<StudentProfile> students, int count)
        {
            var f = new Faker("vi");
            var attendances = new List<Attendance>();

            for (int i = 1; i <= count; i++)
            {
                var l = lessons[(i - 1) % lessons.Count];
                var s = students[(i - 1) % students.Count];
                var status = f.PickRandom("PRESENT", "PRESENT", "PRESENT", "LATE", "ABSENT");

                attendances.Add(new Attendance
                {
                    Id = i,
                    LessonId = l.Id,
                    StudentId = s.Id,
                    Status = status,
                    TeacherNotes = status == "LATE" ? "Vào lớp muộn 10 phút do kẹt xe" : (status == "ABSENT" ? "Có phép từ phụ huynh" : "Bé tiếp thu bài tốt, thao tác hạt nhanh")
                });
            }

            return attendances;
        }

        private List<LeaveRequest> GenerateLeaveRequests(List<StudentProfile> students, List<Lesson> lessons, int count)
        {
            var f = new Faker("vi");
            var requests = new List<LeaveRequest>();
            var reasons = new[]
            {
                "Bé bị sốt phát ban, xin phép cho bé nghỉ dưỡng bệnh.",
                "Gia đình có việc bận về quê đột xuất, xin nghỉ 1 buổi.",
                "Bé tham gia kỳ thi tiếng Anh ở trường tiểu học trùng lịch học.",
                "Bé bị cảm cúm, gia đình xin học bù vào buổi khác."
            };

            for (int i = 1; i <= count; i++)
            {
                var s = students[(i - 1) % students.Count];
                var l = lessons[(i - 1) % lessons.Count];

                requests.Add(new LeaveRequest
                {
                    Id = i,
                    StudentId = s.Id,
                    LessonId = l.Id,
                    Reason = f.PickRandom(reasons),
                    Status = f.PickRandom("APPROVED", "APPROVED", "PENDING", "REJECTED"),
                    CreatedAt = l.LessonDate.AddDays(-2)
                });
            }

            return requests;
        }

        private List<QuestionBank> GenerateQuestionBanks(List<Course> courses, int count)
        {
            var f = new Faker("vi");
            var banks = new List<QuestionBank>();
            var bankCategories = new[]
            {
                "Thao tác hạt đơn vị & Bàn tính cơ sở",
                "Công thức Anh bạn nhỏ (Cộng & Trừ)",
                "Công thức Anh bạn lớn qua cầu 10",
                "Ảo tính Anzan nhẩm không bàn tính",
                "Phản xạ Finger Math tốc độ cao 0-99",
                "Bar Model sơ đồ khối & Bài toán có lời văn",
                "Toán đố logic & Quy luật hình học",
                "Đề thi thử học sinh giỏi tư duy cấp trường",
                "Luyện tập phản xạ tính nhanh đếm ngược",
                "Toán tư duy Singapore CPA chuyên sâu"
            };

            for (int i = 1; i <= count; i++)
            {
                var course = courses[(i - 1) % courses.Count];
                var category = bankCategories[(i - 1) % bankCategories.Length];

                banks.Add(new QuestionBank
                {
                    Id = i,
                    CourseId = course.Id,
                    Name = $"Ngân hàng {course.Code} - {category} #{i}",
                    Description = $"Bộ câu hỏi trắc nghiệm, điền đáp số và tự luận dành cho khóa {course.Name}."
                });
            }

            return banks;
        }

        private List<Question> GenerateQuestions(List<QuestionBank> banks, int count)
        {
            var f = new Faker("vi");
            var questions = new List<Question>();

            // Danh sách các mẫu câu hỏi tư duy phong phú
            for (int i = 1; i <= count; i++)
            {
                var bank = banks[(i - 1) % banks.Count];
                int modType = i % 3;

                if (modType == 1)
                {
                    // MULTIPLE_CHOICE (Trắc nghiệm theo chuẩn parseMultipleChoice)
                    int a = f.Random.Number(1, 9);
                    int b = f.Random.Number(1, 9);
                    int correct = a + b;
                    int wrong1 = correct + 1;
                    int wrong2 = Math.Max(1, correct - 1);
                    int wrong3 = correct + 2;

                    questions.Add(new Question
                    {
                        Id = i,
                        BankId = bank.Id,
                        Type = "MULTIPLE_CHOICE",
                        Content = $"Khi gảy bàn tính Soroban thực hiện phép tính {a} + {b}, kết quả chính xác bằng bao nhiêu?\n" +
                                  $"A. {correct}\n" +
                                  $"B. {wrong1}\n" +
                                  $"C. {wrong2}\n" +
                                  $"D. {wrong3}",
                        ImageUrl = i % 2 == 0 ? "https://images.unsplash.com/photo-1596495578065-6e0763fa1178?w=600" : null,
                        Difficulty = f.PickRandom("EASY", "MEDIUM", "HARD"),
                        Answer = $"A. {correct}\n- Lời giải: Ta thực hiện gảy {a} hạt, sau đó thêm {b} hạt theo quy ước bàn tính. Tổng thu được là {correct}."
                    });
                }
                else if (modType == 2)
                {
                    // FILL_IN (Điền đáp số chuẩn regex split: \n- Hướng dẫn giải:)
                    int x = f.Random.Number(10, 50);
                    int y = f.Random.Number(5, 30);
                    int ans = x - y;

                    questions.Add(new Question
                    {
                        Id = i,
                        BankId = bank.Id,
                        Type = "FILL_IN",
                        Content = $"Thực hiện ảo tính Anzan trong đầu: {x} - {y} = ?",
                        ImageUrl = null,
                        Difficulty = f.PickRandom("EASY", "MEDIUM", "HARD"),
                        Answer = $"{ans}\n- Hướng dẫn giải: Hình dung bàn tính trong tâm trí: gạt {x} ở hàng chục và đơn vị, sau đó gạt bớt {y}. Kết quả còn lại là {ans}."
                    });
                }
                else
                {
                    // ESSAY (Tự luận chuẩn: - Hướng dẫn chấm & Lời giải:)
                    questions.Add(new Question
                    {
                        Id = i,
                        BankId = bank.Id,
                        Type = "ESSAY",
                        Content = $"Bài toán suy luận: Bạn An có số kẹo gấp đôi bạn Bình. Nếu An cho Bình 4 chiếc kẹo thì số kẹo hai bạn bằng nhau. Hỏi ban đầu mỗi bạn có bao nhiêu chiếc kẹo? Hãy vẽ sơ đồ đoạn thẳng và trình bày bài giải.",
                        ImageUrl = "https://images.unsplash.com/photo-1509228468518-180dd4864904?w=600",
                        Difficulty = "HARD",
                        Answer = "- Hướng dẫn chấm & Lời giải:\n" +
                                 "+ Bước 1 (0.5đ): Vẽ sơ đồ đoạn thẳng biểu diễn số kẹo của An là 2 phần, Bình là 1 phần.\n" +
                                 "+ Bước 2 (0.5đ): Hiệu số kẹo giữa An và Bình là 4 x 2 = 8 chiếc kẹo.\n" +
                                 "+ Bước 3 (0.5đ): Hiệu số phần bằng nhau: 2 - 1 = 1 phần. Vậy Bình có 8 chiếc kẹo.\n" +
                                 "+ Bước 4 (0.5đ): An có số kẹo là: 8 x 2 = 16 chiếc kẹo. Đáp số: An: 16, Bình: 8."
                    });
                }
            }

            return questions;
        }

        private List<Assignment> GenerateAssignments(List<Lesson> lessons, int count)
        {
            var f = new Faker("vi");
            var assignments = new List<Assignment>();

            for (int i = 1; i <= count; i++)
            {
                var lesson = lessons[(i - 1) % lessons.Count];
                var startTime = lesson.LessonDate.AddHours(12);

                assignments.Add(new Assignment
                {
                    Id = i,
                    LessonId = lesson.Id,
                    StageId = $"STAGE-BOGUS-{((i % 20) + 1):D2}",
                    Title = $"Bài tập rèn luyện tư duy #{i:D3}: {lesson.Title}",
                    StartTime = startTime,
                    Deadline = startTime.AddDays(5),
                    TimeLimitMinutes = f.PickRandom(15, 20, 30, 45),
                    MaxAttempt = f.PickRandom(1, 2, 3),
                    Status = "ACTIVE"
                });
            }

            return assignments;
        }

        private List<QuestionAssignment> GenerateQuestionAssignments(List<Assignment> assignments, List<Question> questions, int count)
        {
            var f = new Faker("vi");
            var list = new List<QuestionAssignment>();

            for (int i = 1; i <= count; i++)
            {
                var a = assignments[(i - 1) % assignments.Count];
                var q = questions[(i - 1) % questions.Count];
                int orderIndex = ((i - 1) % 10) + 1;

                list.Add(new QuestionAssignment
                {
                    Id = i,
                    AssignmentId = a.Id,
                    QuestionId = q.Id,
                    OrderIndex = orderIndex,
                    PointValue = f.PickRandom(1.0f, 1.5f, 2.0f)
                });
            }

            return list;
        }

        private List<StudentAssignment> GenerateStudentAssignments(List<Assignment> assignments, List<StudentProfile> students, int count)
        {
            var f = new Faker("vi");
            var list = new List<StudentAssignment>();

            for (int i = 1; i <= count; i++)
            {
                var a = assignments[(i - 1) % assignments.Count];
                var s = students[(i - 1) % students.Count];
                float score = (float)Math.Round(f.Random.Double(6.5, 10.0), 1);

                list.Add(new StudentAssignment
                {
                    Id = i,
                    AssignmentId = a.Id,
                    StudentId = s.Id,
                    Score = score,
                    Status = "GRADED",
                    TeacherFeedback = score >= 9.0f ? "Xuất sắc! Con thao tác tính nhẩm rất chính xác." : "Con cần rèn thêm kỹ năng cộng hạt có nhớ nhé.",
                    CompletedAt = a.StartTime.AddDays(f.Random.Number(1, 3))
                });
            }

            return list;
        }

        private List<Conversation> GenerateConversations(List<ParentProfile> parents, List<TeacherProfile> teachers, int count)
        {
            var f = new Faker("vi");
            var conversations = new List<Conversation>();

            for (int i = 1; i <= count; i++)
            {
                var parent = parents[(i - 1) % parents.Count];
                // 10% cuộc trò chuyện đầu là AI (mỗi parent tối đa 1 phòng AI)
                bool isAi = (i <= Math.Min(100, parents.Count));

                if (isAi)
                {
                    conversations.Add(new Conversation
                    {
                        Id = i,
                        ParentId = parent.Id,
                        TeacherId = null,
                        ChatType = "AI",
                        LastMessage = "Trợ lý AI đã giải đáp thắc mắc về phương pháp học Soroban tại nhà.",
                        LastMessageAt = DateTime.UtcNow.AddHours(-f.Random.Number(1, 48)),
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    });
                }
                else
                {
                    var teacher = teachers[(i - 1) % teachers.Count];
                    conversations.Add(new Conversation
                    {
                        Id = i,
                        ParentId = parent.Id,
                        TeacherId = teacher.Id,
                        ChatType = "HUMAN",
                        LastMessage = "Thầy cô đã gửi nhận xét tiến độ học tập tuần này của bé.",
                        LastMessageAt = DateTime.UtcNow.AddHours(-f.Random.Number(1, 72)),
                        CreatedAt = DateTime.UtcNow.AddDays(-60)
                    });
                }
            }

            return conversations;
        }

        private List<Message> GenerateMessages(List<Conversation> conversations, int count)
        {
            var f = new Faker("vi");
            var messages = new List<Message>();
            var sampleTexts = new[]
            {
                "Dạ chào thầy cô, hôm nay bé An làm bài tập về nhà rất hào hứng ạ.",
                "Chào phụ huynh, tuần này bé tiến bộ vượt bậc ở kỹ năng ảo tính Anzan nhé!",
                "Thầy cô cho em hỏi buổi học bù vào thứ 7 diễn ra lúc mấy giờ ạ?",
                "Buổi học bù sẽ bắt đầu lúc 9:00 sáng tại Phòng 201 phụ huynh nhé.",
                "Cảm ơn thầy cô nhiều ạ! Chúc thầy cô một ngày làm việc vui vẻ."
            };

            for (int i = 1; i <= count; i++)
            {
                var c = conversations[(i - 1) % conversations.Count];
                string senderType;
                int? senderId;

                if (i % 2 == 0)
                {
                    if (c.ChatType == "AI")
                    {
                        senderType = "AI";
                        senderId = null;
                    }
                    else
                    {
                        senderType = "TEACHER";
                        senderId = c.TeacherId;
                    }
                }
                else
                {
                    senderType = "PARENT";
                    senderId = c.ParentId;
                }

                messages.Add(new Message
                {
                    Id = i,
                    ConversationId = c.Id,
                    SenderType = senderType,
                    SenderId = senderId,
                    MessageType = "TEXT",
                    Content = sampleTexts[(i - 1) % sampleTexts.Length],
                    IsRead = true,
                    CreatedAt = c.CreatedAt.AddHours(i)
                });
            }

            return messages;
        }

        private List<Notification> GenerateNotifications(List<ApplicationUser> accounts, int count)
        {
            var f = new Faker("vi");
            var notifications = new List<Notification>();
            var notifTypes = new[]
            {
                ("CLASS_REMINDER", "Nhắc nhở lịch học hôm nay", "Lớp học tư duy của con sẽ bắt đầu trong 30 phút nữa."),
                ("INVOICE_PAID", "Xác nhận thanh toán học phí thành công", "Học phí khóa học của con đã được ghi nhận thành công qua VNPay."),
                ("ASSIGNMENT_NEW", "Bài tập tư duy mới đã mở", "Giáo viên vừa giao bài tập về nhà môn Soroban. Hạn nộp còn 3 ngày."),
                ("ATTENDANCE_NOTICE", "Thông báo điểm danh buổi học", "Bé đã có mặt đầy đủ và đúng giờ tại lớp học hôm nay.")
            };

            for (int i = 1; i <= count; i++)
            {
                var spec = notifTypes[(i - 1) % notifTypes.Length];
                var creator = accounts[(i - 1) % accounts.Count];

                notifications.Add(new Notification
                {
                    Id = i,
                    Type = spec.Item1,
                    Title = $"{spec.Item2} #{i}",
                    Body = spec.Item3,
                    RefId = $"REF-{i:D6}",
                    CreatedByAccountId = creator.Id,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-f.Random.Number(1, 60))
                });
            }

            return notifications;
        }

        private List<OutboxMessage> GenerateOutboxMessages(List<Notification> notifications, int count)
        {
            var f = new Faker("vi");
            var outbox = new List<OutboxMessage>();

            for (int i = 1; i <= count; i++)
            {
                var notif = notifications[(i - 1) % notifications.Count];

                outbox.Add(new OutboxMessage
                {
                    Id = i,
                    NotificationId = notif.Id,
                    AudienceSpec = "{\"role\": \"Parent\"}",
                    Status = (int)OutboxStatus.Done,
                    Attempts = 1,
                    AvailableAt = notif.CreatedAt,
                    CreatedAt = notif.CreatedAt,
                    ProcessedAt = notif.CreatedAt.AddSeconds(5).DateTime,
                    LastError = null
                });
            }

            return outbox;
        }

        private List<UserInbox> GenerateUserInboxes(List<Notification> notifications, List<ApplicationUser> accounts, int count)
        {
            var f = new Faker("vi");
            var inboxes = new List<UserInbox>();
            var usedPairs = new HashSet<(long, int)>();

            int id = 1;
            for (int i = 1; i <= count; i++)
            {
                var notif = notifications[(i - 1) % notifications.Count];
                var acc = accounts[(i - 1) % accounts.Count];

                if (usedPairs.Add((notif.Id, acc.Id)))
                {
                    inboxes.Add(new UserInbox
                    {
                        Id = id++,
                        NotificationId = notif.Id,
                        AccountId = acc.Id,
                        Type = notif.Type,
                        RefId = notif.RefId,
                        IsRead = f.Random.Bool(0.7f),
                        CreatedAt = notif.CreatedAt,
                        ReadAt = notif.CreatedAt.AddHours(2)
                    });
                }
            }

            return inboxes;
        }

        private List<NotificationSetting> GenerateNotificationSettings(List<ApplicationUser> accounts, int count)
        {
            var f = new Faker("vi");
            var settings = new List<NotificationSetting>();
            var types = new[] { "CLASS_REMINDER", "INVOICE", "ASSIGNMENT", "ATTENDANCE" };
            var usedPairs = new HashSet<(int, string)>();

            int id = 1;
            for (int i = 1; i <= count; i++)
            {
                var acc = accounts[(i - 1) % accounts.Count];
                var type = types[(i - 1) % types.Length];

                if (usedPairs.Add((acc.Id, type)))
                {
                    settings.Add(new NotificationSetting
                    {
                        Id = id++,
                        AccountId = acc.Id,
                        Type = type,
                        Enabled = true,
                        UpdatedAt = DateTime.UtcNow.AddMonths(-2)
                    });
                }
            }

            return settings;
        }

        private List<SalaryAdjustment> GenerateSalaryAdjustments(List<TeacherProfile> teachers, int count)
        {
            var f = new Faker("vi");
            var adjustments = new List<SalaryAdjustment>();
            var reasons = new[]
            {
                ("BONUS", 500000m, "Thưởng thành tích học viên đạt điểm cao kỳ thi Soroban"),
                ("BONUS", 300000m, "Thưởng chuyên cần và giảng dạy nhiệt tình trong tháng"),
                ("PENALTY", 100000m, "Khấu trừ vào muộn buổi học không báo trước"),
                ("BONUS", 1000000m, "Thưởng sáng kiến đóng góp ngân hàng câu hỏi tư duy mới")
            };

            for (int i = 1; i <= count; i++)
            {
                var t = teachers[(i - 1) % teachers.Count];
                var r = reasons[(i - 1) % reasons.Length];
                var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-f.Random.Number(1, 90)));

                adjustments.Add(new SalaryAdjustment
                {
                    Id = i,
                    TeacherId = t.Id,
                    Type = r.Item1,
                    Amount = r.Item2,
                    Reason = $"{r.Item3} (Đợt #{i})",
                    AppliedDate = date
                });
            }

            return adjustments;
        }

        private List<PayrollSummary> GeneratePayrollSummaries(List<TeacherProfile> teachers, int count)
        {
            var f = new Faker("vi");
            var summaries = new List<PayrollSummary>();
            var usedTeacherMonths = new HashSet<(int, int, int)>();

            int id = 1;
            for (int i = 1; i <= count; i++)
            {
                var t = teachers[(i - 1) % teachers.Count];
                int month = ((i - 1) % 12) + 1;
                int year = 2026;

                if (usedTeacherMonths.Add((t.Id, month, year)))
                {
                    int sessions = f.Random.Number(16, 32);
                    decimal baseRate = 250000m;
                    decimal baseSalary = sessions * baseRate;
                    decimal bonus = f.Random.Number(2, 8) * 100000m;
                    decimal penalty = f.Random.Number(0, 2) * 100000m;
                    decimal finalSalary = baseSalary + bonus - penalty;

                    summaries.Add(new PayrollSummary
                    {
                        Id = id++,
                        TeacherId = t.Id,
                        Month = month,
                        Year = year,
                        TotalValidSessions = sessions,
                        BaseSalary = baseSalary,
                        TotalBonus = bonus,
                        TotalPenalty = penalty,
                        FinalSalary = finalSalary,
                        EmailStatus = "Sent",
                        EmailSentAt = new DateTime(year, month, 28, 17, 0, 0, DateTimeKind.Utc)
                    });
                }
            }

            return summaries;
        }

        // =========================================================================================
        // TIỆN ÍCH BATCH INSERT NHANH QUA EF CORE
        // =========================================================================================

        private async Task SaveBatchAsync<TEntity>(DbSet<TEntity> dbSet, List<TEntity> items, string tableName, Dictionary<string, int> summary) where TEntity : class
        {
            const int batchSize = 1000;
            int totalInserted = 0;

            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                await dbSet.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
                totalInserted += batch.Count;
            }

            summary[tableName] = totalInserted;
            Console.WriteLine($"  [✔ Đã lưu] Bảng {tableName,-22}: {totalInserted,6:N0} dòng");
        }

        /// <summary>
        /// Tạo file script SQL hoàn chỉnh chứa dữ liệu mẫu Bogus (~1000 dòng/bảng).
        /// </summary>
        public async Task<string> GenerateSqlScriptAsync(string outputPath, int rowsPerTable = 1000)
        {
            Console.WriteLine($">>> Đang khởi tạo script SQL xuất dữ liệu mẫu Bogus: {outputPath}");

            // Đặt seed cố định
            Randomizer.Seed = new Random(20261115);

            var sb = new StringBuilder();
            sb.AppendLine("-- =============================================================================");
            sb.AppendLine($"-- SCRIPT SEED DATA BOGUS 1000 DÒNG/BẢNG - HỆ THỐNG TOÁN TƯ DUY");
            sb.AppendLine($"-- Tạo tự động bằng BogusDataSeeder lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("-- =============================================================================");
            sb.AppendLine("SET NOCOUNT ON;");
            sb.AppendLine("SET XACT_ABORT ON;");
            sb.AppendLine("SET QUOTED_IDENTIFIER ON;");
            sb.AppendLine("SET ANSI_NULLS ON;");
            sb.AppendLine("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';");
            sb.AppendLine();
            sb.AppendLine("BEGIN TRY");
            sb.AppendLine("    BEGIN TRANSACTION;");
            sb.AppendLine();

            // Sinh các danh sách
            var courses = GenerateCourses();
            var courseLevels = GenerateCourseLevels(courses, rowsPerTable);
            var badges = GenerateBadges();
            var accounts = GenerateAccounts(rowsPerTable);
            var adminAccounts = accounts.Where(a => a.Role == "Admin").ToList();
            var teacherAccounts = accounts.Where(a => a.Role == "Teacher").ToList();
            var parentAccounts = accounts.Where(a => a.Role == "Parent").ToList();
            var adminProfiles = GenerateAdminProfiles(adminAccounts);
            var teacherProfiles = GenerateTeacherProfiles(teacherAccounts);
            var parentProfiles = GenerateParentProfiles(parentAccounts);
            var salaryConfigs = GenerateTeacherSalaryConfigs(teacherProfiles);
            var students = GenerateStudents(parentProfiles, rowsPerTable);
            var studentBadges = GenerateStudentBadges(students, badges, rowsPerTable);
            var classes = GenerateClasses(courseLevels, teacherProfiles, rowsPerTable);
            var schedules = GenerateClassSchedules(classes, rowsPerTable);
            var invoices = GenerateInvoices(parentProfiles, students, classes, rowsPerTable);
            var classStudents = GenerateClassStudents(classes, students, invoices, rowsPerTable);
            var lessons = GenerateLessons(classes, rowsPerTable);
            var attendances = GenerateAttendances(lessons, students, rowsPerTable);
            var leaveRequests = GenerateLeaveRequests(students, lessons, rowsPerTable);
            var questionBanks = GenerateQuestionBanks(courses, rowsPerTable);
            var questions = GenerateQuestions(questionBanks, rowsPerTable);
            var assignments = GenerateAssignments(lessons, rowsPerTable);
            var questionAssignments = GenerateQuestionAssignments(assignments, questions, rowsPerTable);
            var studentAssignments = GenerateStudentAssignments(assignments, students, rowsPerTable);
            var conversations = GenerateConversations(parentProfiles, teacherProfiles, rowsPerTable);
            var messages = GenerateMessages(conversations, rowsPerTable);
            var notifications = GenerateNotifications(accounts, rowsPerTable);
            var outboxMessages = GenerateOutboxMessages(notifications, rowsPerTable);
            var userInboxes = GenerateUserInboxes(notifications, accounts, rowsPerTable);
            var notificationSettings = GenerateNotificationSettings(accounts, rowsPerTable);
            var salaryAdjustments = GenerateSalaryAdjustments(teacherProfiles, rowsPerTable);
            var payrollSummaries = GeneratePayrollSummaries(teacherProfiles, rowsPerTable);

            // Ghi DELETE
            sb.AppendLine("    PRINT N'>>> Đang xóa dữ liệu cũ...';");
            var tablesToDelete = new[]
            {
                "MESSAGE", "CONVERSATION", "SALARY_ADJUSTMENT", "PAYROLL_SUMMARY", "TEACHER_SALARY_CONFIG",
                "USER_INBOX", "OUTBOX", "NOTIFICATION", "NOTIFICATION_SETTING",
                "QUESTION_ASSIGNMENT", "STUDENT_ASSIGNMENT", "ASSIGNMENT", "QUESTION", "QUESTION_BANK",
                "LEAVE_REQUEST", "ATTENDANCE", "LESSON", "CLASS_STUDENT", "INVOICE", "CLASS_SCHEDULE", "CLASSES",
                "StudentBadge", "BADGE", "STUDENT_PROFILE", "PARENT_PROFILE", "TEACHER_PROFILE", "ADMIN_PROFILE",
                "COURSE_LEVEL", "COURSE", "EMAIL_VERIFICATION_TOKEN", "ACCOUNT"
            };
            foreach (var t in tablesToDelete)
            {
                sb.AppendLine($"    DELETE FROM [dbo].[{t}];");
            }
            sb.AppendLine();

            // Viết các lệnh INSERT kèm IDENTITY_INSERT (ép InvariantCulture để dấu thập phân luôn là '.')
            var previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                AppendSqlInserts(sb, "COURSE", courses, c => $"({c.Id}, '{Escape(c.Code)}', N'{Escape(c.Name)}', N'{Escape(c.Description)}', {c.MinAge}, {c.MaxAge}, '{Escape(c.ImageUrl)}', '{Escape(c.VideoUrl)}', N'{Escape(c.Benefits)}', N'{Escape(c.FacilityImages)}', {c.Rating}, {c.Reviews}, '{c.Status}', '{c.CreatedAt:yyyy-MM-dd HH:mm:ss}', '{c.UpdatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, code, name, description, min_age, max_age, image_url, video_url, benefits, facility_images, rating, reviews, status, created_at, updated_at");

                AppendSqlInserts(sb, "COURSE_LEVEL", courseLevels, cl => $"({cl.Id}, {cl.CourseId}, '{Escape(cl.Code)}', N'{Escape(cl.Name)}', {cl.OrderIndex}, '{cl.Status}', {cl.Fee:F2}, {cl.TotalSessions}, N'{Escape(cl.Syllabus)}', N'{Escape(cl.Faqs)}', '{cl.CreatedAt:yyyy-MM-dd HH:mm:ss}', '{cl.UpdatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, course_id, code, name, order_index, status, fee, total_sessions, syllabus, faqs, created_at, updated_at");

                AppendSqlInserts(sb, "BADGE", badges, b => $"({b.Id}, N'{Escape(b.Name)}', N'{Escape(b.Description)}', '{Escape(b.IconUrl)}', '{b.ConditionType}', {b.ConditionValue})",
                    "id, name, description, icon_url, condition_type, condition_value");

                AppendSqlInserts(sb, "ACCOUNT", accounts, a => $"({a.Id}, '{a.PhoneNumber}', '{a.PasswordHash}', '{a.Role}', NULL, {(a.IsActive ? 1 : 0)}, '{a.CreatedAt:yyyy-MM-dd HH:mm:ss}', '{a.UserName}', '{a.NormalizedUserName}', '{a.Email}', '{a.NormalizedEmail}', 1, 1, 0, 0, 0, '{a.SecurityStamp}', '{a.ConcurrencyStamp}')",
                    "id, phone, password_hash, role, refresh_token, is_active, created_at, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, SecurityStamp, ConcurrencyStamp");

                AppendSqlInserts(sb, "ADMIN_PROFILE", adminProfiles, ap => $"({ap.Id}, {ap.AccountId}, N'{Escape(ap.FullName)}', '{Escape(ap.Avatar)}', '{ap.CreatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, account_id, full_name, avatar, created_at");

                AppendSqlInserts(sb, "TEACHER_PROFILE", teacherProfiles, tp => $"({tp.Id}, {tp.AccountId}, N'{Escape(tp.FullName)}', '{Escape(tp.Email)}', '{Escape(tp.Avatar)}', N'{Escape(tp.Specialize)}', '{tp.CreatedAt}')",
                    "id, account_id, full_name, email, avatar, specialize, created_at");

                AppendSqlInserts(sb, "PARENT_PROFILE", parentProfiles, pp => $"({pp.Id}, {pp.AccountId}, N'{Escape(pp.FullName)}', '{pp.Pin}', '{Escape(pp.Avatar)}', '{pp.CreatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, account_id, full_name, pin, avatar, created_at");

                AppendSqlInserts(sb, "TEACHER_SALARY_CONFIG", salaryConfigs, sc => $"({sc.Id}, {sc.TeacherId}, {sc.BaseRatePerSession:F2})",
                    "id, teacher_id, base_rate_per_session");

                AppendSqlInserts(sb, "STUDENT_PROFILE", students, sp => $"({sp.Id}, {sp.ParentId}, N'{Escape(sp.FullName)}', '{Escape(sp.Avatar)}', {sp.TotalPoints}, N'{sp.Gender}', {sp.Stars}, {sp.StreakDays}, {(sp.IsActive ? 1 : 0)}, '{sp.CreatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, parent_id, full_name, avatar, total_points, gender, stars, streak_days, is_active, created_at");

                AppendSqlInserts(sb, "StudentBadge", studentBadges, sbItem => $"({sbItem.Id}, {sbItem.StudentId}, {sbItem.BadgeId}, '{sbItem.EarnedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, student_id, badge_id, earned_at");

                AppendSqlInserts(sb, "CLASSES", classes, c => $"({c.Id}, {c.CourseLevelId}, {c.TeacherId}, N'{Escape(c.Name)}', {c.MaxCapacity}, '{c.StartDate:yyyy-MM-dd HH:mm:ss}', '{c.EndDate:yyyy-MM-dd HH:mm:ss}', '{c.Status}', '{c.CreatedAt:yyyy-MM-dd HH:mm:ss}', '{c.UpdatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, course_level_id, teacher_id, name, max_capacity, start_date, end_date, status, created_at, updated_at");

                AppendSqlInserts(sb, "CLASS_SCHEDULE", schedules, cs => $"({cs.Id}, {cs.ClassId}, {cs.DayOfWeek}, '{cs.StartTime}', '{cs.EndTime}', N'{Escape(cs.Room)}', '{cs.CreatedAt:yyyy-MM-dd HH:mm:ss}', '{cs.UpdatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, class_id, day_of_week, start_time, end_time, room, created_at, updated_at");

                AppendSqlInserts(sb, "INVOICE", invoices, inv => $"({inv.Id}, {inv.ParentId}, {inv.StudentId}, {inv.ClassId}, '{inv.TxnRef}', {inv.Amount}, '{inv.Status}', {(inv.PaymentMethod != null ? $"'{inv.PaymentMethod}'" : "NULL")}, {(inv.TransactionId != null ? $"'{inv.TransactionId}'" : "NULL")}, '{Escape(inv.CheckoutUrl)}', '{Escape(inv.QrCode)}', '{inv.CreatedAt:yyyy-MM-dd HH:mm:ss}', {(inv.PaidAt.HasValue ? $"'{inv.PaidAt.Value:yyyy-MM-dd HH:mm:ss}'" : "NULL")})",
                    "id, parent_id, student_id, class_id, txn_ref, amount, status, payment_method, transaction_id, checkout_url, qr_code, created_at, paid_at");

                AppendSqlInserts(sb, "CLASS_STUDENT", classStudents, cs => $"({cs.Id}, {cs.ClassId}, {cs.StudentId}, {(cs.InvoiceId.HasValue ? cs.InvoiceId.Value.ToString() : "NULL")}, '{cs.JoinedAt:yyyy-MM-dd HH:mm:ss}', '{cs.Status}')",
                    "id, class_id, student_id, invoice_id, joined_at, status");

                AppendSqlInserts(sb, "LESSON", lessons, l => $"({l.Id}, {l.ClassId}, N'{Escape(l.Title)}', '{l.LessonDate:yyyy-MM-dd}', '{l.StartTime:yyyy-MM-dd HH:mm:ss}', '{l.EndTime:yyyy-MM-dd HH:mm:ss}', '{l.Status}', {(l.IsImportan ? 1 : 0)})",
                    "id, class_id, title, lesson_date, start_time, end_time, status, is_importan");

                AppendSqlInserts(sb, "ATTENDANCE", attendances, att => $"({att.Id}, {att.LessonId}, {att.StudentId}, '{att.Status}', N'{Escape(att.TeacherNotes)}')",
                    "id, lesson_id, student_id, status, teacher_notes");

                AppendSqlInserts(sb, "LEAVE_REQUEST", leaveRequests, lr => $"({lr.Id}, {lr.StudentId}, {lr.LessonId}, N'{Escape(lr.Reason)}', '{lr.Status}', '{lr.CreatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, student_id, lesson_id, reason, status, created_at");

                AppendSqlInserts(sb, "QUESTION_BANK", questionBanks, qb => $"({qb.Id}, {qb.CourseId}, N'{Escape(qb.Name)}', N'{Escape(qb.Description)}')",
                    "id, course_id, name, description");

                AppendSqlInserts(sb, "QUESTION", questions, q => $"({q.Id}, {q.BankId}, '{q.Type}', N'{Escape(q.Content)}', {(q.ImageUrl != null ? $"'{Escape(q.ImageUrl)}'" : "NULL")}, '{q.Difficulty}', N'{Escape(q.Answer)}')",
                    "id, bank_id, type, content, image_url, difficulty, answer");

                AppendSqlInserts(sb, "ASSIGNMENT", assignments, asm => $"({asm.Id}, {asm.LessonId}, '{asm.StageId}', N'{Escape(asm.Title)}', '{asm.StartTime:yyyy-MM-dd HH:mm:ss}', '{asm.Deadline:yyyy-MM-dd HH:mm:ss}', {asm.TimeLimitMinutes}, {asm.MaxAttempt}, '{asm.Status}')",
                    "id, lesson_id, stage_id, title, start_time, deadline, time_limit_minutes, max_attempt, status");

                AppendSqlInserts(sb, "QUESTION_ASSIGNMENT", questionAssignments, qa => $"({qa.Id}, {qa.AssignmentId}, {qa.QuestionId}, {qa.OrderIndex}, {qa.PointValue:F1})",
                    "id, assignment_id, question_id, order_index, point_value");

                AppendSqlInserts(sb, "STUDENT_ASSIGNMENT", studentAssignments, sa => $"({sa.Id}, {sa.AssignmentId}, {sa.StudentId}, {sa.Score:F1}, '{sa.Status}', N'{Escape(sa.TeacherFeedback)}', '{sa.CompletedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, assignment_id, student_id, score, status, teacher_feedback, completed_at");

                AppendSqlInserts(sb, "CONVERSATION", conversations, c => $"({c.Id}, {c.ParentId}, {(c.TeacherId.HasValue ? c.TeacherId.Value.ToString() : "NULL")}, '{c.ChatType}', N'{Escape(c.LastMessage)}', '{c.LastMessageAt:yyyy-MM-dd HH:mm:ss}', '{c.CreatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, parent_id, teacher_id, chat_type, last_message, last_message_at, created_at");

                AppendSqlInserts(sb, "MESSAGE", messages, m => $"({m.Id}, {m.ConversationId}, '{m.SenderType}', {(m.SenderId.HasValue ? m.SenderId.Value.ToString() : "NULL")}, '{m.MessageType}', N'{Escape(m.Content)}', NULL, {(m.IsRead ? 1 : 0)}, '{m.CreatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, conversation_id, sender_type, sender_id, message_type, content, attachment_url, is_read, created_at");

                AppendSqlInserts(sb, "NOTIFICATION", notifications, n => $"({n.Id}, '{n.Type}', N'{Escape(n.Title)}', N'{Escape(n.Body)}', '{n.RefId}', {n.CreatedByAccountId}, '{n.CreatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, type, title, body, ref_id, created_by_account_id, created_at");

                AppendSqlInserts(sb, "OUTBOX", outboxMessages, o => $"({o.Id}, {o.NotificationId}, N'{Escape(o.AudienceSpec)}', {o.Status}, {o.Attempts}, '{o.AvailableAt:yyyy-MM-dd HH:mm:ss}', '{o.CreatedAt:yyyy-MM-dd HH:mm:ss}', '{o.ProcessedAt:yyyy-MM-dd HH:mm:ss}', NULL)",
                    "id, notification_id, audience_spec, status, attempts, available_at, created_at, processed_at, last_error");

                AppendSqlInserts(sb, "USER_INBOX", userInboxes, u => $"({u.Id}, {u.NotificationId}, {u.AccountId}, '{u.Type}', '{u.RefId}', {(u.IsRead ? 1 : 0)}, '{u.CreatedAt:yyyy-MM-dd HH:mm:ss}', '{u.ReadAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, notification_id, account_id, type, ref_id, is_read, created_at, read_at");

                AppendSqlInserts(sb, "NOTIFICATION_SETTING", notificationSettings, ns => $"({ns.Id}, {ns.AccountId}, '{ns.Type}', {(ns.Enabled ? 1 : 0)}, '{ns.UpdatedAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, account_id, type, enabled, updated_at");

                AppendSqlInserts(sb, "SALARY_ADJUSTMENT", salaryAdjustments, sa => $"({sa.Id}, {sa.TeacherId}, '{sa.Type}', {sa.Amount:F2}, N'{Escape(sa.Reason)}', '{sa.AppliedDate:yyyy-MM-dd}')",
                    "id, teacher_id, type, amount, reason, applied_date");

                AppendSqlInserts(sb, "PAYROLL_SUMMARY", payrollSummaries, ps => $"({ps.Id}, {ps.TeacherId}, {ps.Month}, {ps.Year}, {ps.TotalValidSessions}, {ps.BaseSalary:F2}, {ps.TotalPenalty:F2}, {ps.TotalBonus:F2}, {ps.FinalSalary:F2}, '{ps.EmailStatus}', '{ps.EmailSentAt:yyyy-MM-dd HH:mm:ss}')",
                    "id, teacher_id, month, year, total_valid_sessions, base_salary, total_penalty, total_bonus, final_salary, email_status, email_sent_at");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }

            sb.AppendLine();
            sb.AppendLine("    COMMIT TRANSACTION;");
            sb.AppendLine("    PRINT N'>>> Nạp dữ liệu Bogus thành công 100%!';");
            sb.AppendLine("END TRY");
            sb.AppendLine("BEGIN CATCH");
            sb.AppendLine("    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;");
            sb.AppendLine("    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();");
            sb.AppendLine("    DECLARE @ErrSeverity INT = ERROR_SEVERITY();");
            sb.AppendLine("    DECLARE @ErrState INT = ERROR_STATE();");
            sb.AppendLine("    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);");
            sb.AppendLine("END CATCH;");
            sb.AppendLine();
            sb.AppendLine("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';");

            await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"[✔] Đã tạo file SQL thành công tại: {outputPath}");

            return outputPath;
        }

        private static void AppendSqlInserts<T>(StringBuilder sb, string tableName, List<T> items, Func<T, string> formatter, string columns)
        {
            if (items == null || items.Count == 0) return;

            sb.AppendLine($"    -- ==================== {tableName} ({items.Count:N0} dòng) ====================");
            sb.AppendLine($"    SET IDENTITY_INSERT [dbo].[{tableName}] ON;");

            const int chunkSize = 500;
            for (int i = 0; i < items.Count; i += chunkSize)
            {
                var chunk = items.Skip(i).Take(chunkSize).ToList();
                sb.AppendLine($"    INSERT INTO [dbo].[{tableName}] ({columns}) VALUES");
                for (int j = 0; j < chunk.Count; j++)
                {
                    string delimiter = (j == chunk.Count - 1) ? ";" : ",";
                    sb.AppendLine($"    {formatter(chunk[j])}{delimiter}");
                }
            }

            sb.AppendLine($"    SET IDENTITY_INSERT [dbo].[{tableName}] OFF;");
            sb.AppendLine();
        }

        private static string Escape(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace("'", "''");
        }
    }

    public class SeederResult
    {
        public bool Success { get; set; }
        public double ElapsedSeconds { get; set; }
        public Dictionary<string, int> TableCounts { get; set; } = new();
    }
}
