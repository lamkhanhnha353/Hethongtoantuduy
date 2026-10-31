
using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface ILessonService
    {
        Task<IEnumerable<StudentLessonResponse>> GetLessonsByStudentIdAsync(int accountId, int studentId);
        Task<IEnumerable<StudentRoadmapResponse>> GetRoadmapsByStudentIdAsync(int accountId, int studentId);
    }
}