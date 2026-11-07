using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/question-banks")]
    [Authorize]
    public class QuestionBanksController : ControllerBase
    {
        private readonly IQuestionBankService _bankService;

        public QuestionBanksController(IQuestionBankService bankService)
        {
            _bankService = bankService;
        }

        #region Ngân hàng câu hỏi (QuestionBank)

        /// <summary>
        /// Lấy danh sách ngân hàng câu hỏi (có thể lọc theo khóa học hoặc từ khóa).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? courseId, [FromQuery] string? keyword)
        {
            var result = await _bankService.GetAllBanksAsync(courseId, keyword);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết ngân hàng câu hỏi kèm danh sách câu hỏi bên trong.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _bankService.GetBankByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy ngân hàng câu hỏi với ID {id}." });
            }
            return Ok(result);
        }

        /// <summary>
        /// Tạo ngân hàng câu hỏi mới.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([FromBody] QuestionBankRequest request)
        {
            var result = await _bankService.CreateBankAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Cập nhật thông tin ngân hàng câu hỏi.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] QuestionBankRequest request)
        {
            var success = await _bankService.UpdateBankAsync(id, request);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy ngân hàng câu hỏi với ID {id}." });
            }
            return Ok(new { message = "Cập nhật ngân hàng câu hỏi thành công." });
        }

        /// <summary>
        /// Xóa ngân hàng câu hỏi.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _bankService.DeleteBankAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy ngân hàng câu hỏi với ID {id}." });
            }
            return Ok(new { message = "Xóa ngân hàng câu hỏi thành công." });
        }

        #endregion

        #region Câu hỏi (Question)

        /// <summary>
        /// Lấy danh sách câu hỏi trong một ngân hàng câu hỏi (hỗ trợ lọc theo độ khó, loại câu hỏi).
        /// </summary>
        [HttpGet("{bankId:int}/questions")]
        public async Task<IActionResult> GetQuestionsByBank(int bankId, [FromQuery] string? difficulty, [FromQuery] string? type)
        {
            var result = await _bankService.GetQuestionsByBankAsync(bankId, difficulty, type);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một câu hỏi theo ID.
        /// </summary>
        [HttpGet("questions/{id:int}")]
        public async Task<IActionResult> GetQuestionById(int id)
        {
            var result = await _bankService.GetQuestionByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy câu hỏi với ID {id}." });
            }
            return Ok(result);
        }

        /// <summary>
        /// Thêm câu hỏi mới vào ngân hàng câu hỏi.
        /// </summary>
        [HttpPost("questions")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> CreateQuestion([FromBody] QuestionRequest request)
        {
            var result = await _bankService.CreateQuestionAsync(request);
            return CreatedAtAction(nameof(GetQuestionById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Thêm hàng loạt câu hỏi vào ngân hàng câu hỏi (hỗ trợ Import Worker).
        /// </summary>
        [HttpPost("questions/bulk")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> BulkCreateQuestions([FromBody] BulkQuestionCreateRequest request)
        {
            var result = await _bankService.BulkCreateQuestionsAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật câu hỏi trong ngân hàng câu hỏi.
        /// </summary>
        [HttpPut("questions/{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionRequest request)
        {
            var success = await _bankService.UpdateQuestionAsync(id, request);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy câu hỏi với ID {id}." });
            }
            return Ok(new { message = "Cập nhật câu hỏi thành công." });
        }

        /// <summary>
        /// Xóa câu hỏi khỏi ngân hàng.
        /// </summary>
        [HttpDelete("questions/{id:int}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var success = await _bankService.DeleteQuestionAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy câu hỏi với ID {id}." });
            }
            return Ok(new { message = "Xóa câu hỏi thành công." });
        }

        /// <summary>
        /// Xóa hàng loạt câu hỏi khỏi ngân hàng.
        /// </summary>
        [HttpPost("questions/bulk-delete")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> BulkDeleteQuestions([FromBody] BulkQuestionDeleteRequest request)
        {
            if (request?.Ids == null || request.Ids.Count == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn ít nhất một câu hỏi để xóa." });
            }

            var result = await _bankService.DeleteQuestionsAsync(request.Ids);
            if (result.DeletedCount == 0)
            {
                return NotFound(new { message = result.Message });
            }

            return Ok(result);
        }

        #endregion
    }
}
