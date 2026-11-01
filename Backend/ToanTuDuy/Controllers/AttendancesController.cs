using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/attendance")]
    [Authorize]
    public class AttendancesController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendancesController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        /// <summary>
        /// Lấy danh sách điểm danh của một buổi học (danh sách học sinh trong lớp, trạng thái điểm danh, đơn xin nghỉ).
        /// </summary>
        /// <param name="lessonId">ID của buổi học (Lesson)</param>
        [HttpGet("lesson/{lessonId:int}")]
        public async Task<IActionResult> GetByLesson(int lessonId)
        {
            var result = await _attendanceService.GetAttendanceByLessonIdAsync(lessonId);
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy buổi học với ID {lessonId}." });
            }

            return Ok(result);
        }

        /// <summary>
        /// Điểm danh cho nhiều học sinh trong một buổi học theo lessonId trên URL.
        /// </summary>
        /// <param name="lessonId">ID của buổi học</param>
        /// <param name="items">Danh sách điểm danh các học sinh</param>
        [HttpPost("lesson/{lessonId:int}")]
        public async Task<IActionResult> SaveBatchAttendanceByLessonId(int lessonId, [FromBody] List<AttendanceItemDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return BadRequest(new { message = "Danh sách điểm danh không được để trống." });
            }

            try
            {
                var request = new BatchAttendanceRequest
                {
                    LessonId = lessonId,
                    Attendances = items
                };

                var result = await _attendanceService.SaveBatchAttendanceAsync(request);
                return Ok(new
                {
                    message = "Lưu điểm danh cho buổi học thành công.",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lưu điểm danh.", error = ex.Message });
            }
        }

        /// <summary>
        /// Điểm danh cho nhiều học sinh trong một buổi học (dạng batch request chung).
        /// </summary>
        [HttpPost("batch")]
        public async Task<IActionResult> SaveBatchAttendance([FromBody] BatchAttendanceRequest request)
        {
            if (request == null || request.Attendances == null || request.Attendances.Count == 0)
            {
                return BadRequest(new { message = "Dữ liệu điểm danh không hợp lệ." });
            }

            try
            {
                var result = await _attendanceService.SaveBatchAttendanceAsync(request);
                return Ok(new
                {
                    message = "Lưu điểm danh thành công.",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lưu điểm danh.", error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật điểm danh cho một học sinh lẻ trong buổi học.
        /// </summary>
        [HttpPut("lesson/{lessonId:int}/student/{studentId:int}")]
        public async Task<IActionResult> UpdateSingleAttendance(int lessonId, int studentId, [FromBody] AttendanceItemDto dto)
        {
            try
            {
                dto.StudentId = studentId;
                var result = await _attendanceService.SaveSingleAttendanceAsync(lessonId, dto);
                return Ok(new
                {
                    message = "Cập nhật điểm danh học sinh thành công.",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật điểm danh.", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy lịch sử điểm danh của một học sinh.
        /// </summary>
        /// <param name="studentId">ID của học sinh</param>
        /// <param name="classId">ID của lớp học (tùy chọn, để lọc theo lớp)</param>
        [HttpGet("student/{studentId:int}")]
        public async Task<IActionResult> GetStudentHistory(int studentId, [FromQuery] int? classId)
        {
            var history = await _attendanceService.GetStudentAttendanceHistoryAsync(studentId, classId);
            return Ok(history);
        }
    }
}
