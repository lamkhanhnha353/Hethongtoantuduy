using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            var levels = await _context.CourseLevels
                .Include(cl => cl.Course)
                .Where(cl => cl.CourseId == courseId)
                .ToListAsync();

            return levels.Select(cl => new CourseLevelResponse
            {
                Id = cl.Id,
                CourseId = cl.CourseId,
                Code = cl.Code,
                Name = cl.Name,
                Fee = cl.Fee,
                TotalSessions = cl.TotalSessions,
                OrderIndex = cl.OrderIndex,
                Status = cl.Status,

                CourseName = cl.Course?.Name,
                Description = cl.Course?.Description,
                MinAge = cl.Course?.MinAge ?? 0,
                MaxAge = cl.Course?.MaxAge ?? 0,
                Rating = cl.Course?.Rating ?? 5.0,
                Reviews = cl.Course?.Reviews ?? 0,
                VideoUrl = cl.Course?.VideoUrl,
                Benefits = string.IsNullOrEmpty(cl.Course?.Benefits) ? null : JsonSerializer.Deserialize<List<string>>(cl.Course.Benefits, (JsonSerializerOptions)null),
                FacilityImages = string.IsNullOrEmpty(cl.Course?.FacilityImages) ? null : JsonSerializer.Deserialize<List<string>>(cl.Course.FacilityImages, (JsonSerializerOptions)null),
                Syllabus = string.IsNullOrEmpty(cl.Syllabus) ? null : JsonSerializer.Deserialize<object>(cl.Syllabus, (JsonSerializerOptions)null),
                Faqs = string.IsNullOrEmpty(cl.Faqs) ? null : JsonSerializer.Deserialize<object>(cl.Faqs, (JsonSerializerOptions)null)
            }).ToList();
        }

        public async Task<CourseLevelResponse> GetLevelByIdAsync(int id)
        {
            var cl = await _context.CourseLevels
                .Include(x => x.Course)
                .FirstOrDefaultAsync(x => x.Id == id);

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
                Status = cl.Status,

                CourseName = cl.Course?.Name,
                Description = cl.Course?.Description,
                MinAge = cl.Course?.MinAge ?? 0,
                MaxAge = cl.Course?.MaxAge ?? 0,
                Rating = cl.Course?.Rating ?? 5.0,
                Reviews = cl.Course?.Reviews ?? 0,
                VideoUrl = cl.Course?.VideoUrl,
                Benefits = string.IsNullOrEmpty(cl.Course?.Benefits) ? null : JsonSerializer.Deserialize<List<string>>(cl.Course.Benefits, (JsonSerializerOptions)null),
                FacilityImages = string.IsNullOrEmpty(cl.Course?.FacilityImages) ? null : JsonSerializer.Deserialize<List<string>>(cl.Course.FacilityImages, (JsonSerializerOptions)null),
                Syllabus = string.IsNullOrEmpty(cl.Syllabus) ? null : JsonSerializer.Deserialize<object>(cl.Syllabus, (JsonSerializerOptions)null),
                Faqs = string.IsNullOrEmpty(cl.Faqs) ? null : JsonSerializer.Deserialize<object>(cl.Faqs, (JsonSerializerOptions)null)
            };
        }

        public async Task<CourseLevelResponse> CreateLevelAsync(CourseLevelRequest request)
        {
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
                Syllabus = request.Syllabus != null ? JsonSerializer.Serialize(request.Syllabus, (JsonSerializerOptions)null) : null,
                Faqs = request.Faqs != null ? JsonSerializer.Serialize(request.Faqs, (JsonSerializerOptions)null) : null,
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
            level.Syllabus = request.Syllabus != null ? JsonSerializer.Serialize(request.Syllabus, (JsonSerializerOptions)null) : null;
            level.Faqs = request.Faqs != null ? JsonSerializer.Serialize(request.Faqs, (JsonSerializerOptions)null) : null;
            level.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLevelAsync(int id)
        {
            var level = await _context.CourseLevels.Include(l => l.Classes).FirstOrDefaultAsync(l => l.Id == id);
            if (level == null) return false;

            if (level.Classes.Any())
                throw new Exception("Không thể xóa Cấp độ đang có Lớp học (Classes).");

            _context.CourseLevels.Remove(level);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}