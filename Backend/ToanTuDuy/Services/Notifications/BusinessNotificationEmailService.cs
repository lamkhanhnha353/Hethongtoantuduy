using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToanTuDuy.Data;
using ToanTuDuy.Services.Email;

namespace ToanTuDuy.Services.Notifications
{
    public sealed class BusinessNotificationEmailService : IBusinessNotificationEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailDeliveryService _emailDeliveryService;
        private readonly ILogger<BusinessNotificationEmailService> _logger;

        public BusinessNotificationEmailService(
            ApplicationDbContext context,
            IEmailDeliveryService emailDeliveryService,
            ILogger<BusinessNotificationEmailService> logger)
        {
            _context = context;
            _emailDeliveryService = emailDeliveryService;
            _logger = logger;
        }

        public Task SendAsync(
            BusinessEmailRecipient recipient,
            BusinessEmailPayload payload,
            CancellationToken cancellationToken = default)
        {
            return SendBatchAsync(new[] { recipient }, _ => payload, cancellationToken);
        }

        public async Task SendBatchAsync(
            IEnumerable<BusinessEmailRecipient> recipients,
            Func<BusinessEmailRecipient, BusinessEmailPayload> payloadFactory,
            CancellationToken cancellationToken = default)
        {
            if (!_emailDeliveryService.IsConfigured(out var configReason))
            {
                _logger.LogWarning("Bỏ qua gửi email thông báo nghiệp vụ: {Reason}", configReason);
                return;
            }

            var validRecipients = recipients
                .Where(r => !string.IsNullOrWhiteSpace(r.Email))
                .GroupBy(r => r.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First() with { Email = group.Key })
                .ToList();

            if (validRecipients.Count == 0)
            {
                return;
            }

            var accountIds = validRecipients
                .Where(r => r.AccountId.HasValue)
                .Select(r => r.AccountId!.Value)
                .Distinct()
                .ToList();

            var optedOutAccountIds = accountIds.Count == 0
                ? new HashSet<int>()
                : await _context.NotificationSettings
                    .AsNoTracking()
                    .Where(setting => accountIds.Contains(setting.AccountId)
                        && setting.Type == NotificationSettingTypes.Email
                        && !setting.Enabled)
                    .Select(setting => setting.AccountId)
                    .ToHashSetAsync(cancellationToken);

            foreach (var recipient in validRecipients)
            {
                if (recipient.AccountId.HasValue && optedOutAccountIds.Contains(recipient.AccountId.Value))
                {
                    continue;
                }

                var payload = payloadFactory(recipient);
                try
                {
                    await _emailDeliveryService.SendAsync(
                        new EmailDeliveryMessage(
                            recipient.Email!,
                            payload.Subject,
                            BuildTextBody(recipient.DisplayName, payload),
                            BuildHtmlBody(recipient.DisplayName, payload)),
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Lỗi gửi email không được làm gián đoạn nghiệp vụ chính.
                    _logger.LogWarning(
                        ex,
                        "Không gửi được email thông báo nghiệp vụ tới {Email} (Chủ đề: {Subject}).",
                        recipient.Email,
                        payload.Subject);
                }
            }
        }

        public async Task SendToTeacherAsync(
            int teacherId,
            BusinessEmailPayload payload,
            CancellationToken cancellationToken = default)
        {
            var teacher = await _context.TeacherProfiles
                .AsNoTracking()
                .Where(profile => profile.Id == teacherId)
                .Select(profile => new BusinessEmailRecipient(
                    profile.AccountId,
                    !string.IsNullOrWhiteSpace(profile.Email) ? profile.Email : profile.Account.Email,
                    profile.FullName))
                .FirstOrDefaultAsync(cancellationToken);

            if (teacher == null || string.IsNullOrWhiteSpace(teacher.Email))
            {
                _logger.LogDebug("Giáo viên {TeacherId} không có địa chỉ email cá nhân để gửi thông báo.", teacherId);
                return;
            }

            await SendAsync(teacher, payload, cancellationToken);
        }

        public async Task SendToStudentParentAsync(
            int studentId,
            BusinessEmailPayload payload,
            CancellationToken cancellationToken = default)
        {
            var parent = await _context.StudentProfiles
                .AsNoTracking()
                .Where(student => student.Id == studentId)
                .Select(student => new BusinessEmailRecipient(
                    student.Parent.AccountId,
                    student.Parent.Account.Email,
                    student.Parent.FullName))
                .FirstOrDefaultAsync(cancellationToken);

            if (parent == null || string.IsNullOrWhiteSpace(parent.Email))
            {
                _logger.LogDebug("Phụ huynh của học sinh {StudentId} không có email cá nhân để gửi thông báo.", studentId);
                return;
            }

            await SendAsync(parent, payload, cancellationToken);
        }

        public async Task SendToAccountsAsync(
            IEnumerable<int> accountIds,
            BusinessEmailPayload payload,
            CancellationToken cancellationToken = default)
        {
            var normalizedIds = accountIds.Where(id => id > 0).Distinct().ToList();
            if (normalizedIds.Count == 0)
            {
                return;
            }

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => normalizedIds.Contains(u.Id) && u.IsActive)
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync(cancellationToken);

            if (users.Count == 0)
            {
                return;
            }

            var teachers = (await _context.TeacherProfiles
                    .AsNoTracking()
                    .Where(t => normalizedIds.Contains(t.AccountId))
                    .Select(t => new { t.AccountId, t.FullName, t.Email })
                    .ToListAsync(cancellationToken))
                .GroupBy(t => t.AccountId)
                .ToDictionary(g => g.Key, g => g.First());

            var parents = (await _context.ParentProfiles
                    .AsNoTracking()
                    .Where(p => normalizedIds.Contains(p.AccountId))
                    .Select(p => new { p.AccountId, p.FullName })
                    .ToListAsync(cancellationToken))
                .GroupBy(p => p.AccountId)
                .ToDictionary(g => g.Key, g => g.First().FullName);

            var admins = (await _context.AdminProfiles
                    .AsNoTracking()
                    .Where(a => normalizedIds.Contains(a.AccountId))
                    .Select(a => new { a.AccountId, a.FullName })
                    .ToListAsync(cancellationToken))
                .GroupBy(a => a.AccountId)
                .ToDictionary(g => g.Key, g => g.First().FullName);

            var recipients = users.Select(user =>
            {
                teachers.TryGetValue(user.Id, out var teacher);
                var personalEmail = !string.IsNullOrWhiteSpace(teacher?.Email) ? teacher!.Email : user.Email;
                var displayName = !string.IsNullOrWhiteSpace(teacher?.FullName)
                    ? teacher!.FullName
                    : (parents.TryGetValue(user.Id, out var parentName) && !string.IsNullOrWhiteSpace(parentName)
                        ? parentName
                        : (admins.TryGetValue(user.Id, out var adminName) && !string.IsNullOrWhiteSpace(adminName)
                            ? adminName
                            : (user.UserName ?? user.Email ?? "Quý thầy/cô và phụ huynh")));
                return new BusinessEmailRecipient(user.Id, personalEmail, displayName);
            });

            await SendBatchAsync(recipients, _ => payload, cancellationToken);
        }

