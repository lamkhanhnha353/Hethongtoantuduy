using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceHistoryResponse>> GetHistoryAsync(int accountId);
        Task<IEnumerable<AdminInvoiceResponse>> GetAllForAdminAsync();
        Task<AdminInvoicePagedResponse> GetPagedForAdminAsync(
            int page,
            int pageSize,
            string? keyword = null,
            string? status = null,
            CancellationToken cancellationToken = default);
        Task CancelAsync(int accountId, int invoiceId);
    }
}
