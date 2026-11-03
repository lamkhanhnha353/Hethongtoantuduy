using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceHistoryResponse>> GetHistoryAsync(int accountId);
        Task CancelAsync(int accountId, int invoiceId);
    }
}
