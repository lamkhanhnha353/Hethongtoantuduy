
using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface ICourseLevelService
    {
        Task<IEnumerable<CourseLevelResponse>> GetLevelsByCourseIdAsync(int courseId);
        Task<CourseLevelResponse> GetLevelByIdAsync(int id);
        Task<CourseLevelResponse> CreateLevelAsync(CourseLevelRequest request);
        Task<bool> UpdateLevelAsync(int id, CourseLevelRequest request);
        Task<bool> DeleteLevelAsync(int id);
    }
}