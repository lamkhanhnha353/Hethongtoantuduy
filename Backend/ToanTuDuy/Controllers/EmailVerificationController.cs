using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    /// <summary>
    /// Xác minh email qua liên kết: trang HTML khi người dùng bấm link trong email
    /// và endpoint gửi lại link cho app.
    /// </summary>
    [ApiController]
    [Route("api/email-verification")]
    public class EmailVerificationController : ControllerBase
    {
        private readonly IEmailVerificationService _verificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmailVerificationController(
            IEmailVerificationService verificationService,
            UserManager<ApplicationUser> userManager)
        {
            _verificationService = verificationService;
            _userManager = userManager;
        }

        /// <summary>
        /// Người dùng bấm link trong email → nhận token từ query string → kích hoạt tài khoản.
        /// Luôn trả về trang HTML (kể cả lỗi) vì đây là trang trình duyệt mở từ app mail.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Verify([FromQuery] string? token)
        {
            var result = await _verificationService.VerifyAsync(token);

            var page = result.Status switch
            {
                VerifyEmailStatus.Success => BuildResultPage(
                    success: true,
                    title: "Xác minh thành công",
                    message: $"Địa chỉ email <strong>{System.Net.WebUtility.HtmlEncode(result.Email ?? string.Empty)}</strong> " +
                             "đã được xác minh. Từ giờ email này sẽ được khóa trong trang Cá nhân."),

                VerifyEmailStatus.Expired => BuildResultPage(
                    success: false,
                    title: "Liên kết đã hết hạn",
                    message: "Liên kết xác minh này không còn hiệu lực. Hãy gửi lại link mới từ trang Cá nhân rồi thử lại."),

                VerifyEmailStatus.AlreadyUsed => BuildResultPage(
                    success: false,
                    title: "Liên kết đã được sử dụng",
                    message: "Mỗi liên kết chỉ dùng được một lần. Nếu bạn cần xác minh lại, hãy gửi link mới từ trang Cá nhân."),

                VerifyEmailStatus.EmailChanged => BuildResultPage(
                    success: false,
                    title: "Email đã thay đổi",
                    message: "Địa chỉ email của tài khoản đã thay đổi sau khi liên kết này được tạo. Hãy gửi link mới từ trang Cá nhân."),

                _ => BuildResultPage(
                    success: false,
                    title: "Liên kết không hợp lệ",
                    message: "Liên kết xác minh không đúng hoặc đã bị thu hồi. Hãy gửi lại link mới từ trang Cá nhân."),
            };

            return Content(page, "text/html; charset=utf-8");
        }

        /// <summary>
        /// Gửi lại link xác minh tới email hiện tại (cooldown → 429 nếu gửi quá dày).
        /// Nhận cả POST /api/email-verification và POST /api/email-verification/resend.
        /// </summary>
        [HttpPost("")]
        [HttpPost("resend")]
        [Authorize]
        public async Task<IActionResult> Resend()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var accountId))
            {
                throw ApiException.Unauthorized("Không xác định được tài khoản.");
            }

            var user = await _userManager.FindByIdAsync(accountId.ToString());
            if (user == null)
            {
                throw ApiException.NotFound("Không tìm thấy tài khoản.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw ApiException.BadRequest("Tài khoản chưa có email để xác minh.");
            }

            await _verificationService.ResendAsync(accountId, user.Email);

            return Ok(new { message = "Đã gửi lại link xác minh đến email của bạn." });
        }

        private static string BuildResultPage(bool success, string title, string message)
        {
            var accent = success ? "#16a34a" : "#dc2626";
            var icon = success ? "&#10004;" : "&#10006;";

            return "<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\" />" +
                   "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />" +
                   $"<title>{title} — Hệ thống toán tư duy</title></head>" +
                   "<body style=\"margin:0;padding:24px;background:#f8fafc;font-family:Arial,Helvetica,sans-serif;" +
                   "display:flex;justify-content:center;align-items:center;min-height:100vh;\">" +
                   "<div style=\"max-width:440px;width:100%;background:#ffffff;border-radius:16px;padding:32px 28px;" +
                   "text-align:center;box-shadow:0 4px 16px rgba(15,23,42,0.08);\">" +
                   $"<div style=\"width:56px;height:56px;line-height:56px;border-radius:50%;background:{accent}1a;" +
                   $"color:{accent};font-size:26px;margin:0 auto 16px;\">{icon}</div>" +
                   $"<h1 style=\"margin:0 0 8px;font-size:20px;color:#0f172a;\">{title}</h1>" +
                   $"<p style=\"margin:0;color:#475569;line-height:1.6;font-size:15px;\">{message}</p>" +
                   "</div></body></html>";
        }
    }
}
