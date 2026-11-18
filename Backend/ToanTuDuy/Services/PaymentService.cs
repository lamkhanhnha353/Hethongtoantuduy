using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;
using ToanTuDuy.Services.Notifications;
using ToanTuDuy.Utils;

namespace ToanTuDuy.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IBusinessNotificationEmailService _businessEmailService;

        public PaymentService(
            IConfiguration configuration,
            ApplicationDbContext context,
            INotificationService notificationService,
            IBusinessNotificationEmailService businessEmailService)
        {
            _configuration = configuration;
            _context = context;
            _notificationService = notificationService;
            _businessEmailService = businessEmailService;
        }

        public async Task<string> CreatePaymentUrl(int invoiceId, int accountId, string clientIpAddress)
        {
            var invoice = await _context.Invoices
                .Include(item => item.Parent)
                .Include(item => item.ClassStudent)
                .FirstOrDefaultAsync(item =>
                    item.Id == invoiceId
                    && item.Parent.AccountId == accountId);

            if (invoice == null)
            {
                throw ApiException.NotFound("Không tìm thấy hóa đơn của phụ huynh này.");
            }

            if (invoice.Status == "FAILED" && invoice.ClassStudent?.Status == "PENDING")
            {
                invoice.Status = "PENDING";
            }

            if (invoice.Status != "PENDING")
            {
                throw ApiException.BadRequest("Hóa đơn không ở trạng thái chờ thanh toán.");
            }

            var activeStudentsCount = await _context.ClassStudents
                .CountAsync(enrollment =>
                    enrollment.ClassId == invoice.ClassId
                    && enrollment.Status == "ACTIVE");

            var maxCapacity = await _context.Classes
                .Where(classInfo => classInfo.Id == invoice.ClassId)
                .Select(classInfo => classInfo.MaxCapacity)
                .FirstAsync();

            if (activeStudentsCount >= maxCapacity)
            {
                throw ApiException.Conflict("Lớp học đã đủ học viên, không thể tiếp tục thanh toán.");
            }

            invoice.TxnRef = $"INV{invoice.Id}_{DateTime.UtcNow.Ticks}";
            await _context.SaveChangesAsync();

            var payment = new VnPayLibrary();
            var vietnamTime = DateTime.UtcNow.AddHours(7);

            payment.AddRequestData("vnp_Version", "2.1.0");
            payment.AddRequestData("vnp_Command", "pay");
            payment.AddRequestData("vnp_TmnCode", _configuration["VnPay:TmnCode"]!);
            payment.AddRequestData(
                "vnp_Amount",
                ((long)invoice.Amount * 100).ToString(CultureInfo.InvariantCulture));
            payment.AddRequestData("vnp_CreateDate", vietnamTime.ToString("yyyyMMddHHmmss"));
            payment.AddRequestData("vnp_CurrCode", "VND");
            payment.AddRequestData("vnp_IpAddr", clientIpAddress);
            payment.AddRequestData("vnp_Locale", "vn");
            payment.AddRequestData("vnp_OrderInfo", $"Thanh toan hoa don {invoice.Id}");
            payment.AddRequestData("vnp_OrderType", "other");
            payment.AddRequestData("vnp_ReturnUrl", _configuration["VnPay:ReturnUrl"]!);
            payment.AddRequestData("vnp_TxnRef", invoice.TxnRef);

            return payment.CreateRequestUrl(
                _configuration["VnPay:BaseUrl"]!,
                _configuration["VnPay:HashSecret"]!);
        }

        public async Task<Invoice?> PaymentExecuteReturn(IQueryCollection collections)
        {
            var payment = CreatePaymentResponse(collections);
            if (!IsValidSignature(payment, collections))
            {
                return null;
            }

            var invoice = await FindInvoiceAsync(payment);
            if (invoice == null || !HasValidAmount(payment, invoice))
            {
                return null;
            }

            await ApplyPaymentResultAsync(invoice, payment);
            return invoice;
        }

        public async Task<VnPayIpnResponse> PaymentExecuteIpn(IQueryCollection collections)
        {
            var payment = CreatePaymentResponse(collections);
            if (!IsValidSignature(payment, collections))
            {
                return new VnPayIpnResponse { RspCode = "97", Message = "Invalid signature" };
            }

            var invoice = await FindInvoiceAsync(payment);
            if (invoice == null)
            {
                return new VnPayIpnResponse { RspCode = "01", Message = "Order not found" };
            }

            if (!HasValidAmount(payment, invoice))
            {
                return new VnPayIpnResponse { RspCode = "04", Message = "Invalid amount" };
            }

            if (invoice.Status != "PENDING")
            {
                return new VnPayIpnResponse { RspCode = "02", Message = "Order already confirmed" };
            }

            await ApplyPaymentResultAsync(invoice, payment);
            return new VnPayIpnResponse { RspCode = "00", Message = "Confirm Success" };
        }

        private static VnPayLibrary CreatePaymentResponse(IQueryCollection collections)
        {
            var payment = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (key.StartsWith("vnp_", StringComparison.Ordinal))
                {
                    payment.AddResponseData(key, value.ToString());
                }
            }

            return payment;
        }

        private bool IsValidSignature(VnPayLibrary payment, IQueryCollection collections)
        {
            var secureHash = collections["vnp_SecureHash"].ToString();
            return payment.ValidateSignature(secureHash, _configuration["VnPay:HashSecret"]!);
        }

        private Task<Invoice?> FindInvoiceAsync(VnPayLibrary payment)
        {
            var transactionReference = payment.GetResponseData("vnp_TxnRef");
            if (string.IsNullOrWhiteSpace(transactionReference))
            {
                return Task.FromResult<Invoice?>(null);
            }

            return _context.Invoices
                .Include(invoice => invoice.ClassStudent)
                .Include(invoice => invoice.Student)
                .FirstOrDefaultAsync(invoice => invoice.TxnRef == transactionReference);
        }

        private static bool HasValidAmount(VnPayLibrary payment, Invoice invoice)
        {
            return long.TryParse(
                    payment.GetResponseData("vnp_Amount"),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var amount)
                && amount == (long)invoice.Amount * 100;
        }

        private async Task ApplyPaymentResultAsync(Invoice invoice, VnPayLibrary payment)
        {
            if (invoice.Status != "PENDING")
            {
                return;
            }

            var isSuccessful = payment.GetResponseData("vnp_ResponseCode") == "00"
                && payment.GetResponseData("vnp_TransactionStatus") == "00";

            if (isSuccessful)
            {
                invoice.Status = "PAID";
                invoice.PaymentMethod = "VNPAY";
                invoice.TransactionId = payment.GetResponseData("vnp_TransactionNo");
                invoice.PaidAt = DateTime.UtcNow;

                if (invoice.ClassStudent != null)
                {
                    invoice.ClassStudent.Status = "ACTIVE";
                }
            }
            else
            {
                invoice.Status = "FAILED";
            }

            await _context.SaveChangesAsync();

            try
            {
                if (invoice.Status == "PAID")
                {
                    await NotifyPaymentSuccessAsync(invoice);
                }
                else if (invoice.Status == "FAILED")
                {
                    var studentName = invoice.Student?.FullName ?? "Học sinh";
                    await _notificationService.PublishAsync(
                        new NotificationMessage(
                            NotificationTypes.Tuition,
                            "Thanh toán học phí chưa thành công",
                            $"Giao dịch cho hóa đơn #{invoice.Id} của bé {studentName} chưa hoàn tất. Bạn có thể vào mục Hóa đơn để thanh toán lại.",
                            $"INVOICE:{invoice.Id}:FAILED:{DateTime.UtcNow:yyyyMMddHHmmss}"),
                        AudienceSpec.StudentParent(invoice.StudentId));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentService] Không thể gửi thông báo kết quả thanh toán cho hóa đơn #{invoice.Id}: {ex.Message}");
            }
        }

        private async Task NotifyPaymentSuccessAsync(Invoice invoice)
        {
            var refId = $"INVOICE:{invoice.Id}:PAID";
            var studentName = invoice.Student?.FullName ?? "Học sinh";
            const string title = "Thanh toán học phí thành công";
            var body = $"Hóa đơn #{invoice.Id} của {studentName} đã được thanh toán thành công.";

            await _notificationService.PublishAsync(
                new NotificationMessage(NotificationTypes.Payment, title, body, refId),
                AudienceSpec.Union(
                    AudienceSpec.AllAdmins(),
                    AudienceSpec.ClassTeacher(invoice.ClassId)));

            var classInfo = await _context.Classes
                .AsNoTracking()
                .Where(c => c.Id == invoice.ClassId)
                .Select(c => new { c.Name, c.StartDate, c.TeacherId })
                .FirstOrDefaultAsync();

            var className = classInfo?.Name ?? $"Lớp #{invoice.ClassId}";
            var allocRefId = $"ENROLLMENT:{invoice.Id}:ALLOCATED";
            const string allocTitle = "Học sinh đã được phân bổ vào lớp";
            var allocBody = $"{studentName} đã hoàn tất học phí và được xếp chính thức vào lớp {className}.";

            await _notificationService.PublishAsync(
                new NotificationMessage(NotificationTypes.Enrollment, allocTitle, allocBody, allocRefId),
                AudienceSpec.StudentParent(invoice.StudentId));

            var details = new System.Collections.Generic.List<(string Label, string Value)>
            {
                ("Học sinh", studentName),
                ("Lớp học", className),
                ("Mã hóa đơn", $"#{invoice.Id}"),
                ("Số tiền đã thanh toán", $"{invoice.Amount:N0} VNĐ")
            };
            if (classInfo != null)
            {
                details.Add(("Ngày khai giảng", $"{classInfo.StartDate:dd/MM/yyyy}"));
            }

            await _businessEmailService.SendToStudentParentAsync(
                invoice.StudentId,
                new BusinessEmailPayload(
                    $"Xác nhận phân bổ lớp {className} — Toán Tư Duy",
                    allocTitle,
                    allocBody,
                    details));

            if (classInfo?.TeacherId != null)
            {
                await _businessEmailService.SendToTeacherAsync(
                    classInfo.TeacherId.Value,
                    new BusinessEmailPayload(
                        $"Học sinh mới vào lớp {className} — Toán Tư Duy",
                        "Lớp có học sinh mới hoàn tất ghi danh",
                        $"Học sinh {studentName} vừa hoàn tất học phí và chính thức tham gia lớp {className}.",
                        details));
            }
        }
    }
}
