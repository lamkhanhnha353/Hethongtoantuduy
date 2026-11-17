using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.DTOs;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Parent")]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        [HttpGet("course-level/{courseLevelId:int}/registrations")]
        public async Task<IActionResult> GetCourseLevelRegistrations(int courseLevelId)
        {
            var registrations = await _enrollmentService.GetCourseLevelRegistrationsAsync(
                User.GetAccountId(),
                courseLevelId);

            return Ok(registrations);
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterClass([FromBody] EnrollmentRequest request)
        {
            var invoiceId = await _enrollmentService.RegisterClassAsync(
                User.GetAccountId(),
                request);

            return Ok(new
            {
                message = "Đăng ký giữ chỗ thành công. Hóa đơn đang chờ thanh toán.",
                invoiceId
            });
        }
    }
}
