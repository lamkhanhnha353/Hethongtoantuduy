using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [Authorize(Roles = "Parent")]
        [HttpGet("history")]
        public async Task<IActionResult> GetInvoiceHistory()
        {
            var invoices = await _invoiceService.GetHistoryAsync(User.GetAccountId());
            return Ok(invoices);
        }

        [Authorize(Roles = "Parent")]
        [HttpPost("cancel/{invoiceId:int}")]
        public async Task<IActionResult> CancelInvoice(int invoiceId)
        {
            await _invoiceService.CancelAsync(User.GetAccountId(), invoiceId);
            return Ok(new { message = "Hủy hóa đơn và giải phóng Slot thành công." });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/api/admin/invoices")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var invoices = await _invoiceService.GetAllForAdminAsync();
            return Ok(invoices);
        }
    }
}
