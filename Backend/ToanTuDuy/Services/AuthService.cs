using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
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
                Role = request.Role 
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

                // 5. Nếu mọi thứ thành công => Sinh Token
                var accessToken = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                await _userManager.UpdateAsync(user);

                // 6. Hoàn tất chuỗi hành động và Commit Db
                await transaction.CommitAsync();

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
            
            // CheckPasswordAsync sẽ tự động lấy Password gửi lên, Hash và so sánh với HashDB
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                throw new UnauthorizedAccessException("Số điện thoại hoặc mật khẩu không chính xác.");
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
                new Claim(ClaimTypes.Role, user.Role ?? "")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"] ?? "120")),
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
