using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ToanTuDuy.DTOs.Student;

namespace ToanTuDuy.Services
{
    public interface IStudentService
    {
        Task<IReadOnlyList<StudentResponse>> GetByParentAsync(int accountId);
        Task<StudentResponse> GetByIdAsync(int accountId, int studentId);
        Task<StudentResponse> CreateAsync(int accountId, CreateStudentRequest request);
        Task<StudentResponse> UpdateAsync(int accountId, int studentId, UpdateStudentRequest request);
        Task<StudentResponse> UpdateAvatarAsync(int accountId, int studentId, IFormFile? file, CancellationToken cancellationToken = default);
        Task<StudentResponse> RemoveAvatarAsync(int accountId, int studentId, CancellationToken cancellationToken = default);
        Task DeleteAsync(int accountId, int studentId);
    }
}
