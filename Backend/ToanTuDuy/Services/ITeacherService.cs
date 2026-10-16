using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface ITeacherService
    {
        Task<IEnumerable<TeacherResponseDto>> GetAllAsync(string? keyword = null);
        Task<TeacherResponseDto?> GetByIdAsync(int id);
        Task<TeacherResponseDto> CreateAsync(CreateTeacherDto dto);
        Task<bool> UpdateAsync(int id, UpdateTeacherDto dto);
        Task<bool> DeleteAsync(int id);
    }
}