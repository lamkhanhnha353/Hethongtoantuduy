using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ToanTuDuy.Services.Storage
{
    /// <summary>
    /// Tải ảnh/video của khóa học lên Cloudinary. DB chỉ lưu URL công khai trả về.
    /// </summary>
    public interface ICourseMediaStorage
    {
        /// <summary>
        /// Upload media khóa học. <paramref name="mediaType"/> nhận "image" hoặc "video".
        /// </summary>
        Task<CourseMediaUpload> UploadAsync(
            IFormFile? file,
            string? mediaType,
            CancellationToken cancellationToken = default);
    }

    public sealed record CourseMediaUpload(string Url, string MediaType, string FileName);
}
