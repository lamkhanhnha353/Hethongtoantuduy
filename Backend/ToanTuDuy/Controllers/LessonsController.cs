
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Parent")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        // API TRẢ VỀ LỊCH HỌC TIMELINE CHO APP PHỤ HUYNH
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentLessons(int studentId)
        {
            var result = await _lessonService.GetLessonsByStudentIdAsync(User.GetAccountId(), studentId);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/roadmap")]
        public async Task<IActionResult> GetStudentRoadmaps(int studentId)
        {
            var result = await _lessonService.GetRoadmapsByStudentIdAsync(User.GetAccountId(), studentId);
            return Ok(result);
        }
    }
}