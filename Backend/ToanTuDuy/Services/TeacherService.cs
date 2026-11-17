using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Extensions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IEnumerable<TeacherResponseDto>> GetAllAsync(string? keyword = null)
        {
            return await BuildTeacherQuery(keyword, isActive: null).ToListAsync();
        }

        public async Task<PagedResult<TeacherResponseDto>> GetPagedAsync(
            int page = 1,
            int pageSize = PaginationDefaults.DefaultPageSize,
            string? keyword = null,
            bool? isActive = null)
        {
            return await BuildTeacherQuery(keyword, isActive)
                .ToPagedResultAsync(page, pageSize);
        }

        private IQueryable<TeacherResponseDto> BuildTeacherQuery(string? keyword, bool? isActive)
        {
            var query = _context.TeacherProfiles
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                var hasNumericId = int.TryParse(kw, out var parsedId);

                query = query.Where(t =>
                    (hasNumericId && t.Id == parsedId) ||
                    EF.Functions.Like(t.FullName, $"%{kw}%") ||
                    (t.Specialize != null && EF.Functions.Like(t.Specialize, $"%{kw}%")) ||
                    (t.Email != null && EF.Functions.Like(t.Email, $"%{kw}%")) ||
                    (t.Account != null && t.Account.PhoneNumber != null && EF.Functions.Like(t.Account.PhoneNumber, $"%{kw}%"))
                );
            }

            if (isActive.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                if (isActive.Value)
                {
                    query = query.Where(t =>
                        t.Account != null && t.Account.IsActive &&
                        (t.Account.LockoutEnd == null || t.Account.LockoutEnd <= now));
                }
                else
                {
                    query = query.Where(t =>
                        t.Account == null || !t.Account.IsActive ||
                        (t.Account.LockoutEnd != null && t.Account.LockoutEnd > now));
                }
            }

            return query
                .OrderByDescending(t => t.Id)
                .Select(t => new TeacherResponseDto
                {
                    Id = t.Id,
                    AccountId = t.AccountId,
                    FullName = t.FullName,
                    Email = t.Email,
                    Phone = t.Account != null ? (t.Account.PhoneNumber ?? "") : "",
                    Specialize = t.Specialize,
                    IsActive = t.Account != null && t.Account.IsActive &&
                        (t.Account.LockoutEnd == null || t.Account.LockoutEnd <= DateTimeOffset.UtcNow)
                });
        }

        public async Task<TeacherResponseDto?> GetByIdAsync(int id)
        {
            var teacher = await _context.TeacherProfiles
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null) return null;

            return new TeacherResponseDto
            {
                Id = teacher.Id,
                AccountId = teacher.AccountId,
                FullName = teacher.FullName,
                Email = teacher.Email,
                Phone = teacher.Account.PhoneNumber ?? "",
                Specialize = teacher.Specialize,
                IsActive = teacher.Account.IsActive &&
                    (teacher.Account.LockoutEnd == null || teacher.Account.LockoutEnd <= DateTimeOffset.UtcNow)
            };
        }

        public async Task<TeacherResponseDto> CreateAsync(CreateTeacherDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Phone,
                    PhoneNumber = dto.Phone,
                    Role = "Teacher",
                    IsActive = true
                };

                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Không thể tạo tài khoản: {errors}");
                }

                var teacher = new TeacherProfile
                {
                    AccountId = user.Id,
                    FullName = dto.FullName.Trim(),
                    Email = dto.Email
                };

                _context.TeacherProfiles.Add(teacher);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new TeacherResponseDto
                {
                    Id = teacher.Id,
                    AccountId = user.Id,
                    FullName = teacher.FullName,
                    Email = teacher.Email,
                    Phone = user.PhoneNumber ?? "",
                    Specialize = teacher.Specialize,
                    IsActive = true
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, UpdateTeacherDto dto)
        {
            var teacher = await _context.TeacherProfiles.FindAsync(id);
            if (teacher == null) return false;

            teacher.FullName = dto.FullName.Trim();

            // Chỉ cập nhật Email nếu client có truyền giá trị không rỗng
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                teacher.Email = dto.Email.Trim();
            }

            teacher.Specialize = string.IsNullOrWhiteSpace(dto.Specialize) ? null : dto.Specialize.Trim();

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, bool isActive)
        {
            var teacher = await _context.TeacherProfiles
                .Include(profile => profile.Account)
                .FirstOrDefaultAsync(profile => profile.Id == id);

            if (teacher == null) return false;
            if (teacher.Account == null)
            {
                throw new InvalidOperationException("Tài khoản giáo viên không tồn tại.");
            }

            teacher.Account.IsActive = isActive;
            teacher.Account.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
            teacher.Account.RefreshToken = null;

            var result = await _userManager.UpdateSecurityStampAsync(teacher.Account);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Không thể thu hồi phiên đăng nhập: {errors}");
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var teacher = await _context.TeacherProfiles
                    .Include(t => t.Account)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (teacher == null) return false;

                _context.TeacherProfiles.Remove(teacher);
                await _context.SaveChangesAsync();

                if (teacher.Account != null)
                {
                    await _userManager.DeleteAsync(teacher.Account);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}