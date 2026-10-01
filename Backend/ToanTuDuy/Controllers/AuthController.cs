using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(response); // Trả về Token và ProfileId
            }
            catch (ArgumentException ex) // Lỗi do Request ko hợp lệ logic
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
            catch (InvalidOperationException ex) // Lỗi do Identity validate
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                // Trên thực tế nên dùng ILogger để log
                return StatusCode(500, new { code = 500, message = "Đã xảy ra lỗi hệ thống trong quá trình đăng ký.", details = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { code = 401, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 500, message = "Đã xảy ra lỗi hệ thống trong quá trình đăng nhập.", details = ex.Message });
            }
        }
    }
}
