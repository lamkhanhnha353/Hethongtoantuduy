using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Models;

namespace ToanTuDuy.Tests.Infrastructure;

public sealed record AuthenticatedActor(
    int AccountId,
    int ProfileId,
    string Phone,
    string Email,
    string Password,
    string Token,
    HttpClient Client);

public static class AuthTestHelper
{
    private static int _sequence = 1000;

    public static string NextPhone(string prefix = "098") =>
        $"{prefix}{Interlocked.Increment(ref _sequence):D7}"[..10];

    public static async Task<AuthenticatedActor> CreateAdminAsync(
        TestWebApplicationFactory factory,
        string? phone = null,
        string? email = null,
        string password = "Password@123")
    {
        phone ??= NextPhone("091");
        email ??= $"admin_{phone}@test.local";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new ApplicationUser
        {
            UserName = phone,
            PhoneNumber = phone,
            Email = email,
            EmailConfirmed = true,
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var profile = new AdminProfile
        {
            AccountId = user.Id,
            FullName = $"Admin {phone}",
            CreatedAt = DateTime.UtcNow
        };
        db.AdminProfiles.Add(profile);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var auth = await LoginAsync(client, phone, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return new AuthenticatedActor(user.Id, profile.Id, phone, email, password, auth.AccessToken, client);
    }

    public static async Task<AuthenticatedActor> RegisterParentAsync(
        TestWebApplicationFactory factory,
        string? phone = null,
        string fullName = "Phụ Huynh Test",
        string password = "Password@123")
    {
        phone ??= NextPhone("093");
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Phone = phone,
            Password = password,
            Role = "Parent",
            FullName = fullName
        });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accountId = db.Users.Where(u => u.UserName == phone).Select(u => u.Id).First();

        return new AuthenticatedActor(accountId, auth.ProfileId, phone, string.Empty, password, auth.AccessToken, client);
    }

    public static async Task<AuthenticatedActor> RegisterTeacherAsync(
        TestWebApplicationFactory factory,
        string? phone = null,
        string? email = null,
        string fullName = "Giáo Viên Test",
        string password = "Password@123",
        bool markEmailConfirmed = false)
    {
        phone ??= NextPhone("092");
        email ??= $"teacher_{phone}@test.local";

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Phone = phone,
            Password = password,
            Role = "Teacher",
            FullName = fullName,
            Email = email
        });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        int accountId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = db.Users.First(u => u.UserName == phone);
            accountId = user.Id;
            if (markEmailConfirmed)
            {
                user.EmailConfirmed = true;
                await db.SaveChangesAsync();
            }
        }

        return new AuthenticatedActor(accountId, auth.ProfileId, phone, email, password, auth.AccessToken, client);
    }

    public static async Task<AuthResponse> LoginAsync(HttpClient client, string phone, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Phone = phone,
            Password = password
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
