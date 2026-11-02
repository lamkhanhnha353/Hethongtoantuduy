using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Threading.Tasks;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Parent")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentController(IPaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _configuration = configuration;
        }

        [HttpPost("create-url/{invoiceId:int}")]
        public async Task<IActionResult> CreatePaymentUrl(int invoiceId)
        {
            if (invoiceId <= 0)
            {
                return BadRequest(new { message = "Mã hóa đơn không hợp lệ." });
            }

            var clientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var paymentUrl = await _paymentService.CreatePaymentUrl(
                invoiceId,
                User.GetAccountId(),
                clientIpAddress);

            return Ok(new { paymentUrl });
        }

        [AllowAnonymous]
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> PaymentReturn()
        {
            var invoice = await _paymentService.PaymentExecuteReturn(Request.Query);
            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var mobileDeepLink = _configuration["VnPay:MobileDeepLink"];

            if (string.IsNullOrWhiteSpace(mobileDeepLink))
            {
                throw new InvalidOperationException("Thiếu cấu hình VnPay:MobileDeepLink.");
            }

            var redirectUrl = WebUtility.HtmlEncode(
                $"{mobileDeepLink}?code={WebUtility.UrlEncode(responseCode)}");
            var isSuccessful = invoice != null && responseCode == "00";
            var statusColor = isSuccessful ? "green" : invoice == null ? "red" : "orange";
            var statusIcon = isSuccessful ? "✓" : invoice == null ? string.Empty : "⚠";
            var statusMessage = isSuccessful
                ? "Thanh toán thành công!"
                : invoice == null
                    ? "Giao dịch không hợp lệ"
                    : "Giao dịch bị hủy hoặc lỗi";

            var htmlContent = $"""
                <html>
                    <head>
                        <meta charset="UTF-8">
                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    </head>
                    <body style="display:flex;justify-content:center;align-items:center;height:100vh;flex-direction:column;font-family:sans-serif;text-align:center;">
                        <h1 style="color:{statusColor};font-size:70px;margin:0;">{statusIcon}</h1>
                        <h2 style="color:{statusColor};">{statusMessage}</h2>
                        <a href="{redirectUrl}" style="display:inline-block;margin-top:20px;padding:12px 24px;background-color:#4f46e5;color:white;text-decoration:none;border-radius:8px;font-weight:bold;font-size:16px;">
                            QUAY LẠI ỨNG DỤNG
                        </a>
                    </body>
                </html>
                """;

            return Content(htmlContent, "text/html; charset=utf-8");
        }

        [AllowAnonymous]
        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> PaymentIpn()
        {
            var response = await _paymentService.PaymentExecuteIpn(Request.Query);
            return Ok(response);
        }
    }
}
