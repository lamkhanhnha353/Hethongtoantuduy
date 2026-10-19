using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using ToanTuDuy.Data;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;
using ToanTuDuy.Options;

namespace ToanTuDuy.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IResend _resend;
        private readonly EmailVerificationSettings _settings;
        private readonly ILogger<EmailVerificationService> _logger;

        public EmailVerificationService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IResend resend,
            IOptions<EmailVerificationSettings> settings,
            ILogger<EmailVerificationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _resend = resend;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> IssueAsync(int accountId, string email)
        {
            // Token mới vô hiệu token cũ chưa dùng của cùng tài khoản (một link sống một lúc).
            var activeTokens = await _context.EmailVerificationTokens
                .Where(t => t.AccountId == accountId && t.UsedAt == null)
                .ToListAsync();

            if (activeTokens.Count > 0)
            {
                _context.EmailVerificationTokens.RemoveRange(activeTokens);
            }

            var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var now = DateTime.UtcNow;

            _context.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                AccountId = accountId,
                TokenHash = HashToken(rawToken),
                EmailAtIssue = email,
                ExpiresAt = now.AddHours(_settings.TokenExpiryHours),
                CreatedAt = now,
            });

            await _context.SaveChangesAsync();
            return rawToken;
        }

        public async Task SendEmailAsync(string email, string rawToken)
        {
            var link = BuildVerifyLink(rawToken);

            try
            {
                var message = new EmailMessage
                {
                    From = _settings.FromEmail,
                    Subject = "Xác minh email của bạn — Hệ thống Toán Tư Duy",
                    TextBody = BuildTextBody(link),
                    HtmlBody = BuildHtmlBody(link),
                };
                message.To.Add(email);

                await _resend.EmailSendAsync(message);
                // In link ra console để dev test được ngay khi không mở inbox.
                _logger.LogInformation("Đã gửi email xác minh tới {Email}. Link xác minh: {Link}", email, link);
            }
            catch (Exception ex)
            {
                // Không được làm hỏng luồng chính vì lỗi gửi mail — link vẫn in ra để test.
                _logger.LogWarning(
                    ex,
                    "Không gửi được email xác minh tới {Email} qua Resend. Link xác minh (dùng để test): {Link}",
                    email,
                    link);
            }
        }

        public async Task<VerifyEmailResult> VerifyAsync(string? rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return new VerifyEmailResult(VerifyEmailStatus.NotFound, null);
            }

            var hash = HashToken(rawToken);
            var token = await _context.EmailVerificationTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == hash);

            if (token == null)
            {
                return new VerifyEmailResult(VerifyEmailStatus.NotFound, null);
            }

            if (token.UsedAt != null)
            {
                return new VerifyEmailResult(VerifyEmailStatus.AlreadyUsed, null);
            }

            if (token.ExpiresAt <= DateTime.UtcNow)
            {
                return new VerifyEmailResult(VerifyEmailStatus.Expired, null);
            }

            var user = await _userManager.FindByIdAsync(token.AccountId.ToString());
            if (user == null
                || string.IsNullOrEmpty(user.Email)
                || !string.Equals(user.Email, token.EmailAtIssue, StringComparison.OrdinalIgnoreCase))
            {
                // Email đã bị thay đổi sau khi cấp link — link cũ không còn giá trị.
                return new VerifyEmailResult(VerifyEmailStatus.EmailChanged, null);
            }

            var now = DateTime.UtcNow;

            // Claim nguyên tử: chỉ một request được đánh dấu UsedAt (chống click trùng).
            var claimed = await _context.EmailVerificationTokens
                .Where(t => t.Id == token.Id && t.UsedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now));

            if (claimed == 0)
            {
                return new VerifyEmailResult(VerifyEmailStatus.AlreadyUsed, null);
            }

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw ApiException.BadRequest($"Không thể kích hoạt xác minh email: {errors}");
            }

            _logger.LogInformation("Email {Email} đã được xác minh (account {AccountId}).", token.EmailAtIssue, token.AccountId);
            return new VerifyEmailResult(VerifyEmailStatus.Success, token.EmailAtIssue);
        }

        public async Task ResendAsync(int accountId, string email)
        {
            var latest = await _context.EmailVerificationTokens
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (latest != null)
            {
                var elapsedSeconds = (DateTime.UtcNow - latest.CreatedAt).TotalSeconds;
                if (elapsedSeconds < _settings.ResendCooldownSeconds)
                {
                    var waitSeconds = (int)Math.Ceiling(_settings.ResendCooldownSeconds - elapsedSeconds);
                    throw ApiException.TooManyRequests(
                        $"Vui lòng đợi khoảng {waitSeconds} giây trước khi gửi lại link xác minh.");
                }
            }

            var rawToken = await IssueAsync(accountId, email);
            await SendEmailAsync(email, rawToken);
        }

        private string BuildVerifyLink(string rawToken)
        {
            var baseUrl = (_settings.BaseUrl ?? string.Empty).TrimEnd('/');
            return $"{baseUrl}/api/email-verification?token={Uri.EscapeDataString(rawToken)}";
        }

        private static string HashToken(string rawToken)
        {
            var bytes = Encoding.UTF8.GetBytes(rawToken);
            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        private string BuildTextBody(string link) =>
            "Xác minh địa chỉ email của bạn cho tài khoản Hệ thống Toán Tư Duy.\n\n" +
            $"Bấm vào link sau (có hiệu lực trong {_settings.TokenExpiryHours} giờ):\n{link}\n\n" +
            "Nếu bạn không yêu cầu, hãy bỏ qua email này.";

        private string BuildHtmlBody(string link) =>
            "<div style=\"font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:0 auto;padding:32px 24px;background:#f8fafc;border-radius:12px;\">" +
            "<h2 style=\"color:#0f172a;margin:0 0 12px;font-size:20px;\">Xác minh email của bạn</h2>" +
            "<p style=\"color:#334155;line-height:1.6;margin:0 0 20px;\">Chúng tôi nhận được yêu cầu xác minh địa chỉ email này cho tài khoản Hệ thống Toán Tư Duy. " +
            $"Bấm nút bên dưới để xác minh (link có hiệu lực trong {_settings.TokenExpiryHours} giờ):</p>" +
            $"<a href=\"{link}\" style=\"display:inline-block;background:#0284c7;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:8px;font-weight:bold;\">Xác minh email</a>" +
            "<p style=\"color:#94a3b8;font-size:12px;line-height:1.5;margin:20px 0 0;\">Nếu bạn không yêu cầu, hãy bỏ qua email này. " +
            $"Nếu nút không bấm được, sao chép link sau vào trình duyệt:<br/><span style=\"word-break:break-all;\">{link}</span></p>" +
            "</div>";
    }
}
