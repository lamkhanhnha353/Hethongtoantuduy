
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseLevelsController : ControllerBase
    {
        private readonly ICourseLevelService _courseLevelService;

        public CourseLevelsController(ICourseLevelService courseLevelService)
        {
            _courseLevelService = courseLevelService;
        }

        // Lấy danh sách Levels của một Khóa học cụ thể
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetLevelsByCourse(int courseId)
        {
            var result = await _courseLevelService.GetLevelsByCourseIdAsync(courseId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _courseLevelService.GetLevelByIdAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy cấp độ." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CourseLevelRequest request)
        {
            try
            {
                var result = await _courseLevelService.CreateLevelAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseLevelRequest request)
        {
            var success = await _courseLevelService.UpdateLevelAsync(id, request);
            if (!success) return NotFound(new { message = "Không tìm thấy cấp độ." });

            return Ok(new { message = "Cập nhật thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _courseLevelService.DeleteLevelAsync(id);
                if (!success) return NotFound(new { message = "Không tìm thấy cấp độ." });

                return Ok(new { message = "Xóa thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}