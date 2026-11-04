using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations(CancellationToken cancellationToken) =>
            Ok(new
            {
                success = true,
                data = await _chatService.GetHumanConversationsAsync(
                    User.GetAccountId(), User.GetRole(), cancellationToken)
            });

        [HttpGet("{conversationId:int}/messages")]
        public async Task<IActionResult> GetMessages(
            int conversationId,
            [FromQuery] int page = 1,
            CancellationToken cancellationToken = default) =>
            Ok(new
            {
                success = true,
                data = await _chatService.GetMessagesAsync(
                    User.GetAccountId(), User.GetRole(), conversationId, page, cancellationToken)
            });

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage(
            [FromBody] SendMessageRequest request,
            CancellationToken cancellationToken) =>
            Ok(new
            {
                success = true,
                data = await _chatService.SendMessageAsync(
                    User.GetAccountId(), User.GetRole(), request, cancellationToken)
            });

        [HttpPost("send-attachment")]
        [RequestSizeLimit(21 * 1024 * 1024)]
        public async Task<IActionResult> SendAttachment(
            [FromForm] SendChatAttachmentRequest request,
            CancellationToken cancellationToken) =>
            Ok(new
            {
                success = true,
                data = await _chatService.SendAttachmentAsync(
                    User.GetAccountId(), User.GetRole(), request, cancellationToken)
            });

        [HttpDelete("{conversationId:int}/messages/{messageId:int}")]
        public async Task<IActionResult> DeleteMessage(
            int conversationId, int messageId, CancellationToken cancellationToken)
        {
            await _chatService.DeleteMessageAsync(
                User.GetAccountId(), User.GetRole(), conversationId, messageId, cancellationToken);
            return Ok(new { success = true });
        }

    }
}
