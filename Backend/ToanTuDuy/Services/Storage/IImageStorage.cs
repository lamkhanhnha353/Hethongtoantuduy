using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ToanTuDuy.Services.Storage
{
    /// <summary>
    /// Trừu tượng nơi lưu ảnh. DB chỉ lưu URL công khai (<c>SecureUrl</c>) trả về từ đây,
    /// không lưu file/binary. Đổi nhà cung cấp (Cloudinary/S3/Azure) chỉ cần thay implementation.
    /// </summary>
    public interface IImageStorage
    {
        /// <summary>Upload ảnh và trả về URL công khai (https).</summary>
        Task<string> UploadAsync(
            Stream content,
            string fileName,
            string contentType,
            string folder,
            CancellationToken cancellationToken = default);

        /// <summary>Xóa ảnh theo URL đã lưu (dùng khi thay/xóa avatar).</summary>
        Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default);
    }
}
