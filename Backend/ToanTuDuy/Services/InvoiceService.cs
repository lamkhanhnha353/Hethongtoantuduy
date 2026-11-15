using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Extensions;
using ToanTuDuy.Models;

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

        public async Task<IEnumerable<AdminInvoiceResponse>> GetAllForAdminAsync()
        {
            return await BuildAdminInvoiceQuery(null, null)
                .ToListAsync();
        }

        public async Task<AdminInvoicePagedResponse> GetPagedForAdminAsync(
            int page,
            int pageSize,
            string? keyword = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            var paged = await BuildAdminInvoiceQuery(keyword, status)
                .ToPagedResultAsync(page, pageSize, cancellationToken: cancellationToken);

            // Compute whole-table KPI counts in a single grouped query; cast Amount to long to avoid int32 sum overflow.
            var statusStats = await _context.Invoices
                .AsNoTracking()
                .GroupBy(invoice => invoice.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count(),
                    Amount = group.Sum(item => (long)item.Amount)
                })
                .ToListAsync(cancellationToken);

            var paidStat = statusStats.FirstOrDefault(s => s.Status == "PAID");
            var pendingStat = statusStats.FirstOrDefault(s => s.Status == "PENDING");

            return new AdminInvoicePagedResponse
            {
                Items = paged.Items,
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                TotalAllCount = statusStats.Sum(s => s.Count),
                PaidCount = paidStat?.Count ?? 0,
                PendingCount = pendingStat?.Count ?? 0,
                PaidTotalAmount = paidStat?.Amount ?? 0L
            };
        }

        private IQueryable<AdminInvoiceResponse> BuildAdminInvoiceQuery(string? keyword, string? status)
        {
            var query = _context.Invoices.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Trim().Equals("ALL", System.StringComparison.OrdinalIgnoreCase))
            {
                var normalizedStatus = status.Trim().ToUpperInvariant();
                query = query.Where(invoice => invoice.Status == normalizedStatus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(invoice =>
                    invoice.TxnRef.ToLower().Contains(kw) ||
                    (invoice.TransactionId != null && invoice.TransactionId.ToLower().Contains(kw)) ||
                    invoice.Parent.FullName.ToLower().Contains(kw) ||
                    (invoice.Parent.Account.Email != null && invoice.Parent.Account.Email.ToLower().Contains(kw)) ||
                    (invoice.Parent.Account.PhoneNumber != null && invoice.Parent.Account.PhoneNumber.Contains(kw)) ||
                    invoice.Student.FullName.ToLower().Contains(kw) ||
                    invoice.Class.Name.ToLower().Contains(kw) ||
                    invoice.Class.CourseLevel.Name.ToLower().Contains(kw));
            }

            return query
                .OrderByDescending(invoice => invoice.CreatedAt)
                .ThenByDescending(invoice => invoice.Id)
                .Select(invoice => new AdminInvoiceResponse
                {
                    InvoiceId = invoice.Id,
                    TxnRef = invoice.TxnRef,
                    Amount = invoice.Amount,
                    Status = invoice.Status,
                    CreatedAt = invoice.CreatedAt,
                    PaidAt = invoice.PaidAt,
                    PaymentMethod = invoice.PaymentMethod,
                    TransactionId = invoice.TransactionId,
                    ParentName = invoice.Parent.FullName,
                    ParentEmail = invoice.Parent.Account.Email,
                    ParentPhone = invoice.Parent.Account.PhoneNumber,
                    StudentName = invoice.Student.FullName,
                    ClassName = invoice.Class.Name,
                    CourseLevelName = invoice.Class.CourseLevel.Name
                });
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
