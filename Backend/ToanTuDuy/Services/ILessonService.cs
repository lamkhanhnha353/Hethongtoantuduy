using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface ILessonService
    {
        Task<IEnumerable<LessonResponse>> GetAllLessonsAsync();
        Task<PagedResult<LessonResponse>> GetPagedLessonsAsync(
            int page,
            int pageSize,
            int? classId = null,
            string? status = null,
            string? keyword = null,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<LessonResponse>> GetLessonsByClassIdAsync(int classId);
        Task<IEnumerable<StudentLessonResponse>> GetLessonsByStudentIdAsync(int accountId, int studentId);
        Task<IEnumerable<StudentRoadmapResponse>> GetRoadmapsByStudentIdAsync(int accountId, int studentId);
        Task<LessonResponse?> GetLessonByIdAsync(int id);
        Task<LessonResponse> CreateLessonAsync(LessonRequest request);
        Task<bool> UpdateLessonAsync(int id, LessonRequest request);
        Task<bool> DeleteLessonAsync(int id);
    }
}
