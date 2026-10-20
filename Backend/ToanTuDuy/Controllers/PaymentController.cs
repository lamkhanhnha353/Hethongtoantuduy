
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("create-url/{invoiceId}")]
        public async Task<IActionResult> CreatePaymentUrl(int invoiceId)
        {
            try
            {
                var url = await _paymentService.CreatePaymentUrl(invoiceId, HttpContext);
                return Ok(new { paymentUrl = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> PaymentReturn()
        {
            var invoice = await _paymentService.PaymentExecuteReturn(Request.Query);
            if (invoice == null) return BadRequest(new { message = "Giao dịch không hợp lệ hoặc chữ ký sai." });

            var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
            if (vnp_ResponseCode == "00")
            {
                return Ok(new { message = "Thanh toán thành công!", invoiceId = invoice.Id });
            }
            return BadRequest(new { message = "Thanh toán thất bại hoặc khách hàng đã hủy." });
        }

        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> PaymentIpn()
        {
            var response = await _paymentService.PaymentExecuteIpn(Request.Query);
            return Content(response, "application/json");
        }
    }
}