using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IParentStudentService _service;

        public StudentsController(IParentStudentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? keyword, [FromQuery] int? parentAccountId)
        {
            return Ok(await _service.GetAllStudentsAsync(keyword, parentAccountId));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var student = await _service.GetStudentByIdAsync(id);
            if (student == null) return NotFound(new { message = "Không tìm thấy học sinh." });
            return Ok(student);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
        {
            try
            {
                var created = await _service.CreateStudentAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentDto dto)
        {
            var success = await _service.UpdateStudentAsync(id, dto);
            if (!success) return NotFound(new { message = "Cập nhật thất bại." });
            return Ok(new { message = "Cập nhật thông tin học sinh thành công." });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteStudentAsync(id);
            if (!success) return NotFound(new { message = "Không tìm thấy học sinh để xóa." });
            return Ok(new { message = "Đã xóa học sinh thành công." });
        }
    }
}