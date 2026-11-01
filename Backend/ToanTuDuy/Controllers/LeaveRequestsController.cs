
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ILeaveRequestService _leaveRequestService;

        public LeaveRequestsController(ILeaveRequestService leaveRequestService)
        {
            _leaveRequestService = leaveRequestService;
        }

        // API ĐỂ PHỤ HUYNH TẠO ĐƠN (POST)
        [HttpPost]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> Create([FromBody] LeaveRequestCreateRequest request)
        {
            var result = await _leaveRequestService.CreateLeaveRequestAsync(User.GetAccountId(), request);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _leaveRequestService.CancelLeaveRequestAsync(User.GetAccountId(), id);
            return NoContent();
        }

        // Giáo viên xem các đơn xin vắng của lớp mình phụ trách.
        [HttpGet("pending")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetPending()
        {
            var result = await _leaveRequestService.GetPendingRequestsAsync(User.GetAccountId());
            return Ok(result);
        }

        // Giáo viên duyệt/từ chối đơn xin vắng của lớp mình phụ trách.
        [HttpPut("{id}/process")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ProcessRequest(int id, [FromQuery] bool isApproved)
        {
            var success = await _leaveRequestService.ProcessLeaveRequestAsync(
                User.GetAccountId(), id, isApproved);
            if (!success) return NotFound(new { message = "Không tìm thấy đơn xin nghỉ hoặc đơn đã được xử lý!" });

            string msg = isApproved ? "Đã CHẤP THUẬN và tự động lưu Điểm danh (Có phép)." : "Đã TỪ CHỐI đơn.";
            return Ok(new { message = msg });
        }

        // API ĐỂ PHỤ HUYNH XEM LỊCH SỬ XIN NGHỈ (GET)
        [HttpGet("student/{studentId}")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var result = await _leaveRequestService.GetRequestsByStudentIdAsync(User.GetAccountId(), studentId);
            return Ok(result);
        }
    }
}