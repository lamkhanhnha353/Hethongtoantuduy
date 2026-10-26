
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Data;

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

                        // Tính mốc thời gian: Những đơn nào tạo TRƯỚC mốc này sẽ bị hủy
                        var expirationTime = DateTime.UtcNow.AddMinutes(-expirationMinutes);

                        // Tìm các Hóa đơn PENDING đã quá hạn
                        var expiredInvoices = await dbContext.Invoices
                            .Include(i => i.ClassStudent)
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