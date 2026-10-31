
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
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly ApplicationDbContext _context;

        public LeaveRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LeaveRequestResponse> CreateLeaveRequestAsync(int accountId, LeaveRequestCreateRequest request)
        {
            var student = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.Id == request.StudentId && s.Parent!.AccountId == accountId);
            if (student == null) throw ApiException.NotFound("Không tìm thấy học sinh.");

            var lesson = await _context.Lessons.FindAsync(request.LessonId);
            if (lesson == null) throw ApiException.NotFound("Không tìm thấy buổi học.");

            // CHỐT CHẶN 0: BÉ CÓ HỌC LỚP NÀY KHÔNG? 
            var isEnrolled = await _context.ClassStudents.AnyAsync(cs =>
                cs.StudentId == request.StudentId &&
                cs.ClassId == lesson.ClassId &&
                cs.Status == "ACTIVE"); // Chỉ cho phép học sinh đang học (ACTIVE) xin nghỉ

            if (!isEnrolled)
                throw ApiException.BadRequest("Học sinh không có tên trong danh sách lớp của buổi học này!");
            // ==========================================


            // Chốt chặn 4: Buổi học phải đang ở trạng thái bình thường (SCHEDULED)
            if (lesson.Status != "SCHEDULED")
                throw ApiException.BadRequest("Chỉ được xin nghỉ cho các buổi học đang lên lịch!");

            var vietnamNow = DateTime.UtcNow.AddHours(7);
            if (!lesson.StartTime.HasValue)
            {
                throw ApiException.BadRequest("Buổi học chưa có giờ bắt đầu nên không thể gửi đơn xin nghỉ.");
            }

            var lessonStart = DateTime.SpecifyKind(lesson.StartTime.Value, DateTimeKind.Unspecified);
            if (lesson.LessonDate.Date < vietnamNow.Date || lessonStart <= vietnamNow)
            {
                throw ApiException.BadRequest("Không thể xin nghỉ cho buổi học trong quá khứ.");
            }

            if (lessonStart - vietnamNow < TimeSpan.FromHours(2))
            {
                throw ApiException.BadRequest(
                    "Đơn xin nghỉ cần được gửi trước giờ học ít nhất 2 tiếng. Vui lòng gọi Hotline nếu cần hỗ trợ.");
            }

            // Chốt chặn 3: Chống spam đơn (1 bé chỉ được gửi 1 đơn cho 1 buổi)
            var existingRequest = await _context.LeaveRequests
                .AnyAsync(lr =>
                    lr.StudentId == request.StudentId &&
                    lr.LessonId == request.LessonId &&
                    lr.Status != "CANCELLED");
            if (existingRequest)
                throw ApiException.Conflict("Bạn đã gửi đơn xin phép cho buổi học này rồi!");

            // Vượt qua chốt chặn -> Lưu đơn
            var leaveRequest = new LeaveRequest
            {
                StudentId = request.StudentId,
                LessonId = request.LessonId,
                Reason = request.Reason,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return await GetRequestByIdAsync(leaveRequest.Id);
        }

        public async Task<bool> CancelLeaveRequestAsync(int accountId, int requestId)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(request => request.Student)
                    .ThenInclude(student => student.Parent)
                .FirstOrDefaultAsync(request =>
                    request.Id == requestId &&
                    request.Student.Parent.AccountId == accountId);

            if (leaveRequest == null)
            {
                throw ApiException.NotFound("Không tìm thấy đơn xin nghỉ.");
            }

            if (leaveRequest.Status != "PENDING")
            {
                throw ApiException.Conflict("Chỉ có thể hủy đơn xin nghỉ đang chờ duyệt.");
            }

            leaveRequest.Status = "CANCELLED";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<LeaveRequestResponse>> GetPendingRequestsAsync()
        {
            return await _context.LeaveRequests
                .Include(lr => lr.Student)
                .Include(lr => lr.Lesson)
                .Where(lr => lr.Status == "PENDING")
                .Select(lr => new LeaveRequestResponse
                {
                    Id = lr.Id,
                    StudentId = lr.StudentId,
                    StudentName = lr.Student.FullName, // Giả sử StudentProfile có FullName
                    LessonId = lr.LessonId,
                    LessonTitle = lr.Lesson.Title ?? "Buổi học",
                    LessonDate = lr.Lesson.LessonDate,
                    Reason = lr.Reason,
                    Status = lr.Status,
                    CreatedAt = lr.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> ProcessLeaveRequestAsync(int requestId, bool isApproved)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(requestId);
            if (leaveRequest == null || leaveRequest.Status != "PENDING")
                return false;

            // Đổi trạng thái đơn xin nghỉ
            leaveRequest.Status = isApproved ? "APPROVED" : "REJECTED";

            // NẾU ADMIN DUYỆT -> TỰ ĐỘNG LƯU ĐIỂM DANH VẮNG CÓ PHÉP (EXCUSED)
            if (isApproved)
            {
                // Kiểm tra xem đã có record điểm danh chưa (phòng trường hợp lỗi sinh data)
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.LessonId == leaveRequest.LessonId && a.StudentId == leaveRequest.StudentId);

                if (existingAttendance != null)
                {
                    existingAttendance.Status = "EXCUSED";
                    existingAttendance.TeacherNotes = $"Xin phép nghỉ: {leaveRequest.Reason}";
                }
                else
                {
                    var attendance = new Attendance
                    {
                        LessonId = leaveRequest.LessonId,
                        StudentId = leaveRequest.StudentId,
                        Status = "EXCUSED", // Đánh dấu vắng có phép
                        TeacherNotes = $"Xin phép nghỉ: {leaveRequest.Reason}"
                    };
                    _context.Attendances.Add(attendance);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Hàm phụ dùng trong class
        private async Task<LeaveRequestResponse> GetRequestByIdAsync(int id)
        {
            var lr = await _context.LeaveRequests
               .Include(l => l.Student)
               .Include(l => l.Lesson)
               .FirstOrDefaultAsync(l => l.Id == id);
            if (lr == null) return null;

            return new LeaveRequestResponse
            {
                Id = lr.Id,
                StudentId = lr.StudentId,
                StudentName = lr.Student.FullName,
                LessonId = lr.LessonId,
                LessonTitle = lr.Lesson.Title ?? "Buổi học",
                LessonDate = lr.Lesson.LessonDate,
                Reason = lr.Reason,
                Status = lr.Status,
                CreatedAt = lr.CreatedAt
            };
        }

        public async Task<IEnumerable<LeaveRequestResponse>> GetRequestsByStudentIdAsync(int accountId, int studentId)
        {
            var isParentOfStudent = await _context.StudentProfiles
                .AnyAsync(s => s.Id == studentId && s.Parent!.AccountId == accountId);
            if (!isParentOfStudent)
            {
                throw ApiException.NotFound("Không tìm thấy học sinh.");
            }

            return await _context.LeaveRequests
                .Include(lr => lr.Student)
                .Include(lr => lr.Lesson)
                .Where(lr => lr.StudentId == studentId)
                .OrderByDescending(lr => lr.CreatedAt)
                .Select(lr => new LeaveRequestResponse
                {
                    Id = lr.Id,
                    StudentId = lr.StudentId,
                    StudentName = lr.Student.FullName,
                    LessonId = lr.LessonId,
                    LessonTitle = lr.Lesson.Title ?? "Buổi học",
                    LessonDate = lr.Lesson.LessonDate,
                    Reason = lr.Reason,
                    Status = lr.Status,
                    CreatedAt = lr.CreatedAt
                })
                .ToListAsync();
        }
    }
}