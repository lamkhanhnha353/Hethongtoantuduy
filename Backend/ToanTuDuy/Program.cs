using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resend;
using System.Security.Claims;
using System.Text;
using ToanTuDuy.Data;
using ToanTuDuy.Middleware;
using ToanTuDuy.Models;
using ToanTuDuy.Options;
using ToanTuDuy.Services;
using ToanTuDuy.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Global exception handling: map mọi exception về envelope { code, message, details? }
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Setup DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=ToanTuDuyDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

// Setup Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Setup JWT Bearer authentication: validate token do AuthService sinh ra
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = Encoding.UTF8.GetBytes(jwtSettings["Key"]
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key."));

builder.Services.AddAuthentication(options =>
    {
        // Identity đặt mặc định là cookie scheme; ghi đè để API dùng JWT Bearer.
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.Zero,
        };
    });

// Setup CORS for browser clients (Expo web, web frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Register Custom Services (Dependency Injection)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IPinService, PinService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICourseLevelService, CourseLevelService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IClassScheduleService, ClassScheduleService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IParentStudentService, ParentStudentService>();

// Gửi email xác minh tài khoản qua Resend
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["Resend:ApiToken"] ?? string.Empty;
});
builder.Services.Configure<EmailVerificationSettings>(
    builder.Configuration.GetSection(EmailVerificationSettings.SectionName));
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();

// Khôi phục mật khẩu khi quên: sinh mật khẩu tạm và gửi qua email đã xác minh
builder.Services.Configure<PasswordResetSettings>(
    builder.Configuration.GetSection(PasswordResetSettings.SectionName));
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// Hasher riêng cho PIN của phụ huynh (không lưu plaintext)
builder.Services.AddScoped<IPasswordHasher<ParentProfile>, PasswordHasher<ParentProfile>>();

// Lưu ảnh trên Cloudinary (DB chỉ giữ URL trả về)
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection(CloudinarySettings.SectionName));
builder.Services.AddSingleton<IImageStorage, CloudinaryImageStorage>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors("DevCors");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
