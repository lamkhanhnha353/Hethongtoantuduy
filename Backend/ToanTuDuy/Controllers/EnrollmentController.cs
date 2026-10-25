
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Extensions;
using ToanTuDuy.Models;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;

        public EnrollmentController(ApplicationDbContext context, IPaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }

        [Authorize(Roles = "Parent")]
        [HttpGet("course-level/{courseLevelId:int}/registrations")]
        public async Task<IActionResult> GetCourseLevelRegistrations(int courseLevelId)
        {
            var accountId = User.GetAccountId();
            var studentIds = _context.StudentProfiles
                .Where(s => s.Parent!.AccountId == accountId)
                .Select(s => s.Id);

            var invoiceRegistrations = await _context.Invoices
                .AsNoTracking()
                .Where(i => studentIds.Contains(i.StudentId)
                    && i.Class.CourseLevelId == courseLevelId
                    && (i.Status == "PENDING" || i.Status == "PAID"))
                .Select(i => new
                {
                    studentId = i.StudentId,
                    classId = i.ClassId,
                    className = i.Class.Name,
                    studentName = i.Student.FullName,
                    status = i.Status == "PAID" ? "ACTIVE" : "PENDING"
                })
                .ToListAsync();

            var activeRegistrations = await _context.ClassStudents
                .AsNoTracking()
                .Where(cs => studentIds.Contains(cs.StudentId)
                    && cs.Class.CourseLevelId == courseLevelId
                    && cs.Status == "ACTIVE")
                .Select(cs => new
                {
                    studentId = cs.StudentId,
                    classId = cs.ClassId,
                    className = cs.Class.Name,
                    studentName = cs.Student.FullName,
                    status = "ACTIVE"
                })
                .ToListAsync();

            var registrations = invoiceRegistrations
                .Concat(activeRegistrations)
                .GroupBy(registration => registration.studentId)
                .Select(group => group
                    .OrderBy(registration => registration.status == "ACTIVE" ? 0 : 1)
                    .First())
                .ToList();

            return Ok(registrations);
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterClass([FromBody] EnrollmentRequest request)
        {
            try
            {
                // 1. Kiểm tra Lớp học có tồn tại và xem lớp đã đầy chưa
                var classInfo = await _context.Classes
                    .Include(c => c.ClassStudents)
                    .FirstOrDefaultAsync(c => c.Id == request.ClassId);

                if (classInfo == null)
                    return NotFound(new { message = "Không tìm thấy lớp học này." });

                var hasActiveEnrollment = await _context.ClassStudents
                    .AnyAsync(cs => cs.StudentId == request.StudentId
                        && cs.Class.CourseLevelId == classInfo.CourseLevelId
                        && cs.Status == "ACTIVE");

                var hasValidInvoice = await _context.Invoices
                    .AnyAsync(i => i.StudentId == request.StudentId
                        && i.Class.CourseLevelId == classInfo.CourseLevelId
                        && (i.Status == "PENDING" || i.Status == "PAID"));

                if (hasActiveEnrollment || hasValidInvoice)
                {
                    return BadRequest(new
                    {
                        message = "Học sinh này đã đăng ký hoặc đang có hóa đơn chờ thanh toán cho khóa học này."
                    });
                }

                // Đếm số lượng học sinh đã thanh toán thành công (ACTIVE)
                int activeStudents = classInfo.ClassStudents.Count(cs => cs.Status == "ACTIVE");
                if (activeStudents >= classInfo.MaxCapacity)
                {
                    return BadRequest(new { message = "Lớp học này đã đủ số lượng học viên." });
                }

                // 2. Kiểm tra xem bé đã đăng ký lớp này chưa (CHỈ chặn nếu đang PENDING hoặc PAID/ACTIVE)
                var existingEnrollment = await _context.ClassStudents
                    .FirstOrDefaultAsync(cs => cs.StudentId == request.StudentId && cs.ClassId == request.ClassId);

                if (existingEnrollment != null)
                {
                    // Tái sử dụng chỗ ngồi nếu các hóa đơn trước đó đều FAILED.
                    existingEnrollment.Status = "PENDING";
                }

                // 3. Nếu chưa từng đăng ký, tạo chỗ ngồi PENDING mới
                ClassStudent classStudent = existingEnrollment;
                if (classStudent == null)
                {
                    classStudent = new ClassStudent
                    {
                        StudentId = request.StudentId,
                        ClassId = request.ClassId,
                        Status = "PENDING"
                    };
                    _context.ClassStudents.Add(classStudent);
                }

                // 4. Tạo Hóa đơn (INVOICE)
                var invoice = new Invoice
                {
                    ParentId = request.ParentId,
                    StudentId = request.StudentId,
                    ClassId = request.ClassId,
                    Amount = request.Amount > 0 ? request.Amount : 200000,
                    Status = "PENDING",
                    TxnRef = "TEMP_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                    CreatedAt = DateTime.UtcNow,
                    ClassStudent = classStudent // Gắn kết với chỗ ngồi
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // 5. Lấy đường link VNPay
                var paymentUrl = await _paymentService.CreatePaymentUrl(invoice.Id, HttpContext);

                return Ok(new
                {
                    message = "Tạo đơn hàng thành công!",
                    invoiceId = invoice.Id,
                    paymentUrl = paymentUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }
}