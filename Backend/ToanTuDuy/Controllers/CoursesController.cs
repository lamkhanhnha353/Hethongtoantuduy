
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;
using ToanTuDuy.Services.Storage;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ICourseMediaStorage _courseMediaStorage;

        public CoursesController(
            ICourseService courseService,
            ICourseMediaStorage courseMediaStorage)
        {
            _courseService = courseService;
            _courseMediaStorage = courseMediaStorage;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _courseService.GetAllCoursesAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _courseService.GetCourseByIdAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy khóa học." });
            return Ok(result);
        }

        /// <summary>
        /// Tải ảnh hoặc video khóa học trực tiếp lên Cloudinary.
        /// Trả về URL https để lưu vào ImageUrl/VideoUrl/FacilityImages.
        /// </summary>
        [HttpPost("media")]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(105 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMedia(
            [FromForm] UploadCourseMediaRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _courseMediaStorage.UploadAsync(
                request.File, request.Type, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CourseRequest request)
        {
            try
            {
                var result = await _courseService.CreateCourseAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseRequest request)
        {
            var success = await _courseService.UpdateCourseAsync(id, request);
            if (!success) return NotFound(new { message = "Không tìm thấy khóa học." });

            return Ok(new { message = "Cập nhật thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _courseService.DeleteCourseAsync(id);
                if (!success) return NotFound(new { message = "Không tìm thấy khóa học." });

                return Ok(new { message = "Xóa thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}