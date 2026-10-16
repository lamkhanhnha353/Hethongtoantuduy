using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IParentStudentService
    {
        // CRUD Phụ huynh
        Task<IEnumerable<ParentResponseDto>> GetAllParentsAsync(string? keyword = null);
        Task<ParentResponseDto?> GetParentByIdAsync(int id);
        Task<ParentResponseDto> CreateParentAsync(CreateParentDto dto);
        Task<bool> UpdateParentAsync(int id, UpdateParentDto dto);
        Task<bool> DeleteParentAsync(int id);

        // CRUD Học sinh
        Task<IEnumerable<StudentResponseDto>> GetAllStudentsAsync(string? keyword = null, int? parentAccountId = null);
        Task<StudentResponseDto?> GetStudentByIdAsync(int id);
        Task<StudentResponseDto> CreateStudentAsync(CreateStudentDto dto);
        Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto);
        Task<bool> DeleteStudentAsync(int id);
    }
}
