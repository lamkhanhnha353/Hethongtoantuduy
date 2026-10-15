using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs.Student;

namespace ToanTuDuy.Services
{
    public interface IStudentService
    {
        Task<IReadOnlyList<StudentResponse>> GetByParentAsync(int accountId);
        Task<StudentResponse> GetByIdAsync(int accountId, int studentId);
        Task<StudentResponse> CreateAsync(int accountId, CreateStudentRequest request);
        Task<StudentResponse> UpdateAsync(int accountId, int studentId, UpdateStudentRequest request);
        Task DeleteAsync(int accountId, int studentId);
    }
}
