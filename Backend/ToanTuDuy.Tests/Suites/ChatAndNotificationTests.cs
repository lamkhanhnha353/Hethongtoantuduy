using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Notification;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Models;
using ToanTuDuy.Services.BackgroundJobs;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class ChatAndNotificationTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Chat_EnforcesClassAssignment_UploadsDeletesAttachment_AndBlocksAdminSend()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacherAssigned = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var teacherOther = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Chat" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "CHAT-C", Name = "Toán Chat", MinAge = 6, MaxAge = 10, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var level = new CourseLevel { CourseId = course.Id, Code = "CHAT-L", Name = "L1", Fee = 1000000m, TotalSessions = 2, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacherAssigned.ProfileId, Name = "Lớp Chat", MaxCapacity = 10, StartDate = DateTime.UtcNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = student.Id, Status = "ACTIVE" });
            await db.SaveChangesAsync();
        }

        // Parent cannot message unassigned teacher -> 403
        var forbiddenChat = await parent.Client.PostAsJsonAsync("/api/chat/send", new SendMessageRequest
        {
            ReceiverId = teacherOther.ProfileId,
            ReceiverType = "TEACHER",
            MessageType = "TEXT",
            Content = "Xin chào"
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenChat.StatusCode);

        // Admin cannot use user chat endpoint -> 403
        var adminChat = await admin.Client.PostAsJsonAsync("/api/chat/send", new SendMessageRequest
        {
            ReceiverId = teacherAssigned.ProfileId,
            ReceiverType = "TEACHER",
            MessageType = "TEXT",
            Content = "Admin chat"
        });
        Assert.Equal(HttpStatusCode.Forbidden, adminChat.StatusCode);

        // Parent sends TEXT to assigned teacher -> 200
        var okSend = await parent.Client.PostAsJsonAsync("/api/chat/send", new SendMessageRequest
        {
            ReceiverId = teacherAssigned.ProfileId,
            ReceiverType = "TEACHER",
            MessageType = "TEXT",
            Content = "Chào thầy/cô"
        });
        Assert.Equal(HttpStatusCode.OK, okSend.StatusCode);
        using var sendDoc = JsonDocument.Parse(await okSend.Content.ReadAsStringAsync());
        var conversationId = sendDoc.RootElement.GetProperty("data").GetProperty("conversationId").GetInt32();

        // Teacher uploads attachment
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(parent.ProfileId.ToString()), "ReceiverId");
        form.Add(new StringContent("PARENT"), "ReceiverType");
        var pdfBytes = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4 test"));
        pdfBytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(pdfBytes, "File", "homework.pdf");

        var attachResp = await teacherAssigned.Client.PostAsync("/api/chat/send-attachment", form);
        Assert.Equal(HttpStatusCode.OK, attachResp.StatusCode);
        using var attachDoc = JsonDocument.Parse(await attachResp.Content.ReadAsStringAsync());
        var attachMsgId = attachDoc.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        var attachUrl = attachDoc.RootElement.GetProperty("data").GetProperty("attachmentUrl").GetString()!;

        // Other teacher cannot read messages -> 403
        Assert.Equal(HttpStatusCode.Forbidden, (await teacherOther.Client.GetAsync($"/api/chat/{conversationId}/messages")).StatusCode);

        // Delete attachment message -> deletes file from FakeChatStorage too
        var delResp = await teacherAssigned.Client.DeleteAsync($"/api/chat/{conversationId}/messages/{attachMsgId}");
        Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);
        Assert.Contains(attachUrl, Factory.FakeChatStorage.DeletedUrls);
    }

    [Fact]
    public async Task Notifications_OutboxFanout_RespectsOptOut_And_MarkReadUpdatesUnreadCount()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var parentInAppOn = await AuthTestHelper.RegisterParentAsync(Factory);
        var parentInAppOff = await AuthTestHelper.RegisterParentAsync(Factory);

        // Parent 2 opts out of IN_APP_NOTIFICATION
        var optOutResp = await parentInAppOff.Client.PutAsJsonAsync("/api/notifications/settings", new UpdateNotificationSettingsRequest
        {
            Settings = [new NotificationSettingItemRequest { Type = "IN_APP_NOTIFICATION", Enabled = false }]
        });
        Assert.Equal(HttpStatusCode.OK, optOutResp.StatusCode);

        // Admin sends ANNOUNCEMENT to all parents
        var sendResp = await admin.Client.PostAsJsonAsync("/api/admin/notifications", new CreateAdminNotificationRequest
        {
            RecipientGroup = "PARENT",
            Type = "ANNOUNCEMENT",
            Title = "Thông báo nghỉ lễ",
            Body = "Trung tâm nghỉ lễ 2/9.",
            SendEmail = false
        });
        Assert.Equal(HttpStatusCode.Accepted, sendResp.StatusCode);

        // Wait for NotificationFanoutWorker to process outbox & update unread count
        NotificationListResponse? inboxOn = null;
        await ApiAssertHelper.EventuallyAsync(async () =>
        {
            inboxOn = await parentInAppOn.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications");
            return inboxOn is not null && inboxOn.Items.Count > 0 && inboxOn.UnreadCount == 1;
        });

        Assert.Equal(1, inboxOn!.UnreadCount);
        var notifId = inboxOn.Items[0].Id;

        var inboxOff = await parentInAppOff.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications");
        Assert.Empty(inboxOff!.Items);
        Assert.Equal(0, inboxOff.UnreadCount);

        // Mark read idempotently
        Assert.Equal(HttpStatusCode.NoContent, (await parentInAppOn.Client.PutAsync($"/api/notifications/{notifId}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await parentInAppOn.Client.PutAsync($"/api/notifications/{notifId}/read", null)).StatusCode);

        var unreadAfter = await parentInAppOn.Client.GetFromJsonAsync<NotificationUnreadCountResponse>("/api/notifications/unread-count");
        Assert.Equal(0, unreadAfter!.UnreadCount);
    }

    [Fact]
    public async Task Chat_Pagination30PerPage_AdminConversations_DeleteLastUpdatesSummary_AndRejectsExe()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Chat 2" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "CHAT2-C", Name = "Toán Chat 2", MinAge = 6, MaxAge = 10, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var level = new CourseLevel { CourseId = course.Id, Code = "CHAT2-L", Name = "L1", Fee = 1000000m, TotalSessions = 1, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp Chat 2", MaxCapacity = 10, StartDate = DateTime.UtcNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = student.Id, Status = "ACTIVE" });
            await db.SaveChangesAsync();
        }

        // TC-COMM-003: Invalid .exe attachment -> 400
        using var badForm = new MultipartFormDataContent();
        badForm.Add(new StringContent(teacher.ProfileId.ToString()), "ReceiverId");
        badForm.Add(new StringContent("TEACHER"), "ReceiverType");
        var exeBytes = new ByteArrayContent(Encoding.UTF8.GetBytes("MZ-fake-exe"));
        exeBytes.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        badForm.Add(exeBytes, "File", "malware.exe");
        Assert.Equal(HttpStatusCode.BadRequest, (await parent.Client.PostAsync("/api/chat/send-attachment", badForm)).StatusCode);

        // SC-COMM-02: Send 35 messages -> check page 1 has 30 ascending & page 2 has 5
        int conversationId = 0;
        int lastMsgId = 0;
        for (var i = 1; i <= 35; i++)
        {
            var resp = await parent.Client.PostAsJsonAsync("/api/chat/send", new SendMessageRequest
            {
                ReceiverId = teacher.ProfileId,
                ReceiverType = "TEACHER",
                MessageType = "TEXT",
                Content = $"Tin nhắn {i:D2}"
            });
            resp.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var data = doc.RootElement.GetProperty("data");
            conversationId = data.GetProperty("conversationId").GetInt32();
            lastMsgId = data.GetProperty("id").GetInt32();
        }

        using var page1Doc = JsonDocument.Parse(await parent.Client.GetStringAsync($"/api/chat/{conversationId}/messages?page=1"));
        var page1Msgs = page1Doc.RootElement.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(30, page1Msgs.Count);
        Assert.Equal("Tin nhắn 06", page1Msgs[0].GetProperty("content").GetString());
        Assert.Equal("Tin nhắn 35", page1Msgs[^1].GetProperty("content").GetString());

        using var page2Doc = JsonDocument.Parse(await parent.Client.GetStringAsync($"/api/chat/{conversationId}/messages?page=2"));
        var page2Msgs = page2Doc.RootElement.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(5, page2Msgs.Count);
        Assert.Equal("Tin nhắn 01", page2Msgs[0].GetProperty("content").GetString());

        // Delete last message -> LastMessage rolls back to "Tin nhắn 34"
        Assert.Equal(HttpStatusCode.OK, (await parent.Client.DeleteAsync($"/api/chat/{conversationId}/messages/{lastMsgId}")).StatusCode);

        using var adminConvDoc = JsonDocument.Parse(await admin.Client.GetStringAsync("/api/chat/admin/conversations"));
        var conv = adminConvDoc.RootElement.GetProperty("data").EnumerateArray()
            .Single(c => c.GetProperty("id").GetInt32() == conversationId);
        Assert.Equal("Tin nhắn 34", conv.GetProperty("lastMessage").GetString());
    }

    [Fact]
    public async Task SignalR_ChatAndNotificationHubs_RejectMissingToken_AndDeliverRealtimeEvents()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé SignalR" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "SIG-C", Name = "Toán SignalR", MinAge = 6, MaxAge = 10, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var level = new CourseLevel { CourseId = course.Id, Code = "SIG-L", Name = "L1", Fee = 1000000m, TotalSessions = 1, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp SignalR", MaxCapacity = 10, StartDate = DateTime.UtcNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = student.Id, Status = "ACTIVE" });
            await db.SaveChangesAsync();
        }

        // TC-COMM-004: Missing token -> rejected on HubConnection.StartAsync
        await using var anonChatHub = new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, "/chathub"), opts =>
            {
                opts.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            })
            .Build();
        await Assert.ThrowsAnyAsync<Exception>(() => anonChatHub.StartAsync());

        // Authenticated connections to /chathub and /hubs/notifications receive pushed events
        var chatReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var teacherChatHub = new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, $"/chathub?access_token={Uri.EscapeDataString(teacher.Token)}"), opts =>
            {
                opts.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            })
            .Build();
        teacherChatHub.On<JsonElement>("ReceiveNewMessage", payload =>
        {
            if (payload.TryGetProperty("content", out var content))
            {
                chatReceived.TrySetResult(content.GetString() ?? string.Empty);
            }
        });
        await teacherChatHub.StartAsync();

        var notifReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var parentNotifHub = new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, $"/hubs/notifications?access_token={Uri.EscapeDataString(parent.Token)}"), opts =>
            {
                opts.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            })
            .Build();
        parentNotifHub.On<JsonElement>("ReceiveNotification", payload =>
        {
            if (payload.TryGetProperty("title", out var title))
            {
                notifReceived.TrySetResult(title.GetString() ?? string.Empty);
            }
        });
        await parentNotifHub.StartAsync();

        // Trigger ReceiveNewMessage
        Assert.Equal(HttpStatusCode.OK, (await parent.Client.PostAsJsonAsync("/api/chat/send", new SendMessageRequest
        {
            ReceiverId = teacher.ProfileId,
            ReceiverType = "TEACHER",
            MessageType = "TEXT",
            Content = "Ping SignalR"
        })).StatusCode);

        var chatCompleted = await Task.WhenAny(chatReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(chatReceived.Task, chatCompleted);
        Assert.Equal("Ping SignalR", await chatReceived.Task);

        // Trigger ReceiveNotification via Admin broadcast
        Assert.Equal(HttpStatusCode.Accepted, (await admin.Client.PostAsJsonAsync("/api/admin/notifications", new CreateAdminNotificationRequest
        {
            RecipientGroup = "PARENT",
            Type = "ANNOUNCEMENT",
            Title = "SignalR Notify",
            Body = "Push realtime",
            SendEmail = false
        })).StatusCode);

        var notifCompleted = await Task.WhenAny(notifReceived.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(notifReceived.Task, notifCompleted);
        Assert.Equal("SignalR Notify", await notifReceived.Task);
    }

    [Fact]
    public async Task Notifications_MarkAllRead_DevTestEndpoint_And_UpcomingLessonReminderDedup_Work()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Nhắc Học" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        // Verify /api/notifications/test is 404 in Production and 200 in Development
        Assert.Equal(HttpStatusCode.NotFound, (await parent.Client.PostAsJsonAsync("/api/notifications/test", new CreateTestNotificationRequest
        {
            Title = "Prod Blocked"
        })).StatusCode);

        {
            await using var devFactory = new DevWebAppFactory();
            using var devAnon = devFactory.CreateClient();
            var devAuth = await AuthTestHelper.LoginAsync(devAnon, parent.Phone, parent.Password);
            using var devParentClient = devFactory.CreateClient();
            devParentClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", devAuth.AccessToken);

            Assert.Equal(HttpStatusCode.OK, (await devParentClient.PostAsJsonAsync("/api/notifications/test", new CreateTestNotificationRequest
            {
                Title = "Dev Test 1"
            })).StatusCode);
        }

        // MarkAllRead after 2 notifications from Admin
        for (var i = 1; i <= 2; i++)
        {
            Assert.Equal(HttpStatusCode.Accepted, (await admin.Client.PostAsJsonAsync("/api/admin/notifications", new CreateAdminNotificationRequest
            {
                RecipientGroup = "PARENT",
                Type = "ANNOUNCEMENT",
                Title = $"Tin {i}",
                Body = "Nội dung",
                SendEmail = false
            })).StatusCode);
        }

        await ApiAssertHelper.EventuallyAsync(async () =>
        {
            var unread = await parent.Client.GetFromJsonAsync<NotificationUnreadCountResponse>("/api/notifications/unread-count");
            return unread?.UnreadCount >= 2;
        });

        Assert.Equal(HttpStatusCode.OK, (await parent.Client.PutAsync("/api/notifications/read-all", null)).StatusCode);
        var afterReadAll = await parent.Client.GetFromJsonAsync<NotificationUnreadCountResponse>("/api/notifications/unread-count");
        Assert.Equal(0, afterReadAll!.UnreadCount);

        // TC-COMM-007: UpcomingLessonReminderService sends 1 notification & 1 email even when invoked twice
        Factory.FakeEmail.Reset();
        var tzId = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
        var vnTz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTz);
        var offsetMinutes = vnNow.TimeOfDay < TimeSpan.FromHours(23.5) ? 30 : 5;
        var lessonStart = vnNow.AddMinutes(offsetMinutes);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var parentAccount = await db.Users.SingleAsync(u => u.Id == parent.AccountId);
            parentAccount.Email = $"parent_{parent.Phone}@test.local";
            parentAccount.EmailConfirmed = true;

            var course = new Course { Code = "REM-C", Name = "Toán Nhắc", MinAge = 6, MaxAge = 10, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var level = new CourseLevel { CourseId = course.Id, Code = "REM-L", Name = "L1", Fee = 1000000m, TotalSessions = 1, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp Nhắc", MaxCapacity = 10, StartDate = vnNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = student.Id, Status = "ACTIVE" });
            db.Lessons.Add(new Lesson
            {
                ClassId = cls.Id,
                Title = "Buổi sắp học",
                LessonDate = vnNow.Date,
                StartTime = lessonStart,
                EndTime = lessonStart.AddHours(1),
                Status = "SCHEDULED"
            });
            await db.SaveChangesAsync();
        }

        var scopeFactory = Factory.Services.GetRequiredService<IServiceScopeFactory>();
        var reminderService = new UpcomingLessonReminderService(
            scopeFactory,
            NullLogger<UpcomingLessonReminderService>.Instance);

        await reminderService.ProcessUpcomingLessonsAsync(CancellationToken.None);
        await reminderService.ProcessUpcomingLessonsAsync(CancellationToken.None);

        await ApiAssertHelper.EventuallyAsync(async () =>
        {
            var tInbox = await teacher.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications");
            var pInbox = await parent.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications");
            return tInbox is not null && tInbox.Items.Any(n => n.Type == "ATTENDANCE" && n.Title.Contains("Hôm nay bạn có buổi học"))
                && pInbox is not null && pInbox.Items.Any(n => n.Type == "ATTENDANCE" && n.Title.Contains("Hôm nay bạn có buổi học"));
        });

        var teacherScheduleCount = (await teacher.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications"))!
            .Items.Count(n => n.Type == "ATTENDANCE" && n.Title.Contains("Hôm nay bạn có buổi học"));
        var parentScheduleCount = (await parent.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications"))!
            .Items.Count(n => n.Type == "ATTENDANCE" && n.Title.Contains("Hôm nay bạn có buổi học"));
        Assert.Equal(1, teacherScheduleCount);
        Assert.Equal(1, parentScheduleCount);

        var parentEmailCount = Factory.FakeEmail.SentMessages.Count(m => m.ToEmail == $"parent_{parent.Phone}@test.local");
        var teacherEmailCount = Factory.FakeEmail.SentMessages.Count(m => m.ToEmail == teacher.Email);
        Assert.Equal(1, parentEmailCount);
        Assert.Equal(1, teacherEmailCount);
    }
}
