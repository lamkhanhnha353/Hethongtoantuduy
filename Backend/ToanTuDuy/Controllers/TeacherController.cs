using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeachersController : ControllerBase
    {
        private readonly ITeacherService _teacherService;

        public TeachersController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? keyword)
        {
            return Ok(await _teacherService.GetAllAsync(keyword));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _teacherService.GetByIdAsync(id);
            if (res == null) return NotFound(new { message = "Không tìm thấy giáo viên." });
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTeacherDto dto)
        {
            try
            {
                var created = await _teacherService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTeacherDto dto)
        {
            var success = await _teacherService.UpdateAsync(id, dto);
            if (!success) return NotFound(new { message = "Cập nhật thất bại." });
            return Ok(new { message = "Cập nhật thông tin giáo viên thành công." });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _teacherService.DeleteAsync(id);
            if (!success) return NotFound(new { message = "Không tìm thấy giáo viên để xóa." });
            return Ok(new { message = "Xóa giáo viên và tài khoản liên kết thành công." });
        }
    }
}