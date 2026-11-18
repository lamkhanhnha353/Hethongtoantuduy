
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Services
{
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public ReservationCleanupService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Lấy cấu hình từ appsettings, nếu không có sẽ lấy mặc định là 2880 phút (48h) và quét mỗi 60 giây
            int expirationMinutes = _configuration.GetValue<int>("ReservationSettings:ExpirationMinutes", 2880);
            int intervalSeconds = _configuration.GetValue<int>("ReservationSettings:CheckIntervalSeconds", 60);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Phải tạo Scope mới vì BackgroundService là Singleton, còn DbContext là Scoped
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                        var nowUtc = DateTime.UtcNow;

                        // Tính mốc thời gian: Những đơn nào tạo TRƯỚC mốc này sẽ bị hủy
                        var expirationTime = nowUtc.AddMinutes(-expirationMinutes);

                        // 1. Nhắc nhở các hóa đơn PENDING đã đi qua ít nhất một nửa thời gian giữ chỗ nhưng chưa quá hạn
                        double reminderThresholdMinutes = Math.Max(0.5, expirationMinutes / 2.0);
                        var reminderCutoff = nowUtc.AddMinutes(-reminderThresholdMinutes);

                        var nearExpiryInvoices = await dbContext.Invoices
                            .AsNoTracking()
                            .Include(i => i.Student)
                            .Include(i => i.Class)
                            .Where(i => i.Status == "PENDING"
                                && i.CreatedAt >= expirationTime
                                && i.CreatedAt <= reminderCutoff)
                            .ToListAsync(stoppingToken);

                        if (nearExpiryInvoices.Any())
                        {
                            var reminderRefIds = nearExpiryInvoices
                                .Select(i => $"INVOICE:{i.Id}:REMINDER")
                                .ToList();

                            var alreadyReminded = await dbContext.Notifications
                                .AsNoTracking()
                                .Where(n => n.RefId != null && reminderRefIds.Contains(n.RefId))
                                .Select(n => n.RefId!)
                                .ToListAsync(stoppingToken);

                            var alreadyRemindedSet = new HashSet<string>(alreadyReminded, StringComparer.Ordinal);
                            var queuedReminders = 0;

                            foreach (var invoice in nearExpiryInvoices)
                            {
                                var refId = $"INVOICE:{invoice.Id}:REMINDER";
                                if (alreadyRemindedSet.Contains(refId))
                                {
                                    continue;
                                }

                                var studentName = invoice.Student?.FullName ?? "Học sinh";
                                var className = invoice.Class?.Name ?? $"Lớp #{invoice.ClassId}";

                                await notificationService.EnqueueAsync(
                                    new NotificationMessage(
                                        NotificationTypes.Tuition,
                                        "Nhắc nhở: Hóa đơn học phí sắp hết hạn",
                                        $"Hóa đơn #{invoice.Id} ({invoice.Amount:N0} VNĐ) của bé {studentName} tại lớp {className} sắp hết thời gian giữ chỗ. Vui lòng thanh toán ngay để tránh bị hủy.",
                                        refId),
                                    AudienceSpec.StudentParent(invoice.StudentId),
                                    stoppingToken);
                                queuedReminders++;
                            }

                            if (queuedReminders > 0)
                            {
                                await dbContext.SaveChangesAsync(stoppingToken);
                                Console.WriteLine($"[Hệ thống] Đã gửi nhắc đóng học phí cho {queuedReminders} hóa đơn sắp hết hạn.");
                            }
                        }

                        // 2. Tìm các Hóa đơn PENDING đã quá hạn
                        var expiredInvoices = await dbContext.Invoices
                            .Include(i => i.ClassStudent)
                            .Include(i => i.Student)
                            .Include(i => i.Class)
                            .Where(i => i.Status == "PENDING" && i.CreatedAt < expirationTime)
                            .ToListAsync(stoppingToken);

                        if (expiredInvoices.Any())
                        {
                            foreach (var invoice in expiredInvoices)
                            {
                                invoice.Status = "CANCELLED"; // Hủy hóa đơn

                                // Nhả slot (chỗ ngồi)
                                if (invoice.ClassStudent != null && invoice.ClassStudent.Status == "PENDING")
                                {
                                    invoice.ClassStudent.Status = "CANCELLED";
                                }

                                var studentName = invoice.Student?.FullName ?? "Học sinh";
                                var className = invoice.Class?.Name ?? $"Lớp #{invoice.ClassId}";

                                await notificationService.EnqueueAsync(
                                    new NotificationMessage(
                                        NotificationTypes.Tuition,
                                        "Hóa đơn học phí đã hết hạn",
                                        $"Hóa đơn #{invoice.Id} của bé {studentName} tại lớp {className} đã tự động hủy do quá thời hạn thanh toán. Chỗ giữ đã được giải phóng.",
                                        $"INVOICE:{invoice.Id}:EXPIRED"),
                                    AudienceSpec.StudentParent(invoice.StudentId),
                                    stoppingToken);
                            }

                            await dbContext.SaveChangesAsync(stoppingToken);
                            Console.WriteLine($"[Hệ thống] Đã dọn dẹp tự động {expiredInvoices.Count} hóa đơn và slot hết hạn.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Lỗi Hệ Thống Dọn Rác] {ex.Message}");
                }

                // Tạm dừng Job theo chu kỳ cấu hình trước khi chạy vòng lặp tiếp theo
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
    }
}