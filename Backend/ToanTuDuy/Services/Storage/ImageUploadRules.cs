using Microsoft.AspNetCore.Http;
using ToanTuDuy.Exceptions;

namespace ToanTuDuy.Services.Storage
{
    /// <summary>Quy tắc kiểm tra file ảnh tải lên (dùng chung cho avatar hồ sơ/học sinh).</summary>
    public static class ImageUploadRules
    {
        public const long MaxBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
        };

        public static void EnsureValid(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                throw ApiException.BadRequest("Vui lòng chọn file ảnh.");
            }

            if (file.Length > MaxBytes)
            {
                throw ApiException.BadRequest("Ảnh vượt quá giới hạn 5MB.");
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                throw ApiException.BadRequest("Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.");
            }
        }
    }
}
