
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
                // Lấy tên Level và tên Giáo viên nếu có
                CourseLevelName = c.CourseLevel != null ? c.CourseLevel.Name : "",
                TeacherName = c.Teacher != null ? c.Teacher.FullName : null
            };
        }

        public async Task<IEnumerable<ClassResponse>> GetAllClassesAsync()
        {
            var classes = await _context.Classes
                .Include(c => c.CourseLevel)
                .Include(c => c.Teacher) // Join với bảng giáo viên
                .ToListAsync();

            return classes.Select(MapToResponse);
        }

        public async Task<IEnumerable<ClassResponse>> GetClassesByCourseLevelIdAsync(int courseLevelId)
        {
            var classes = await _context.Classes
                .Include(c => c.CourseLevel)
                .Include(c => c.Teacher)
                .Where(c => c.CourseLevelId == courseLevelId)
                .ToListAsync();

            return classes.Select(MapToResponse);
        }

        public async Task<ClassResponse> GetClassByIdAsync(int id)
        {
            var c = await _context.Classes
                .Include(cls => cls.CourseLevel)
                .Include(cls => cls.Teacher)
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
                CreatedAt = DateTime.UtcNow // (Hoặc dùng hàm ConvertTime nếu bạn đã chốt xài giờ VN)
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
    }
}