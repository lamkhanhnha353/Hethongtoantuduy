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
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LessonAttendanceDetailResponse?> GetAttendanceByLessonIdAsync(int lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return null;

            // Lấy danh sách học sinh đang học trong lớp này (Status = ACTIVE)
            var classStudents = await _context.ClassStudents
                .Include(cs => cs.Student)
                .Where(cs => cs.ClassId == lesson.ClassId && cs.Status == "ACTIVE")
                .ToListAsync();

            // Lấy các bản ghi điểm danh đã có của buổi học
            var attendances = await _context.Attendances
                .Where(a => a.LessonId == lessonId)
                .ToDictionaryAsync(a => a.StudentId);

            // Lấy các đơn xin nghỉ phép của học sinh trong buổi học
            var leaveRequests = await _context.LeaveRequests
                .Where(lr => lr.LessonId == lessonId)
                .ToDictionaryAsync(lr => lr.StudentId);

            var studentResponses = new List<StudentAttendanceItemResponse>();

            foreach (var cs in classStudents)
            {
                var student = cs.Student;
                if (student == null) continue;

                attendances.TryGetValue(student.Id, out var att);
                leaveRequests.TryGetValue(student.Id, out var leave);

                studentResponses.Add(new StudentAttendanceItemResponse
                {
                    AttendanceId = att?.Id,
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    Avatar = student.Avatar,
                    Gender = student.Gender,
                    Status = att != null ? att.Status : "UNMARKED",
                    TeacherNotes = att?.TeacherNotes,
                    HasLeaveRequest = leave != null,
                    LeaveReason = leave?.Reason,
                    LeaveRequestStatus = leave?.Status
                });
            }

            // Sắp xếp theo tên học sinh
            studentResponses = studentResponses.OrderBy(s => s.StudentName).ToList();

            return new LessonAttendanceDetailResponse
            {
                LessonId = lesson.Id,
                ClassId = lesson.ClassId,
                ClassName = lesson.Class?.Name ?? string.Empty,
                LessonDate = lesson.LessonDate,
                StartTime = lesson.StartTime,
                EndTime = lesson.EndTime,
                LessonStatus = lesson.Status,
                TotalStudents = studentResponses.Count,
                PresentCount = studentResponses.Count(s => s.Status == "PRESENT"),
                AbsentCount = studentResponses.Count(s => s.Status == "ABSENT"),
                LateCount = studentResponses.Count(s => s.Status == "LATE"),
                ExcusedCount = studentResponses.Count(s => s.Status == "EXCUSED"),
                UnmarkedCount = studentResponses.Count(s => s.Status == "UNMARKED"),
                Students = studentResponses
            };
        }

        public async Task<LessonAttendanceDetailResponse> SaveBatchAttendanceAsync(BatchAttendanceRequest request)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.Id == request.LessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy buổi học với ID {request.LessonId}.");
            }

            // Lấy danh sách ID học sinh thuộc lớp
            var validStudentIds = await _context.ClassStudents
                .Where(cs => cs.ClassId == lesson.ClassId && cs.Status == "ACTIVE")
                .Select(cs => cs.StudentId)
                .ToListAsync();

            var validStudentSet = new HashSet<int>(validStudentIds);

            // Kiểm tra các StudentId gửi lên
            var invalidIds = request.Attendances
                .Select(a => a.StudentId)
                .Where(id => !validStudentSet.Contains(id))
                .Distinct()
                .ToList();

            if (invalidIds.Any())
            {
                throw new InvalidOperationException($"Học sinh có ID [{string.Join(", ", invalidIds)}] không thuộc lớp học này.");
            }

            // Lấy danh sách điểm danh hiện tại của buổi học
            var existingAttendances = await _context.Attendances
                .Where(a => a.LessonId == request.LessonId)
                .ToListAsync();

            var attendanceMap = existingAttendances.ToDictionary(a => a.StudentId);

            foreach (var item in request.Attendances)
            {
                var normalizedStatus = item.Status?.Trim().ToUpper() ?? "PRESENT";

                if (attendanceMap.TryGetValue(item.StudentId, out var existing))
                {
                    // Cập nhật bản ghi điểm danh đã có
                    existing.Status = normalizedStatus;
                    existing.TeacherNotes = item.TeacherNotes;
                }
                else
                {
                    // Tạo bản ghi điểm danh mới
                    var newAttendance = new Attendance
                    {
                        LessonId = request.LessonId,
                        StudentId = item.StudentId,
                        Status = normalizedStatus,
                        TeacherNotes = item.TeacherNotes
                    };
                    _context.Attendances.Add(newAttendance);
                }
            }

            await _context.SaveChangesAsync();

            // Trả về dữ liệu chi tiết sau khi đã cập nhật
            var result = await GetAttendanceByLessonIdAsync(request.LessonId);
            return result!;
        }

        public async Task<StudentAttendanceItemResponse> SaveSingleAttendanceAsync(int lessonId, AttendanceItemDto dto)
        {
            var lesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy buổi học với ID {lessonId}.");
            }

            // Kiểm tra học sinh có thuộc lớp không
            var isStudentInClass = await _context.ClassStudents
                .AnyAsync(cs => cs.ClassId == lesson.ClassId && cs.StudentId == dto.StudentId && cs.Status == "ACTIVE");

            if (!isStudentInClass)
            {
                throw new InvalidOperationException($"Học sinh với ID {dto.StudentId} không thuộc lớp của buổi học này.");
            }

            var student = await _context.StudentProfiles.FindAsync(dto.StudentId);
            if (student == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy học sinh với ID {dto.StudentId}.");
            }

            var normalizedStatus = dto.Status?.Trim().ToUpper() ?? "PRESENT";

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.LessonId == lessonId && a.StudentId == dto.StudentId);

            if (attendance != null)
            {
                attendance.Status = normalizedStatus;
                attendance.TeacherNotes = dto.TeacherNotes;
            }
            else
            {
                attendance = new Attendance
                {
                    LessonId = lessonId,
                    StudentId = dto.StudentId,
                    Status = normalizedStatus,
                    TeacherNotes = dto.TeacherNotes
                };
                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();

            var leave = await _context.LeaveRequests
                .FirstOrDefaultAsync(lr => lr.LessonId == lessonId && lr.StudentId == dto.StudentId);

            return new StudentAttendanceItemResponse
            {
                AttendanceId = attendance.Id,
                StudentId = student.Id,
                StudentName = student.FullName,
                Avatar = student.Avatar,
                Gender = student.Gender,
                Status = attendance.Status,
                TeacherNotes = attendance.TeacherNotes,
                HasLeaveRequest = leave != null,
                LeaveReason = leave?.Reason,
                LeaveRequestStatus = leave?.Status
            };
        }

        public async Task<IEnumerable<StudentAttendanceHistoryResponse>> GetStudentAttendanceHistoryAsync(int studentId, int? classId = null)
        {
            var query = _context.Attendances
                .Include(a => a.Lesson)
                .ThenInclude(l => l.Class)
                .Where(a => a.StudentId == studentId);

            if (classId.HasValue)
            {
                query = query.Where(a => a.Lesson.ClassId == classId.Value);
            }

            return await query
                .OrderByDescending(a => a.Lesson.LessonDate)
                .Select(a => new StudentAttendanceHistoryResponse
                {
                    AttendanceId = a.Id,
                    LessonId = a.LessonId,
                    ClassId = a.Lesson.ClassId,
                    ClassName = a.Lesson.Class.Name,
                    LessonDate = a.Lesson.LessonDate,
                    StartTime = a.Lesson.StartTime,
                    EndTime = a.Lesson.EndTime,
                    Status = a.Status,
                    TeacherNotes = a.TeacherNotes
                })
                .ToListAsync();
        }
    }
}
