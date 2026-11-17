using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using ToanTuDuy.Options;

namespace ToanTuDuy.Services.Email
{
    /// <summary>
    /// Lớp gửi email thống nhất: ưu tiên SMTP (hỗ trợ Gmail App Password / SMTP riêng) khi đã cấu hình,
    /// nếu chưa có sẽ dùng Resend API khi có Resend:ApiToken.
    /// </summary>
    public sealed class EmailDeliveryService : IEmailDeliveryService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _configuration;
        private readonly SmtpSettings _smtp;
        private readonly EmailVerificationSettings _emailSettings;
        private readonly ILogger<EmailDeliveryService> _logger;

        public EmailDeliveryService(
            IResend resend,
            IConfiguration configuration,
            IOptions<SmtpSettings> smtpSettings,
            IOptions<EmailVerificationSettings> emailSettings,
            ILogger<EmailDeliveryService> logger)
        {
            _resend = resend;
            _configuration = configuration;
            _smtp = smtpSettings.Value;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        private bool HasSmtpCredentials =>
            string.IsNullOrWhiteSpace(_smtp.Username) || !string.IsNullOrWhiteSpace(_smtp.Password);

        private string? ResolveSmtpFromEmail(string? preferredFromEmail) =>
            !string.IsNullOrWhiteSpace(_smtp.FromEmail)
                ? _smtp.FromEmail.Trim()
                : (!string.IsNullOrWhiteSpace(_smtp.Username) && _smtp.Username.Contains('@')
                    ? _smtp.Username.Trim()
                    : (!string.IsNullOrWhiteSpace(preferredFromEmail)
                        ? preferredFromEmail.Trim()
                        : _emailSettings.FromEmail?.Trim()));

        private bool IsSmtpConfigured() =>
            !string.IsNullOrWhiteSpace(_smtp.Host)
            && _smtp.Port > 0
            && HasSmtpCredentials
            && !string.IsNullOrWhiteSpace(ResolveSmtpFromEmail(null));

        private bool IsResendConfigured() =>
            !string.IsNullOrWhiteSpace(_configuration["Resend:ApiToken"])
            && !string.IsNullOrWhiteSpace(_emailSettings.FromEmail);

        public bool IsConfigured(out string? reason)
        {
            if (IsSmtpConfigured() || IsResendConfigured())
            {
                reason = null;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(_smtp.Host) && !HasSmtpCredentials)
            {
                reason = "Đã khai báo Smtp:Host nhưng thiếu Smtp:Password (App Password). Vui lòng bổ sung trong .env hoặc appsettings.json.";
                return false;
            }

            reason = "Chưa cấu hình dịch vụ gửi email (SMTP hoặc Resend:ApiToken). Vui lòng thiết lập Smtp:* (ví dụ Gmail App Password) hoặc Resend:ApiToken để gửi ra email cá nhân.";
            return false;
        }

        public async Task SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            var recipient = (message.ToEmail ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(recipient))
            {
                throw new ArgumentException("Địa chỉ email người nhận không được để trống.", nameof(message));
            }

            if (IsSmtpConfigured())
            {
                await SendViaSmtpAsync(message, recipient, cancellationToken);
                return;
            }

            if (IsResendConfigured())
            {
                await SendViaResendAsync(message, recipient, cancellationToken);
                return;
            }

            IsConfigured(out var reason);
            throw new InvalidOperationException(reason ?? "Chưa cấu hình dịch vụ gửi email.");
        }

        private async Task SendViaSmtpAsync(
            EmailDeliveryMessage message,
            string recipient,
            CancellationToken cancellationToken)
        {
            var fromAddress = ResolveSmtpFromEmail(message.PreferredFromEmail)
                ?? throw new InvalidOperationException("Chưa cấu hình Smtp:FromEmail.");

            using var mailMessage = new MailMessage
            {
                From = string.IsNullOrWhiteSpace(_smtp.FromName)
                    ? new MailAddress(fromAddress)
                    : new MailAddress(fromAddress, _smtp.FromName.Trim(), Encoding.UTF8),
                Subject = message.Subject,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
            };
            mailMessage.To.Add(new MailAddress(recipient));

            if (!string.IsNullOrWhiteSpace(message.TextBody) && !string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                mailMessage.AlternateViews.Add(
                    AlternateView.CreateAlternateViewFromString(message.TextBody, Encoding.UTF8, MediaTypeNames.Text.Plain));
                mailMessage.AlternateViews.Add(
                    AlternateView.CreateAlternateViewFromString(message.HtmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));
            }
            else if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                mailMessage.Body = message.HtmlBody;
                mailMessage.IsBodyHtml = true;
            }
            else
            {
                mailMessage.Body = message.TextBody ?? string.Empty;
                mailMessage.IsBodyHtml = false;
            }

            using var client = new SmtpClient(_smtp.Host.Trim(), _smtp.Port)
            {
                EnableSsl = _smtp.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000,
            };

            if (!string.IsNullOrWhiteSpace(_smtp.Username))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_smtp.Username.Trim(), _smtp.Password);
            }

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Đã gửi email qua SMTP ({Host}:{Port}) tới {Email} (Chủ đề: {Subject}).",
                _smtp.Host, _smtp.Port, recipient, message.Subject);
        }

        private async Task SendViaResendAsync(
            EmailDeliveryMessage message,
            string recipient,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fromEmail = !string.IsNullOrWhiteSpace(message.PreferredFromEmail)
                ? message.PreferredFromEmail.Trim()
                : _emailSettings.FromEmail.Trim();

            var email = new EmailMessage
            {
                From = fromEmail,
                Subject = message.Subject,
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody,
            };
            email.To.Add(recipient);

            await _resend.EmailSendAsync(email);
            _logger.LogInformation("Đã gửi email qua Resend tới {Email} (Chủ đề: {Subject}).",
                recipient, message.Subject);
        }
    }
}
