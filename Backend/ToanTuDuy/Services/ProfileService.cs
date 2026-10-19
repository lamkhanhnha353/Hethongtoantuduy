using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Profile;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Services.Storage;

namespace ToanTuDuy.Services
{
    public class ProfileService : IProfileService
    {
        private const string ParentRole = "Parent";
        private const string TeacherRole = "Teacher";
        private const string AvatarFolder = "avatars/parents";

        private readonly ApplicationDbContext _context;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<ProfileService> _logger;
        private readonly IParentStudentService _parentStudentService;

        public ProfileService(
            ApplicationDbContext context,
            IImageStorage imageStorage,
            ILogger<ProfileService> logger,
            IParentStudentService parentStudentService)
        {
            _context = context;
            _imageStorage = imageStorage;
            _logger = logger;
            _parentStudentService = parentStudentService;
        }

        public async Task<ProfileResponse> GetAsync(int accountId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");

            return user.Role switch
            {
                ParentRole => await BuildParentResponseAsync(user.Id),
                TeacherRole => await BuildTeacherResponseAsync(user.Id),
                _ => throw ApiException.BadRequest("Role không hợp lệ."),
            };
        }

        public async Task<ProfileResponse> UpdateAsync(int accountId, UpdateProfileRequest request)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");

            switch (user.Role)
            {
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
                    throw ApiException.BadRequest("Role không hợp lệ.");
            }
        }

        public async Task<ProfileResponse> UpdateEmailAsync(int accountId, string email)
        {
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

            var parent = await _context.ParentProfiles
                .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

            await using var stream = file!.OpenReadStream();
            var url = await _imageStorage.UploadAsync(stream, file.FileName, file.ContentType, AvatarFolder, cancellationToken);

            // Xóa ảnh cũ (nếu có) sau khi upload ảnh mới thành công.
            await SafeDeleteAsync(parent.Avatar, cancellationToken);

            parent.Avatar = url;
            await _context.SaveChangesAsync(cancellationToken);

            var (phone, email, emailConfirm) = await GetContactAsync(accountId);
            return MapParent(phone, email, emailConfirm, parent);
        }

        public async Task<ProfileResponse> RemoveAvatarAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var parent = await _context.ParentProfiles
                .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

            if (!string.IsNullOrWhiteSpace(parent.Avatar))
            {
                await SafeDeleteAsync(parent.Avatar, cancellationToken);
                parent.Avatar = null;
                await _context.SaveChangesAsync(cancellationToken);
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
            Avatar = null,
            HasPin = false,
        };
    }
}
