using System.Threading.Tasks;
using ToanTuDuy.DTOs.Profile;

namespace ToanTuDuy.Services
{
    /// <summary>
    /// Quản lý PIN bảo vệ thao tác của phụ huynh: đặt/đổi và xác thực.
    /// Tách riêng khỏi ProfileService để filter xác thực PIN tái sử dụng được.
    /// </summary>
    public interface IPinService
    {
        Task SetAsync(int accountId, SetPinRequest request);

        /// <summary>
        /// Xác thực PIN. Ném <see cref="Exceptions.ApiException"/> nếu chưa thiết lập PIN.
        /// Trả về <c>Valid = false</c> khi PIN sai.
        /// </summary>
        Task<PinVerificationResponse> VerifyAsync(int accountId, string pin);
    }
}
