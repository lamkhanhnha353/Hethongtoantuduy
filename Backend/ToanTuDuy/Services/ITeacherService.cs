using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface ITeacherService
    {
        Task<IEnumerable<TeacherResponseDto>> GetAllAsync(string? keyword = null);
        Task<PagedResult<TeacherResponseDto>> GetPagedAsync(int page = 1, int pageSize = PaginationDefaults.DefaultPageSize, string? keyword = null, bool? isActive = null);
        Task<TeacherResponseDto?> GetByIdAsync(int id);
        Task<TeacherResponseDto> CreateAsync(CreateTeacherDto dto);
        Task<bool> UpdateAsync(int id, UpdateTeacherDto dto);
        Task<bool> UpdateStatusAsync(int id, bool isActive);
        Task<bool> DeleteAsync(int id);
    }
}