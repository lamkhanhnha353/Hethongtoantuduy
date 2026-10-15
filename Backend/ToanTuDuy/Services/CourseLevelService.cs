
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
    public class CourseLevelService : ICourseLevelService
    {
        private readonly ApplicationDbContext _context;

        public CourseLevelService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CourseLevelResponse>> GetLevelsByCourseIdAsync(int courseId)
        {
            return await _context.CourseLevels
                .Where(cl => cl.CourseId == courseId)
                .Select(cl => new CourseLevelResponse
                {
                    Id = cl.Id,
                    CourseId = cl.CourseId, // Nhớ thêm thuộc tính này vào CourseLevelResponse
                    Code = cl.Code,
                    Name = cl.Name,
                    Fee = cl.Fee,
                    TotalSessions = cl.TotalSessions,
                    OrderIndex = cl.OrderIndex,
                    Status = cl.Status
                })
                .ToListAsync();
        }

        public async Task<CourseLevelResponse> GetLevelByIdAsync(int id)
        {
            var cl = await _context.CourseLevels.FindAsync(id);
            if (cl == null) return null;

            return new CourseLevelResponse
            {
                Id = cl.Id,
                CourseId = cl.CourseId,
                Code = cl.Code,
                Name = cl.Name,
                Fee = cl.Fee,
                TotalSessions = cl.TotalSessions,
                OrderIndex = cl.OrderIndex,
                Status = cl.Status
            };
        }

        public async Task<CourseLevelResponse> CreateLevelAsync(CourseLevelRequest request)
        {
            // Kiểm tra xem CourseId có tồn tại không
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
            if (!courseExists) throw new Exception("Khóa học cha không tồn tại!");

            var level = new CourseLevel
            {
                CourseId = request.CourseId,
                Code = request.Code,
                Name = request.Name,
                Fee = request.Fee,
                TotalSessions = request.TotalSessions,
                OrderIndex = request.OrderIndex,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.CourseLevels.Add(level);
            await _context.SaveChangesAsync();

            return await GetLevelByIdAsync(level.Id);
        }

        public async Task<bool> UpdateLevelAsync(int id, CourseLevelRequest request)
        {
            var level = await _context.CourseLevels.FindAsync(id);
            if (level == null) return false;

            level.Code = request.Code;
            level.Name = request.Name;
            level.Fee = request.Fee;
            level.TotalSessions = request.TotalSessions;
            level.OrderIndex = request.OrderIndex;
            level.Status = request.Status;
            level.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLevelAsync(int id)
        {
            var level = await _context.CourseLevels.Include(l => l.Classes).FirstOrDefaultAsync(l => l.Id == id);
            if (level == null) return false;

            // Chặn xóa nếu Level này đã có Lớp học được mở
            if (level.Classes.Any())
                throw new Exception("Không thể xóa Cấp độ đang có Lớp học (Classes).");

            _context.CourseLevels.Remove(level);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}