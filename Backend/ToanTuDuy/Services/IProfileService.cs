using System.Threading.Tasks;
using ToanTuDuy.DTOs.Profile;

namespace ToanTuDuy.Services
{
    public interface IProfileService
    {
        Task<ProfileResponse> GetAsync(int accountId);
        Task<ProfileResponse> UpdateAsync(int accountId, UpdateProfileRequest request);
    }
}
