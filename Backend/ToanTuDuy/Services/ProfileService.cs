using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Profile;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;
using ToanTuDuy.Services.Storage;

namespace ToanTuDuy.Services
{
    public class ProfileService : IProfileService
    {
        private const string ParentRole = "Parent";
        private const string TeacherRole = "Teacher";
        private const string AdminRole = "Admin";
        private const string AvatarFolder = "avatars/parents";
        private const string AdminAvatarFolder = "avatars/admins";
        private const string TeacherAvatarFolder = "avatars/teachers";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<ProfileService> _logger;
        private readonly IParentStudentService _parentStudentService;
        private readonly IEmailVerificationService _verificationService;

        public ProfileService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IImageStorage imageStorage,
            ILogger<ProfileService> logger,
            IParentStudentService parentStudentService,
            IEmailVerificationService verificationService)
        {
            _context = context;
            _userManager = userManager;
            _imageStorage = imageStorage;
            _logger = logger;
            _parentStudentService = parentStudentService;
            _verificationService = verificationService;
        }

        public async Task<ProfileResponse> GetAsync(int accountId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");

            return NormalizeRole(user.Role) switch
            {
                ParentRole => await BuildParentResponseAsync(user.Id),
                TeacherRole => await BuildTeacherResponseAsync(user.Id),
                AdminRole => await BuildAdminResponseAsync(user.Id),
                _ => throw UnsupportedRole(user.Role),
            };
        }

        public async Task<ProfileResponse> UpdateAsync(int accountId, UpdateProfileRequest request)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");

            switch (NormalizeRole(user.Role))
            {
                case AdminRole:
                {
                    var admin = await _userManager.FindByIdAsync(accountId.ToString())
                        ?? throw ApiException.NotFound("Không tìm thấy tài khoản Admin.");
                    var fullName = request.FullName.Trim();
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        throw ApiException.BadRequest("Họ và tên là bắt buộc.");
                    }

                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    if (!string.IsNullOrWhiteSpace(request.Phone)
                        && !string.Equals(admin.PhoneNumber, request.Phone.Trim(), System.StringComparison.Ordinal))
                    {
                        var phone = request.Phone.Trim();
                        var existingUser = await _userManager.FindByNameAsync(phone);
                        if (existingUser != null && existingUser.Id != accountId)
                        {
                            throw ApiException.Conflict("Số điện thoại đã được sử dụng.");
                        }

                        var usernameResult = await _userManager.SetUserNameAsync(admin, phone);
                        if (!usernameResult.Succeeded)
                        {
                            throw ApiException.BadRequest(
                                $"Không thể cập nhật số điện thoại: {string.Join("; ", usernameResult.Errors.Select(error => error.Description))}");
                        }

                        var phoneResult = await _userManager.SetPhoneNumberAsync(admin, phone);
                        if (!phoneResult.Succeeded)
                        {
                            throw ApiException.BadRequest(
                                $"Không thể cập nhật số điện thoại: {string.Join("; ", phoneResult.Errors.Select(error => error.Description))}");
                        }

                        admin.RefreshToken = null;
                    }

                    var updateResult = await _userManager.UpdateAsync(admin);
                    if (!updateResult.Succeeded)
                    {
                        throw ApiException.BadRequest(
                            $"Không thể cập nhật hồ sơ: {string.Join("; ", updateResult.Errors.Select(error => error.Description))}");
                    }

                    var adminProfile = await _context.AdminProfiles
                        .FirstOrDefaultAsync(profile => profile.AccountId == accountId)
                        ?? throw ApiException.NotFound("Không tìm thấy hồ sơ Admin.");
                    adminProfile.FullName = fullName;
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return MapAdmin(admin, adminProfile);
                }

                case ParentRole:
                    var parent = await _context.ParentProfiles
                        .FirstOrDefaultAsync(p => p.AccountId == accountId)
                        ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

                    parent.FullName = request.FullName;
                    await _context.SaveChangesAsync();

                    return MapParent(user.PhoneNumber ?? string.Empty, user.Email, user.EmailConfirmed, parent);

                case TeacherRole:
                    var teacher = await _context.TeacherProfiles
                        .FirstOrDefaultAsync(t => t.AccountId == accountId)
                        ?? throw ApiException.NotFound("Không tìm thấy hồ sơ giáo viên.");

                    teacher.FullName = request.FullName;
                    await _context.SaveChangesAsync();

                    return MapTeacher(user.PhoneNumber ?? string.Empty, teacher);

