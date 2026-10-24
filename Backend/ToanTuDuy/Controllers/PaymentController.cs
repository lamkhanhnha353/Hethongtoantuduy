using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public PaymentController(IPaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _configuration = configuration;
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
            var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();

            // Đọc Deep Link động từ file appsettings.json
            string mobileDeepLink = _configuration["VnPay:MobileDeepLink"];

            // Gắn mã kết quả vào cuối đường link
            string finalRedirectUrl = $"{mobileDeepLink}?code={vnp_ResponseCode}";

            string htmlContent;

            string headContent = @"
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        </head>";

            // Nút bấm đã được thiết kế lại. Thay vì dùng href đơn giản, ta dùng onclick 
            // với window.location.href để trình duyệt xử lý chuyển hướng ổn định hơn.
            string redirectButton = $@"
        <button onclick=""window.location.href='{finalRedirectUrl}'"" 
           style='display:inline-block; margin-top:20px; padding:12px 24px; background-color:#4f46e5; color:white; border:none; border-radius:8px; font-weight:bold; font-size:16px; cursor:pointer;'>
            QUAY LẠI ỨNG DỤNG
        </button>";

            if (invoice == null)
            {
                htmlContent = $"<html>{headContent}<body style='display:flex;justify-content:center;align-items:center;height:100vh;flex-direction:column;font-family:sans-serif;text-align:center;'><h2 style='color:red;'>Giao dịch không hợp lệ</h2>{redirectButton}</body></html>";
                return Content(htmlContent, "text/html; charset=utf-8");
            }

            if (vnp_ResponseCode == "00")
            {
                htmlContent = $"<html>{headContent}<body style='display:flex;justify-content:center;align-items:center;height:100vh;flex-direction:column;font-family:sans-serif;text-align:center;'><h1 style='color:green;font-size:70px;margin:0;'>✓</h1><h2 style='color:green;'>Thanh toán thành công!</h2>{redirectButton}</body></html>";
            }
            else
            {
                htmlContent = $"<html>{headContent}<body style='display:flex;justify-content:center;align-items:center;height:100vh;flex-direction:column;font-family:sans-serif;text-align:center;'><h1 style='color:orange;font-size:70px;margin:0;'>⚠</h1><h2 style='color:orange;'>Giao dịch bị hủy hoặc lỗi</h2>{redirectButton}</body></html>";
            }

            return Content(htmlContent, "text/html; charset=utf-8");
        }
    }
}