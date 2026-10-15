
using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IClassScheduleService
    {
        Task<IEnumerable<ClassScheduleResponse>> GetSchedulesByClassIdAsync(int classId);
        Task<ClassScheduleResponse> GetScheduleByIdAsync(int id);
        Task<ClassScheduleResponse> CreateScheduleAsync(ClassScheduleRequest request);
        Task<bool> UpdateScheduleAsync(int id, ClassScheduleRequest request);
        Task<bool> DeleteScheduleAsync(int id);
    }
}