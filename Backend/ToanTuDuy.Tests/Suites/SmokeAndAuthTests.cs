using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Resend;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Profile;
using ToanTuDuy.Options;
using ToanTuDuy.Services;
using ToanTuDuy.Services.Email;
using ToanTuDuy.Services.Notifications;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class SmokeAndAuthTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task OpenApi_Returns404InProduction_And200InDevelopment()
    {
        using var prodClient = Factory.CreateClient();
        var prodResponse = await prodClient.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.NotFound, prodResponse.StatusCode);

        await using var devFactory = new DevWebAppFactory();
        using var devClient = devFactory.CreateClient();
        var devResponse = await devClient.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, devResponse.StatusCode);
    }

    [Fact]
    public void EmailDeliveryService_IsConfigured_WhenGoogleSmtpSettingsProvided()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resend:ApiToken"] = "",
                ["Smtp:Host"] = "smtp.gmail.com",
                ["Smtp:Port"] = "587",
                ["Smtp:EnableSsl"] = "true",
                ["Smtp:Username"] = "test@gmail.com",
                ["Smtp:Password"] = "app-password",
                ["Smtp:FromEmail"] = "test@gmail.com",
                ["Smtp:FromName"] = "Hệ thống Toán Tư Duy",
                ["EmailVerification:FromEmail"] = "test@gmail.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddResend(o => o.ApiToken = "");
        services.Configure<SmtpSettings>(config.GetSection(SmtpSettings.SectionName));
        services.Configure<EmailVerificationSettings>(config.GetSection(EmailVerificationSettings.SectionName));
        services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEmailDeliveryService>();

        var configured = service.IsConfigured(out var reason);
        Assert.True(configured, reason);
        Assert.Null(reason);
    }

    [Fact]
    public async Task ScopedEmailServices_ResolveRealEmailDelivery_WithoutRootScopeViolationInDevelopment()
    {
        await using var realDiFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = TestWebApplicationFactory.TestConnectionString,
                        ["Redis:ConnectionString"] = "localhost:6379,abortConnect=false,connectTimeout=200,syncTimeout=200",
                        ["Resend:ApiToken"] = "",
                        ["Smtp:Host"] = "smtp.gmail.com",
                        ["Smtp:Port"] = "587",
                        ["Smtp:EnableSsl"] = "true",
                        ["Smtp:Username"] = "test@gmail.com",
                        ["Smtp:Password"] = "app-password",
                        ["Smtp:FromEmail"] = "test@gmail.com",
                        ["Smtp:FromName"] = "Hệ thống Toán Tư Duy",
                        ["EmailVerification:FromEmail"] = "test@gmail.com"
                    });
                });
            });

        await using var scope = realDiFactory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var emailDelivery = sp.GetRequiredService<IEmailDeliveryService>();
        Assert.IsType<EmailDeliveryService>(emailDelivery);
        Assert.True(emailDelivery.IsConfigured(out var reason), reason);

        Assert.NotNull(sp.GetRequiredService<IBusinessNotificationEmailService>());
        Assert.NotNull(sp.GetRequiredService<IPayslipMailService>());
        Assert.NotNull(sp.GetRequiredService<IEmailVerificationService>());
        Assert.NotNull(sp.GetRequiredService<IPasswordResetService>());
    }

    [Fact]
    public async Task RegisterParent_CreatesProfile_RejectsDuplicatePhone_AndInvalidRole()
    {
        using var client = Factory.CreateClient();
        var phone = AuthTestHelper.NextPhone("093");

        var okResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Phone = phone,
            Password = "Password@123",
            Role = "Parent",
            FullName = "Nguyễn Văn Phụ Huynh"
        });
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        var auth = await okResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal("Parent", auth.Role);
        Assert.True(auth.ProfileId > 0);

        var dupResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Phone = phone,
            Password = "Password@123",
            Role = "Parent",
            FullName = "Trùng SĐT"
        });
        Assert.Equal(HttpStatusCode.BadRequest, dupResponse.StatusCode);

        var badRoleResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Phone = AuthTestHelper.NextPhone("093"),
            Password = "Password@123",
            Role = "Admin",
            FullName = "Không Hợp Lệ"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badRoleResponse.StatusCode);
    }

    [Fact]
    public async Task RegisterTeacher_RequiresEmail_AndVerifiesIdempotently()
    {
        using var client = Factory.CreateClient();

        var missingEmail = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Phone = AuthTestHelper.NextPhone("092"),
            Password = "Password@123",
            Role = "Teacher",
            FullName = "GV Thiếu Mail"
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingEmail.StatusCode);

        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: false);
        var mail = Assert.Single(Factory.FakeEmail.SentMessages, m => m.ToEmail == teacher.Email);
        var tokenMatch = Regex.Match(mail.TextBody ?? "", @"token=([^\s&]+)");
        Assert.True(tokenMatch.Success, "Verification link token missing from email body.");
        var rawToken = Uri.UnescapeDataString(tokenMatch.Groups[1].Value);

        // Click verification link 1st time
        var verify1 = await client.GetAsync($"/api/email-verification?token={Uri.EscapeDataString(rawToken)}");
        Assert.Equal(HttpStatusCode.OK, verify1.StatusCode);
        var html1 = await verify1.Content.ReadAsStringAsync();
        Assert.Contains("Xác minh thành công", html1);

        var profile = await teacher.Client.GetFromJsonAsync<ProfileResponse>("/api/profile");
        Assert.NotNull(profile);
        Assert.Equal(teacher.Email, profile.Email);

        // Verify Account.EmailConfirmed persisted as true (note: ProfileService.MapTeacher currently omits EmailConfirm)
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == teacher.AccountId);
            Assert.True(user.EmailConfirmed);
        }

        // Click verification link 2nd time -> idempotent "already used"
        var verify2 = await client.GetAsync($"/api/email-verification?token={Uri.EscapeDataString(rawToken)}");
        Assert.Equal(HttpStatusCode.OK, verify2.StatusCode);
        var html2 = await verify2.Content.ReadAsStringAsync();
        Assert.Contains("Liên kết đã được sử dụng", html2);
    }

    [Fact]
    public async Task ForgotPassword_And_ChangePassword_WorkEndToEnd()
    {
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        using var anonClient = Factory.CreateClient();

        // Non-existent email still returns 200 anti-enumeration
        var unknown = await anonClient.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = "nobody@test.local"
        });
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);

        // Valid confirmed email receives temporary password
        var forgot1 = await anonClient.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = teacher.Email
        });
        Assert.Equal(HttpStatusCode.OK, forgot1.StatusCode);

        // Immediate retry triggers 429 cooldown
        var forgot2 = await anonClient.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = teacher.Email
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, forgot2.StatusCode);

        var sentMail = Factory.FakeEmail.SentMessages.Last(m => m.ToEmail == teacher.Email);
        var passMatch = Regex.Match(sentMail.TextBody ?? "", @"Mật khẩu tạm của bạn:\s*(\S+)");
        Assert.True(passMatch.Success, "Temporary password not found in email.");
        var tempPassword = passMatch.Groups[1].Value;

        // Login with temporary password & change password
        var loginResp = await AuthTestHelper.LoginAsync(anonClient, teacher.Phone, tempPassword);
        using var tempClient = Factory.CreateClient();
        tempClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResp.AccessToken);

        var changeResp = await tempClient.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = tempPassword,
            NewPassword = "NewPassword@456",
            ConfirmNewPassword = "NewPassword@456"
        });
        Assert.Equal(HttpStatusCode.OK, changeResp.StatusCode);

        var relogin = await AuthTestHelper.LoginAsync(anonClient, teacher.Phone, "NewPassword@456");
        Assert.False(string.IsNullOrWhiteSpace(relogin.AccessToken));
    }

    [Fact]
    public async Task AdminLockAccount_RevokesExistingJwtImmediately_AndCannotSelfLock()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);

        var beforeLock = await parent.Client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, beforeLock.StatusCode);

        var selfLock = await admin.Client.PutAsJsonAsync($"/api/admin/accounts/{admin.AccountId}/status", new UpdateAccountStatusRequest
        {
            IsActive = false
        });
        Assert.Equal(HttpStatusCode.Conflict, selfLock.StatusCode);

        var lockParent = await admin.Client.PutAsJsonAsync($"/api/admin/accounts/{parent.AccountId}/status", new UpdateAccountStatusRequest
        {
            IsActive = false
        });
        Assert.Equal(HttpStatusCode.OK, lockParent.StatusCode);

        // Old JWT is rejected by JwtBearerEvents.OnTokenValidated
        var afterLock = await parent.Client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLock.StatusCode);
    }

    [Fact]
    public async Task ErrorContracts_ModelStateValidation_And_ApiException_FollowExpectedShapes()
    {
        using var client = Factory.CreateClient();

        // TC-INF-003: Empty body triggers ModelState ValidationProblemDetails with errors
        var validationResp = await client.PostAsJsonAsync("/api/auth/register", new { });
        Assert.Equal(HttpStatusCode.BadRequest, validationResp.StatusCode);
        var validationBody = await validationResp.Content.ReadAsStringAsync();
        using (var doc = System.Text.Json.JsonDocument.Parse(validationBody))
        {
            Assert.True(doc.RootElement.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Phone", out _));
            Assert.True(errors.TryGetProperty("Password", out _));
            Assert.True(errors.TryGetProperty("FullName", out _));
        }

        // GlobalExceptionHandler envelope { code, message }
        var apiErrResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Phone = AuthTestHelper.NextPhone("093"),
            Password = "Password@123",
            Role = "Admin",
            FullName = "Invalid Role"
        });
        Assert.Equal(HttpStatusCode.BadRequest, apiErrResp.StatusCode);
        using var errDoc = System.Text.Json.JsonDocument.Parse(await apiErrResp.Content.ReadAsStringAsync());
        Assert.Equal(400, errDoc.RootElement.GetProperty("code").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(errDoc.RootElement.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task Pagination_NormalizesOutOfRangePageAndPageSize()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);

        // Seed 25 extra users directly for fast pagination verification (TC-INF-004)
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 0; i < 25; i++)
            {
                var phone = AuthTestHelper.NextPhone("094");
                db.Users.Add(new ToanTuDuy.Models.ApplicationUser
                {
                    UserName = phone,
                    NormalizedUserName = phone,
                    PhoneNumber = phone,
                    Role = "Parent",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            await db.SaveChangesAsync();
        }

        var clamped = (await admin.Client.GetFromJsonAsync<AdminAccountPagedResponse>(
            "/api/admin/accounts/paged?page=0&pageSize=999"))!;
        Assert.Equal(1, clamped.Page);
        Assert.Equal(100, clamped.PageSize);
        Assert.True(clamped.TotalCount >= 26);
        Assert.Equal(1, clamped.TotalPages);
        Assert.False(clamped.HasPreviousPage);
        Assert.False(clamped.HasNextPage);

        var page2 = (await admin.Client.GetFromJsonAsync<AdminAccountPagedResponse>(
            "/api/admin/accounts/paged?page=2&pageSize=10"))!;
        Assert.Equal(2, page2.Page);
        Assert.Equal(10, page2.PageSize);
        Assert.Equal(10, page2.Items.Count);
        Assert.True(page2.HasPreviousPage);
        Assert.True(page2.HasNextPage);
    }

    [Fact]
    public async Task Login_SucceedsForAllRoles_AndRejectsWrongPasswordUnknownPhoneAndCorruptedHash()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        using var anon = Factory.CreateClient();

        // TC-AUTH-006: Admin, Teacher, Parent login succeed
        foreach (var (phone, password, expectedRole) in new[]
        {
            (admin.Phone, admin.Password, "Admin"),
            (teacher.Phone, teacher.Password, "Teacher"),
            (parent.Phone, parent.Password, "Parent")
        })
        {
            var ok = await anon.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Phone = phone,
                Password = password
            });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            var body = (await ok.Content.ReadFromJsonAsync<AuthResponse>())!;
            Assert.Equal(expectedRole, body.Role);
            Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        }

        // TC-AUTH-007: Wrong password -> 401
        var wrongPass = await anon.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Phone = parent.Phone,
            Password = "WrongPassword@999"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPass.StatusCode);

        // Non-existent phone -> 401
        var unknownPhone = await anon.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Phone = "0999999999",
            Password = "Password@123"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, unknownPhone.StatusCode);

        // Corrupted password hash -> 401 instead of 500 FormatException
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == parent.AccountId);
            user.PasswordHash = "not-valid-base64!!!";
            await db.SaveChangesAsync();
        }

        var corruptedLogin = await anon.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Phone = parent.Phone,
            Password = parent.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, corruptedLogin.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_Returns401OnInvalidToken_And503WhenClientIdMissing()
    {
        using var client = Factory.CreateClient();
        var invalidResp = await client.PostAsJsonAsync("/api/auth/google-login", new GoogleLoginDto
        {
            IdToken = "invalid-token"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, invalidResp.StatusCode);

        await using var unconfiguredFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Google:ClientId"] = "",
                    ["Google:AdditionalClientIds:0"] = "",
                    ["Google:AdditionalClientIds:1"] = ""
                });
            });
        });
        using var unconfiguredClient = unconfiguredFactory.CreateClient();
        var missingConfigResp = await unconfiguredClient.PostAsJsonAsync("/api/auth/google-login", new GoogleLoginDto
        {
            IdToken = "any-token"
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, missingConfigResp.StatusCode);
    }

    [Fact]
    public async Task EmailVerification_ResendCooldown429_InvalidAndExpiredTokens_AndPasswordEdgeCases()
    {
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: false);
        using var anon = Factory.CreateClient();

        // TC-AUTH-012: Immediate resend triggers 429 cooldown
        var resendTooSoon = await teacher.Client.PostAsync("/api/email-verification/resend", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, resendTooSoon.StatusCode);

        // Invalid token returns HTML with error message
        var invalidVerify = await anon.GetAsync("/api/email-verification?token=nonexistent-token-123");
        Assert.Equal(HttpStatusCode.OK, invalidVerify.StatusCode);
        Assert.Contains("Liên kết không hợp lệ", await invalidVerify.Content.ReadAsStringAsync());

        // Expired token returns HTML "Liên kết đã hết hạn"
        var expiredRawToken = $"expired_{Guid.NewGuid():N}";
        var expiredHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(expiredRawToken)));
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.EmailVerificationTokens.Add(new ToanTuDuy.Models.EmailVerificationToken
            {
                AccountId = teacher.AccountId,
                EmailAtIssue = teacher.Email,
                TokenHash = expiredHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-10),
                CreatedAt = DateTime.UtcNow.AddHours(-25)
            });
            await db.SaveChangesAsync();
        }

        var expiredVerify = await anon.GetAsync($"/api/email-verification?token={expiredRawToken}");
        Assert.Equal(HttpStatusCode.OK, expiredVerify.StatusCode);
        Assert.Contains("Liên kết đã hết hạn", await expiredVerify.Content.ReadAsStringAsync());

        // TC-AUTH-018: ForgotPassword for unconfirmed email returns 400
        var unconfirmedForgot = await anon.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = teacher.Email
        });
        Assert.Equal(HttpStatusCode.BadRequest, unconfirmedForgot.StatusCode);

        // TC-AUTH-014 & TC-AUTH-015: ChangePassword wrong current (401) and same new password (400)
        var wrongCurrent = await teacher.Client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword@1",
            NewPassword = "NewPassword@123",
            ConfirmNewPassword = "NewPassword@123"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongCurrent.StatusCode);

        var samePass = await teacher.Client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = teacher.Password,
            NewPassword = teacher.Password,
            ConfirmNewPassword = teacher.Password
        });
        Assert.Equal(HttpStatusCode.BadRequest, samePass.StatusCode);
    }
}
