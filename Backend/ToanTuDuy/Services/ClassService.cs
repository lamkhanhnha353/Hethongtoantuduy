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
    public class ClassService : IClassService
    {
        private readonly ApplicationDbContext _context;

        public ClassService(ApplicationDbContext context)
        {
            _context = context;
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

                // Đếm số lượng học sinh ACTIVE từ danh sách ClassStudents
                // Đếm số lượng học sinh đã thanh toán (ACTIVE) và đang giữ chỗ (PENDING)
                ActiveStudentsCount = c.ClassStudents != null ? c.ClassStudents.Count(cs => cs.Status == "ACTIVE" || cs.Status == "PENDING") : 0
            };
        }

        public async Task<IEnumerable<ClassResponse>> GetAllClassesAsync()
        {
            var classes = await _context.Classes
                .Include(c => c.CourseLevel)
                    .ThenInclude(cl => cl.Course)
                .Include(c => c.Teacher) // Join với bảng giáo viên
                .Include(c => c.ClassStudents) // Include để lấy dữ liệu tính slot
                .ToListAsync();

            return classes.Select(MapToResponse);
        }

        public async Task<IEnumerable<ClassResponse>> GetClassesByCourseLevelIdAsync(int courseLevelId)
        {
            var classes = await _context.Classes
                .Include(c => c.CourseLevel)
                    .ThenInclude(cl => cl.Course)
                .Include(c => c.Teacher)
                .Include(c => c.ClassStudents) // Include để lấy dữ liệu tính slot
                .Where(c => c.CourseLevelId == courseLevelId)
                .ToListAsync();

            return classes.Select(MapToResponse);
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

            return await GetClassByIdAsync(newClass.Id);
        }

        public async Task<bool> UpdateClassAsync(int id, ClassRequest request)
        {
            var c = await _context.Classes.FindAsync(id);
            if (c == null) return false;

            c.CourseLevelId = request.CourseLevelId;
            c.TeacherId = request.TeacherId;
            c.Name = request.Name;
            c.MaxCapacity = request.MaxCapacity;
            c.StartDate = request.StartDate;
            c.EndDate = request.EndDate;
            c.Status = request.Status;
            c.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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
            var classStudents = await _context.ClassStudents
                .Include(cs => cs.Student)
                    .ThenInclude(s => s.Parent)
                        .ThenInclude(p => p.Account)
                .Where(cs => cs.ClassId == classId)
                .OrderByDescending(cs => cs.JoinedAt)
                .ToListAsync();

            return classStudents.Select(cs => new ClassStudentResponse
            {
                Id = cs.Id,
                StudentId = cs.StudentId,
                StudentName = cs.Student != null ? cs.Student.FullName : "",
                Avatar = cs.Student?.Avatar,
                TotalPoints = cs.Student?.TotalPoints ?? 0,
                Stars = cs.Student?.Stars ?? 0,
                StreakDays = cs.Student?.StreakDays ?? 0,
                ParentId = cs.Student?.ParentId,
                ParentName = cs.Student?.Parent != null ? cs.Student.Parent.FullName : null,
                ParentPhone = cs.Student?.Parent?.Account?.PhoneNumber,
                ParentEmail = cs.Student?.Parent?.Account?.Email,
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
            var currentCount = classItem.ClassStudents.Count(cs => cs.Status == "ACTIVE" || cs.Status == "PENDING");
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
    }
}
