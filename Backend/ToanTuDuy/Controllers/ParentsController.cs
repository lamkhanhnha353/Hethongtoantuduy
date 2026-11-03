using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [AllowAnonymous] // Bỏ qua kiểm tra Token

    public class ParentsController : ControllerBase
    {
        private readonly IParentStudentService _service;

        public ParentsController(IParentStudentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? keyword)
        {
            return Ok(await _service.GetAllParentsAsync(keyword));
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetAllStudents([FromQuery] string? keyword)
        {
            return Ok(await _service.GetAllStudentsAsync(keyword));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var parent = await _service.GetParentByIdAsync(id);
            if (parent == null) return NotFound(new { message = "Không tìm thấy phụ huynh." });
            return Ok(parent);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateParentDto dto)
        {
            try
            {
                var created = await _service.CreateParentAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateParentDto dto)
        {
            var success = await _service.UpdateParentAsync(id, dto);
            if (!success) return NotFound(new { message = "Cập nhật thất bại." });
            return Ok(new { message = "Cập nhật thông tin phụ huynh thành công." });
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateParentStatusDto dto)
        {
            var success = await _service.UpdateParentStatusAsync(id, dto.IsActive!.Value);
            if (!success) return NotFound(new { message = "Không tìm thấy phụ huynh." });
            return Ok(new { message = "Cập nhật trạng thái phụ huynh thành công.", dto.IsActive });
        }

        [HttpPut("{id:int}/email")]
        public async Task<IActionResult> UpdateEmail(
            int id,
            [FromBody] UpdateParentEmailDto dto
        )
        {
            var success = await _service.UpdateParentEmailAsync(id, dto.Email);
            if (!success) return NotFound(new { message = "Không tìm thấy phụ huynh." });
            return Ok(new { message = "Cập nhật email phụ huynh thành công." });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteParentAsync(id);
            if (!success) return NotFound(new { message = "Không tìm thấy phụ huynh để xóa." });
            return Ok(new { message = "Xóa phụ huynh và toàn bộ tài khoản học sinh liên kết thành công." });
        }
    }
}
