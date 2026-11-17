using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IClassService
    {
        Task<IEnumerable<ClassResponse>> GetAllClassesAsync();
        Task<PagedResult<ClassResponse>> GetPagedClassesAsync(
            int page = 1,
            int pageSize = 12,
            string? keyword = null,
            int? courseId = null,
            int? courseLevelId = null,
            int? teacherId = null,
            string? status = null,
            string? capacity = null,
            string? sort = null);
        Task<IEnumerable<ClassResponse>> GetClassesByCourseLevelIdAsync(int courseLevelId);
        Task<ClassResponse> GetClassByIdAsync(int id);
        Task<ClassResponse> CreateClassAsync(ClassRequest request);
        Task<bool> UpdateClassAsync(int id, ClassRequest request);
        Task<bool> DeleteClassAsync(int id);

        // Quản lý học viên trong lớp
        Task<IEnumerable<ClassStudentResponse>> GetStudentsByClassIdAsync(int classId);
        Task<PagedResult<ClassStudentResponse>> GetPagedStudentsByClassIdAsync(
            int classId,
            int page = 1,
            int pageSize = PaginationDefaults.DefaultPageSize,
            string? keyword = null);
        Task<ClassStudentResponse> AddStudentToClassAsync(int classId, int studentId);
        Task<bool> RemoveStudentFromClassAsync(int classId, int studentId);
    }
}
