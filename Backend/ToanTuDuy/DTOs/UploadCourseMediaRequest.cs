using Microsoft.AspNetCore.Http;

namespace ToanTuDuy.DTOs
{
    public class UploadCourseMediaRequest
    {
        /// <summary>File ảnh hoặc video cần tải lên Cloudinary.</summary>
        public IFormFile? File { get; set; }

        /// <summary>Loại media: "image" hoặc "video".</summary>
        public string? Type { get; set; }
    }
}
