using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class AuthService : IAuthService
    {
        public const string SecurityStampClaimType = "security_stamp";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailVerificationService _verificationService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailVerificationService verificationService)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _verificationService = verificationService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // 1. Validate logic cơ bản
            if (request.Role != "Parent" && request.Role != "Teacher")
                throw new ArgumentException("Role không hợp lệ. Chỉ chấp nhận 'Parent' hoặc 'Teacher'.");

            if (request.Role == "Teacher" && string.IsNullOrEmpty(request.Email))
                throw new ArgumentException("Email là thông tin bắt buộc đối với Teacher.");

            // 2. Setup user, dùng Phone làm UserName
            var user = new ApplicationUser
            {
                UserName = request.Phone, 
                PhoneNumber = request.Phone,
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // 3. Khởi tạo Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // CreateAsync sẽ tự động Hash Password
                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Lỗi tạo tài khoản: {errors}");
                }

                // Teacher đăng ký kèm email — gán vào Identity account để EmailConfirmed có ý nghĩa
                if (request.Role == "Teacher" && !string.IsNullOrEmpty(request.Email))
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, request.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        var errors = string.Join("; ", setEmailResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Lỗi gán email tài khoản: {errors}");
                    }
                }

                int profileId = 0;

                // 4. Tạo Profile tương ứng theo Role
                if (request.Role == "Parent")
                {
                    var parent = new ParentProfile
                    {
                        AccountId = user.Id,
                        FullName = request.FullName
                    };
                    _context.ParentProfiles.Add(parent);
                    await _context.SaveChangesAsync();
                    profileId = parent.Id;
                }
                else if (request.Role == "Teacher")
                {
                    var teacher = new TeacherProfile
                    {
                        AccountId = user.Id,
                        FullName = request.FullName,
                        Email = request.Email
                    };
                    _context.TeacherProfiles.Add(teacher);
                    await _context.SaveChangesAsync();
                    profileId = teacher.Id;
                }

                // 4.1. Cấp token xác minh email (trong transaction).
                //     Parent không có email lúc đăng ký nên chỉ Teacher nhận link ngay;
                //     Parent sẽ nhận link khi đặt email ở trang Cá nhân.
                string? verificationEmail = null;
                string? verificationRawToken = null;
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    verificationEmail = user.Email;
                    verificationRawToken = await _verificationService.IssueAsync(user.Id, verificationEmail);
                }

                // 5. Nếu mọi thứ thành công => Sinh Token
                var accessToken = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                await _userManager.UpdateAsync(user);

                // 6. Hoàn tất chuỗi hành động và Commit Db
                await transaction.CommitAsync();

                // 7. Gửi email xác minh sau khi commit — service tự nuốt lỗi gửi,
                //    không bao giờ rollback được việc đăng ký vì lỗi gửi mail.
                if (verificationRawToken != null && verificationEmail != null)
                {
                    await _verificationService.SendEmailAsync(verificationEmail, verificationRawToken);
                }

                return new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Role = user.Role,
                    ProfileId = profileId
                };
            }
            catch (Exception)
            {
                // Rollback toàn bộ dữ liệu
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Dùng request.Phone làm UserName để map với hệ thống Identity
            var user = await _userManager.FindByNameAsync(request.Phone);
            
            // Hash placeholder/corrupt trong dữ liệu seed có thể làm Identity ném FormatException.
            bool passwordIsValid;
            try
            {
                passwordIsValid = user != null
                    && await _userManager.CheckPasswordAsync(user, request.Password);
            }
            catch (FormatException)
            {
                throw new UnauthorizedAccessException("Số điện thoại hoặc mật khẩu không chính xác.");
            }

            if (!passwordIsValid)
            {
                throw new UnauthorizedAccessException("Số điện thoại hoặc mật khẩu không chính xác.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");
            }

            if (string.IsNullOrWhiteSpace(user.SecurityStamp))
            {
                var stampResult = await _userManager.UpdateSecurityStampAsync(user);
                if (!stampResult.Succeeded)
                {
                    var errors = string.Join("; ", stampResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Không thể khởi tạo phiên đăng nhập: {errors}");
                }
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Lưu refresh token vào DB
            user.RefreshToken = refreshToken;
            await _userManager.UpdateAsync(user);

            // Lấy ra Profile Id tương ứng
            int profileId = 0;
            if (user.Role == "Parent")
            {
                var profile = await _context.ParentProfiles.FirstOrDefaultAsync(p => p.AccountId == user.Id);
                profileId = profile?.Id ?? 0;
            }
            else if (user.Role == "Teacher")
            {
                var profile = await _context.TeacherProfiles.FirstOrDefaultAsync(p => p.AccountId == user.Id);
                profileId = profile?.Id ?? 0;
            }

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = user.Role,
                ProfileId = profileId
            };
        }

        public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginDto request)
        {
            var googleSettings = _configuration.GetSection("Google");
            var clientIds = new[] { googleSettings["ClientId"] }
                .Concat(googleSettings.GetSection("AdditionalClientIds").Get<string[]>() ?? Array.Empty<string>())
                .Where(clientId => !string.IsNullOrWhiteSpace(clientId))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (clientIds.Length == 0)
                throw new ApiException(
                    StatusCodes.Status503ServiceUnavailable,
                    "Đăng nhập Google hiện chưa được cấu hình.");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = clientIds });
            }
            catch (Exception exception) when (
                exception is InvalidJwtException or ArgumentException)
            {
                throw new UnauthorizedAccessException("Google ID token không hợp lệ.");
            }

            if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
                throw new UnauthorizedAccessException("Tài khoản Google chưa xác minh địa chỉ email.");

            var user = await _userManager.FindByEmailAsync(payload.Email);
            if (user is null)
                throw new UnauthorizedAccessException(
                    "Tài khoản chưa tồn tại trong hệ thống. Vui lòng liên hệ quản trị viên.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

            return await CreateGoogleAuthResponseAsync(user, payload.Email, payload.Name);
        }

        public async Task<ChangePasswordResponse> ChangePasswordAsync(int accountId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(accountId.ToString());
            if (user == null)
                throw ApiException.Unauthorized("Người dùng không tồn tại.");

            if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
                throw ApiException.Unauthorized("Mật khẩu hiện tại không chính xác.");

            if (request.NewPassword == request.CurrentPassword)
                throw ApiException.BadRequest("Mật khẩu mới phải khác mật khẩu hiện tại.");

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw ApiException.BadRequest($"Không thể đổi mật khẩu: {errors}");
            }

            // Thu hồi refresh token cũ: mọi phiên đang lưu refresh token sẽ không dùng được nữa.
            user.RefreshToken = null;
            await _userManager.UpdateAsync(user);

            return new ChangePasswordResponse
            {
                Message = "Đổi mật khẩu thành công."
            };
        }

        private async Task<AuthResponse> CreateGoogleAuthResponseAsync(
            ApplicationUser user,
            string email,
            string? googleName)
        {
            if (string.IsNullOrWhiteSpace(user.SecurityStamp))
            {
                EnsureIdentitySucceeded(
                    await _userManager.UpdateSecurityStampAsync(user),
                    "Không thể khởi tạo phiên đăng nhập Google.");
            }

            user.RefreshToken = GenerateRefreshToken();
            EnsureIdentitySucceeded(
                await _userManager.UpdateAsync(user),
                "Không thể tạo phiên đăng nhập Google.");

            var profileId = user.Role switch
            {
                "Parent" => await _context.ParentProfiles
                    .Where(profile => profile.AccountId == user.Id)
                    .Select(profile => profile.Id)
                    .FirstOrDefaultAsync(),
                "Teacher" => await _context.TeacherProfiles
                    .Where(profile => profile.AccountId == user.Id)
                    .Select(profile => profile.Id)
                    .FirstOrDefaultAsync(),
                "Admin" => await _context.AdminProfiles
                    .Where(profile => profile.AccountId == user.Id)
                    .Select(profile => profile.Id)
                    .FirstOrDefaultAsync(),
                _ => 0
            };

            return new AuthResponse
            {
                AccessToken = GenerateJwtToken(user),
                RefreshToken = user.RefreshToken,
                Role = user.Role,
                ProfileId = profileId,
                Email = email,
                FullName = string.IsNullOrWhiteSpace(googleName) ? email.Split('@')[0] : googleName
            };
        }

        private static void EnsureIdentitySucceeded(IdentityResult result, string message)
        {
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"{message} {errors}");
            }
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"] ?? "Chuoi-Bi-Mat-Rat-Dai-De-Ma-Hoa-HmacSha256-Key-Ne-32-Characters"; // Fallback
            var key = Encoding.UTF8.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? ""),
                new Claim(SecurityStampClaimType, user.SecurityStamp ?? "")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "120")),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
