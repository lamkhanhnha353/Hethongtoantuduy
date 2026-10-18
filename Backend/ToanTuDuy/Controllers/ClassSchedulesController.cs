
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassSchedulesController : ControllerBase
    {
        private readonly IClassScheduleService _scheduleService;

        public ClassSchedulesController(IClassScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetByClassId(int classId)
        {
            var result = await _scheduleService.GetSchedulesByClassIdAsync(classId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _scheduleService.GetScheduleByIdAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy lịch học." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClassScheduleRequest request)
        {
            try
            {
                var result = await _scheduleService.CreateScheduleAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClassScheduleRequest request)
        {
            try
            {
                var success = await _scheduleService.UpdateScheduleAsync(id, request);
                if (!success) return NotFound(new { message = "Không tìm thấy lịch học." });
                return Ok(new { message = "Cập nhật thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _scheduleService.DeleteScheduleAsync(id);
            if (!success) return NotFound(new { message = "Không tìm thấy lịch học." });
            return Ok(new { message = "Xóa thành công." });
        }
    }
}