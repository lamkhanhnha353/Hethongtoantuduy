
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.Models;
using ToanTuDuy.Utils;

namespace ToanTuDuy.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public PaymentService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<string> CreatePaymentUrl(int invoiceId, HttpContext context)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) throw new Exception("Không tìm thấy hóa đơn");

            // Tạo mã giao dịch riêng biệt gửi VNPay (chống trùng lặp)
            invoice.TxnRef = $"INV{invoice.Id}_{DateTime.Now.Ticks}";
            await _context.SaveChangesAsync();

            var pay = new VnPayLibrary();
            var timeNow = DateTime.UtcNow.AddHours(7); // Giờ Việt Nam

            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", _configuration["VnPay:TmnCode"]!);
            pay.AddRequestData("vnp_Amount", (invoice.Amount * 100).ToString()); // VNPay yêu cầu nhân 100
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", $"Thanh toan hoa don {invoice.Id}");
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", _configuration["VnPay:ReturnUrl"]!);
            pay.AddRequestData("vnp_TxnRef", invoice.TxnRef);

            var paymentUrl = pay.CreateRequestUrl(_configuration["VnPay:BaseUrl"]!, _configuration["VnPay:HashSecret"]!);
            return paymentUrl;
        }

        public async Task<Invoice?> PaymentExecuteReturn(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    pay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_TxnRef = pay.GetResponseData("vnp_TxnRef");
            var vnp_SecureHash = collections.FirstOrDefault(k => k.Key == "vnp_SecureHash").Value.ToString();

            bool checkSignature = pay.ValidateSignature(vnp_SecureHash, _configuration["VnPay:HashSecret"]!);
            if (!checkSignature) return null; // Chữ ký giả mạo

            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.TxnRef == vnp_TxnRef);
            return invoice;
        }

        public async Task<string> PaymentExecuteIpn(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    pay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_TxnRef = pay.GetResponseData("vnp_TxnRef");
            var vnp_SecureHash = collections.FirstOrDefault(k => k.Key == "vnp_SecureHash").Value.ToString();
            var vnp_ResponseCode = pay.GetResponseData("vnp_ResponseCode");

            bool checkSignature = pay.ValidateSignature(vnp_SecureHash, _configuration["VnPay:HashSecret"]!);
            if (!checkSignature) return "{\"RspCode\":\"97\",\"Message\":\"Invalid signature\"}";

            var invoice = await _context.Invoices
                .Include(i => i.ClassStudent)
                .FirstOrDefaultAsync(i => i.TxnRef == vnp_TxnRef);

            if (invoice == null) return "{\"RspCode\":\"01\",\"Message\":\"Order not found\"}";

            var vnp_Amount = Convert.ToInt64(pay.GetResponseData("vnp_Amount")) / 100;
            if (invoice.Amount != vnp_Amount) return "{\"RspCode\":\"04\",\"Message\":\"Invalid amount\"}";

            if (invoice.Status == "PAID") return "{\"RspCode\":\"02\",\"Message\":\"Order already confirmed\"}";

            // Xử lý trạng thái dựa trên mã VNPay trả về
            if (vnp_ResponseCode == "00")
            {
                invoice.Status = "PAID";
                invoice.PaymentMethod = "VNPAY";
                invoice.TransactionId = pay.GetResponseData("vnp_TransactionNo");
                invoice.PaidAt = DateTime.UtcNow;

                if (invoice.ClassStudent != null)
                {
                    invoice.ClassStudent.Status = "ACTIVE"; // Kích hoạt chỗ ngồi cho học sinh
                }
            }
            else
            {
                invoice.Status = "FAILED";
            }

            await _context.SaveChangesAsync();
            return "{\"RspCode\":\"00\",\"Message\":\"Confirm Success\"}"; // Phản hồi lại VNPay
        }
    }
}