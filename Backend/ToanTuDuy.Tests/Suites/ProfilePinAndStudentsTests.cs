using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Profile;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Models;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class ProfilePinAndStudentsTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Profile_RolePermissions_And_AvatarValidation_Work()
    {
        using var anon = Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/profile")).StatusCode);

        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);

        // Teacher cannot update email via /api/profile/email
        var teacherEmailUpdate = await teacher.Client.PutAsJsonAsync("/api/profile/email", new UpdateEmailRequest
        {
            Email = "new@test.local"
        });
        Assert.Equal(HttpStatusCode.Forbidden, teacherEmailUpdate.StatusCode);

        // Admin updates PendingEmail without overwriting confirmed Email yet
        var adminEmailUpdate = await admin.Client.PutAsJsonAsync("/api/profile/email", new UpdateEmailRequest
        {
            Email = "admin.pending@test.local"
        });
        Assert.Equal(HttpStatusCode.OK, adminEmailUpdate.StatusCode);
        var adminProfile = await adminEmailUpdate.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("admin.pending@test.local", adminProfile!.PendingEmail);
        Assert.Equal(admin.Email, adminProfile.Email);

        // Invalid avatar MIME rejected with 400
        using var badForm = new MultipartFormDataContent();
        var badFile = new ByteArrayContent(Encoding.UTF8.GetBytes("not-an-image"));
        badFile.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        badForm.Add(badFile, "file", "notes.txt");
        var badUpload = await parent.Client.PostAsync("/api/profile/avatar", badForm);
        Assert.Equal(HttpStatusCode.BadRequest, badUpload.StatusCode);

        // Valid PNG avatar succeeds and Delete removes it
        using var okForm = new MultipartFormDataContent();
        var pngFile = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 });
        pngFile.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        okForm.Add(pngFile, "file", "avatar.png");
        var okUpload = await parent.Client.PostAsync("/api/profile/avatar", okForm);
        Assert.Equal(HttpStatusCode.OK, okUpload.StatusCode);
        var updatedProfile = await okUpload.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Contains("res.cloudinary.com", updatedProfile!.Avatar);

        var delAvatar = await parent.Client.DeleteAsync("/api/profile/avatar");
        Assert.Equal(HttpStatusCode.OK, delAvatar.StatusCode);
        var clearedProfile = await delAvatar.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Null(clearedProfile!.Avatar);
    }

    [Fact]
    public async Task Pin_SetVerify_And_StudentDeleteRequirePin_And_Isolation()
    {
        var parentA = await AuthTestHelper.RegisterParentAsync(Factory, fullName: "Phụ Huynh A");
        var parentB = await AuthTestHelper.RegisterParentAsync(Factory, fullName: "Phụ Huynh B");

        // Create student for Parent A
        var createResp = await parentA.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest
        {
            FullName = "Bé An"
        });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var studentA = (await createResp.Content.ReadFromJsonAsync<StudentResponse>())!;

        // Parent B cannot read/update/delete Parent A's student
        Assert.Equal(HttpStatusCode.NotFound, (await parentB.Client.GetAsync($"/api/students/{studentA.Id}")).StatusCode);

        // Deleting without PIN configured fails with 400
        var noPinResp = await parentA.Client.DeleteAsync($"/api/students/{studentA.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, noPinResp.StatusCode);

        // Set PIN for Parent A
        var setPinResp = await parentA.Client.PutAsJsonAsync("/api/profile/pin", new SetPinRequest
        {
            NewPin = "123456"
        });
        Assert.Equal(HttpStatusCode.NoContent, setPinResp.StatusCode);

        // Verify PIN right & wrong
        var verifyOk = await (await parentA.Client.PostAsJsonAsync("/api/profile/pin/verify", new VerifyPinRequest { Pin = "123456" }))
            .Content.ReadFromJsonAsync<PinVerificationResponse>();
        Assert.True(verifyOk!.Valid);

        var verifyWrong = await (await parentA.Client.PostAsJsonAsync("/api/profile/pin/verify", new VerifyPinRequest { Pin = "654321" }))
            .Content.ReadFromJsonAsync<PinVerificationResponse>();
        Assert.False(verifyWrong!.Valid);

        // Delete with wrong X-Pin -> 403
        using var wrongPinReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/students/{studentA.Id}");
        wrongPinReq.Headers.Add("X-Pin", "000000");
        var wrongPinResp = await parentA.Client.SendAsync(wrongPinReq);
        Assert.Equal(HttpStatusCode.Forbidden, wrongPinResp.StatusCode);

        // Create second student with related ClassStudent -> 409 Conflict even with valid X-Pin
        var studentEnrolled = (await (await parentA.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Bình" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "TEST-C", Name = "Toán Test", MinAge = 5, MaxAge = 10, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            var level = new CourseLevel { CourseId = course.Id, Code = "L1", Name = "Level 1", OrderIndex = 1, Status = "ACTIVE", Fee = 1000000m, TotalSessions = 4 };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();
            var cls = new Classes { CourseLevelId = level.Id, Name = "Lớp 1", MaxCapacity = 10, StartDate = DateTime.UtcNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = studentEnrolled.Id, Status = "ACTIVE" });
            await db.SaveChangesAsync();
        }

        using var conflictReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/students/{studentEnrolled.Id}");
        conflictReq.Headers.Add("X-Pin", "123456");
        Assert.Equal(HttpStatusCode.Conflict, (await parentA.Client.SendAsync(conflictReq)).StatusCode);

        // Delete clean student with valid X-Pin -> 204 NoContent
        using var validPinReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/students/{studentA.Id}");
        validPinReq.Headers.Add("X-Pin", "123456");
        Assert.Equal(HttpStatusCode.NoContent, (await parentA.Client.SendAsync(validPinReq)).StatusCode);
    }
}
