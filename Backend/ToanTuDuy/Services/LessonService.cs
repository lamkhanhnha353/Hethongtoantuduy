using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class LessonService : ILessonService
    {
        private readonly ApplicationDbContext _context;

        public LessonService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LessonResponse>> GetAllLessonsAsync()
        {
            return await _context.Lessons
                .Include(l => l.Class)
                .OrderByDescending(l => l.LessonDate)
                .Select(l => new LessonResponse
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
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<LessonResponse>> GetLessonsByClassIdAsync(int classId)
        {
            return await _context.Lessons
                .Include(l => l.Class)
                .Where(l => l.ClassId == classId)
                .OrderBy(l => l.LessonDate)
                .Select(l => new LessonResponse
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
                })
                .ToListAsync();
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

        public async Task<LessonResponse?> GetLessonByIdAsync(int id)
        {
            var l = await _context.Lessons
                .Include(l => l.Class)
                .Include(l => l.Attendances)
                .Include(l => l.LeaveRequests)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (l == null) return null;

            return new LessonResponse
            {
                Id = l.Id,
                ClassId = l.ClassId,
                ClassName = l.Class?.Name ?? string.Empty,
                LessonDate = l.LessonDate,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Status = l.Status,
                IsImportan = l.IsImportan,
                AttendancesCount = l.Attendances.Count,
                LeaveRequestsCount = l.LeaveRequests.Count
            };
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

            return (await GetLessonByIdAsync(lesson.Id))!;
        }

        public async Task<bool> UpdateLessonAsync(int id, LessonRequest request)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return false;

            var classExists = await _context.Classes.AnyAsync(c => c.Id == request.ClassId);
            if (!classExists)
            {
                throw new Exception("Lớp học không tồn tại.");
            }

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.EndTime <= request.StartTime)
            {
                throw new Exception("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
            }

            lesson.ClassId = request.ClassId;
            lesson.LessonDate = request.LessonDate;
            lesson.StartTime = request.StartTime;
            lesson.EndTime = request.EndTime;
            lesson.Status = request.Status;
            lesson.IsImportan = request.IsImportan;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLessonAsync(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Attendances)
                .Include(l => l.LeaveRequests)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return false;

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
    }
}
