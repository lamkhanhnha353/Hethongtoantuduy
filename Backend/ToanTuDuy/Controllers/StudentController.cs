using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Extensions;
using ToanTuDuy.Filters;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/students")]
    [Authorize(Roles = "Parent")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var students = await _studentService.GetByParentAsync(User.GetAccountId());
            return Ok(students);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var student = await _studentService.GetByIdAsync(User.GetAccountId(), id);
            return Ok(student);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
        {
            var student = await _studentService.CreateAsync(User.GetAccountId(), request);
            return Ok(student);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentRequest request)
        {
            var student = await _studentService.UpdateAsync(User.GetAccountId(), id, request);
            return Ok(student);
        }

        [HttpDelete("{id:int}")]
        [RequirePin]
        public async Task<IActionResult> Delete(int id)
        {
            await _studentService.DeleteAsync(User.GetAccountId(), id);
            return NoContent();
        }
    }
}
