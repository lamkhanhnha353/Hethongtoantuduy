using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Hubs;
using ToanTuDuy.Models;
using ToanTuDuy.Services.Storage;

namespace ToanTuDuy.Services
{
    public class ChatService : IChatService
    {
        private const int PageSize = 30;
        private const int MaxPage = 10000;
        private const string ParentRole = "PARENT";
        private const string TeacherRole = "TEACHER";
        private const string HumanChatType = "HUMAN";

        private readonly ApplicationDbContext _context;
        private readonly IChatAttachmentStorage _attachmentStorage;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ApplicationDbContext context,
            IChatAttachmentStorage attachmentStorage,
            IHubContext<ChatHub> hubContext,
            ILogger<ChatService> logger)
        {
            _context = context;
            _attachmentStorage = attachmentStorage;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<List<ConversationDto>> GetHumanConversationsAsync(
            int accountId, string role, CancellationToken cancellationToken = default)
        {
            var actor = await ResolveActorAsync(accountId, role, cancellationToken);
            var query = _context.Conversations
                .AsNoTracking()
                .Where(conversation => conversation.ChatType == HumanChatType
                    && conversation.TeacherId != null);

            query = actor.Role == ParentRole
                ? query.Where(conversation => conversation.ParentId == actor.ProfileId)
                : query.Where(conversation => conversation.TeacherId == actor.ProfileId);

            return await query
                .OrderByDescending(conversation => conversation.LastMessageAt)
                .Select(conversation => new ConversationDto
                {
                    Id = conversation.Id,
                    ChatType = conversation.ChatType,
                    PartnerId = actor.Role == ParentRole
                        ? conversation.TeacherId!.Value
                        : conversation.ParentId,
                    PartnerName = actor.Role == ParentRole
                        ? conversation.Teacher!.FullName
                        : conversation.Parent.FullName,
                    PartnerAvatar = actor.Role == ParentRole ? null : conversation.Parent.Avatar,
                    LastMessage = conversation.LastMessage,
                    LastMessageAt = conversation.LastMessageAt,
                    UnreadCount = 0
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ConversationDto>> GetAdminHumanConversationsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .AsNoTracking()
                .Where(conversation =>
                    conversation.ChatType == HumanChatType && conversation.TeacherId != null)
                .OrderByDescending(conversation => conversation.LastMessageAt)
                .Select(conversation => new ConversationDto
                {
                    Id = conversation.Id,
                    ChatType = conversation.ChatType,
                    PartnerId = conversation.ParentId,
                    PartnerName = conversation.Parent.FullName,
                    PartnerAvatar = conversation.Parent.Avatar,
                    LastMessage = conversation.LastMessage,
                    LastMessageAt = conversation.LastMessageAt,
                    LastMessageSenderType = _context.Messages
                        .Where(message => message.ConversationId == conversation.Id)
                        .OrderByDescending(message => message.CreatedAt)
                        .ThenByDescending(message => message.Id)
                        .Select(message => message.SenderType)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ChatMessageDto>> GetMessagesAsync(
            int accountId, string role, int conversationId, int page = 1,
            CancellationToken cancellationToken = default)
        {
            if (page < 1 || page > MaxPage)
            {
                throw ApiException.BadRequest($"Số trang phải từ 1 đến {MaxPage}.");
            }

            if (!await CanAccessConversationAsync(accountId, role, conversationId, cancellationToken))
            {
                var actor = await ResolveActorAsync(accountId, role, cancellationToken);
                var conversation = await _context.Conversations
                    .AsNoTracking()
                    .Where(item => item.Id == conversationId && item.ChatType == HumanChatType)
                    .Select(item => new { item.ParentId, item.TeacherId })
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw ApiException.NotFound("Không tìm thấy cuộc hội thoại.");

                if ((actor.Role == ParentRole && conversation.ParentId != actor.ProfileId)
                    || (actor.Role == TeacherRole && conversation.TeacherId != actor.ProfileId))
                {
                    throw ApiException.Forbidden("Bạn không có quyền xem cuộc hội thoại này.");
                }
            }

            var messages = await _context.Messages
                .AsNoTracking()
                .Where(message => message.ConversationId == conversationId)
                .OrderByDescending(message => message.CreatedAt)
                .ThenByDescending(message => message.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(message => new ChatMessageDto
                {
                    Id = message.Id,
                    ConversationId = message.ConversationId,
                    SenderType = message.SenderType,
                    SenderId = message.SenderId,
                    MessageType = message.MessageType,
                    Content = message.Content,
                    AttachmentUrl = message.AttachmentUrl,
                    IsRead = message.IsRead,
                    CreatedAt = message.CreatedAt
                })
                .ToListAsync(cancellationToken);

            messages.Reverse();
            return messages;
        }

        public async Task DeleteMessageAsync(
            int accountId, string role, int conversationId, int messageId,
            CancellationToken cancellationToken = default)
        {
            var message = await GetAccessibleMessageAsync(
                accountId, role, conversationId, messageId, cancellationToken);
            var conversation = await _context.Conversations
                .FirstAsync(item => item.Id == conversationId, cancellationToken);
            var otherAccountId = await GetOtherParticipantAccountIdAsync(
                role, conversation, cancellationToken);
            var deletedMessageId = message.Id;
            var deletedMessageTime = message.CreatedAt;

            if (!string.IsNullOrWhiteSpace(message.AttachmentUrl))
            {
                await _attachmentStorage.DeleteAsync(
                    message.AttachmentUrl, message.MessageType, cancellationToken);
            }

            _context.Messages.Remove(message);
            if (conversation.LastMessageAt == deletedMessageTime)
            {
                var previousMessage = await _context.Messages
                    .AsNoTracking()
                    .Where(item => item.ConversationId == conversationId && item.Id != deletedMessageId)
                    .OrderByDescending(item => item.CreatedAt)
                    .ThenByDescending(item => item.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                conversation.LastMessage = previousMessage is null
                    ? null
                    : GetConversationPreview(previousMessage);
                conversation.LastMessageAt = previousMessage?.CreatedAt ?? conversation.CreatedAt;
            }

            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await _hubContext.Clients.Users(new[] { accountId.ToString(), otherAccountId.ToString() })
                    .SendAsync(
                        "MessageDeleted",
                        conversationId,
                        deletedMessageId,
                        CancellationToken.None);
            }
            catch (HubException exception)
            {
                _logger.LogWarning(exception,
                    "Tin nhắn {MessageId} đã xóa vĩnh viễn nhưng không thể phát SignalR cho hội thoại {ConversationId}.",
                    deletedMessageId, conversationId);
            }
        }

        private async Task<Message> GetAccessibleMessageAsync(
            int accountId, string role, int conversationId, int messageId,
            CancellationToken cancellationToken)
        {
            if (!await CanAccessConversationAsync(accountId, role, conversationId, cancellationToken))
            {
                throw ApiException.Forbidden("Bạn không có quyền thao tác với cuộc hội thoại này.");
            }

            return await _context.Messages
                .FirstOrDefaultAsync(
                    item => item.Id == messageId && item.ConversationId == conversationId,
                    cancellationToken)
                ?? throw ApiException.NotFound("Không tìm thấy tin nhắn.");
        }

        private async Task<int> GetOtherParticipantAccountIdAsync(
            string role, Conversation conversation,
            CancellationToken cancellationToken)
        {
            if (string.Equals(role, ParentRole, StringComparison.OrdinalIgnoreCase))
            {
                return await _context.TeacherProfiles
                    .Where(profile => profile.Id == conversation.TeacherId)
                    .Select(profile => profile.AccountId)
                    .FirstAsync(cancellationToken);
            }

            return await _context.ParentProfiles
                .Where(profile => profile.Id == conversation.ParentId)
                .Select(profile => profile.AccountId)
                .FirstAsync(cancellationToken);
        }

        private static string GetConversationPreview(Message message)
        {
            var preview = message.MessageType switch
            {
                "TEXT" => message.Content,
                "IMAGE" => "[Hình ảnh]",
                "RECALLED" => "Tin nhắn đã được thu hồi",
                _ => $"[Tệp] {message.Content}"
            };
            return preview is { Length: > 500 } ? preview[..500] : preview ?? string.Empty;
        }

        private Task<bool> CanAccessConversationAsync(
            int accountId, string role, int conversationId, CancellationToken cancellationToken)
        {
            var normalizedRole = role.ToUpperInvariant();
            if (normalizedRole == ParentRole)
            {
                return (
                    from profile in _context.ParentProfiles.AsNoTracking()
                    join conversation in _context.Conversations.AsNoTracking()
                        on profile.Id equals conversation.ParentId
                    where profile.AccountId == accountId
                        && conversation.Id == conversationId
                        && conversation.ChatType == HumanChatType
                    select conversation.Id)
                    .AnyAsync(cancellationToken);
            }

            if (normalizedRole == TeacherRole)
            {
                return (
                    from profile in _context.TeacherProfiles.AsNoTracking()
                    join conversation in _context.Conversations.AsNoTracking()
                        on profile.Id equals conversation.TeacherId
                    where profile.AccountId == accountId
                        && conversation.Id == conversationId
                        && conversation.ChatType == HumanChatType
                    select conversation.Id)
                    .AnyAsync(cancellationToken);
            }

            return Task.FromResult(false);
        }

        public async Task<ChatMessageDto> SendMessageAsync(
            int accountId, string role, SendMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            var actor = await ResolveActorAsync(accountId, role, cancellationToken);
            var content = request.Content?.Trim();
            if (!string.Equals(request.MessageType, "TEXT", StringComparison.OrdinalIgnoreCase))
            {
                throw ApiException.BadRequest("Tệp đính kèm cần được gửi qua chức năng tải tệp.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw ApiException.BadRequest("Nội dung tin nhắn không được để trống.");
            }

            var (parentId, teacherId, recipientAccountId) = await ResolveConversationPairAsync(
                actor, request.ReceiverId, request.ReceiverType, cancellationToken);
            await EnsureTeacherAssignedToParentAsync(parentId, teacherId, cancellationToken);

            var conversation = await GetOrCreateConversationAsync(
                parentId, teacherId, cancellationToken);

            var message = await SaveMessageAsync(
                conversation, actor, "TEXT", content, null, cancellationToken);
            await NotifyRecipientAsync(recipientAccountId, message);
            return message;
        }

        public async Task<ChatMessageDto> SendAttachmentAsync(
            int accountId, string role, SendChatAttachmentRequest request,
            CancellationToken cancellationToken = default)
        {
            var actor = await ResolveActorAsync(accountId, role, cancellationToken);
            var (parentId, teacherId, recipientAccountId) = await ResolveConversationPairAsync(
                actor, request.ReceiverId, request.ReceiverType, cancellationToken);
            await EnsureTeacherAssignedToParentAsync(parentId, teacherId, cancellationToken);

            var attachment = await _attachmentStorage.UploadAsync(request.File, cancellationToken);
            var conversation = await GetOrCreateConversationAsync(
                parentId, teacherId, cancellationToken);
            var message = await SaveMessageAsync(
                conversation,
                actor,
                attachment.MessageType,
                attachment.FileName,
                attachment.Url,
                cancellationToken);
            await NotifyRecipientAsync(recipientAccountId, message);
            return message;
        }

        private async Task EnsureTeacherAssignedToParentAsync(
            int parentId, int teacherId, CancellationToken cancellationToken)
        {
            var isAuthorized = await (
                from student in _context.StudentProfiles
                join classStudent in _context.ClassStudents on student.Id equals classStudent.StudentId
                join classroom in _context.Classes on classStudent.ClassId equals classroom.Id
                where student.ParentId == parentId && classroom.TeacherId == teacherId
                select student.Id)
                .AnyAsync(cancellationToken);

            if (!isAuthorized)
            {
                throw ApiException.Forbidden(
                    "Bạn không có quyền nhắn tin với giáo viên này vì giáo viên không phụ trách lớp của con bạn.");
            }
        }

        private async Task<(int ParentId, int TeacherId, int RecipientAccountId)> ResolveConversationPairAsync(
            ChatActor actor, int receiverId, string receiverType, CancellationToken cancellationToken)
        {
            var expectedReceiverType = actor.Role == ParentRole ? TeacherRole : ParentRole;
            if (!string.Equals(receiverType, expectedReceiverType, StringComparison.OrdinalIgnoreCase))
            {
                throw ApiException.BadRequest("Loại người nhận không hợp lệ.");
            }

            if (actor.Role == ParentRole)
            {
                var teacher = await _context.TeacherProfiles
                    .AsNoTracking()
                    .Where(profile => profile.Id == receiverId)
                    .Select(profile => new { profile.Id, profile.AccountId })
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw ApiException.NotFound("Không tìm thấy hồ sơ giáo viên.");
                return (actor.ProfileId, teacher.Id, teacher.AccountId);
            }

            var parent = await _context.ParentProfiles
                .AsNoTracking()
                .Where(profile => profile.Id == receiverId)
                .Select(profile => new { profile.Id, profile.AccountId })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");
            return (parent.Id, actor.ProfileId, parent.AccountId);
        }

        private async Task<Conversation> GetOrCreateConversationAsync(
            int parentId, int teacherId, CancellationToken cancellationToken)
        {
            var conversation = await _context.Conversations.FirstOrDefaultAsync(
                item => item.ParentId == parentId
                    && item.TeacherId == teacherId
                    && item.ChatType == HumanChatType,
                cancellationToken);

            if (conversation is not null)
            {
                return conversation;
            }

            conversation = new Conversation
            {
                ParentId = parentId,
                TeacherId = teacherId,
                ChatType = HumanChatType,
                CreatedAt = DateTime.UtcNow
            };
            _context.Conversations.Add(conversation);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return conversation;
            }
            catch (DbUpdateException)
            {
                _context.Entry(conversation).State = EntityState.Detached;
                var existing = await _context.Conversations.FirstOrDefaultAsync(
                    item => item.ParentId == parentId
                        && item.TeacherId == teacherId
                        && item.ChatType == HumanChatType,
                    cancellationToken);

                if (existing is null)
                {
                    throw;
                }

                return existing;
            }
        }

        private async Task<ChatMessageDto> SaveMessageAsync(
            Conversation conversation,
            ChatActor actor,
            string messageType,
            string? content,
            string? attachmentUrl,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderType = actor.Role,
                SenderId = actor.ProfileId,
                MessageType = messageType,
                Content = content,
                AttachmentUrl = attachmentUrl,
                IsRead = false,
                CreatedAt = now
            };

            var preview = messageType == "TEXT"
                ? content
                : messageType == "IMAGE" ? "[Hình ảnh]" : $"[Tệp] {content}";
            conversation.LastMessage = preview is { Length: > 500 } ? preview[..500] : preview;
            conversation.LastMessageAt = now;
            _context.Messages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);

            return new ChatMessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderType = message.SenderType,
                SenderId = message.SenderId,
                MessageType = message.MessageType,
                Content = message.Content,
                AttachmentUrl = message.AttachmentUrl,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt
            };
        }

        private async Task NotifyRecipientAsync(
            int recipientAccountId, ChatMessageDto message)
        {
            try
            {
                await _hubContext.Clients.User(recipientAccountId.ToString())
                    .SendAsync("ReceiveNewMessage", message, CancellationToken.None);
            }
            catch (HubException exception)
            {
                _logger.LogWarning(exception,
                    "Tin nhắn {MessageId} đã lưu nhưng không thể phát SignalR tới tài khoản {AccountId}.",
                    message.Id, recipientAccountId);
            }
        }

        private static ChatMessageDto ToChatMessageDto(Message message) => new()
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderType = message.SenderType,
            SenderId = message.SenderId,
            MessageType = message.MessageType,
            Content = message.Content,
            AttachmentUrl = message.AttachmentUrl,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };

        private async Task<ChatActor> ResolveActorAsync(
            int accountId, string role, CancellationToken cancellationToken)
        {
            var normalizedRole = role.ToUpperInvariant();
            if (normalizedRole == ParentRole)
            {
                var profileId = await _context.ParentProfiles
                    .AsNoTracking()
                    .Where(profile => profile.AccountId == accountId)
                    .Select(profile => (int?)profile.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");
                return new ChatActor(profileId, ParentRole);
            }

            if (normalizedRole == TeacherRole)
            {
                var profileId = await _context.TeacherProfiles
                    .AsNoTracking()
                    .Where(profile => profile.AccountId == accountId)
                    .Select(profile => (int?)profile.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw ApiException.NotFound("Không tìm thấy hồ sơ giáo viên.");
                return new ChatActor(profileId, TeacherRole);
            }

            throw ApiException.Forbidden("Vai trò của bạn không được phép sử dụng tính năng chat.");
        }

        private sealed record ChatActor(int ProfileId, string Role);
    }
}
