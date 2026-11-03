using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;

namespace ToanTuDuy.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InvoiceHistoryResponse>> GetHistoryAsync(int accountId)
        {
            return await _context.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.Parent.AccountId == accountId)
                .OrderByDescending(invoice => invoice.CreatedAt)
                .Select(invoice => new InvoiceHistoryResponse
                {
                    InvoiceId = invoice.Id,
                    Amount = invoice.Amount,
                    Status = invoice.Status,
                    CreatedAt = invoice.CreatedAt,
                    PaidAt = invoice.PaidAt,
                    TxnRef = invoice.TxnRef,
                    PaymentMethod = invoice.PaymentMethod,
                    ClassId = invoice.ClassId,
                    CourseLevelId = invoice.Class.CourseLevelId,
                    CourseLevelName = invoice.Class.CourseLevel.Name,
                    ClassName = invoice.Class.Name,
                    StudentName = invoice.Student.FullName
                })
                .ToListAsync();
        }

        public async Task CancelAsync(int accountId, int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(item => item.ClassStudent)
                .FirstOrDefaultAsync(item =>
                    item.Id == invoiceId
                    && item.Parent.AccountId == accountId);

            if (invoice == null)
            {
                throw ApiException.NotFound("Không tìm thấy hóa đơn.");
            }

            if (invoice.Status != "PENDING")
            {
                throw ApiException.BadRequest("Chỉ có thể hủy hóa đơn đang chờ thanh toán.");
            }

            invoice.Status = "CANCELLED";
            if (invoice.ClassStudent?.Status == "PENDING")
            {
                invoice.ClassStudent.Status = "CANCELLED";
            }

            await _context.SaveChangesAsync();
        }
    }
}
