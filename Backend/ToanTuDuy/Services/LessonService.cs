using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Extensions;
using ToanTuDuy.Models;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Services
{
    public class LessonService : ILessonService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public LessonService(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<LessonResponse>> GetAllLessonsAsync()
        {
            return await BuildLessonQuery(classId: null, status: null, keyword: null, ascendingByDate: false)
                .ToListAsync();
        }

        public async Task<PagedResult<LessonResponse>> GetPagedLessonsAsync(
            int page,
            int pageSize,
            int? classId = null,
            string? status = null,
            string? keyword = null,
            CancellationToken cancellationToken = default)
        {
            return await BuildLessonQuery(classId, status, keyword, ascendingByDate: false)
                .ToPagedResultAsync(page, pageSize, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<LessonResponse>> GetLessonsByClassIdAsync(int classId)
        {
            return await BuildLessonQuery(classId, status: null, keyword: null, ascendingByDate: true)
                .ToListAsync();
        }

        private IQueryable<LessonResponse> BuildLessonQuery(
            int? classId,
            string? status,
            string? keyword,
            bool ascendingByDate)
        {
            var query = _context.Lessons.AsNoTracking();

            if (classId.HasValue)
            {
                query = query.Where(l => l.ClassId == classId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Trim().Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                var normalizedStatus = status.Trim().ToUpperInvariant();
                query = query.Where(l => l.Status == normalizedStatus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(l =>
                    l.Class.Name.ToLower().Contains(kw) ||
                    (l.Title != null && l.Title.ToLower().Contains(kw)));
            }

            var ordered = ascendingByDate
                ? query.OrderBy(l => l.LessonDate).ThenBy(l => l.StartTime).ThenBy(l => l.Id)
                : query.OrderByDescending(l => l.LessonDate).ThenByDescending(l => l.StartTime).ThenByDescending(l => l.Id);

            return ordered.Select(l => new LessonResponse
            {
                Id = l.Id,
                ClassId = l.ClassId,
                ClassName = l.Class.Name,
                LessonDate = l.LessonDate,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Status = l.Status,
                IsImportan = l.IsImportan,
                AttendancesCount = l.Attendances.Count,
                LeaveRequestsCount = l.LeaveRequests.Count
            });
        }

        public async Task<LessonResponse?> GetLessonByIdAsync(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .Include(l => l.Attendances)
                .Include(l => l.LeaveRequests)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return null;
            }

            return new LessonResponse
            {
                Id = lesson.Id,
                ClassId = lesson.ClassId,
                ClassName = lesson.Class?.Name ?? string.Empty,
                LessonDate = lesson.LessonDate,
                StartTime = lesson.StartTime,
                EndTime = lesson.EndTime,
                Status = lesson.Status,
                IsImportan = lesson.IsImportan,
                AttendancesCount = lesson.Attendances.Count,
                LeaveRequestsCount = lesson.LeaveRequests.Count
            };
        }

        public async Task<IEnumerable<StudentLessonResponse>> GetLessonsByStudentIdAsync(int accountId, int studentId)
        {
            var isParentOfStudent = await _context.StudentProfiles
                .AnyAsync(s => s.Id == studentId && s.Parent!.AccountId == accountId);

            if (!isParentOfStudent)
            {
                throw ApiException.NotFound("Không tìm thấy học sinh.");
            }

            var activeClassIds = await _context.ClassStudents
                .Where(cs => cs.StudentId == studentId && cs.Status == "ACTIVE")
                .Select(cs => cs.ClassId)
                .ToListAsync();

            var lessons = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Class)
                    .ThenInclude(c => c.Teacher)
                .Include(l => l.Class)
                    .ThenInclude(c => c.ClassSchedules)
                .Where(l => activeClassIds.Contains(l.ClassId))
                .OrderBy(l => l.LessonDate)
                .ThenBy(l => l.StartTime)
                .ToListAsync();

            return lessons.Select(l => new StudentLessonResponse
            {
                LessonId = l.Id,
                ClassId = l.ClassId,
                ClassName = l.Class.Name,
                LessonDate = l.LessonDate,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Title = l.Title ?? "Buổi học",
                Status = l.Status,
                IsImportant = l.IsImportan,
                TeacherName = l.Class.Teacher?.FullName,
                Room = l.Class.ClassSchedules
                    .FirstOrDefault(schedule => schedule.DayOfWeek == (int)l.LessonDate.DayOfWeek)
                    ?.Room
            });
        }

        public async Task<IEnumerable<StudentRoadmapResponse>> GetRoadmapsByStudentIdAsync(int accountId, int studentId)
        {
            var student = await _context.StudentProfiles
                .AsNoTracking()
                .Where(s => s.Id == studentId && s.Parent!.AccountId == accountId)
                .Select(s => new { s.Id, s.FullName })
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw ApiException.NotFound("Không tìm thấy học sinh.");
            }

            var enrollments = await _context.ClassStudents
                .AsNoTracking()
                .Where(enrollment => enrollment.StudentId == studentId && enrollment.Status == "ACTIVE")
                .Include(enrollment => enrollment.Class)
                    .ThenInclude(@class => @class.CourseLevel)
                        .ThenInclude(level => level.Course)
                .ToListAsync();

            var classIds = enrollments.Select(enrollment => enrollment.ClassId).ToList();
            var lessonsByClass = await _context.Lessons
                .AsNoTracking()
                .Where(lesson => classIds.Contains(lesson.ClassId))
                .OrderBy(lesson => lesson.LessonDate)
                .ThenBy(lesson => lesson.StartTime)
                .ToListAsync();

            return enrollments.Select(enrollment =>
            {
                var courseClass = enrollment.Class;
                var courseLevel = courseClass.CourseLevel;
                var classLessons = lessonsByClass
                    .Where(lesson => lesson.ClassId == courseClass.Id)
                    .ToList();
                var syllabusTitles = ReadSyllabusTitles(courseLevel.Syllabus);
                var totalSessions = Math.Max(
                    courseLevel.TotalSessions,
                    Math.Max(classLessons.Count, syllabusTitles.Count));
                var nextScheduledSessionAssigned = false;
                var roadmapLessons = new List<StudentRoadmapLessonResponse>(totalSessions);

                for (var index = 0; index < totalSessions; index++)
                {
                    var lesson = index < classLessons.Count ? classLessons[index] : null;
                    var status = lesson?.Status ?? "LOCKED";
                    if (lesson?.Status == "SCHEDULED")
                    {
                        status = nextScheduledSessionAssigned ? "LOCKED" : "IN_PROGRESS";
                        nextScheduledSessionAssigned = true;
                    }

                    roadmapLessons.Add(new StudentRoadmapLessonResponse
                    {
                        SessionNumber = index + 1,
                        LessonId = lesson?.Id,
                        Title = lesson?.Title
                            ?? (index < syllabusTitles.Count ? syllabusTitles[index] : $"Buổi học {index + 1}"),
                        Status = status,
                        LessonDate = lesson?.LessonDate
                    });
                }

                return new StudentRoadmapResponse
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    ClassId = courseClass.Id,
                    ClassName = courseClass.Name,
                    CourseName = courseLevel.Course?.Name ?? courseLevel.Name,
                    CourseLevelName = courseLevel.Name,
                    TotalSessions = totalSessions,
                    CompletedSessions = classLessons.Count(lesson => lesson.Status == "COMPLETED"),
                    Lessons = roadmapLessons
                };
            }).ToList();
        }

        public async Task<LessonResponse> CreateLessonAsync(LessonRequest request)
        {
            var classExists = await _context.Classes.AnyAsync(c => c.Id == request.ClassId);
            if (!classExists)
            {
                throw new Exception("Lớp học không tồn tại.");
            }

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.EndTime <= request.StartTime)
            {
                throw new Exception("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
            }

            var lesson = new Lesson
            {
                ClassId = request.ClassId,
                LessonDate = request.LessonDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = request.Status,
                IsImportan = request.IsImportan
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            if (lesson.IsImportan)
            {
                await NotifySpecialLessonAsync(lesson);
            }

            return (await GetLessonByIdAsync(lesson.Id))!;
        }

        public async Task<bool> UpdateLessonAsync(int id, LessonRequest request)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                return false;
            }

            var classExists = await _context.Classes.AnyAsync(c => c.Id == request.ClassId);
            if (!classExists)
            {
                throw new Exception("Lớp học không tồn tại.");
            }

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.EndTime <= request.StartTime)
            {
                throw new Exception("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
            }

            var wasImportant = lesson.IsImportan;

            lesson.ClassId = request.ClassId;
            lesson.LessonDate = request.LessonDate;
            lesson.StartTime = request.StartTime;
            lesson.EndTime = request.EndTime;
            lesson.Status = request.Status;
            lesson.IsImportan = request.IsImportan;

            await _context.SaveChangesAsync();

            if (!wasImportant && lesson.IsImportan)
            {
                await NotifySpecialLessonAsync(lesson);
            }

            return true;
        }

        private async Task NotifySpecialLessonAsync(Lesson lesson)
        {
            var className = await _context.Classes
                .AsNoTracking()
                .Where(c => c.Id == lesson.ClassId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? $"Lớp #{lesson.ClassId}";

            var timeRange = lesson.StartTime.HasValue && lesson.EndTime.HasValue
                ? $" ({lesson.StartTime.Value:HH:mm} - {lesson.EndTime.Value:HH:mm})"
                : (lesson.StartTime.HasValue ? $" (lúc {lesson.StartTime.Value:HH:mm})" : string.Empty);

            var title = $"Thông báo đặc biệt: Buổi học quan trọng lớp {className}";
            var body = $"Lớp {className} có buổi học đặc biệt / quan trọng vào ngày {lesson.LessonDate:dd/MM/yyyy}{timeRange}. Vui lòng chú ý sắp xếp tham gia đầy đủ.";

            var audience = AudienceSpec.Union(
                AudienceSpec.ClassTeacher(lesson.ClassId),
                AudienceSpec.ClassParents(lesson.ClassId));
            audience.SendEmail = true;

            await _notificationService.PublishAsync(
                new NotificationMessage(
                    NotificationTypes.Special,
                    title,
                    body,
                    $"LESSON:{lesson.Id}:SPECIAL:{DateTime.UtcNow:yyyyMMddHHmmssfff}"),
                audience);
        }

        public async Task<bool> DeleteLessonAsync(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Attendances)
                .Include(l => l.LeaveRequests)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return false;
            }

            if (lesson.Attendances.Any())
            {
                throw new Exception("Không thể xóa buổi học đã có dữ liệu điểm danh.");
            }

            if (lesson.LeaveRequests.Any())
            {
                throw new Exception("Không thể xóa buổi học đã có đơn xin nghỉ phép.");
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
            return true;
        }

        private static List<string> ReadSyllabusTitles(string? syllabus)
        {
            if (string.IsNullOrWhiteSpace(syllabus))
            {
                return new List<string>();
            }

            try
            {
                using var document = JsonDocument.Parse(syllabus);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return new List<string>();
                }

                var titles = new List<string>();
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    string? resolvedTitle = null;
                    foreach (var prop in item.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, "title", StringComparison.OrdinalIgnoreCase))
                        {
                            resolvedTitle = prop.Value.ValueKind == JsonValueKind.String
                                ? prop.Value.GetString()
                                : prop.Value.ValueKind != JsonValueKind.Null && prop.Value.ValueKind != JsonValueKind.Undefined
                                    ? prop.Value.ToString()
                                    : null;
                            break;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(resolvedTitle))
                    {
                        titles.Add(resolvedTitle);
                    }
                }

                return titles;
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }
}
