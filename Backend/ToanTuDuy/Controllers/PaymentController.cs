using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

            var mobileResponseCode = invoice?.Status == "PAID"
                ? "00"
                : responseCode == "00"
                    ? "99"
                    : responseCode;

            var redirectUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
                mobileDeepLink,
                "code",
                mobileResponseCode);

            return Redirect(redirectUrl);
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
