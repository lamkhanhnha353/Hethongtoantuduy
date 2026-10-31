using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;

namespace ToanTuDuy.Services
{
    public class LessonService : ILessonService
    {
        private readonly ApplicationDbContext _context;

        public LessonService(ApplicationDbContext context)
        {
            _context = context;
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

        private static List<string> ReadSyllabusTitles(string? syllabus)
        {
            if (string.IsNullOrWhiteSpace(syllabus))
            {
                return new List<string>();
            }

            using var document = JsonDocument.Parse(syllabus);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            return document.RootElement
                .EnumerateArray()
                .Select(item =>
                    item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("title", out var title) &&
                    title.ValueKind == JsonValueKind.String
                        ? title.GetString()
                        : null)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Select(title => title!)
                .ToList();
        }
    }
}