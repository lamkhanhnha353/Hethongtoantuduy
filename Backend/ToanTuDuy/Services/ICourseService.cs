
using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface ICourseService
    {
        Task<IEnumerable<CourseResponse>> GetAllCoursesAsync();
        Task<CourseResponse> GetCourseByIdAsync(int id);
        Task<CourseResponse> CreateCourseAsync(CourseRequest request);
        Task<bool> UpdateCourseAsync(int id, CourseRequest request);
        Task<bool> DeleteCourseAsync(int id);
    }
}