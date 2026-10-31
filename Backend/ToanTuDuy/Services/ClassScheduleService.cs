
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class ClassScheduleService : IClassScheduleService
    {
        private readonly ApplicationDbContext _context;

        public ClassScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClassScheduleResponse>> GetSchedulesByClassIdAsync(int classId)
        {
            return await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new ClassScheduleResponse
                {
                    Id = cs.Id,
                    ClassId = cs.ClassId,
                    ClassName = cs.Class.Name,
                    DayOfWeek = cs.DayOfWeek,
                    StartTime = cs.StartTime,
                    EndTime = cs.EndTime,
                    Room = cs.Room,
                    CreatedAt = cs.CreatedAt,
                    UpdatedAt = cs.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<ClassScheduleResponse> GetScheduleByIdAsync(int id)
        {
            var cs = await _context.ClassSchedules.Include(c => c.Class).FirstOrDefaultAsync(c => c.Id == id);
            if (cs == null) return null;

            return new ClassScheduleResponse
            {
                Id = cs.Id,
                ClassId = cs.ClassId,
                ClassName = cs.Class.Name,
                DayOfWeek = cs.DayOfWeek,
                StartTime = cs.StartTime,
                EndTime = cs.EndTime,
                Room = cs.Room,
                CreatedAt = cs.CreatedAt,
                UpdatedAt = cs.UpdatedAt
            };
        }

        public async Task<ClassScheduleResponse> CreateScheduleAsync(ClassScheduleRequest request)
        {
            await ValidateScheduleAsync(request);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var schedule = new ClassSchedule
            {
                ClassId = request.ClassId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Room = request.Room,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            await GenerateLessonsAndCalculateEndDateAsync(request.ClassId);
            await transaction.CommitAsync();

            return await GetScheduleByIdAsync(schedule.Id);
        }

        public async Task<bool> UpdateScheduleAsync(int id, ClassScheduleRequest request)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null) return false;

            await ValidateScheduleAsync(request, id);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var previousClassId = schedule.ClassId;

            // Vượt qua kiểm duyệt, tiến hành cập nhật dữ liệu
            schedule.ClassId = request.ClassId;
            schedule.DayOfWeek = request.DayOfWeek;
            schedule.StartTime = request.StartTime;
            schedule.EndTime = request.EndTime;
            schedule.Room = request.Room;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Gọi thuật toán tự động rải lại buổi học vật lý vào bảng LESSON
            if (previousClassId != request.ClassId)
            {
                await GenerateLessonsAndCalculateEndDateAsync(previousClassId);
            }
            await GenerateLessonsAndCalculateEndDateAsync(request.ClassId);
            await transaction.CommitAsync();

            return true;
        }

        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null) return false;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            int classId = schedule.ClassId;
            _context.ClassSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            await GenerateLessonsAndCalculateEndDateAsync(classId);
            await transaction.CommitAsync();
            return true;
        }


        // THUẬT TOÁN TÍNH TỰ ĐỘNG END_DATE CỦA LỚP

        // Hàm này sẽ Sinh ra danh sách Buổi học vật lý (Lesson) và Tính toán ngày Bế giảng (EndDate)
        private async Task GenerateLessonsAndCalculateEndDateAsync(int classId)
        {
            var targetClass = await _context.Classes
                .Include(c => c.CourseLevel)
                .Include(c => c.ClassSchedules)
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (targetClass == null || targetClass.CourseLevel == null)
            {
                return;
            }

            var today = DateTime.UtcNow.Date;
            var schedules = targetClass.ClassSchedules
                .OrderBy(schedule => schedule.DayOfWeek)
                .ThenBy(schedule => schedule.StartTime)
                .ToList();
            var futureScheduledLessons = targetClass.Lessons
                .Where(lesson => lesson.Status == "SCHEDULED" && lesson.LessonDate.Date >= today)
                .OrderBy(lesson => lesson.LessonDate)
                .ThenBy(lesson => lesson.StartTime)
                .ThenBy(lesson => lesson.Id)
                .ToList();
            var lessonIds = futureScheduledLessons.Select(lesson => lesson.Id).ToList();
            var lessonsWithLeaveRequests = await _context.LeaveRequests
                .AsNoTracking()
                .Where(request =>
                    lessonIds.Contains(request.LessonId) &&
                    request.Status != "CANCELLED")
                .Select(request => request.LessonId)
                .Distinct()
                .ToHashSetAsync();

            var syllabus = string.IsNullOrWhiteSpace(targetClass.CourseLevel.Syllabus)
                ? new List<Dictionary<string, string>>()
                : System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(
                    targetClass.CourseLevel.Syllabus) ?? new List<Dictionary<string, string>>();

            var fixedLessons = targetClass.Lessons
                .Where(lesson =>
                    (lesson.Status != "SCHEDULED" &&
                     lesson.Status != "CANCELED" &&
                     lesson.Status != "CANCELLED") ||
                    lesson.LessonDate.Date < today)
                .ToList();
            var completedSessionCount = fixedLessons.Count;
            var remainingSessions = Math.Max(
                0,
                targetClass.CourseLevel.TotalSessions - completedSessionCount);
            var expectedLessons = new List<Lesson>();
            var currentDate = targetClass.StartDate.Date > today
                ? targetClass.StartDate.Date
                : today;

            while (expectedLessons.Count < remainingSessions && schedules.Count > 0)
            {
                foreach (var schedule in schedules.Where(
                    item => item.DayOfWeek == (int)currentDate.DayOfWeek))
                {
                    if (expectedLessons.Count >= remainingSessions)
                    {
                        break;
                    }

                    var sessionIndex = completedSessionCount + expectedLessons.Count;
                    var title = sessionIndex < syllabus.Count &&
                                syllabus[sessionIndex].TryGetValue("title", out var syllabusTitle)
                        ? syllabusTitle
                        : $"Buổi học {sessionIndex + 1}";

                    expectedLessons.Add(new Lesson
                    {
                        ClassId = targetClass.Id,
                        Title = title,
                        LessonDate = currentDate,
                        StartTime = currentDate.Add(schedule.StartTime),
                        EndTime = currentDate.Add(schedule.EndTime),
                        Status = "SCHEDULED",
                        IsImportan = false
                    });
                }

                currentDate = currentDate.AddDays(1);
            }

            for (var index = 0; index < futureScheduledLessons.Count; index++)
            {
                var existingLesson = futureScheduledLessons[index];
                if (index >= expectedLessons.Count)
                {
                    if (lessonsWithLeaveRequests.Contains(existingLesson.Id))
                    {
                        throw ApiException.Conflict(
                            "Không thể thay đổi lịch vì buổi học sắp bị hủy đã có đơn xin nghỉ. Hãy xử lý đơn trước.");
                    }

                    existingLesson.Status = "CANCELED";
                    continue;
                }

                var expectedLesson = expectedLessons[index];
                var scheduleChanged =
                    existingLesson.LessonDate.Date != expectedLesson.LessonDate.Date ||
                    existingLesson.StartTime != expectedLesson.StartTime ||
                    existingLesson.EndTime != expectedLesson.EndTime ||
                    existingLesson.Title != expectedLesson.Title;
                if (scheduleChanged && lessonsWithLeaveRequests.Contains(existingLesson.Id))
                {
                    throw ApiException.Conflict(
                        "Không thể thay đổi ngày, giờ hoặc chủ đề của buổi học đã có đơn xin nghỉ.");
                }

                existingLesson.LessonDate = expectedLesson.LessonDate;
                existingLesson.StartTime = expectedLesson.StartTime;
                existingLesson.EndTime = expectedLesson.EndTime;
                existingLesson.Title = expectedLesson.Title;
            }

            if (expectedLessons.Count > futureScheduledLessons.Count)
            {
                _context.Lessons.AddRange(expectedLessons.Skip(futureScheduledLessons.Count));
            }

            var activeLessons = fixedLessons.Concat(expectedLessons).ToList();
            targetClass.EndDate = activeLessons.Count == 0
                ? null
                : activeLessons.Max(lesson => lesson.LessonDate);

            await _context.SaveChangesAsync();
        }

        private async Task ValidateScheduleAsync(
            ClassScheduleRequest request,
            int? excludedScheduleId = null)
        {
            if (request.DayOfWeek < 0 || request.DayOfWeek > 6)
            {
                throw ApiException.BadRequest("Ngày trong tuần phải từ 0 đến 6.");
            }

            if (request.EndTime <= request.StartTime)
            {
                throw ApiException.BadRequest("Giờ kết thúc phải muộn hơn giờ bắt đầu.");
            }

            var targetClass = await _context.Classes
                .FirstOrDefaultAsync(courseClass => courseClass.Id == request.ClassId);
            if (targetClass == null)
            {
                throw ApiException.NotFound("Lớp học không tồn tại.");
            }

            var schedulesForClass = _context.ClassSchedules
                .Where(schedule => schedule.ClassId == request.ClassId);
            if (excludedScheduleId.HasValue)
            {
                schedulesForClass = schedulesForClass.Where(
                    schedule => schedule.Id != excludedScheduleId.Value);
            }

            if (await schedulesForClass.AnyAsync(schedule =>
                    schedule.DayOfWeek == request.DayOfWeek &&
                    request.StartTime < schedule.EndTime &&
                    request.EndTime > schedule.StartTime))
            {
                throw ApiException.Conflict("Lớp học đã có ca khác bị trùng giờ.");
            }

            if (!string.IsNullOrWhiteSpace(request.Room))
            {
                var schedulesForRoom = _context.ClassSchedules
                    .Where(schedule =>
                        schedule.Room == request.Room &&
                        schedule.DayOfWeek == request.DayOfWeek);
                if (excludedScheduleId.HasValue)
                {
                    schedulesForRoom = schedulesForRoom.Where(
                        schedule => schedule.Id != excludedScheduleId.Value);
                }

                if (await schedulesForRoom.AnyAsync(schedule =>
                        request.StartTime < schedule.EndTime &&
                        request.EndTime > schedule.StartTime))
                {
                    throw ApiException.Conflict(
                        $"Phòng {request.Room} đã được lớp khác sử dụng vào khung giờ này.");
                }
            }

            if (targetClass.TeacherId.HasValue)
            {
                var schedulesForTeacher = _context.ClassSchedules
                    .Include(schedule => schedule.Class)
                    .Where(schedule =>
                        schedule.Class.TeacherId == targetClass.TeacherId &&
                        schedule.DayOfWeek == request.DayOfWeek);
                if (excludedScheduleId.HasValue)
                {
                    schedulesForTeacher = schedulesForTeacher.Where(
                        schedule => schedule.Id != excludedScheduleId.Value);
                }

                if (await schedulesForTeacher.AnyAsync(schedule =>
                        request.StartTime < schedule.EndTime &&
                        request.EndTime > schedule.StartTime))
                {
                    throw ApiException.Conflict(
                        "Giáo viên phụ trách lớp này đã có lịch dạy khác bị trùng giờ.");
                }
            }

        }
    }
}