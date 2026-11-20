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

    [Fact]
    public async Task Profile_UpdatePhoneAndEmailRules_WorkAcrossRoles()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory, fullName: "PH Ban Đầu");
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);

        // TC-PROF-002: Admin updates FullName & Phone; duplicate Phone -> 409
        var newAdminPhone = AuthTestHelper.NextPhone("097");
        var adminUpdate = await admin.Client.PutAsJsonAsync("/api/profile", new UpdateProfileRequest
        {
            FullName = "Admin Mới",
            Phone = newAdminPhone
        });
        Assert.Equal(HttpStatusCode.OK, adminUpdate.StatusCode);
        var updatedAdmin = (await adminUpdate.Content.ReadFromJsonAsync<ProfileResponse>())!;
        Assert.Equal("Admin Mới", updatedAdmin.FullName);
        Assert.Equal(newAdminPhone, updatedAdmin.Phone);

        // Changing Phone/UserName rotates SecurityStamp, so Admin must re-login with the new phone
        using var anon = Factory.CreateClient();
        var reauth = await AuthTestHelper.LoginAsync(anon, newAdminPhone, admin.Password);
        admin.Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", reauth.AccessToken);

        var dupPhoneResp = await admin.Client.PutAsJsonAsync("/api/profile", new UpdateProfileRequest
        {
            FullName = "Admin Trùng",
            Phone = parent.Phone
        });
        Assert.Equal(HttpStatusCode.Conflict, dupPhoneResp.StatusCode);

        // Parent & Teacher cannot change Phone (keeps original Phone)
        var parentUpdate = await parent.Client.PutAsJsonAsync("/api/profile", new UpdateProfileRequest
        {
            FullName = "PH Đã Đổi",
            Phone = AuthTestHelper.NextPhone("099")
        });
        Assert.Equal(HttpStatusCode.OK, parentUpdate.StatusCode);
        var updatedParent = (await parentUpdate.Content.ReadFromJsonAsync<ProfileResponse>())!;
        Assert.Equal("PH Đã Đổi", updatedParent.FullName);
        Assert.Equal(parent.Phone, updatedParent.Phone);

        // TC-PROF-005: Unconfirmed Parent can update email, then once confirmed -> 403 Forbidden
        var parentEmail = $"parent_{parent.Phone}@test.local";
        var setEmail = await parent.Client.PutAsJsonAsync("/api/profile/email", new UpdateEmailRequest
        {
            Email = parentEmail
        });
        Assert.Equal(HttpStatusCode.OK, setEmail.StatusCode);

        var mail = Factory.FakeEmail.SentMessages.Last(m => m.ToEmail == parentEmail);
        var match = System.Text.RegularExpressions.Regex.Match(mail.TextBody ?? "", @"token=([^\s&]+)");
        Assert.True(match.Success);
        var verifyResp = await anon.GetAsync($"/api/email-verification?token={match.Groups[1].Value}");
        Assert.Equal(HttpStatusCode.OK, verifyResp.StatusCode);

        // SetEmailAsync / verification rotates SecurityStamp, so Parent must re-login before calling authenticated endpoint
        var parentReauth = await AuthTestHelper.LoginAsync(anon, parent.Phone, parent.Password);
        parent.Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", parentReauth.AccessToken);

        var blockedAfterConfirm = await parent.Client.PutAsJsonAsync("/api/profile/email", new UpdateEmailRequest
        {
            Email = "another@test.local"
        });
        Assert.Equal(HttpStatusCode.Forbidden, blockedAfterConfirm.StatusCode);

        // Duplicate email used by Teacher -> 409 Conflict for Admin pending email
        var dupEmail = await admin.Client.PutAsJsonAsync("/api/profile/email", new UpdateEmailRequest
        {
            Email = teacher.Email
        });
        Assert.Equal(HttpStatusCode.Conflict, dupEmail.StatusCode);
    }

    [Fact]
    public async Task Pin_ChangeRequiresCurrentPin_And_StudentUpdateAvatarEnforcesIsolation()
    {
        var parentA = await AuthTestHelper.RegisterParentAsync(Factory, fullName: "PH A");
        var parentB = await AuthTestHelper.RegisterParentAsync(Factory, fullName: "PH B");

        // TC-PROF-009: Invalid length PIN -> 400
        var invalidPin = await parentA.Client.PutAsJsonAsync("/api/profile/pin", new SetPinRequest
        {
            NewPin = "123"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPin.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await parentA.Client.PutAsJsonAsync("/api/profile/pin", new SetPinRequest
        {
            NewPin = "123456"
        })).StatusCode);

        // Changing PIN with wrong currentPin -> 400
        var wrongCurrent = await parentA.Client.PutAsJsonAsync("/api/profile/pin", new SetPinRequest
        {
            CurrentPin = "000000",
            NewPin = "654321"
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrongCurrent.StatusCode);

        // Changing PIN with correct currentPin -> 204
        var rightCurrent = await parentA.Client.PutAsJsonAsync("/api/profile/pin", new SetPinRequest
        {
            CurrentPin = "123456",
            NewPin = "654321"
        });
        Assert.Equal(HttpStatusCode.NoContent, rightCurrent.StatusCode);

        // Student Update & Avatar Upload/Delete + Parent B isolation
        var student = (await (await parentA.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Cũ" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        var updateOk = await parentA.Client.PutAsJsonAsync($"/api/students/{student.Id}", new UpdateStudentRequest
        {
            FullName = "Bé Mới"
        });
        Assert.Equal(HttpStatusCode.OK, updateOk.StatusCode);
        Assert.Equal("Bé Mới", (await updateOk.Content.ReadFromJsonAsync<StudentResponse>())!.FullName);

        using var form = new MultipartFormDataContent();
        var png = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 });
        png.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(png, "file", "student.png");
        var uploadResp = await parentA.Client.PostAsync($"/api/students/{student.Id}/avatar", form);
        Assert.Equal(HttpStatusCode.OK, uploadResp.StatusCode);
        var uploaded = (await uploadResp.Content.ReadFromJsonAsync<StudentResponse>())!;
        Assert.Contains("res.cloudinary.com", uploaded.Avatar);

        var delAvatarResp = await parentA.Client.DeleteAsync($"/api/students/{student.Id}/avatar");
        Assert.Equal(HttpStatusCode.OK, delAvatarResp.StatusCode);
        Assert.Null((await delAvatarResp.Content.ReadFromJsonAsync<StudentResponse>())!.Avatar);

        // Parent B isolated from Parent A's student on PUT & DELETE
        Assert.Equal(HttpStatusCode.NotFound, (await parentB.Client.PutAsJsonAsync($"/api/students/{student.Id}", new UpdateStudentRequest
        {
            FullName = "Hack"
        })).StatusCode);

        await parentB.Client.PutAsJsonAsync("/api/profile/pin", new SetPinRequest { NewPin = "1111" });
        using var crossDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/students/{student.Id}");
        crossDelete.Headers.Add("X-Pin", "1111");
        Assert.Equal(HttpStatusCode.NotFound, (await parentB.Client.SendAsync(crossDelete)).StatusCode);
    }
}
