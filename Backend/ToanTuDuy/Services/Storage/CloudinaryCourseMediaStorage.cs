using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Options;

namespace ToanTuDuy.Services.Storage
{
    /// <summary>
    /// Upload ảnh/video khóa học trực tiếp lên Cloudinary.
    /// DB chỉ giữ URL https trả về (không lưu file/binary).
    /// </summary>
    public sealed class CloudinaryCourseMediaStorage : ICourseMediaStorage
    {
        private const long MaxImageBytes = 10 * 1024 * 1024;
        private const long MaxVideoBytes = 100 * 1024 * 1024;
        private const string ImageFolder = "courses/images";
        private const string VideoFolder = "courses/videos";

        private static readonly IReadOnlyDictionary<string, string> AllowedImages =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".webp"] = "image/webp",
            };

        private static readonly IReadOnlyDictionary<string, string> AllowedVideos =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".mp4"] = "video/mp4",
                [".mov"] = "video/quicktime",
                [".webm"] = "video/webm",
                [".m4v"] = "video/x-m4v",
            };

        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<CloudinaryCourseMediaStorage> _logger;

        public CloudinaryCourseMediaStorage(
            IOptions<CloudinarySettings> options,
            ILogger<CloudinaryCourseMediaStorage> logger)
        {
            _logger = logger;
            var settings = options.Value;

            if (string.IsNullOrWhiteSpace(settings.CloudName)
                || string.IsNullOrWhiteSpace(settings.ApiKey)
                || string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                _logger.LogWarning(
                    "Cloudinary chưa được cấu hình đầy đủ. Upload media khóa học sẽ thất bại.");
                return;
            }

            _cloudinary = new Cloudinary(
                new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
            _cloudinary.Api.Secure = true;
        }

        private Cloudinary Client =>
            _cloudinary ?? throw new ApiException(
                503,
                "Chức năng tải media chưa được cấu hình trên máy chủ. Hãy cấu hình Cloudinary:CloudName, Cloudinary:ApiKey và Cloudinary:ApiSecret.");

        public async Task<CourseMediaUpload> UploadAsync(
            IFormFile? file,
            string? mediaType,
            CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                throw ApiException.BadRequest("Vui lòng chọn file ảnh hoặc video.");
            }

            var type = (mediaType ?? string.Empty).Trim().ToLowerInvariant();
            if (type is not ("image" or "video"))
            {
                throw ApiException.BadRequest("Loại media không hợp lệ. Chỉ hỗ trợ 'image' hoặc 'video'.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw ApiException.BadRequest("Không xác định được định dạng file.");
            }

            var fileName = Path.GetFileName(file.FileName);
            await using var stream = file.OpenReadStream();

            if (type == "image")
            {
                EnsureValidImage(file, extension);
                var result = await Client.UploadAsync(new ImageUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Folder = ImageFolder,
                    UseFilename = false,
                    UniqueFilename = true,
                    Overwrite = false,
                }, cancellationToken);

                if (result.Error is not null)
                {
                    _logger.LogError("Cloudinary upload ảnh khóa học lỗi: {Message}", result.Error.Message);
                    throw new ApiException(502, "Không tải được ảnh khóa học lên máy chủ lưu trữ.");
                }

                return new CourseMediaUpload(result.SecureUrl.ToString(), type, fileName);
            }

            EnsureValidVideo(file, extension);
            var videoResult = await Client.UploadAsync(new VideoUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = VideoFolder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
            }, cancellationToken);

            if (videoResult.Error is not null)
            {
                _logger.LogError("Cloudinary upload video khóa học lỗi: {Message}", videoResult.Error.Message);
                throw new ApiException(502, "Không tải được video khóa học lên máy chủ lưu trữ.");
            }

            return new CourseMediaUpload(videoResult.SecureUrl.ToString(), type, fileName);
        }

        private static void EnsureValidImage(IFormFile file, string extension)
        {
            if (!AllowedImages.TryGetValue(extension, out var expectedContentType))
            {
                throw ApiException.BadRequest("Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.");
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType)
                && !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw ApiException.BadRequest("Định dạng ảnh không khớp với phần mở rộng file.");
            }

            if (file.Length > MaxImageBytes)
            {
                throw ApiException.BadRequest("Ảnh vượt quá giới hạn 10MB.");
            }
        }

        private static void EnsureValidVideo(IFormFile file, string extension)
        {
            if (!AllowedVideos.TryGetValue(extension, out var expectedContentType))
            {
                throw ApiException.BadRequest("Chỉ chấp nhận video MP4, MOV, WEBM hoặc M4V.");
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType)
                && !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase)
                && !file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                throw ApiException.BadRequest("Định dạng video không hợp lệ.");
            }

            if (file.Length > MaxVideoBytes)
            {
                throw ApiException.BadRequest("Video vượt quá giới hạn 100MB.");
            }
        }
    }
}
