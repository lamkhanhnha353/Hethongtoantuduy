using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
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
