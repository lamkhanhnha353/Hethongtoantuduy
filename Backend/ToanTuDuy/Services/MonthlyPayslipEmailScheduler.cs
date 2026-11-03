namespace ToanTuDuy.Services;

public sealed class MonthlyPayslipEmailScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<MonthlyPayslipEmailScheduler> logger,
    IConfiguration configuration) : BackgroundService
{
    private static readonly TimeZoneInfo VietnamTimeZone = FindVietnamTimeZone();
    private DateTimeOffset? _lastCompletedScheduledTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = configuration.GetValue<int>("PayrollEmailSchedule:IntervalMinutes");
        if (intervalMinutes < 0)
        {
            throw new InvalidOperationException("PayrollEmailSchedule:IntervalMinutes không được nhỏ hơn 0.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var utcNow = DateTimeOffset.UtcNow;
            var scheduledEvent = intervalMinutes > 0
                ? CreateTestSendEvent(utcNow.AddMinutes(intervalMinutes))
                : GetNextScheduledEvent(utcNow, _lastCompletedScheduledTime);
            var delay = scheduledEvent.ScheduledAt - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mailService = scope.ServiceProvider.GetRequiredService<IPayslipMailService>();
                if (scheduledEvent.IsReviewReminder)
                {
                    var result = await mailService.SendAdminPayrollReviewReminderAsync(
                        scheduledEvent.Month,
                        scheduledEvent.Year,
                        stoppingToken);
                    logger.LogInformation(
                        "Đã gửi {SentCount} email nhắc Admin kiểm tra lương tháng {Month:D2}/{Year}; bỏ qua {SkippedCount}, lỗi {FailedCount}.",
                        result.SentCount,
                        scheduledEvent.Month,
                        scheduledEvent.Year,
                        result.SkippedCount,
                        result.FailedCount);
                }
                else
                {
                    var result = await mailService.QueueScheduledBatchEmailsAsync(
                        scheduledEvent.Month,
                        scheduledEvent.Year,
                        stoppingToken);
                    logger.LogInformation(
                        "Đã tự động đưa {QueuedCount} email phiếu lương tháng {Month:D2}/{Year} vào hàng đợi; bỏ qua {SkippedCount}.",
                        result.QueuedCount,
                        scheduledEvent.Month,
                        scheduledEvent.Year,
                        result.SkippedCount);
                }

                if (intervalMinutes == 0)
                {
                    _lastCompletedScheduledTime = scheduledEvent.ScheduledAt;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Không thể hoàn tất tác vụ phiếu lương tháng {Month:D2}/{Year}.",
                    scheduledEvent.Month,
                    scheduledEvent.Year);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private static ScheduledPayrollEvent CreateTestSendEvent(DateTimeOffset scheduledAt)
    {
        var payrollPeriod = TimeZoneInfo.ConvertTime(scheduledAt, VietnamTimeZone).AddMonths(-1);
        return new ScheduledPayrollEvent(
            scheduledAt,
            payrollPeriod.Month,
            payrollPeriod.Year,
            IsReviewReminder: false);
    }

    private static ScheduledPayrollEvent GetNextScheduledEvent(
        DateTimeOffset utcNow,
        DateTimeOffset? lastCompletedScheduledTime)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, VietnamTimeZone);
        var localMonth = new DateTime(localNow.Year, localNow.Month, 1);
        var todayAtEightPassed = localNow.TimeOfDay >= TimeSpan.FromHours(8);
        var candidates = new List<ScheduledPayrollEvent>();

        for (var monthOffset = 0; monthOffset <= 1; monthOffset++)
        {
            var scheduledMonth = localMonth.AddMonths(monthOffset);
            var reminderDate = new DateTime(
                scheduledMonth.Year,
                scheduledMonth.Month,
                DateTime.DaysInMonth(scheduledMonth.Year, scheduledMonth.Month),
                8,
                0,
                0,
                DateTimeKind.Unspecified);
            candidates.Add(new ScheduledPayrollEvent(
                new DateTimeOffset(reminderDate, localNow.Offset),
                scheduledMonth.Month,
                scheduledMonth.Year,
                IsReviewReminder: true));

            var sendDate = new DateTime(
                scheduledMonth.Year,
                scheduledMonth.Month,
                1,
                8,
                0,
                0,
                DateTimeKind.Unspecified);
            var payrollPeriod = scheduledMonth.AddMonths(-1);
            candidates.Add(new ScheduledPayrollEvent(
                new DateTimeOffset(sendDate, localNow.Offset),
                payrollPeriod.Month,
                payrollPeriod.Year,
                IsReviewReminder: false));
        }

        return candidates
            .Where(candidate =>
                candidate.ScheduledAt > utcNow
                || (TimeZoneInfo.ConvertTime(candidate.ScheduledAt, VietnamTimeZone).Date == localNow.Date
                    && todayAtEightPassed
                    && candidate.ScheduledAt <= utcNow))
            .Where(candidate =>
                lastCompletedScheduledTime is null
                || candidate.ScheduledAt > lastCompletedScheduledTime.Value)
            .OrderBy(candidate => candidate.ScheduledAt)
            .First();
    }

    private static TimeZoneInfo FindVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private sealed record ScheduledPayrollEvent(
        DateTimeOffset ScheduledAt,
        int Month,
        int Year,
        bool IsReviewReminder);
}
