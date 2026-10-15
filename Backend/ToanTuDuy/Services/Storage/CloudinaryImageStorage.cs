using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Options;

namespace ToanTuDuy.Services.Storage
{
    /// <summary>
    /// Lưu avatar trên Cloudinary. DB chỉ giữ URL https trả về.
    /// </summary>
    public class CloudinaryImageStorage : IImageStorage
    {
        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<CloudinaryImageStorage> _logger;

        public CloudinaryImageStorage(IOptions<CloudinarySettings> options, ILogger<CloudinaryImageStorage> logger)
        {
            var settings = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(settings.CloudName)
                || string.IsNullOrWhiteSpace(settings.ApiKey)
                || string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                _logger.LogWarning(
                    "Cloudinary chưa được cấu hình đầy đủ (CloudName/ApiKey/ApiSecret). " +
                    "Upload avatar sẽ thất bại cho đến khi điền cấu hình.");
                return;
            }

            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        private Cloudinary Client =>
            _cloudinary ?? throw new ApiException(503, "Chức năng tải ảnh chưa được cấu hình trên máy chủ.");

        public async Task<string> UploadAsync(
            Stream content,
            string fileName,
            string contentType,
            string folder,
            CancellationToken cancellationToken = default)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, content),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
            };

            var result = await Client.UploadAsync(uploadParams, cancellationToken);

            if (result.Error is not null)
            {
                _logger.LogError("Cloudinary upload lỗi: {Message}", result.Error.Message);
                throw new ApiException(502, "Không tải được ảnh lên máy chủ lưu trữ.");
            }

            return result.SecureUrl.ToString();
        }

        public async Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            var publicId = ExtractPublicId(imageUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                return;
            }

            var result = await Client.DestroyAsync(new DeletionParams(publicId));

            if (result.Error is not null)
            {
                _logger.LogWarning("Cloudinary xóa ảnh lỗi ({PublicId}): {Message}",
                    publicId, result.Error.Message);
            }
        }

        /// <summary>
        /// Lấy public_id từ URL Cloudinary dạng
        /// https://res.cloudinary.com/{cloud}/image/upload/v{version}/{folder}/{name}.{ext}
        /// (bỏ phần version và phần mở rộng).
        /// </summary>
        private static string? ExtractPublicId(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            const string marker = "/upload/";
            var start = imageUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return null;
            }

            var path = imageUrl[(start + marker.Length)..];

            var query = path.IndexOf('?');
            if (query >= 0)
            {
                path = path[..query];
            }

            // Bỏ segment version dạng "v1712345678/"
            if (path.StartsWith('v'))
            {
                var slash = path.IndexOf('/');
                if (slash > 0 && path[1..slash].All(char.IsDigit))
                {
                    path = path[(slash + 1)..];
                }
            }

            var dot = path.LastIndexOf('.');
            if (dot > 0)
            {
                path = path[..dot];
            }

            return path;
        }
    }
}
