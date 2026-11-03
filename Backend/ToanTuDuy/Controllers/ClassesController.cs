using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassesController : ControllerBase
    {
        private readonly IClassService _classService;

        public ClassesController(IClassService classService)
        {
            _classService = classService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _classService.GetAllClassesAsync());
        }

        [HttpGet("course-level/{courseLevelId}")]
        public async Task<IActionResult> GetByCourseLevel(int courseLevelId)
        {
            return Ok(await _classService.GetClassesByCourseLevelIdAsync(courseLevelId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _classService.GetClassByIdAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy lớp học." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClassRequest request)
        {
            try
            {
                var result = await _classService.CreateClassAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClassRequest request)
        {
            var success = await _classService.UpdateClassAsync(id, request);
            if (!success) return NotFound(new { message = "Không tìm thấy lớp học." });
            return Ok(new { message = "Cập nhật thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _classService.DeleteClassAsync(id);
                if (!success) return NotFound(new { message = "Không tìm thấy lớp học." });
                return Ok(new { message = "Xóa thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // --- Danh sách học viên của lớp học ---
        [HttpGet("{id}/students")]
        public async Task<IActionResult> GetStudents(int id)
        {
            var students = await _classService.GetStudentsByClassIdAsync(id);
            return Ok(students);
        }

        [HttpPost("{id}/students")]
        public async Task<IActionResult> AddStudent(int id, [FromBody] AddClassStudentRequest request)
        {
            try
            {
                var result = await _classService.AddStudentToClassAsync(id, request.StudentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/students/{studentId}")]
        public async Task<IActionResult> RemoveStudent(int id, int studentId)
        {
            var success = await _classService.RemoveStudentFromClassAsync(id, studentId);
            if (!success) return NotFound(new { message = "Không tìm thấy học viên trong lớp." });
            return Ok(new { message = "Xóa học viên khỏi lớp thành công." });
        }
    }
}
