using System.Threading.Tasks;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public enum VerifyEmailStatus
    {
        Success,
        NotFound,
        Expired,
        AlreadyUsed,
        EmailChanged,
    }

    public record VerifyEmailResult(VerifyEmailStatus Status, string? Email);

    /// <summary>
    /// Xác minh email qua liên kết (magic link): cấp token, gửi email qua Resend,
    /// kiểm và kích hoạt EmailConfirmed khi người dùng bấm link.
    /// </summary>
    public interface IEmailVerificationService
    {
        /// <summary>
        /// Thu hồi token chưa dùng cũ của account, tạo token mới (chỉ lưu SHA-256 hash)
        /// và trả về token thô để gửi qua email.
        /// </summary>
        Task<string> IssueAsync(int accountId, string email);

        /// <summary>
        /// Gửi email chứa link xác minh. KHÔNG ném lỗi: gửi thất bại chỉ LogWarning
        /// và in link ra console (dùng để test khi Resend không gửi được).
        /// </summary>
        Task SendEmailAsync(string email, string rawToken);

        /// <summary>
        /// Kiểm token từ query string của link. Trả về kết quả thay vì ném exception
        /// để controller tự render trang HTML cho từng trạng thái.
        /// </summary>
        Task<VerifyEmailResult> VerifyAsync(string? rawToken);

        /// <summary>
        /// Gửi lại link xác minh — chặn nếu lần cấp trước còn trong khoảng cooldown (429).
        /// </summary>
        Task ResendAsync(int accountId, string email);
    }
}
