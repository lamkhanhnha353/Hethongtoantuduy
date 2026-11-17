using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ToanTuDuy.Services.Storage
{
    public interface IChatAttachmentStorage
    {
        Task<ChatAttachmentUpload> UploadAsync(
            IFormFile? file, CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string url, string messageType, CancellationToken cancellationToken = default);
    }

    public sealed record ChatAttachmentUpload(string Url, string FileName, string MessageType);
}
