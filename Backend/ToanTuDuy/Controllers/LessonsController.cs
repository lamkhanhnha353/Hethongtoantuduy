using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _lessonService.GetAllLessonsAsync();
            return Ok(result);
        }

        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetByClassId(int classId)
        {
            var result = await _lessonService.GetLessonsByClassIdAsync(classId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _lessonService.GetLessonByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy buổi học." });
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LessonRequest request)
        {
            try
            {
                var result = await _lessonService.CreateLessonAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] LessonRequest request)
        {
            try
            {
                var success = await _lessonService.UpdateLessonAsync(id, request);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy buổi học." });
                }
                return Ok(new { message = "Cập nhật buổi học thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _lessonService.DeleteLessonAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy buổi học." });
                }
                return Ok(new { message = "Xóa buổi học thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
