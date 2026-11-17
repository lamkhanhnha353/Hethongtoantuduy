using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Extensions;
using ToanTuDuy.Models;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Services
{
    public class ClassService : IClassService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IBusinessNotificationEmailService _businessEmailService;

        public ClassService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IBusinessNotificationEmailService businessEmailService)
        {
            _context = context;
            _notificationService = notificationService;
            _businessEmailService = businessEmailService;
        }

        // Hàm hỗ trợ map dữ liệu để tránh lặp code
        private ClassResponse MapToResponse(Classes c)
        {
            return new ClassResponse
            {
                Id = c.Id,
                CourseLevelId = c.CourseLevelId,
                TeacherId = c.TeacherId,
                Name = c.Name,
                MaxCapacity = c.MaxCapacity,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                // Lấy tên Level, tên Giáo viên và thông tin Khóa học
                CourseLevelName = c.CourseLevel != null ? c.CourseLevel.Name : "",
                TeacherName = c.Teacher != null ? c.Teacher.FullName : null,
                CourseId = c.CourseLevel != null ? c.CourseLevel.CourseId : null,
                CourseName = c.CourseLevel?.Course != null ? c.CourseLevel.Course.Name : null,

                ActiveStudentsCount = c.ClassStudents != null
                    ? c.ClassStudents.Count(cs => cs.Status == "ACTIVE")
                    : 0
            };
        }

        private static IQueryable<ClassResponse> ProjectClassResponses(IQueryable<Classes> query)
        {
            return query.Select(c => new ClassResponse
            {
                Id = c.Id,
                CourseLevelId = c.CourseLevelId,
                TeacherId = c.TeacherId,
                Name = c.Name,
                MaxCapacity = c.MaxCapacity,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CourseLevelName = c.CourseLevel != null ? c.CourseLevel.Name : "",
                TeacherName = c.Teacher != null ? c.Teacher.FullName : null,
                CourseId = c.CourseLevel != null ? c.CourseLevel.CourseId : null,
                CourseName = c.CourseLevel != null && c.CourseLevel.Course != null ? c.CourseLevel.Course.Name : null,
                ActiveStudentsCount = c.ClassStudents.Count(cs => cs.Status == "ACTIVE")
            });
        }

        public async Task<IEnumerable<ClassResponse>> GetAllClassesAsync()
        {
            return await ProjectClassResponses(_context.Classes.AsNoTracking())
                .ToListAsync();
        }

        public async Task<PagedResult<ClassResponse>> GetPagedClassesAsync(
            int page = 1,
            int pageSize = 12,
            string? keyword = null,
            int? courseId = null,
            int? courseLevelId = null,
            int? teacherId = null,
            string? status = null,
            string? capacity = null,
            string? sort = null)
        {
            var query = _context.Classes.AsNoTracking().AsQueryable();

            if (courseId.HasValue && courseId.Value > 0)
            {
                query = query.Where(c => c.CourseLevel != null && c.CourseLevel.CourseId == courseId.Value);
            }

            if (courseLevelId.HasValue && courseLevelId.Value > 0)
            {
                query = query.Where(c => c.CourseLevelId == courseLevelId.Value);
            }

            if (teacherId.HasValue && teacherId.Value > 0)
            {
                query = query.Where(c => c.TeacherId == teacherId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToUpperInvariant();
                query = query.Where(c => c.Status.ToUpper() == normalizedStatus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                var hasNumericId = int.TryParse(kw, out var parsedId);
                query = query.Where(c =>
                    (hasNumericId && c.Id == parsedId) ||
                    EF.Functions.Like(c.Name, $"%{kw}%") ||
                    (c.Teacher != null && EF.Functions.Like(c.Teacher.FullName, $"%{kw}%")) ||
                    (c.CourseLevel != null && EF.Functions.Like(c.CourseLevel.Name, $"%{kw}%")) ||
                    (c.CourseLevel != null && c.CourseLevel.Course != null &&
                        EF.Functions.Like(c.CourseLevel.Course.Name, $"%{kw}%")));
            }

            if (!string.IsNullOrWhiteSpace(capacity))
            {
                var normalizedCap = capacity.Trim().ToLowerInvariant();
                query = normalizedCap switch
                {
                    "available" => query.Where(c =>
                        c.MaxCapacity <= 0 || c.ClassStudents.Count(cs => cs.Status == "ACTIVE") < c.MaxCapacity),
                    "full" => query.Where(c =>
                        c.MaxCapacity > 0 && c.ClassStudents.Count(cs => cs.Status == "ACTIVE") * 10 >= c.MaxCapacity * 9),
                    "low" => query.Where(c =>
                        c.MaxCapacity <= 0 || c.ClassStudents.Count(cs => cs.Status == "ACTIVE") * 2 < c.MaxCapacity),
                    _ => query
                };
            }

            var projected = ProjectClassResponses(query);

            projected = (sort?.Trim().ToLowerInvariant()) switch
            {
                "name_desc" => projected.OrderByDescending(c => c.Name).ThenByDescending(c => c.Id),
                "students_desc" => projected.OrderByDescending(c => c.ActiveStudentsCount).ThenBy(c => c.Id),
                "date_asc" => projected.OrderBy(c => c.StartDate).ThenBy(c => c.Id),
                "newest" => projected.OrderByDescending(c => c.Id),
                _ => projected.OrderBy(c => c.Name).ThenBy(c => c.Id),
            };

            return await projected.ToPagedResultAsync(page, pageSize, defaultPageSize: 12);
        }

        public async Task<IEnumerable<ClassResponse>> GetClassesByCourseLevelIdAsync(int courseLevelId)
        {
            return await ProjectClassResponses(
                _context.Classes
                    .AsNoTracking()
                    .Where(c => c.CourseLevelId == courseLevelId))
                .ToListAsync();
        }

        public async Task<ClassResponse> GetClassByIdAsync(int id)
        {
            var c = await _context.Classes
                .Include(cls => cls.CourseLevel)
                    .ThenInclude(cl => cl.Course)
                .Include(cls => cls.Teacher)
                .Include(cls => cls.ClassStudents) // Include để lấy dữ liệu tính slot
                .FirstOrDefaultAsync(cls => cls.Id == id);

            if (c == null) return null;
            return MapToResponse(c);
        }

        public async Task<ClassResponse> CreateClassAsync(ClassRequest request)
        {
            var levelExists = await _context.CourseLevels.AnyAsync(l => l.Id == request.CourseLevelId);
            if (!levelExists) throw new Exception("Cấp độ khóa học không tồn tại!");

            var newClass = new Classes
            {
                CourseLevelId = request.CourseLevelId,
                TeacherId = request.TeacherId, // Có thể là null
                Name = request.Name,
                MaxCapacity = request.MaxCapacity,
                StartDate = request.StartDate,
                EndDate = request.EndDate, // Có thể là null
                Status = request.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();

            if (newClass.TeacherId.HasValue)
            {
                await NotifyTeacherAllocatedAsync(newClass.Id, newClass.TeacherId.Value);
            }

            return await GetClassByIdAsync(newClass.Id);
        }

        public async Task<bool> UpdateClassAsync(int id, ClassRequest request)
        {
            var c = await _context.Classes.FindAsync(id);
            if (c == null) return false;

            var previousTeacherId = c.TeacherId;

            c.CourseLevelId = request.CourseLevelId;
            c.TeacherId = request.TeacherId;
            c.Name = request.Name;
            c.MaxCapacity = request.MaxCapacity;
            c.StartDate = request.StartDate;
            c.EndDate = request.EndDate;
            c.Status = request.Status;
            c.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (c.TeacherId.HasValue && c.TeacherId != previousTeacherId)
            {
                await NotifyTeacherAllocatedAsync(c.Id, c.TeacherId.Value);
            }

            return true;
        }

        public async Task<bool> DeleteClassAsync(int id)
        {
            // Kiểm tra xem lớp này đã có học sinh nào chưa, nếu có thì chặn không cho xóa
            var c = await _context.Classes.Include(cls => cls.ClassStudents).FirstOrDefaultAsync(cls => cls.Id == id);
            if (c == null) return false;

            if (c.ClassStudents.Any())
                throw new Exception("Không thể xóa Lớp học đang có học sinh đăng ký.");

            _context.Classes.Remove(c);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Quản lý danh sách học viên trong lớp ---
        public async Task<IEnumerable<ClassStudentResponse>> GetStudentsByClassIdAsync(int classId)
        {
            return await BuildClassStudentsQuery(classId, keyword: null).ToListAsync();
        }

        public async Task<PagedResult<ClassStudentResponse>> GetPagedStudentsByClassIdAsync(
            int classId,
            int page = 1,
            int pageSize = PaginationDefaults.DefaultPageSize,
            string? keyword = null)
        {
            return await BuildClassStudentsQuery(classId, keyword)
                .ToPagedResultAsync(page, pageSize);
        }

        private IQueryable<ClassStudentResponse> BuildClassStudentsQuery(int classId, string? keyword)
        {
            var query = _context.ClassStudents
                .AsNoTracking()
                .Where(cs => cs.ClassId == classId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                var hasNumericId = int.TryParse(kw, out var parsedId);
                query = query.Where(cs =>
                    (hasNumericId && cs.StudentId == parsedId) ||
                    (cs.Student != null && EF.Functions.Like(cs.Student.FullName, $"%{kw}%")) ||
                    (cs.Student != null && cs.Student.Parent != null &&
                        EF.Functions.Like(cs.Student.Parent.FullName, $"%{kw}%")) ||
                    (cs.Student != null && cs.Student.Parent != null && cs.Student.Parent.Account != null &&
                        cs.Student.Parent.Account.PhoneNumber != null &&
                        EF.Functions.Like(cs.Student.Parent.Account.PhoneNumber, $"%{kw}%")));
            }

            return query
                .OrderByDescending(cs => cs.JoinedAt)
                .ThenByDescending(cs => cs.Id)
                .Select(cs => new ClassStudentResponse
                {
                    Id = cs.Id,
                    StudentId = cs.StudentId,
                    StudentName = cs.Student != null ? cs.Student.FullName : "",
                    Avatar = cs.Student != null ? cs.Student.Avatar : null,
                    TotalPoints = cs.Student != null ? cs.Student.TotalPoints : 0,
                    Stars = cs.Student != null ? cs.Student.Stars : 0,
                    StreakDays = cs.Student != null ? cs.Student.StreakDays : 0,
                    ParentId = cs.Student != null ? (int?)cs.Student.ParentId : null,
                    ParentName = cs.Student != null && cs.Student.Parent != null ? cs.Student.Parent.FullName : null,
                    ParentPhone = cs.Student != null && cs.Student.Parent != null && cs.Student.Parent.Account != null
                        ? cs.Student.Parent.Account.PhoneNumber
                        : null,
                    ParentEmail = cs.Student != null && cs.Student.Parent != null && cs.Student.Parent.Account != null
                        ? cs.Student.Parent.Account.Email
                        : null,
                    Status = cs.Status,
                    JoinedAt = cs.JoinedAt,
                    InvoiceId = cs.InvoiceId
                });
        }

        public async Task<ClassStudentResponse> AddStudentToClassAsync(int classId, int studentId)
        {
            var classItem = await _context.Classes
                .Include(c => c.ClassStudents)
                .FirstOrDefaultAsync(c => c.Id == classId);
            if (classItem == null)
                throw new Exception("Không tìm thấy lớp học.");

            var student = await _context.StudentProfiles
                .Include(s => s.Parent)
                    .ThenInclude(p => p.Account)
                .FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null)
                throw new Exception("Không tìm thấy hồ sơ học viên.");

            var existing = await _context.ClassStudents
                .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);

            if (existing != null && (existing.Status == "ACTIVE" || existing.Status == "PENDING"))
            {
                throw new Exception("Học viên này đã có trong danh sách lớp học.");
            }

            // Kiểm tra sĩ số lớp
            var currentCount = classItem.ClassStudents.Count(cs => cs.Status == "ACTIVE");
            if (currentCount >= classItem.MaxCapacity)
            {
                throw new Exception($"Lớp học đã đạt sĩ số tối đa ({classItem.MaxCapacity} học viên).");
            }

            if (existing != null)
            {
                existing.Status = "ACTIVE";
                existing.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new ClassStudent
                {
                    ClassId = classId,
                    StudentId = studentId,
                    Status = "ACTIVE",
                    JoinedAt = DateTime.UtcNow
                };
                _context.ClassStudents.Add(existing);
            }

            await _context.SaveChangesAsync();

            await NotifyStudentAllocatedAsync(classItem, student);

            return new ClassStudentResponse
            {
                Id = existing.Id,
                StudentId = student.Id,
                StudentName = student.FullName,
                Avatar = student.Avatar,
                TotalPoints = student.TotalPoints,
                Stars = student.Stars,
                StreakDays = student.StreakDays,
                ParentId = student.ParentId,
                ParentName = student.Parent != null ? student.Parent.FullName : null,
                ParentPhone = student.Parent?.Account?.PhoneNumber,
                ParentEmail = student.Parent?.Account?.Email,
                Status = existing.Status,
                JoinedAt = existing.JoinedAt,
                InvoiceId = existing.InvoiceId
            };
        }

        public async Task<bool> RemoveStudentFromClassAsync(int classId, int studentId)
        {
            var existing = await _context.ClassStudents
                .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
            if (existing == null) return false;

            _context.ClassStudents.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task NotifyTeacherAllocatedAsync(int classId, int teacherId)
        {
            var classInfo = await _context.Classes
                .AsNoTracking()
                .Where(c => c.Id == classId)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.StartDate,
                    c.MaxCapacity,
                    CourseLevelName = c.CourseLevel != null ? c.CourseLevel.Name : null,
                    Schedules = c.ClassSchedules
                        .OrderBy(s => s.DayOfWeek)
                        .ThenBy(s => s.StartTime)
                        .Select(s => new { s.DayOfWeek, s.StartTime, s.EndTime, s.Room })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (classInfo == null)
            {
                return;
            }

            const string title = "Bạn được phân bổ lớp học mới";
            var body = $"Bạn đã được phân công giảng dạy lớp {classInfo.Name} (khai giảng {classInfo.StartDate:dd/MM/yyyy}).";
            var refId = $"CLASS:{classId}:TEACHER_ALLOC:{teacherId}:{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            await _notificationService.PublishAsync(
                new NotificationMessage(NotificationTypes.Enrollment, title, body, refId),
                AudienceSpec.Teacher(teacherId));

            var scheduleSummary = classInfo.Schedules.Count == 0
                ? "Chưa thiết lập lịch cố định"
                : string.Join("; ", classInfo.Schedules.Select(s =>
                {
                    var dayLabel = s.DayOfWeek == 0 ? "Chủ nhật" : $"Thứ {s.DayOfWeek + 1}";
                    var roomPart = string.IsNullOrWhiteSpace(s.Room) ? string.Empty : $" - Phòng {s.Room}";
                    return $"{dayLabel} ({s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}{roomPart})";
                }));

            var details = new List<(string Label, string Value)>
            {
                ("Lớp học", classInfo.Name),
                ("Cấp độ / Khóa học", classInfo.CourseLevelName ?? "—"),
                ("Ngày khai giảng", $"{classInfo.StartDate:dd/MM/yyyy}"),
                ("Sĩ số tối đa", $"{classInfo.MaxCapacity} học viên"),
                ("Lịch học dự kiến", scheduleSummary)
            };

            await _businessEmailService.SendToTeacherAsync(
                teacherId,
                new BusinessEmailPayload(
                    $"Phân bổ lớp học mới: {classInfo.Name} — Toán Tư Duy",
                    title,
                    body,
                    details));
        }

        private async Task NotifyStudentAllocatedAsync(Classes classItem, StudentProfile student)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            const string parentTitle = "Học sinh đã được phân bổ lớp học";
            var parentBody = $"Học sinh {student.FullName} đã được xếp vào lớp {classItem.Name} (khai giảng {classItem.StartDate:dd/MM/yyyy}).";

            await _notificationService.PublishAsync(
                new NotificationMessage(
                    NotificationTypes.Enrollment,
                    parentTitle,
                    parentBody,
                    $"CLASS:{classItem.Id}:STUDENT_ALLOC:{student.Id}:{timestamp}"),
                AudienceSpec.StudentParent(student.Id));

            var details = new List<(string Label, string Value)>
            {
                ("Học sinh", student.FullName),
                ("Lớp học", classItem.Name),
                ("Ngày khai giảng", $"{classItem.StartDate:dd/MM/yyyy}")
            };

            await _businessEmailService.SendToStudentParentAsync(
                student.Id,
                new BusinessEmailPayload(
                    $"Thông báo phân bổ lớp học {classItem.Name} — Toán Tư Duy",
                    parentTitle,
                    parentBody,
                    details));

            if (classItem.TeacherId.HasValue)
            {
                const string teacherTitle = "Lớp có học sinh mới được phân bổ";
                var teacherBody = $"Học sinh {student.FullName} vừa được phân bổ vào lớp {classItem.Name}.";

                await _notificationService.PublishAsync(
                    new NotificationMessage(
                        NotificationTypes.Enrollment,
                        teacherTitle,
                        teacherBody,
                        $"CLASS:{classItem.Id}:TEACHER_STUDENT_ALLOC:{student.Id}:{timestamp}"),
                    AudienceSpec.Teacher(classItem.TeacherId.Value));

                await _businessEmailService.SendToTeacherAsync(
                    classItem.TeacherId.Value,
                    new BusinessEmailPayload(
                        $"Học sinh mới vào lớp {classItem.Name} — Toán Tư Duy",
                        teacherTitle,
                        teacherBody,
                        details));
            }
        }
    }
}
