using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EnrollmentRegistrationResponse>> GetCourseLevelRegistrationsAsync(
            int accountId,
            int courseLevelId)
        {
            var studentIds = _context.StudentProfiles
                .Where(student => student.Parent!.AccountId == accountId)
                .Select(student => student.Id);

            var invoiceRegistrations = await _context.Invoices
                .AsNoTracking()
                .Where(invoice => studentIds.Contains(invoice.StudentId)
                    && invoice.Class.CourseLevelId == courseLevelId
                    && (invoice.Status == "PENDING" || invoice.Status == "PAID"))
                .Select(invoice => new EnrollmentRegistrationResponse
                {
                    InvoiceId = invoice.Id,
                    StudentId = invoice.StudentId,
                    ClassId = invoice.ClassId,
                    ClassName = invoice.Class.Name,
                    StudentName = invoice.Student.FullName,
                    Status = invoice.Status == "PAID" ? "ACTIVE" : "PENDING"
                })
                .ToListAsync();

            var activeRegistrations = await _context.ClassStudents
                .AsNoTracking()
                .Where(enrollment => studentIds.Contains(enrollment.StudentId)
                    && enrollment.Class.CourseLevelId == courseLevelId
                    && (enrollment.Status == "ACTIVE" || enrollment.Status == "PENDING"))
                .Select(enrollment => new EnrollmentRegistrationResponse
                {
                    InvoiceId = enrollment.Status == "ACTIVE" ? enrollment.InvoiceId : null,
                    StudentId = enrollment.StudentId,
                    ClassId = enrollment.ClassId,
                    ClassName = enrollment.Class.Name,
                    StudentName = enrollment.Student.FullName,
                    Status = enrollment.Status
                })
                .ToListAsync();

            return invoiceRegistrations
                .Concat(activeRegistrations)
                .GroupBy(registration => registration.StudentId)
                .Select(group => group
                    .OrderBy(registration => registration.Status == "ACTIVE" ? 0 : 1)
                    .ThenByDescending(registration => registration.InvoiceId.HasValue)
                    .First())
                .ToList();
        }

        public async Task<int> RegisterClassAsync(int accountId, EnrollmentRequest request)
        {
            var student = await _context.StudentProfiles
                .Include(profile => profile.Parent)
                .FirstOrDefaultAsync(profile =>
                    profile.Id == request.StudentId
                    && profile.Parent != null
                    && profile.Parent.AccountId == accountId);

            if (student?.Parent == null)
            {
                throw ApiException.Unauthorized(
                    "Không tìm thấy hồ sơ học viên hoặc học viên không thuộc quyền quản lý của bạn.");
            }

            var classInfo = await _context.Classes
                .Include(classEntity => classEntity.CourseLevel)
                .FirstOrDefaultAsync(classEntity => classEntity.Id == request.ClassId);

            if (classInfo == null)
            {
                throw ApiException.NotFound("Không tìm thấy lớp học này.");
            }

            var hasActiveEnrollment = await _context.ClassStudents
                .AnyAsync(enrollment => enrollment.StudentId == request.StudentId
                    && enrollment.Class.CourseLevelId == classInfo.CourseLevelId
                    && enrollment.Status == "ACTIVE");

            var hasValidInvoice = await _context.Invoices
                .AnyAsync(invoice => invoice.StudentId == request.StudentId
                    && invoice.Class.CourseLevelId == classInfo.CourseLevelId
                    && (invoice.Status == "PENDING" || invoice.Status == "PAID"));

            if (hasActiveEnrollment || hasValidInvoice)
            {
                throw ApiException.BadRequest(
                    "Học sinh này đã đăng ký hoặc đang có hóa đơn chờ thanh toán cho khóa học này.");
            }

            var existingEnrollment = await _context.ClassStudents
                .FirstOrDefaultAsync(enrollment =>
                    enrollment.StudentId == request.StudentId
                    && enrollment.ClassId == request.ClassId);

            var fee = classInfo.CourseLevel.Fee;
            if (fee <= 0 || fee > int.MaxValue)
            {
                throw ApiException.BadRequest("Học phí của cấp độ này không hợp lệ.");
            }

            var amount = decimal.ToInt32(Math.Round(fee, MidpointRounding.AwayFromZero));

            if (amount <= 0)
            {
                throw ApiException.BadRequest("Học phí của cấp độ này không hợp lệ.");
            }

            var classStudent = existingEnrollment;
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
            else
            {
                classStudent.Status = "PENDING";
            }

            var invoice = new Invoice
            {
                ParentId = student.Parent.Id,
                StudentId = request.StudentId,
                ClassId = request.ClassId,
                Amount = amount,
                Status = "PENDING",
                TxnRef = $"TEMP_{Guid.NewGuid():N}"[..15],
                CreatedAt = DateTime.UtcNow,
                ClassStudent = classStudent
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice.Id;
        }
    }
}
