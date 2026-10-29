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
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;

        public CourseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CourseResponse>> GetAllCoursesAsync()
        {
            return await _context.Courses
                .Include(c => c.CourseLevels)
                .Select(c => new CourseResponse
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Description = c.Description,
                    MinAge = c.MinAge,
                    MaxAge = c.MaxAge,
                    ImageUrl = c.ImageUrl,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CourseLevels = c.CourseLevels.Select(cl => new CourseLevelResponse
                    {
                        Id = cl.Id,
                        CourseId = cl.CourseId,
                        Code = cl.Code,
                        Name = cl.Name,
                        Fee = cl.Fee,
                        TotalSessions = cl.TotalSessions,
                        OrderIndex = cl.OrderIndex,
                        Status = cl.Status,
                        Syllabus = string.IsNullOrEmpty(cl.Syllabus) ? null : JsonSerializer.Deserialize<object>(cl.Syllabus, (JsonSerializerOptions)null),
                        Faqs = string.IsNullOrEmpty(cl.Faqs) ? null : JsonSerializer.Deserialize<object>(cl.Faqs, (JsonSerializerOptions)null)
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<CourseResponse> GetCourseByIdAsync(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseLevels)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return null;

            return new CourseResponse
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                Description = course.Description,
                MinAge = course.MinAge,
                MaxAge = course.MaxAge,
                ImageUrl = course.ImageUrl,
                Status = course.Status,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                CourseLevels = course.CourseLevels.Select(cl => new CourseLevelResponse
                {
                    Id = cl.Id,
                    CourseId = cl.CourseId,
                    Code = cl.Code,
                    Name = cl.Name,
                    Fee = cl.Fee,
                    TotalSessions = cl.TotalSessions,
                    OrderIndex = cl.OrderIndex,
                    Status = cl.Status,
                    Syllabus = string.IsNullOrEmpty(cl.Syllabus) ? null : JsonSerializer.Deserialize<object>(cl.Syllabus, (JsonSerializerOptions)null),
                    Faqs = string.IsNullOrEmpty(cl.Faqs) ? null : JsonSerializer.Deserialize<object>(cl.Faqs, (JsonSerializerOptions)null)
                }).ToList()
            };
        }

        public async Task<CourseResponse> CreateCourseAsync(CourseRequest request)
        {
            if (await _context.Courses.AnyAsync(c => c.Code == request.Code))
                throw new Exception("Mã khóa học đã tồn tại.");

            var course = new Course
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                MinAge = request.MinAge,
                MaxAge = request.MaxAge,
                ImageUrl = request.ImageUrl,
                Status = request.Status,
                VideoUrl = request.VideoUrl,
                Benefits = request.Benefits != null ? JsonSerializer.Serialize(request.Benefits, (JsonSerializerOptions)null) : null,
                FacilityImages = request.FacilityImages != null ? JsonSerializer.Serialize(request.FacilityImages, (JsonSerializerOptions)null) : null,
                Rating = 5.0, // Điểm mặc định
                Reviews = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return await GetCourseByIdAsync(course.Id);
        }

        public async Task<bool> UpdateCourseAsync(int id, CourseRequest request)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return false;

            course.Code = request.Code;
            course.Name = request.Name;
            course.Description = request.Description;
            course.MinAge = request.MinAge;
            course.MaxAge = request.MaxAge;
            course.ImageUrl = request.ImageUrl;
            course.Status = request.Status;
            course.VideoUrl = request.VideoUrl;
            course.Benefits = request.Benefits != null ? JsonSerializer.Serialize(request.Benefits, (JsonSerializerOptions)null) : null;
            course.FacilityImages = request.FacilityImages != null ? JsonSerializer.Serialize(request.FacilityImages, (JsonSerializerOptions)null) : null;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            var course = await _context.Courses.Include(c => c.CourseLevels).FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return false;

            if (course.CourseLevels.Any())
                throw new Exception("Không thể xóa khóa học đang có cấp độ (Levels).");

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}