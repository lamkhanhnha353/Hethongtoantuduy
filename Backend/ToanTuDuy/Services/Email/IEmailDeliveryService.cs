using System.Threading;
using System.Threading.Tasks;

namespace ToanTuDuy.Services.Email
{
    public sealed record EmailDeliveryMessage(
        string ToEmail,
        string Subject,
        string? TextBody,
        string? HtmlBody,
        string? PreferredFromEmail = null);

    public interface IEmailDeliveryService
    {
        bool IsConfigured(out string? reason);

        Task SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default);
    }
}
