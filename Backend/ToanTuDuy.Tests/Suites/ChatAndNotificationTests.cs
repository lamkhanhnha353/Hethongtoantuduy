using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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

        // Wait for NotificationFanoutWorker to process outbox
        NotificationListResponse? inboxOn = null;
        await ApiAssertHelper.EventuallyAsync(async () =>
        {
            inboxOn = await parentInAppOn.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications");
            return inboxOn is not null && inboxOn.Items.Count > 0;
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
}
