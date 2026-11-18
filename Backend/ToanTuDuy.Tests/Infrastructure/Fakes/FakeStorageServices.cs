using Microsoft.AspNetCore.Http;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Services.Storage;

namespace ToanTuDuy.Tests.Infrastructure.Fakes;

public sealed class FakeImageStorage : IImageStorage
{
    private readonly object _lock = new();
    public List<string> UploadedUrls { get; } = [];
    public List<string> DeletedUrls { get; } = [];

    public void Reset()
    {
        lock (_lock)
        {
            UploadedUrls.Clear();
            DeletedUrls.Clear();
        }
    }

    public Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://res.cloudinary.com/testcloud/image/upload/v1/{folder}/{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        lock (_lock)
        {
            UploadedUrls.Add(url);
        }
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            DeletedUrls.Add(imageUrl);
        }
        return Task.CompletedTask;
    }
}

public sealed class FakeChatAttachmentStorage : IChatAttachmentStorage
{
    private const long MaxFileBytes = 20 * 1024 * 1024;
    private static readonly Dictionary<string, (string ContentType, string MessageType)> AllowedFiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ("image/jpeg", "IMAGE"),
            [".jpeg"] = ("image/jpeg", "IMAGE"),
            [".png"] = ("image/png", "IMAGE"),
            [".webp"] = ("image/webp", "IMAGE"),
            [".pdf"] = ("application/pdf", "FILE"),
            [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "FILE")
        };

    private readonly object _lock = new();
    public List<string> DeletedUrls { get; } = [];

    public void Reset()
    {
        lock (_lock)
        {
            DeletedUrls.Clear();
        }
    }

    public Task<ChatAttachmentUpload> UploadAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw ApiException.BadRequest("Vui lòng chọn ảnh hoặc tài liệu.");

        if (file.Length > MaxFileBytes)
            throw ApiException.BadRequest("Tệp đính kèm không được vượt quá 20MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)
            || !AllowedFiles.TryGetValue(extension, out var rule)
            || !string.Equals(file.ContentType, rule.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw ApiException.BadRequest("Chỉ chấp nhận ảnh JPG, PNG, WEBP hoặc tài liệu PDF, Word, Excel, PowerPoint.");
        }

        var safeName = Path.GetFileName(file.FileName);
        var resourceType = rule.MessageType == "IMAGE" ? "image" : "raw";
        var url = $"https://res.cloudinary.com/testcloud/{resourceType}/upload/v1/chat/attachments/{Guid.NewGuid():N}_{safeName}";
        return Task.FromResult(new ChatAttachmentUpload(url, safeName, rule.MessageType));
    }

    public Task DeleteAsync(string url, string messageType, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            DeletedUrls.Add(url);
        }
        return Task.CompletedTask;
    }
}

public sealed class FakeCourseMediaStorage : ICourseMediaStorage
{
    public Task<CourseMediaUpload> UploadAsync(
        IFormFile? file,
        string? mediaType,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw ApiException.BadRequest("Vui lòng chọn file ảnh hoặc video.");

        var type = (mediaType ?? string.Empty).Trim().ToLowerInvariant();
        if (type is not ("image" or "video"))
            throw ApiException.BadRequest("Loại media không hợp lệ. Chỉ hỗ trợ 'image' hoặc 'video'.");

        var safeName = Path.GetFileName(file.FileName);
        var url = $"https://res.cloudinary.com/testcloud/{type}/upload/v1/courses/{Guid.NewGuid():N}_{safeName}";
        return Task.FromResult(new CourseMediaUpload(url, type, safeName));
    }
}
