using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;
using ToanTuDuy.Options;

namespace ToanTuDuy.Services
{
    /// <summary>
    /// Khôi phục mật khẩu khi quên: người dùng nhập email đã đăng ký (và đã xác minh),
    /// sinh mật khẩu tạm và gửi qua email đó (không lưu token trong DB).
    /// </summary>
    public class PasswordResetService : IPasswordResetService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IResend _resend;
        private readonly PasswordResetSettings _settings;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(
            UserManager<ApplicationUser> userManager,
            IResend resend,
            IOptions<PasswordResetSettings> settings,
            ILogger<PasswordResetService> logger)
        {
            _userManager = userManager;
            _resend = resend;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync((email ?? string.Empty).Trim());

            // Email không có trong hệ thống: không gửi, không lỗi — trả message chung,
            // không tiết lộ email nào đã được đăng ký.
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return new ForgotPasswordResponse
                {
                    Message = "Nếu tài khoản tồn tại với email đã xác minh, mật khẩu tạm đã được gửi tới email này.",
                };
            }

            // Chỉ gửi vào email đã xác minh: tránh trường hợp email gõ sai lúc đăng ký
            // (chưa bấm link xác minh) mà vẫn nhận được mật khẩu tạm.
            if (!user.EmailConfirmed)
            {
                throw ApiException.BadRequest(
                    "Email của tài khoản chưa được xác minh. Vui lòng liên hệ quản trị viên.");
            }

            var now = DateTime.UtcNow;

            if (user.PasswordResetSentAt.HasValue)
            {
                var elapsedSeconds = (now - user.PasswordResetSentAt.Value).TotalSeconds;

                if (elapsedSeconds < _settings.CooldownSeconds)
                {
                    var waitSeconds = (int)Math.Ceiling(_settings.CooldownSeconds - elapsedSeconds);
                    throw ApiException.TooManyRequests(
                        $"Vui lòng đợi khoảng {waitSeconds} giây trước khi yêu cầu mật khẩu tạm mới.");
                }
            }

            var tempPassword = GenerateTempPassword(_settings.TempPasswordLength);

            // Token do Identity sinh ra chỉ sống trong RAM cho đúng lệnh ResetPasswordAsync,
            // không lưu xuống DB nên không cần bảng token.
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, tempPassword);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                throw ApiException.BadRequest($"Không thể đặt lại mật khẩu: {errors}");
            }

            // Phiên cũ (refresh token) chết ngay; access token hết hạn theo TTL.
            user.RefreshToken = null;
            user.PasswordResetSentAt = now;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                throw ApiException.BadRequest($"Không thể cập nhật tài khoản: {errors}");
            }

            await SendEmailAsync(user.Email, tempPassword);

            return new ForgotPasswordResponse
            {
                Message = "Mật khẩu tạm đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư và đăng nhập ngay.",
            };
        }

        private static string GenerateTempPassword(int length)
        {
            // Bỏ chữ dễ nhầm (I, O, l, 0, 1) để người dùng gõ lại cho đúng.
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string specials = "!@#$%&*?";

            var required = new[]
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                specials[RandomNumberGenerator.GetInt32(specials.Length)],
            };

            var all = upper + lower + digits + specials;
            var size = Math.Max(length, required.Length);
            var chars = new char[size];

            required.CopyTo(chars, 0);

            for (var i = required.Length; i < size; i++)
            {
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            // Trộn ngẫu nhiên để 4 ký tự bắt buộc không nằm yên ở đầu chuỗi.
            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }

        private async Task SendEmailAsync(string email, string tempPassword)
        {
            try
            {
                var message = new EmailMessage
                {
                    From = _settings.FromEmail,
                    Subject = "Mật khẩu tạm của bạn — Hệ thống Toán Tư Duy",
                    TextBody = BuildTextBody(tempPassword),
                    HtmlBody = BuildHtmlBody(tempPassword),
                };
                message.To.Add(email);

                await _resend.EmailSendAsync(message);
                _logger.LogInformation("Đã gửi mật khẩu tạm tới {Email}.", email);
            }
            catch (Exception ex)
            {
                // Không làm hỏng luồng chính vì lỗi gửi mail (giống EmailVerificationService).
                _logger.LogWarning(ex, "Không gửi được email mật khẩu tạm tới {Email} qua Resend.", email);
            }
        }

        private string BuildTextBody(string tempPassword) =>
            "Yêu cầu khôi phục mật khẩu cho tài khoản Hệ thống Toán Tư Duy.\n\n" +
            $"Mật khẩu tạm của bạn: {tempPassword}\n\n" +
            "Mật khẩu cũ đã bị vô hiệu. Hãy đăng nhập bằng mật khẩu tạm này, rồi đổi " +
            "mật khẩu ngay tại tab Cá nhân → Bảo mật → Đổi mật khẩu.\n\n" +
            "Nếu bạn không yêu cầu email này, mật khẩu tài khoản vừa được thay đổi — " +
            "vui lòng liên hệ quản trị viên ngay lập tức.";

        private string BuildHtmlBody(string tempPassword) =>
            "<div style=\"font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:0 auto;padding:32px 24px;background:#f8fafc;border-radius:12px;\">" +
            "<h2 style=\"color:#0f172a;margin:0 0 12px;font-size:20px;\">Khôi phục mật khẩu</h2>" +
            "<p style=\"color:#334155;line-height:1.6;margin:0 0 16px;\">Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn. " +
            "Mật khẩu tạm của bạn là:</p>" +
            $"<p style=\"font-family:ui-monospace,Menlo,Consolas,monospace;font-size:22px;letter-spacing:2px;background:#e2e8f0;color:#0f172a;text-align:center;padding:16px;border-radius:8px;margin:0 0 20px;\">{tempPassword}</p>" +
            "<p style=\"color:#334155;line-height:1.6;margin:0 0 12px;\"><b>Mật khẩu cũ đã bị vô hiệu.</b> Đăng nhập bằng mật khẩu tạm này, " +
            "rồi đổi mật khẩu ngay tại <b>Cá nhân → Bảo mật → Đổi mật khẩu</b>.</p>" +
            "<p style=\"color:#94a3b8;font-size:12px;line-height:1.5;margin:20px 0 0;\">Nếu bạn không yêu cầu email này, mật khẩu tài khoản " +
            "vừa được thay đổi — vui lòng liên hệ quản trị viên ngay lập tức.</p>" +
            "</div>";
    }
}
