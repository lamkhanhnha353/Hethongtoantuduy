using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Profile;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IPinService _pinService;

        public ProfileController(IProfileService profileService, IPinService pinService)
        {
            _profileService = profileService;
            _pinService = pinService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = await _profileService.GetAsync(User.GetAccountId());
            return Ok(response);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
        {
            var response = await _profileService.UpdateAsync(User.GetAccountId(), request);
            return Ok(response);
        }

        [HttpPut("email")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateParentEmailDto dto)
        {
            var response = await _profileService.UpdateEmailAsync(User.GetAccountId(), dto.Email);
            return Ok(response);
        }

        [HttpPost("avatar")]
        [Authorize(Roles = "Parent")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
        {
            var response = await _profileService.UpdateAvatarAsync(User.GetAccountId(), file, cancellationToken);
            return Ok(response);
        }

        [HttpDelete("avatar")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
        {
            var response = await _profileService.RemoveAvatarAsync(User.GetAccountId(), cancellationToken);
            return Ok(response);
        }

        [HttpPut("pin")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> SetPin([FromBody] SetPinRequest request)
        {
            await _pinService.SetAsync(User.GetAccountId(), request);
            return NoContent();
        }

        [HttpPost("pin/verify")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> VerifyPin([FromBody] VerifyPinRequest request)
        {
            var result = await _pinService.VerifyAsync(User.GetAccountId(), request.Pin);
            return Ok(result);
        }
    }
}
