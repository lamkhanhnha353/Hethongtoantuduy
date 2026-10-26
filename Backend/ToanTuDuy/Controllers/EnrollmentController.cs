
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

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentController(ApplicationDbContext context)
        {
            _context = context;
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
                    invoiceId = (int?)i.Id,
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
                    && (cs.Status == "ACTIVE" || cs.Status == "PENDING"))
                .Select(cs => new
                {
                    invoiceId = cs.Status == "ACTIVE" ? cs.InvoiceId : null,
                    studentId = cs.StudentId,
                    classId = cs.ClassId,
                    className = cs.Class.Name,
                    studentName = cs.Student.FullName,
                    status = cs.Status
                })
                .ToListAsync();

            var registrations = invoiceRegistrations
                .Concat(activeRegistrations)
                .GroupBy(registration => registration.studentId)
                .Select(group => group
                    .OrderBy(registration => registration.status == "ACTIVE" ? 0 : 1)
                    .ThenByDescending(registration => registration.invoiceId.HasValue)
                    .First())
                .ToList();

            return Ok(registrations);
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterClass([FromBody] EnrollmentRequest request)
        {
            try
            {
                // 1. Tự động lấy ParentId chuẩn từ Token thông qua StudentProfile
                var accountId = User.GetAccountId();

                // Tìm hồ sơ học sinh và kiểm tra xem có đúng thuộc tài khoản đang đăng nhập không
                var student = await _context.StudentProfiles
                    .Include(s => s.Parent)
                    .FirstOrDefaultAsync(s => s.Id == request.StudentId && s.Parent.AccountId == accountId);

                if (student == null || student.Parent == null)
                {
                    return Unauthorized(new { message = "Lỗi xác thực: Không tìm thấy hồ sơ học viên hoặc không thuộc quyền quản lý của bạn." });
                }

                int validParentId = student.Parent.Id;

                // 2. Kiểm tra Lớp học có tồn tại và xem lớp đã đầy chưa
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

                // Đếm số lượng học sinh đã thanh toán (ACTIVE) VÀ đang giữ chỗ (PENDING)
                var existingEnrollment = await _context.ClassStudents
                    .FirstOrDefaultAsync(cs => cs.StudentId == request.StudentId && cs.ClassId == request.ClassId);

                // Nếu có bé đang giữ chỗ (existingEnrollment) thì không đếm lặp lại bé đó
                var occupiedSlots = classInfo.ClassStudents.Count(cs =>
                    (cs.Status == "ACTIVE" || cs.Status == "PENDING")
                    && cs.Id != existingEnrollment?.Id);

                if (occupiedSlots >= classInfo.MaxCapacity)
                {
                    return BadRequest(new { message = "Lớp học này đã hết chỗ hoặc các slot đang được giữ chờ thanh toán." });
                }

                if (existingEnrollment != null)
                {
                    // Tái sử dụng chỗ ngồi nếu các hóa đơn trước đó đều FAILED
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

                // 4. Tạo Hóa đơn (INVOICE) với ParentId chuẩn xác
                var invoice = new Invoice
                {
                    ParentId = validParentId, // <-- Đã cập nhật ParentId an toàn
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

                return Ok(new
                {
                    message = "Đăng ký giữ chỗ thành công. Hóa đơn đang chờ thanh toán.",
                    invoiceId = invoice.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

    }
}