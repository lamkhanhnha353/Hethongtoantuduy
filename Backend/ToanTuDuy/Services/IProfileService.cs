using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ToanTuDuy.DTOs.Profile;

namespace ToanTuDuy.Services
{
    public interface IProfileService
    {
        Task<ProfileResponse> GetAsync(int accountId);
        Task<ProfileResponse> UpdateAsync(int accountId, UpdateProfileRequest request);
        Task<ProfileResponse> UpdateAvatarAsync(int accountId, IFormFile? file, CancellationToken cancellationToken = default);
        Task<ProfileResponse> RemoveAvatarAsync(int accountId, CancellationToken cancellationToken = default);
    }
}
