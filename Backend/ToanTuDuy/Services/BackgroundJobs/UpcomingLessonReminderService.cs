using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ToanTuDuy.Data;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Services.BackgroundJobs
{
    /// <summary>
    /// Nhắc trước 1 tiếng cho các buổi học diễn ra trong ngày (cả giao diện và email cá nhân).
    /// </summary>
    public sealed class UpcomingLessonReminderService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan ReminderWindow = TimeSpan.FromMinutes(60);
        private static readonly TimeZoneInfo VietnamTimeZone = FindVietnamTimeZone();

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UpcomingLessonReminderService> _logger;

        public UpcomingLessonReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<UpcomingLessonReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessUpcomingLessonsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi quét và gửi nhắc nhở buổi học sắp diễn ra.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        internal async Task ProcessUpcomingLessonsAsync(CancellationToken cancellationToken)
        {
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
            var todayStart = vietnamNow.Date;
            var tomorrowStart = todayStart.AddDays(1);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var businessEmailService = scope.ServiceProvider.GetRequiredService<IBusinessNotificationEmailService>();

            var todayLessons = await context.Lessons
                .AsNoTracking()
                .Include(lesson => lesson.Class)
                    .ThenInclude(@class => @class.Teacher)
                        .ThenInclude(teacher => teacher!.Account)
                .Include(lesson => lesson.Class)
                    .ThenInclude(@class => @class.ClassSchedules)
                .Where(lesson =>
                    lesson.Status == "SCHEDULED" &&
                    lesson.LessonDate >= todayStart &&
                    lesson.LessonDate < tomorrowStart &&
                    lesson.StartTime.HasValue)
                .ToListAsync(cancellationToken);

            var lessonsDueSoon = todayLessons
                .Select(lesson =>
                {
                    var startLocal = lesson.LessonDate.Date.Add(lesson.StartTime!.Value.TimeOfDay);
                    var endLocal = lesson.EndTime.HasValue
                        ? lesson.LessonDate.Date.Add(lesson.EndTime.Value.TimeOfDay)
                        : (DateTime?)null;
                    var timeUntilStart = startLocal - vietnamNow;
                    return new
                    {
                        Lesson = lesson,
                        StartLocal = startLocal,
                        EndLocal = endLocal,
                        TimeUntilStart = timeUntilStart
                    };
                })
                .Where(item => item.TimeUntilStart > TimeSpan.Zero && item.TimeUntilStart <= ReminderWindow)
                .ToList();

            if (lessonsDueSoon.Count == 0)
            {
                return;
            }

            var candidateRefIds = lessonsDueSoon
                .SelectMany(item => new[]
                {
                    BuildTeacherRefId(item.Lesson.Id),
                    BuildParentRefId(item.Lesson.Id)
                })
                .ToList();

            var alreadyNotifiedRefIds = await context.Notifications
                .AsNoTracking()
                .Where(notification =>
                    notification.Type == NotificationTypes.Attendance &&
                    notification.RefId != null &&
                    candidateRefIds.Contains(notification.RefId))
                .Select(notification => notification.RefId!)
                .ToHashSetAsync(cancellationToken);

            foreach (var item in lessonsDueSoon)
            {
                var lesson = item.Lesson;
                var courseClass = lesson.Class;
                if (courseClass == null)
                {
                    continue;
                }

                var lessonTitle = string.IsNullOrWhiteSpace(lesson.Title) ? "Buổi học định kỳ" : lesson.Title.Trim();
                var timeRange = item.EndLocal.HasValue
                    ? $"{item.StartLocal:HH:mm} - {item.EndLocal.Value:HH:mm}"
                    : $"{item.StartLocal:HH:mm}";
                var room = courseClass.ClassSchedules
                    .FirstOrDefault(schedule => schedule.DayOfWeek == (int)lesson.LessonDate.DayOfWeek)
                    ?.Room;
                var roomSuffix = string.IsNullOrWhiteSpace(room) ? string.Empty : $" tại phòng {room.Trim()}";
                var minutesLeft = Math.Max(1, (int)Math.Ceiling(item.TimeUntilStart.TotalMinutes));

                var details = new List<(string Label, string Value)>
                {
                    ("Lớp học", courseClass.Name),
                    ("Nội dung buổi học", lessonTitle),
                    ("Ngày học", $"{item.StartLocal:dd/MM/yyyy}"),
                    ("Thời gian", timeRange)
                };
                if (!string.IsNullOrWhiteSpace(room))
                {
                    details.Add(("Phòng học", room.Trim()));
                }

                // 1. Nhắc giáo viên phụ trách lớp (giao diện + email cá nhân)
                var teacherRefId = BuildTeacherRefId(lesson.Id);
                if (courseClass.TeacherId.HasValue
                    && courseClass.Teacher != null
                    && !alreadyNotifiedRefIds.Contains(teacherRefId))
                {
                    var teacherTitle = $"Hôm nay bạn có buổi học: {courseClass.Name}";
                    var teacherBody =
                        $"Buổi học \"{lessonTitle}\" của lớp {courseClass.Name} sẽ bắt đầu lúc {item.StartLocal:HH:mm} hôm nay (còn khoảng {minutesLeft} phút){roomSuffix}.";

                    await notificationService.PublishAsync(
                        new NotificationMessage(
                            NotificationTypes.Attendance,
                            teacherTitle,
                            teacherBody,
                            teacherRefId),
                        AudienceSpec.Teacher(courseClass.TeacherId.Value),
                        cancellationToken);

                    var teacherEmail = courseClass.Teacher.Email ?? courseClass.Teacher.Account?.Email;
                    await businessEmailService.SendAsync(
                        new BusinessEmailRecipient(
                            courseClass.Teacher.AccountId,
                            teacherEmail,
                            courseClass.Teacher.FullName),
                        new BusinessEmailPayload(
                            $"Hôm nay bạn có buổi học ({item.StartLocal:HH:mm}) — {courseClass.Name}",
                            teacherTitle,
                            teacherBody,
                            details),
                        cancellationToken);
                }

                // 2. Nhắc phụ huynh học sinh đang theo học lớp (giao diện + email cá nhân)
                var parentRefId = BuildParentRefId(lesson.Id);
                if (!alreadyNotifiedRefIds.Contains(parentRefId))
                {
                    var activeEnrollments = await context.ClassStudents
                        .AsNoTracking()
                        .Where(cs => cs.ClassId == lesson.ClassId && cs.Status == "ACTIVE")
                        .Select(cs => new
                        {
                            StudentName = cs.Student.FullName,
                            ParentAccountId = cs.Student.Parent.AccountId,
                            ParentName = cs.Student.Parent.FullName,
                            ParentEmail = cs.Student.Parent.Account.Email,
                            ParentIsActive = cs.Student.Parent.Account.IsActive
                        })
                        .ToListAsync(cancellationToken);

                    if (activeEnrollments.Count > 0)
                    {
                        var parentTitle = $"Hôm nay bạn có buổi học: {courseClass.Name}";
                        var parentBody =
                            $"Buổi học \"{lessonTitle}\" của lớp {courseClass.Name} sẽ bắt đầu lúc {item.StartLocal:HH:mm} hôm nay (còn khoảng {minutesLeft} phút){roomSuffix}.";

                        await notificationService.PublishAsync(
                            new NotificationMessage(
                                NotificationTypes.Attendance,
                                parentTitle,
                                parentBody,
                                parentRefId),
                            AudienceSpec.ClassParents(lesson.ClassId),
                            cancellationToken);

                        var parentGroups = activeEnrollments
                            .Where(entry => entry.ParentIsActive && !string.IsNullOrWhiteSpace(entry.ParentEmail))
                            .GroupBy(entry => new { entry.ParentAccountId, Email = entry.ParentEmail!.Trim(), entry.ParentName })
                            .ToList();

                        var studentNamesByAccountId = parentGroups.ToDictionary(
                            group => group.Key.ParentAccountId,
                            group => string.Join(", ", group.Select(x => x.StudentName).Distinct()));

                        await businessEmailService.SendBatchAsync(
                            parentGroups.Select(group => new BusinessEmailRecipient(
                                group.Key.ParentAccountId,
                                group.Key.Email,
                                group.Key.ParentName)),
                            recipient =>
                            {
                                var childNames = recipient.AccountId.HasValue
                                    && studentNamesByAccountId.TryGetValue(recipient.AccountId.Value, out var names)
                                        ? names
                                        : "học sinh";

                                var emailDetails = new List<(string Label, string Value)>(details)
                                {
                                    ("Học sinh", childNames)
                                };

                                return new BusinessEmailPayload(
                                    $"Hôm nay bạn có buổi học ({item.StartLocal:HH:mm}) — {courseClass.Name}",
                                    parentTitle,
                                    $"Học sinh {childNames} có buổi học \"{lessonTitle}\" lớp {courseClass.Name} bắt đầu lúc {item.StartLocal:HH:mm} hôm nay (còn khoảng {minutesLeft} phút){roomSuffix}.",
                                    emailDetails);
                            },
                            cancellationToken);
                    }
                }
            }
        }

        private static string BuildTeacherRefId(int lessonId) => $"LESSON_REMINDER_1H:{lessonId}:TEACHER";
        private static string BuildParentRefId(int lessonId) => $"LESSON_REMINDER_1H:{lessonId}:PARENT";

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
    }
}
