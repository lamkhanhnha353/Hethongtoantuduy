using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Parent")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetInvoiceHistory()
        {
            var invoices = await _invoiceService.GetHistoryAsync(User.GetAccountId());
            return Ok(invoices);
        }

        [HttpPost("cancel/{invoiceId:int}")]
        public async Task<IActionResult> CancelInvoice(int invoiceId)
        {
            await _invoiceService.CancelAsync(User.GetAccountId(), invoiceId);
            return Ok(new { message = "Hủy hóa đơn và giải phóng Slot thành công." });
        }
    }
}
