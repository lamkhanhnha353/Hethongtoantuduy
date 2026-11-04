using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Options;

namespace ToanTuDuy.Services.Storage
{
    public sealed class CloudinaryChatAttachmentStorage : IChatAttachmentStorage
    {
        private const long MaxFileBytes = 20 * 1024 * 1024;
        private const string AttachmentFolder = "chat/attachments";

        private static readonly IReadOnlyDictionary<string, (string ContentType, string MessageType)> AllowedFiles =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = ("image/jpeg", "IMAGE"),
                [".jpeg"] = ("image/jpeg", "IMAGE"),
                [".heic"] = ("image/heic", "IMAGE"),
                [".heif"] = ("image/heif", "IMAGE"),
                [".png"] = ("image/png", "IMAGE"),
                [".webp"] = ("image/webp", "IMAGE"),
                [".pdf"] = ("application/pdf", "FILE"),
                [".doc"] = ("application/msword", "FILE"),
                [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "FILE"),
                [".xls"] = ("application/vnd.ms-excel", "FILE"),
                [".xlsx"] = ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "FILE"),
                [".ppt"] = ("application/vnd.ms-powerpoint", "FILE"),
                [".pptx"] = ("application/vnd.openxmlformats-officedocument.presentationml.presentation", "FILE")
            };

        private readonly Cloudinary? _cloudinary;
        private readonly string _cloudName;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<CloudinaryChatAttachmentStorage> _logger;

        public CloudinaryChatAttachmentStorage(
            IOptions<CloudinarySettings> options,
            IImageStorage imageStorage,
            ILogger<CloudinaryChatAttachmentStorage> logger)
        {
            _imageStorage = imageStorage;
            _logger = logger;
            var settings = options.Value;
            _cloudName = settings.CloudName;

            if (string.IsNullOrWhiteSpace(settings.CloudName)
                || string.IsNullOrWhiteSpace(settings.ApiKey)
                || string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                return;
            }

            _cloudinary = new Cloudinary(
                new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
            _cloudinary.Api.Secure = true;
        }

        public async Task<ChatAttachmentUpload> UploadAsync(
            IFormFile? file, CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                throw ApiException.BadRequest("Vui lòng chọn ảnh hoặc tài liệu.");
            }

            if (file.Length > MaxFileBytes)
            {
                throw ApiException.BadRequest("Tệp đính kèm không được vượt quá 20MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension)
                || !AllowedFiles.TryGetValue(extension, out var fileRules)
                || !string.Equals(file.ContentType, fileRules.ContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw ApiException.BadRequest(
                    "Chỉ chấp nhận ảnh JPG, PNG, WEBP hoặc tài liệu PDF, Word, Excel, PowerPoint.");
            }

            var fileName = Path.GetFileName(file.FileName);
            await using var stream = file.OpenReadStream();
            string url;
            if (fileRules.MessageType == "IMAGE")
            {
                url = await _imageStorage.UploadAsync(
                    stream, fileName, fileRules.ContentType, AttachmentFolder, cancellationToken);
            }
            else
            {
                var client = _cloudinary ?? throw new ApiException(
                    503,
                    "Chức năng tải tệp chưa được cấu hình trên máy chủ. Hãy cấu hình Cloudinary.");
                var result = await client.UploadAsync(new RawUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Folder = AttachmentFolder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                }, "raw", cancellationToken);

                if (result.Error is not null)
                {
                    _logger.LogError("Cloudinary upload tệp chat lỗi: {Message}", result.Error.Message);
                    throw new ApiException(502, "Không tải được tệp lên máy chủ lưu trữ.");
                }

                url = result.SecureUrl.ToString();
            }

            return new ChatAttachmentUpload(url, fileName, fileRules.MessageType);
        }

        public async Task DeleteAsync(
            string url, string messageType, CancellationToken cancellationToken = default)
        {
            var client = _cloudinary ?? throw new ApiException(
                503,
                "Chức năng xóa tệp chưa được cấu hình trên máy chủ. Hãy cấu hình Cloudinary.");
            var isImage = string.Equals(messageType, "IMAGE", StringComparison.OrdinalIgnoreCase);
            if (!isImage && !string.Equals(messageType, "FILE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Không thể xóa tệp đính kèm có loại tin nhắn '{messageType}'.");
            }

            var publicId = ExtractAttachmentPublicId(url, isImage);
            var result = await client.DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = isImage ? ResourceType.Image : ResourceType.Raw,
                Invalidate = true
            });

            if (result.Error is not null)
            {
                throw new InvalidOperationException(
                    $"Cloudinary không xóa được tệp chat {publicId}: {result.Error.Message}");
            }

            if (!string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Cloudinary trả về trạng thái xóa không thành công cho tệp chat {publicId}: {result.Result ?? "không rõ"}.");
            }

            _logger.LogInformation("Đã xóa tài nguyên chat khỏi Cloudinary: {PublicId}", publicId);
        }

        private string ExtractAttachmentPublicId(string url, bool isImage)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("URL tệp chat không phải URL Cloudinary hợp lệ.");
            }

            var segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString)
                .ToArray();
            if (segments.Length < 5
                || !string.Equals(segments[0], _cloudName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("URL tệp chat không thuộc Cloudinary đã cấu hình.");
            }

            var expectedResourceType = isImage ? "image" : "raw";
            var uploadIndex = Array.FindIndex(
                segments,
                segment => string.Equals(segment, "upload", StringComparison.OrdinalIgnoreCase));
            if (uploadIndex != 2
                || !string.Equals(segments[1], expectedResourceType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("URL tệp chat không khớp loại tài nguyên cần xóa.");
            }

            var publicIdSegments = segments.Skip(uploadIndex + 1).ToArray();
            if (publicIdSegments.Length > 0
                && publicIdSegments[0].Length > 1
                && publicIdSegments[0][0] == 'v'
                && publicIdSegments[0][1..].All(char.IsDigit))
            {
                publicIdSegments = publicIdSegments.Skip(1).ToArray();
            }

            var publicId = string.Join('/', publicIdSegments);
            if (!publicId.StartsWith(AttachmentFolder + "/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Tài nguyên nằm ngoài thư mục đính kèm chat.");
            }

            if (isImage)
            {
                var extensionIndex = publicId.LastIndexOf('.');
                if (extensionIndex > publicId.LastIndexOf('/'))
                {
                    publicId = publicId[..extensionIndex];
                }
            }

            return publicId;
        }
    }
}
