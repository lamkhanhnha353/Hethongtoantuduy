using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class UsersAndAdminAccountsTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AdminAccounts_PagedSearchRbac_And_ResetPasswordFlow_Work()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory, fullName: "Nguyễn Văn Filter");
        var confirmedTeacher = await AuthTestHelper.RegisterTeacherAsync(
            Factory,
            fullName: "GV Đã Xác Minh",
            markEmailConfirmed: true);
        var unconfirmedTeacher = await AuthTestHelper.RegisterTeacherAsync(
            Factory,
            fullName: "GV Chưa Xác Minh",
            markEmailConfirmed: false);

        // TC-USR-001: Parent blocked 403
        Assert.Equal(HttpStatusCode.Forbidden,
            (await parent.Client.GetAsync("/api/admin/accounts/paged")).StatusCode);

        // Lock one account so ActiveCount & BlockedCount are both > 0
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.PutAsJsonAsync(
            $"/api/admin/accounts/{unconfirmedTeacher.AccountId}/status",
            new UpdateAccountStatusRequest { IsActive = false })).StatusCode);

        var allPaged = (await admin.Client.GetFromJsonAsync<AdminAccountPagedResponse>(
            "/api/admin/accounts/paged?page=1&pageSize=20"))!;
        Assert.Equal(4, allPaged.TotalCount);
        Assert.Equal(3, allPaged.ActiveCount);
        Assert.Equal(1, allPaged.BlockedCount);

        var filterRole = (await admin.Client.GetFromJsonAsync<AdminAccountPagedResponse>(
            "/api/admin/accounts/paged?role=Teacher&isActive=false"))!;
        var blockedItem = Assert.Single(filterRole.Items);
        Assert.Equal(unconfirmedTeacher.AccountId, blockedItem.Id);

        var filterKeyword = (await admin.Client.GetFromJsonAsync<AdminAccountPagedResponse>(
            "/api/admin/accounts/paged?keyword=Filter"))!;
        Assert.Equal(parent.AccountId, Assert.Single(filterKeyword.Items).Id);

        // TC-USR-003: Cannot reset Admin or unconfirmed email
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.Client.PostAsync($"/api/admin/accounts/{admin.AccountId}/reset-password", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.Client.PostAsync($"/api/admin/accounts/{unconfirmedTeacher.AccountId}/reset-password", null)).StatusCode);

        // Reset confirmed Teacher succeeds, revokes old JWT, sends temp password
        var resetOk = await admin.Client.PostAsync($"/api/admin/accounts/{confirmedTeacher.AccountId}/reset-password", null);
        Assert.Equal(HttpStatusCode.OK, resetOk.StatusCode);

        // Old JWT invalidated immediately
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await confirmedTeacher.Client.GetAsync("/api/profile")).StatusCode);

        var resetMail = Factory.FakeEmail.SentMessages.Last(m => m.ToEmail == confirmedTeacher.Email);
        var match = Regex.Match(resetMail.TextBody ?? "", @"Mật khẩu tạm của bạn:\s*(\S+)");
        Assert.True(match.Success);
        using var anon = Factory.CreateClient();
        var relogin = await AuthTestHelper.LoginAsync(anon, confirmedTeacher.Phone, match.Groups[1].Value);
        Assert.False(string.IsNullOrWhiteSpace(relogin.AccessToken));

        // TC-USR-004: 502 BadGateway when email delivery throws
        Factory.FakeEmail.ThrowOnSend = true;
        var resetFail = await admin.Client.PostAsync($"/api/admin/accounts/{confirmedTeacher.AccountId}/reset-password", null);
        Assert.Equal(HttpStatusCode.BadGateway, resetFail.StatusCode);
    }

    [Fact]
    public async Task Teachers_CrudPagingAndStatusRbac_And_ParentsCrudAndPagedStudents_Work()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacherActor = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);

        // TC-USR-005: Teacher cannot call UpdateStatus (requires Admin -> 403)
        Assert.Equal(HttpStatusCode.Forbidden, (await teacherActor.Client.PutAsJsonAsync(
            $"/api/teachers/{teacherActor.ProfileId}/status",
            new UpdateTeacherStatusDto { IsActive = false })).StatusCode);

        // Admin creates, searches, updates, locks, unlocks, deletes a teacher
        var tPhone = AuthTestHelper.NextPhone("092");
        var createTeacher = await admin.Client.PostAsJsonAsync("/api/teachers", new CreateTeacherDto
        {
            Phone = tPhone,
            Password = "Password@123",
            FullName = "Cô Soroban Mới",
            Email = $"newt_{tPhone}@test.local"
        });
        Assert.Equal(HttpStatusCode.Created, createTeacher.StatusCode);
        var createdTeacher = (await createTeacher.Content.ReadFromJsonAsync<TeacherResponseDto>())!;

        Assert.Equal(HttpStatusCode.OK, (await admin.Client.PutAsJsonAsync(
            $"/api/teachers/{createdTeacher.Id}",
            new UpdateTeacherDto
            {
                FullName = "Cô Soroban Đã Sửa",
                Specialize = "Soroban Nâng Cao"
            })).StatusCode);

        var pagedTeachers = (await admin.Client.GetFromJsonAsync<PagedResult<TeacherResponseDto>>(
            "/api/teachers/paged?keyword=Nâng Cao&isActive=true"))!;
        Assert.Equal(createdTeacher.Id, Assert.Single(pagedTeachers.Items).Id);

        // Admin locks teacherActor via /api/teachers/{id}/status -> revokes JWT
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.PutAsJsonAsync(
            $"/api/teachers/{teacherActor.ProfileId}/status",
            new UpdateTeacherStatusDto { IsActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await teacherActor.Client.GetAsync("/api/profile")).StatusCode);

        // Delete createdTeacher
        Assert.Equal(HttpStatusCode.OK,
            (await admin.Client.DeleteAsync($"/api/teachers/{createdTeacher.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.Client.GetAsync($"/api/teachers/{createdTeacher.Id}")).StatusCode);

        // SC-USR-05: Parents CRUD & paged students
        var pPhone = AuthTestHelper.NextPhone("093");
        var createParent = await admin.Client.PostAsJsonAsync("/api/parents", new CreateParentDto
        {
            Phone = pPhone,
            Password = "Password@123",
            FullName = "Phụ Huynh Quản Trị"
        });
        Assert.Equal(HttpStatusCode.Created, createParent.StatusCode);
        var createdParent = (await createParent.Content.ReadFromJsonAsync<ParentResponseDto>())!;

        Assert.Equal(HttpStatusCode.OK, (await admin.Client.PutAsJsonAsync(
            $"/api/parents/{createdParent.Id}",
            new UpdateParentDto { FullName = "Phụ Huynh Updated" })).StatusCode);

        using var anon = Factory.CreateClient();
        var parentAuth = await AuthTestHelper.LoginAsync(anon, pPhone, "Password@123");
        using var parentClient = Factory.CreateClient();
        parentClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", parentAuth.AccessToken);

        var student = (await (await parentClient.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Filter" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        var parentDetail = (await admin.Client.GetFromJsonAsync<ParentResponseDto>($"/api/parents/{createdParent.Id}"))!;
        Assert.Equal("Phụ Huynh Updated", parentDetail.FullName);
        Assert.Contains(parentDetail.Students, s => s.Id == student.Id);

        var pagedStudents = (await admin.Client.GetFromJsonAsync<PagedResult<StudentResponseDto>>(
            $"/api/parents/students/paged?parentAccountId={createdParent.AccountId}&keyword=Filter"))!;
        Assert.Equal(student.Id, Assert.Single(pagedStudents.Items).Id);

        Assert.Equal(HttpStatusCode.OK,
            (await admin.Client.DeleteAsync($"/api/parents/{createdParent.Id}")).StatusCode);
    }
}
