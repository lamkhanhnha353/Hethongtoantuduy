using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ToanTuDuy.Services.Notifications
{
    public sealed record BusinessEmailRecipient(
        int? AccountId,
        string? Email,
        string DisplayName);

    public sealed record BusinessEmailPayload(
        string Subject,
        string Headline,
        string BodyText,
        IReadOnlyList<(string Label, string Value)>? Details = null);

    public interface IBusinessNotificationEmailService
    {
        Task SendAsync(
            BusinessEmailRecipient recipient,
            BusinessEmailPayload payload,
            CancellationToken cancellationToken = default);

        Task SendBatchAsync(
            IEnumerable<BusinessEmailRecipient> recipients,
            Func<BusinessEmailRecipient, BusinessEmailPayload> payloadFactory,
            CancellationToken cancellationToken = default);

        Task SendToTeacherAsync(
            int teacherId,
            BusinessEmailPayload payload,
            CancellationToken cancellationToken = default);

        Task SendToStudentParentAsync(
            int studentId,
            BusinessEmailPayload payload,
            CancellationToken cancellationToken = default);

        Task SendToAccountsAsync(
            IEnumerable<int> accountIds,
            BusinessEmailPayload payload,
            CancellationToken cancellationToken = default);
    }
}
