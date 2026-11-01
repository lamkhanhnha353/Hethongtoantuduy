using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
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
                    Status = l.Status,
                    IsImportan = l.IsImportan,
                    AttendancesCount = l.Attendances.Count,
                    LeaveRequestsCount = l.LeaveRequests.Count
                })
                .ToListAsync();
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
