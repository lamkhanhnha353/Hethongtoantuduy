
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using ToanTuDuy.DTOs;
using Microsoft.AspNetCore.Http;

namespace ToanTuDuy.Services
{
    public interface IChatService
    {
        Task<List<ConversationDto>> GetHumanConversationsAsync(
            int accountId, string role, CancellationToken cancellationToken = default);

        Task<List<ChatMessageDto>> GetMessagesAsync(
            int accountId, string role, int conversationId, int page = 1,
            CancellationToken cancellationToken = default);

        Task<ChatMessageDto> SendMessageAsync(
            int accountId, string role, SendMessageRequest request,
            CancellationToken cancellationToken = default);

        Task<ChatMessageDto> SendAttachmentAsync(
            int accountId, string role, SendChatAttachmentRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteMessageAsync(
            int accountId, string role, int conversationId, int messageId,
            CancellationToken cancellationToken = default);
    }
}