        private static string BuildTextBody(string displayName, BusinessEmailPayload payload)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Xin chào {displayName},");
            sb.AppendLine();
            sb.AppendLine(payload.Headline);
            sb.AppendLine(payload.BodyText);

            if (payload.Details != null && payload.Details.Count > 0)
            {
                sb.AppendLine();
                foreach (var (label, value) in payload.Details)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        sb.AppendLine($"- {label}: {value}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Vui lòng đăng nhập hệ thống Toán Tư Duy để xem chi tiết.");
            return sb.ToString();
        }

        private static string BuildHtmlBody(string displayName, BusinessEmailPayload payload)
        {
            var encoder = HtmlEncoder.Default;
            var detailsHtml = new StringBuilder();

            if (payload.Details != null && payload.Details.Count > 0)
            {
                detailsHtml.Append("<table role=\"presentation\" style=\"width:100%;border-collapse:collapse;margin:16px 0;background:#fffaf5;border:1px solid #fed7aa;border-radius:8px;\">");
                foreach (var (label, value) in payload.Details)
                {
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    detailsHtml.Append("<tr>");
                    detailsHtml.Append($"<td style=\"padding:10px 14px;font-size:13px;font-weight:600;color:#78716c;width:36%;border-bottom:1px solid #ffedd5;\">{encoder.Encode(label)}</td>");
                    detailsHtml.Append($"<td style=\"padding:10px 14px;font-size:13px;color:#1c1917;border-bottom:1px solid #ffedd5;\">{encoder.Encode(value)}</td>");
                    detailsHtml.Append("</tr>");
                }
                detailsHtml.Append("</table>");
            }

            return $$"""
                <div style="font-family:Arial,Helvetica,sans-serif;background-color:#f5f5f4;padding:24px 12px;color:#1c1917;">
                  <div style="max-width:580px;margin:0 auto;background:#ffffff;border:1px solid #e7e5e4;border-radius:12px;overflow:hidden;">
                    <div style="background:#ea580c;padding:18px 24px;color:#ffffff;">
                      <div style="font-size:12px;font-weight:700;letter-spacing:0.08em;text-transform:uppercase;opacity:0.9;">Hệ thống Toán Tư Duy</div>
                      <h2 style="margin:6px 0 0 0;font-size:18px;font-weight:700;line-height:1.35;">{{encoder.Encode(payload.Headline)}}</h2>
                    </div>
                    <div style="padding:24px;">
                      <p style="margin:0 0 12px 0;font-size:14px;line-height:1.6;">Xin chào <strong>{{encoder.Encode(displayName)}}</strong>,</p>
                      <p style="margin:0 0 14px 0;font-size:14px;line-height:1.6;color:#292524;">{{encoder.Encode(payload.BodyText)}}</p>
                      {{detailsHtml}}
                      <p style="margin:18px 0 0 0;font-size:12px;color:#78716c;line-height:1.5;">Đây là thông báo tự động từ hệ thống Toán Tư Duy. Bạn có thể đăng nhập vào hệ thống để theo dõi thêm chi tiết.</p>
                    </div>
                  </div>
                </div>
                """;
        }
    }
}
