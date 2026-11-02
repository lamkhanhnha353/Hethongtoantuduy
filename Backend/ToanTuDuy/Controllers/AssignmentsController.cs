using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/assignments")]
    [Authorize]
    public class AssignmentsController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;

        public AssignmentsController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        /// <summary>
        /// Lấy tất cả bài kiểm tra gắn với một buổi học (Lesson).
        /// </summary>
        [HttpGet("lesson/{lessonId:int}")]
        public async Task<IActionResult> GetByLesson(int lessonId)
        {
            var result = await _assignmentService.GetAssignmentsByLessonIdAsync(lessonId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết bài kiểm tra theo ID (kèm danh sách câu hỏi và danh sách học sinh được giao).
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _assignmentService.GetAssignmentByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy bài kiểm tra với ID {id}." });
            }
            return Ok(result);
        }

        /// <summary>
        /// Tạo bài kiểm tra gắn với một buổi học:
        /// - Chọn các câu hỏi từ ngân hàng câu hỏi.
        /// - Cho phép giao cho cả lớp (AssignAllStudentsInClass = true) hoặc giao riêng lẻ cho các học sinh cụ thể (SpecificStudentIds).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest request)
        {
            var result = await _assignmentService.CreateAssignmentAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Đính kèm bài kiểm tra có sẵn từ một bài khác (đã chuẩn bị sẵn) sang một buổi học (Lesson) mới.
        /// </summary>
        [HttpPost("clone")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> CloneToLesson([FromBody] CloneAssignmentRequest request)
        {
            var result = await _assignmentService.CloneAssignmentToLessonAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Cập nhật thông tin và cơ cấu câu hỏi của bài kiểm tra.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAssignmentRequest request)
        {
            var success = await _assignmentService.UpdateAssignmentAsync(id, request);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy bài kiểm tra với ID {id}." });
            }
            return Ok(new { message = "Cập nhật bài kiểm tra thành công." });
        }

        /// <summary>
        /// Xóa bài kiểm tra (bao gồm câu hỏi liên kết và danh sách giao bài).
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _assignmentService.DeleteAssignmentAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy bài kiểm tra với ID {id}." });
            }
            return Ok(new { message = "Xóa bài kiểm tra thành công." });
        }

        /// <summary>
        /// Đính kèm / Giao bài kiểm tra riêng lẻ cho một hoặc nhiều học sinh trong lớp.
        /// </summary>
        [HttpPost("{id:int}/students")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> AssignStudents(int id, [FromBody] AssignStudentsRequest request)
        {
            var result = await _assignmentService.AssignStudentsAsync(id, request.StudentIds);
            return Ok(new
            {
                message = "Giao bài kiểm tra cho học sinh thành công.",
                data = result
            });
        }

        /// <summary>
        /// Hủy giao bài kiểm tra cho một học sinh lẻ (khi học sinh chưa nộp bài).
        /// </summary>
        [HttpDelete("{id:int}/students/{studentId:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> UnassignStudent(int id, int studentId)
        {
            var success = await _assignmentService.UnassignStudentAsync(id, studentId);
            if (!success)
            {
                return NotFound(new { message = "Không tìm thấy học sinh trong danh sách giao của bài kiểm tra này." });
            }
            return Ok(new { message = "Đã hủy giao bài kiểm tra cho học sinh." });
        }

        /// <summary>
        /// Lấy danh sách các bài kiểm tra được giao cho một học sinh (Phụ huynh, Giáo viên, Admin có thể xem).
        /// </summary>
        [HttpGet("student/{studentId:int}")]
        [Authorize(Roles = "Teacher,Admin,Parent")]
        public async Task<IActionResult> GetByStudent(int studentId, [FromQuery] int? lessonId)
        {
            var result = await _assignmentService.GetAssignmentsByStudentAsync(studentId, lessonId);
            return Ok(result);
        }
    }
}
