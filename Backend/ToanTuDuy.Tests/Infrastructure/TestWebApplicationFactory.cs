using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Respawn;
using Respawn.Graph;
using ToanTuDuy.Data;
using ToanTuDuy.Services;
using ToanTuDuy.Services.BackgroundJobs;
using ToanTuDuy.Services.Email;
using ToanTuDuy.Services.Notifications;
using ToanTuDuy.Services.Storage;
using ToanTuDuy.Tests.Infrastructure.Fakes;

namespace ToanTuDuy.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=ToanTuDuyDb_AutomatedTests;Trusted_Connection=True;MultipleActiveResultSets=true";

    public const string VnPayHashSecret = "TEST_VNPAY_SECRET_KEY_1234567890ABCDEF";

    private readonly string _environmentName;

    public FakeEmailDeliveryService FakeEmail { get; } = new();
    public FakeImageStorage FakeImages { get; } = new();
    public FakeChatAttachmentStorage FakeChatStorage { get; } = new();
    public FakeCourseMediaStorage FakeCourseMedia { get; } = new();
    public InMemoryUnreadCountState UnreadCountState { get; } = new();

    public TestWebApplicationFactory() : this("Production")
    {
    }

    protected TestWebApplicationFactory(string environmentName)
    {
        _environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["Redis:ConnectionString"] = "localhost:6379,abortConnect=false,connectTimeout=200,syncTimeout=200",
                ["Jwt:AccessTokenExpirationMinutes"] = "120",
                ["EmailVerification:BaseUrl"] = "http://localhost",
                ["EmailVerification:ResendCooldownSeconds"] = "60",
                ["PasswordReset:CooldownSeconds"] = "60",
                ["VnPay:TmnCode"] = "TESTTMN01",
                ["VnPay:HashSecret"] = VnPayHashSecret,
                ["VnPay:BaseUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                ["VnPay:ReturnUrl"] = "http://localhost/api/payment/vnpay-return",
                ["VnPay:IpnUrl"] = "http://localhost/api/payment/vnpay-ipn",
                ["VnPay:MobileDeepLink"] = "toantuduy://payment-result",
                ["Google:ClientId"] = "test-google-client-id.apps.googleusercontent.com"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.Configure<HostOptions>(options =>
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

            // Remove scheduled polling background services that could race with Respawn DB resets
            RemoveHostedService<MonthlyPayslipEmailScheduler>(services);
            RemoveHostedService<UpcomingLessonReminderService>(services);

            // Replace external dependencies with deterministic test fakes
            services.RemoveAll<IEmailDeliveryService>();
            services.AddSingleton<IEmailDeliveryService>(FakeEmail);

            services.RemoveAll<IImageStorage>();
            services.AddSingleton<IImageStorage>(FakeImages);

            services.RemoveAll<IChatAttachmentStorage>();
            services.AddSingleton<IChatAttachmentStorage>(FakeChatStorage);

            services.RemoveAll<ICourseMediaStorage>();
            services.AddSingleton<ICourseMediaStorage>(FakeCourseMedia);

            services.AddSingleton(UnreadCountState);
            services.RemoveAll<IUnreadCountStore>();
            services.AddScoped<IUnreadCountStore, InMemoryUnreadCountStore>();
        });
    }

    private static void RemoveHostedService<THostedService>(IServiceCollection services)
        where THostedService : class, IHostedService
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(THostedService))
            .ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    public void ResetFakes()
    {
        FakeEmail.Reset();
        FakeImages.Reset();
        FakeChatStorage.Reset();
        UnreadCountState.Reset();
    }
}

public sealed class DevWebAppFactory : TestWebApplicationFactory
{
    public DevWebAppFactory() : base("Development")
    {
    }
}

public sealed class DatabaseFixture : IAsyncLifetime
{
    private Respawner? _respawner;

    public TestWebApplicationFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        // Migrate the dedicated test DB BEFORE starting TestServer / BackgroundServices
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(TestWebApplicationFactory.TestConnectionString);
        await using (var db = new ApplicationDbContext(optionsBuilder.Options))
        {
            await db.Database.MigrateAsync();
        }

        _ = Factory.Server;

        await using var connection = new SqlConnection(TestWebApplicationFactory.TestConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public async Task ResetDatabaseAsync()
    {
        Factory.ResetFakes();
        if (_respawner is null) return;

        // Retry briefly if NotificationFanoutWorker writes to USER_INBOX mid-reset
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            await using var connection = new SqlConnection(TestWebApplicationFactory.TestConnectionString);
            await connection.OpenAsync();
            try
            {
                await _respawner.ResetAsync(connection);
                return;
            }
            catch (SqlException) when (attempt < maxAttempts)
            {
                await Task.Delay(150);
            }
        }
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class BackendApiTestCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "BackendApiTests";
}
