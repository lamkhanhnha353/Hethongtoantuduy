
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public interface IPaymentService
    {
        // 1. Tạo đường link thanh toán gửi cho Mobile
        Task<string> CreatePaymentUrl(int invoiceId, HttpContext context);

        // 2. Nhận kết quả khi khách hàng quay lại từ web VNPay
        Task<Invoice?> PaymentExecuteReturn(IQueryCollection collections);

        // 3. Webhook (IPN) - Nhận thông báo ngầm từ máy chủ VNPay để cập nhật Database
        Task<string> PaymentExecuteIpn(IQueryCollection collections);
    }
}