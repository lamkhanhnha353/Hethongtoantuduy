using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Profile;
using ToanTuDuy.Exceptions;

namespace ToanTuDuy.Services
{
    public class ProfileService : IProfileService
    {
        private const string ParentRole = "Parent";
        private const string TeacherRole = "Teacher";

        private readonly ApplicationDbContext _context;

        public ProfileService(ApplicationDbContext context)
        {
            _context = context;
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
                    parent.Avatar = request.Avatar;
                    await _context.SaveChangesAsync();

                    return MapParent(user.PhoneNumber ?? string.Empty, parent);

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

        private async Task<ProfileResponse> BuildParentResponseAsync(int accountId)
        {
            var profile = await _context.ParentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.AccountId == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

            var phone = await GetPhoneAsync(accountId);
            return MapParent(phone, profile);
        }

        private async Task<ProfileResponse> BuildTeacherResponseAsync(int accountId)
        {
            var profile = await _context.TeacherProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.AccountId == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ giáo viên.");

            var phone = await GetPhoneAsync(accountId);
            return MapTeacher(phone, profile);
        }

        private async Task<string> GetPhoneAsync(int accountId) =>
            await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == accountId)
                .Select(u => u.PhoneNumber ?? string.Empty)
                .FirstOrDefaultAsync() ?? string.Empty;

        private static ProfileResponse MapParent(string phone, Models.ParentProfile profile) => new()
        {
            AccountId = profile.AccountId,
            Phone = phone,
            Role = ParentRole,
            FullName = profile.FullName,
            Email = null,
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
