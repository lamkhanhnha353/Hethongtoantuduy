
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
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

        [HttpPost("register")]
        public async Task<IActionResult> RegisterClass([FromBody] EnrollmentRequest request)
        {
            try
            {
                // 1. Kiểm tra xem bé đã đăng ký lớp này chưa (để tránh đăng ký trùng)
                var existingEnrollment = await _context.ClassStudents
                    .FirstOrDefaultAsync(cs => cs.StudentId == request.StudentId && cs.ClassId == request.ClassId);

                if (existingEnrollment != null)
                {
                    return BadRequest(new { message = "Học sinh này đã được đăng ký hoặc đang chờ thanh toán cho lớp này!" });
                }

                // 2. Bắt đầu phiên làm việc với Database (Tạo chỗ ngồi PENDING)
                var classStudent = new ClassStudent
                {
                    StudentId = request.StudentId,
                    ClassId = request.ClassId,
                    Status = "PENDING" // Kích hoạt thành ACTIVE khi VNPay gọi IPN
                };
                _context.ClassStudents.Add(classStudent);

                // 3. Tạo Hóa đơn (INVOICE)
                var invoice = new Invoice
                {
                    ParentId = request.ParentId,
                    StudentId = request.StudentId,
                    ClassId = request.ClassId,
                    Amount = request.Amount > 0 ? request.Amount : 200000,
                    Status = "PENDING",
                    // Tạo mã tạm thời cho TxnRef để vượt qua Unique Constraint, 
                    // PaymentService sẽ tự động cập nhật lại mã chuẩn sau.
                    TxnRef = "TEMP_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                    CreatedAt = DateTime.UtcNow,
                    ClassStudent = classStudent // Gắn kết hóa đơn này với chỗ ngồi của bé
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync(); // Lưu xuống SQL để hệ thống sinh ra invoice.Id

                // 4. Tự động gọi PaymentService để lấy đường link VNPay
                var paymentUrl = await _paymentService.CreatePaymentUrl(invoice.Id, HttpContext);

                // 5. Trả kết quả về cho Mobile App
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