                default:
                    throw UnsupportedRole(user.Role);
            }
        }

        public async Task<ProfileResponse> UpdateEmailAsync(int accountId, string email)
        {
            var user = await _userManager.FindByIdAsync(accountId.ToString())
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");
            email = email.Trim();

            if (NormalizeRole(user.Role) == AdminRole)
            {
                if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
                {
                    throw ApiException.BadRequest("Email không hợp lệ.");
                }

                if (string.Equals(user.Email, email, System.StringComparison.OrdinalIgnoreCase)
                    && user.EmailConfirmed)
                {
                    return await BuildAdminResponseAsync(accountId);
                }

                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null && existingUser.Id != accountId)
                {
                    throw ApiException.Conflict("Email đã được sử dụng bởi tài khoản khác.");
                }

                var emailIsPendingForAnotherAccount = await _context.Users
                    .AnyAsync(candidate => candidate.Id != accountId && candidate.PendingEmail == email);
                if (emailIsPendingForAnotherAccount)
                {
                    throw ApiException.Conflict("Email đang chờ xác minh cho một tài khoản khác.");
                }

                if (string.Equals(user.PendingEmail, email, System.StringComparison.OrdinalIgnoreCase))
                {
                    throw ApiException.Conflict("Email này đang chờ xác minh. Hãy kiểm tra hộp thư.");
                }

                user.PendingEmail = email;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw ApiException.BadRequest(
                        $"Không thể cập nhật email: {string.Join("; ", updateResult.Errors.Select(error => error.Description))}");
                }

                var rawToken = await _verificationService.IssueAsync(accountId, email);
                await _verificationService.SendEmailAsync(email, rawToken);
                return await BuildAdminResponseAsync(accountId);
            }

            var parentId = await _context.ParentProfiles
                .AsNoTracking()
                .Where(p => p.AccountId == accountId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (parentId == 0)
            {
                throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");
            }

            var success = await _parentStudentService.UpdateParentEmailAsync(parentId, email);
            if (!success)
            {
                throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");
            }

            return await BuildParentResponseAsync(accountId);
        }

        public async Task<ProfileResponse> UpdateAvatarAsync(int accountId, IFormFile? file, CancellationToken cancellationToken = default)
        {
            ImageUploadRules.EnsureValid(file);

            var user = await _userManager.FindByIdAsync(accountId.ToString())
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");

            if (NormalizeRole(user.Role) == AdminRole)
            {
                var adminProfile = await _context.AdminProfiles
                    .FirstOrDefaultAsync(profile => profile.AccountId == accountId)
                    ?? throw ApiException.NotFound("Không tìm thấy hồ sơ Admin.");
                await using var adminStream = file!.OpenReadStream();
                var adminUrl = await _imageStorage.UploadAsync(
                    adminStream, file.FileName, file.ContentType, AdminAvatarFolder, cancellationToken);
                var previousAvatar = adminProfile.Avatar;
                adminProfile.Avatar = adminUrl;
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    await SafeDeleteAsync(adminUrl, CancellationToken.None);
                    throw;
                }

                await SafeDeleteAsync(previousAvatar, cancellationToken);
                return MapAdmin(user, adminProfile);
            }

            if (NormalizeRole(user.Role) == TeacherRole)
            {
                var teacherProfile = await _context.TeacherProfiles
                    .FirstOrDefaultAsync(profile => profile.AccountId == accountId, cancellationToken)
                    ?? throw ApiException.NotFound("Không tìm thấy hồ sơ giáo viên.");
                await using var teacherStream = file!.OpenReadStream();
                var teacherUrl = await _imageStorage.UploadAsync(
                    teacherStream, file.FileName, file.ContentType, TeacherAvatarFolder, cancellationToken);
                var previousAvatar = teacherProfile.Avatar;
                teacherProfile.Avatar = teacherUrl;
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    await SafeDeleteAsync(teacherUrl, CancellationToken.None);
                    throw;
                }

                await SafeDeleteAsync(previousAvatar, cancellationToken);
                var (teacherPhone, _, _) = await GetContactAsync(accountId);
                return MapTeacher(teacherPhone, teacherProfile);
            }

            var parent = await _context.ParentProfiles
                .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

            await using var stream = file!.OpenReadStream();
            var url = await _imageStorage.UploadAsync(stream, file.FileName, file.ContentType, AvatarFolder, cancellationToken);
            var previousParentAvatar = parent.Avatar;
            parent.Avatar = url;
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await SafeDeleteAsync(url, CancellationToken.None);
                throw;
            }

            await SafeDeleteAsync(previousParentAvatar, cancellationToken);
            var (phone, email, emailConfirm) = await GetContactAsync(accountId);
            return MapParent(phone, email, emailConfirm, parent);
        }

        public async Task<ProfileResponse> RemoveAvatarAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(accountId.ToString())
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");

            if (NormalizeRole(user.Role) == AdminRole)
            {
                var adminProfile = await _context.AdminProfiles
                    .FirstOrDefaultAsync(profile => profile.AccountId == accountId)
                    ?? throw ApiException.NotFound("Không tìm thấy hồ sơ Admin.");

                var previousAvatar = adminProfile.Avatar;
                if (!string.IsNullOrWhiteSpace(previousAvatar))
                {
                    adminProfile.Avatar = null;
                    await _context.SaveChangesAsync(cancellationToken);
                    await SafeDeleteAsync(previousAvatar, cancellationToken);
                }

                return MapAdmin(user, adminProfile);
            }

            if (NormalizeRole(user.Role) == TeacherRole)
            {
                var teacherProfile = await _context.TeacherProfiles
                    .FirstOrDefaultAsync(profile => profile.AccountId == accountId, cancellationToken)
                    ?? throw ApiException.NotFound("Không tìm thấy hồ sơ giáo viên.");

                var previousAvatar = teacherProfile.Avatar;
                if (!string.IsNullOrWhiteSpace(previousAvatar))
                {
                    teacherProfile.Avatar = null;
                    await _context.SaveChangesAsync(cancellationToken);
                    await SafeDeleteAsync(previousAvatar, cancellationToken);
                }

                var (teacherPhone, _, _) = await GetContactAsync(accountId);
                return MapTeacher(teacherPhone, teacherProfile);
            }

            var parent = await _context.ParentProfiles
                .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

            var previousParentAvatar = parent.Avatar;
            if (!string.IsNullOrWhiteSpace(previousParentAvatar))
            {
                parent.Avatar = null;
                await _context.SaveChangesAsync(cancellationToken);
                await SafeDeleteAsync(previousParentAvatar, cancellationToken);
            }

            var (phone, email, emailConfirm) = await GetContactAsync(accountId);
            return MapParent(phone, email, emailConfirm, parent);
        }

        private async Task SafeDeleteAsync(string? url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                await _imageStorage.DeleteAsync(url, cancellationToken);
            }
            catch (Exception ex)
            {
                // Không chặn thao tác chính nếu xóa ảnh cũ trên cloud thất bại.
                _logger.LogWarning(ex, "Xóa avatar cũ thất bại: {Url}", url);
            }
        }

        private async Task<ProfileResponse> BuildParentResponseAsync(int accountId)
        {
            var profile = await _context.ParentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.AccountId == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

            var (phone, email, emailConfirm) = await GetContactAsync(accountId);
            return MapParent(phone, email, emailConfirm, profile);
        }

        private async Task<ProfileResponse> BuildTeacherResponseAsync(int accountId)
        {
            var profile = await _context.TeacherProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.AccountId == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ giáo viên.");

            var (phone, _, _) = await GetContactAsync(accountId);
            return MapTeacher(phone, profile);
        }

        private async Task<ProfileResponse> BuildAdminResponseAsync(int accountId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(account => account.Id == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");
            var profile = await _context.AdminProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(admin => admin.AccountId == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ Admin.");

            return MapAdmin(user, profile);
        }

        private static ProfileResponse MapAdmin(ApplicationUser user, AdminProfile profile) => new()
        {
            AccountId = user.Id,
            Phone = user.PhoneNumber ?? string.Empty,
            Role = AdminRole,
            FullName = profile.FullName,
            Email = user.Email,
            PendingEmail = user.PendingEmail,
            EmailConfirm = user.EmailConfirmed,
            Avatar = profile.Avatar,
            HasPin = false,
        };

        private static string NormalizeRole(string? role) => role?.Trim().ToUpperInvariant() switch
        {
            "ADMIN" => AdminRole,
            "TEACHER" => TeacherRole,
            "PARENT" => ParentRole,
            _ => string.Empty,
        };

        private static ApiException UnsupportedRole(string? role) =>
            ApiException.BadRequest(
                $"Role tài khoản không được hỗ trợ: '{role ?? "(trống)"}'.");

        private async Task<(string Phone, string? Email, bool EmailConfirmed)> GetContactAsync(int accountId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == accountId)
                .Select(u => new { u.PhoneNumber, u.Email, u.EmailConfirmed })
                .FirstOrDefaultAsync();

            return (user?.PhoneNumber ?? string.Empty, user?.Email, user?.EmailConfirmed ?? false);
        }

        private static ProfileResponse MapParent(
            string phone,
            string? email,
            bool emailConfirm,
            Models.ParentProfile profile) => new()
        {
            AccountId = profile.AccountId,
            Phone = phone,
            Role = ParentRole,
            FullName = profile.FullName,
            Email = email,
            EmailConfirm = emailConfirm,
            Avatar = profile.Avatar,
            HasPin = !string.IsNullOrEmpty(profile.Pin),
        };

        private static ProfileResponse MapTeacher(string phone, Models.TeacherProfile profile) => new()
        {
            AccountId = profile.AccountId,
            Phone = phone,
            Role = TeacherRole,
            FullName = profile.FullName,
            Email = profile.Email,
            Avatar = profile.Avatar,
            HasPin = false,
        };
    }
}
