
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.Extensions; 
using ToanTuDuy.Models;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. API Lấy danh sách lịch sử hóa đơn của Phụ huynh
        [Authorize(Roles = "Parent")]
        [HttpGet("history")]
        public async Task<IActionResult> GetInvoiceHistory()
        {
            var accountId = User.GetAccountId(); // Lấy ID tài khoản từ Token
            
            var invoices = await _context.Invoices
                .Include(i => i.Class)
                .Include(i => i.Student)
                .Where(i => i.Parent != null && i.Parent.AccountId == accountId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    invoiceId = i.Id,
                    amount = i.Amount,
                    status = i.Status,
                    createdAt = i.CreatedAt,
                    paidAt = i.PaidAt,
                    txnRef = i.TxnRef,
                    paymentMethod = i.PaymentMethod,
                    classId = i.ClassId,
                    courseLevelId = i.Class.CourseLevelId,
                    courseLevelName = i.Class.CourseLevel.Name,
                    className = i.Class.Name,
                    studentName = i.Student.FullName
                })
                .ToListAsync();

            return Ok(invoices);
        }

        // 2. API Hủy hóa đơn (Do Mobile App tự gọi khi bấm X hoặc phụ huynh chủ động hủy)
        [Authorize(Roles = "Parent")]
        [HttpPost("cancel/{invoiceId}")]
        public async Task<IActionResult> CancelInvoice(int invoiceId)
        {
            var accountId = User.GetAccountId();

            // Tìm hóa đơn đúng của phụ huynh này
            var invoice = await _context.Invoices
                .Include(i => i.ClassStudent)
                .Include(i => i.Parent)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Parent.AccountId == accountId);

            if (invoice == null)
                return NotFound(new { message = "Không tìm thấy hóa đơn." });

            if (invoice.Status != "PENDING")
                return BadRequest(new { message = "Chỉ có thể hủy hóa đơn đang chờ thanh toán." });

            // Đổi trạng thái hóa đơn thành CANCELLED (hoặc FAILED tùy quy ước của bạn)
            invoice.Status = "CANCELLED";

            // QUAN TRỌNG: Nhả Slot bằng cách hủy ClassStudent
            if (invoice.ClassStudent != null && invoice.ClassStudent.Status == "PENDING")
            {
                invoice.ClassStudent.Status = "CANCELLED";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Hủy hóa đơn và giải phóng Slot thành công." });
        }
        
    }

    
}