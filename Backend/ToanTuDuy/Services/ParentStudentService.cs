using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Extensions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class ParentStudentService : IParentStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailVerificationService _verificationService;

        public ParentStudentService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailVerificationService verificationService)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
        }

        // --- NGHỆP VỤ PHỤ HUYNH ---
        public async Task<IEnumerable<ParentResponseDto>> GetAllParentsAsync(string? keyword = null)
        {
            return await BuildParentQuery(keyword, isActive: null).ToListAsync();
        }

        public async Task<PagedResult<ParentResponseDto>> GetPagedParentsAsync(
            int page = 1,
            int pageSize = PaginationDefaults.DefaultPageSize,
            string? keyword = null,
            bool? isActive = null)
        {
            return await BuildParentQuery(keyword, isActive)
                .ToPagedResultAsync(page, pageSize);
        }

        private IQueryable<ParentResponseDto> BuildParentQuery(string? keyword, bool? isActive)
        {
            var query = _context.ParentProfiles
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                var hasNumericId = int.TryParse(kw, out var parsedId);
                query = query.Where(p =>
                    (hasNumericId && p.Id == parsedId) ||
                    EF.Functions.Like(p.FullName, $"%{kw}%") ||
                    (p.Account.PhoneNumber != null && EF.Functions.Like(p.Account.PhoneNumber, $"%{kw}%")) ||
                    (p.Account.Email != null && EF.Functions.Like(p.Account.Email, $"%{kw}%")) ||
                    p.StudentProfiles.Any(s => EF.Functions.Like(s.FullName, $"%{kw}%")));
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.Account.IsActive == isActive.Value);
            }

            return query
                .OrderByDescending(p => p.Id)
                .Select(p => new ParentResponseDto
                {
                    Id = p.Id,
                    AccountId = p.AccountId,
                    FullName = p.FullName,
                    Phone = p.Account.PhoneNumber ?? "",
                    Email = p.Account.Email,
                    Pin = p.Pin,
                    Avatar = p.Avatar,
                    IsActive = p.Account.IsActive,
                    Students = p.StudentProfiles.Select(s => new StudentResponseDto
                    {
                        Id = s.Id,
                        ParentAccountId = s.ParentId,
                        ParentName = p.FullName,
                        ParentPhone = p.Account.PhoneNumber ?? "",
                        FullName = s.FullName,
                        Avatar = s.Avatar,
                        TotalPoints = s.TotalPoints,
                        Stars = s.Stars,
                        StreakDays = s.StreakDays
                    }).ToList()
                });
        }

        public async Task<ParentResponseDto?> GetParentByIdAsync(int id)
        {
            var parent = await _context.ParentProfiles
                .Include(p => p.Account)
                .Include(p => p.StudentProfiles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parent == null) return null;

            return new ParentResponseDto
            {
                Id = parent.Id,
                AccountId = parent.AccountId,
                FullName = parent.FullName,
                Phone = parent.Account.PhoneNumber ?? "",
                Email = parent.Account.Email,
                Pin = parent.Pin,
                Avatar = parent.Avatar,
                IsActive = parent.Account.IsActive,
                Students = parent.StudentProfiles.Select(s => new StudentResponseDto
                {
                    Id = s.Id,
                    ParentAccountId = s.ParentId,
                    ParentName = parent.FullName,
                    ParentPhone = parent.Account.PhoneNumber ?? "",
                    FullName = s.FullName,
                    Avatar = s.Avatar,
                    TotalPoints = s.TotalPoints,
                    Stars = s.Stars,
                    StreakDays = s.StreakDays
                }).ToList()
            };
        }

        public async Task<ParentResponseDto> CreateParentAsync(CreateParentDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Phone,
                    PhoneNumber = dto.Phone,
                    Role = "Parent",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Không thể tạo tài khoản: {errors}");
                }

                var parent = new ParentProfile
                {
                    AccountId = user.Id,
                    FullName = dto.FullName.Trim(),
                    Pin = dto.Pin,
                    Avatar = dto.Avatar,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ParentProfiles.Add(parent);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ParentResponseDto
                {
                    Id = parent.Id,
                    AccountId = user.Id,
                    FullName = parent.FullName,
                    Phone = user.PhoneNumber ?? "",
                    Email = user.Email,
                    Pin = parent.Pin,
                    Avatar = parent.Avatar,
                    IsActive = true
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateParentAsync(int id, UpdateParentDto dto)
        {
            var parent = await _context.ParentProfiles.FindAsync(id);
            if (parent == null) return false;

            parent.FullName = dto.FullName.Trim();
            if (!string.IsNullOrEmpty(dto.Pin)) parent.Pin = dto.Pin;
            if (!string.IsNullOrEmpty(dto.Avatar)) parent.Avatar = dto.Avatar;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateParentStatusAsync(int id, bool isActive)
        {
            var parent = await _context.ParentProfiles
                .Include(profile => profile.Account)
                .FirstOrDefaultAsync(profile => profile.Id == id);

            if (parent == null) return false;
            if (parent.Account == null)
            {
                throw new InvalidOperationException("Tài khoản phụ huynh không tồn tại.");
            }

            parent.Account.IsActive = isActive;
            parent.Account.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
            parent.Account.RefreshToken = null;

            var result = await _userManager.UpdateSecurityStampAsync(parent.Account);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Không thể thu hồi phiên đăng nhập: {errors}");
            }

            return true;
        }

        public async Task<bool> UpdateParentEmailAsync(int id, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw ApiException.BadRequest("Email không được để trống.");
            }

            email = email.Trim();
            if (!new EmailAddressAttribute().IsValid(email))
            {
                throw ApiException.BadRequest("Email không hợp lệ.");
            }

            var parent = await _context.ParentProfiles
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parent == null || parent.Account == null) return false;

            if (parent.Account.EmailConfirmed)
            {
                throw ApiException.Forbidden("Email đã xác nhận, không thể thay đổi.");
            }

            var existingAccount = await _userManager.FindByEmailAsync(email);
            if (existingAccount != null && existingAccount.Id != parent.AccountId)
            {
                throw ApiException.Conflict("Email đã được sử dụng bởi tài khoản khác.");
            }

            var result = await _userManager.SetEmailAsync(parent.Account, email);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw ApiException.BadRequest($"Không thể cập nhật email: {errors}");
            }

            // Email mới chưa xác nhận → cấp token + gửi link xác minh ngay.
            // SendEmailAsync không ném lỗi (tự LogWarning + in link ra console).
            var rawToken = await _verificationService.IssueAsync(parent.AccountId, email);
            await _verificationService.SendEmailAsync(email, rawToken);
            // console.WriteLine($"[DEBUG] Link xác minh email mới: {rawToken}");/
            return true;
        }

        public async Task<bool> DeleteParentAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var parent = await _context.ParentProfiles
                    .Include(p => p.Account)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (parent == null) return false;

                // 1. Xóa tất cả Hồ sơ Học sinh phụ thuộc vào ParentId thay vì ParentAccountId
                var students = await _context.StudentProfiles
                    .Where(s => s.ParentId == parent.Id)
                    .ToListAsync();
                _context.StudentProfiles.RemoveRange(students);

                // 2. Xóa Hồ sơ Phụ huynh
                _context.ParentProfiles.Remove(parent);
                await _context.SaveChangesAsync();

                // 3. Xóa Tài khoản Identity của Phụ huynh
                if (parent.Account != null)
                {
                    await _userManager.DeleteAsync(parent.Account);
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

        // --- NGHIỆP VỤ HỌC SINH ---
        public async Task<IEnumerable<StudentResponseDto>> GetAllStudentsAsync(string? keyword = null, int? parentAccountId = null)
        {
            return await BuildStudentQuery(keyword, parentAccountId).ToListAsync();
        }

        public async Task<PagedResult<StudentResponseDto>> GetPagedStudentsAsync(
            int page = 1,
            int pageSize = PaginationDefaults.DefaultPageSize,
            string? keyword = null,
            int? parentAccountId = null)
        {
            return await BuildStudentQuery(keyword, parentAccountId)
                .ToPagedResultAsync(page, pageSize);
        }

        private IQueryable<StudentResponseDto> BuildStudentQuery(string? keyword, int? parentAccountId)
        {
            var query = _context.StudentProfiles
                .AsNoTracking()
                .AsQueryable();

            if (parentAccountId.HasValue && parentAccountId.Value > 0)
            {
                query = query.Where(s => s.Parent != null && s.Parent.AccountId == parentAccountId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                var hasNumericId = int.TryParse(kw, out var parsedId);
                query = query.Where(s =>
                    (hasNumericId && s.Id == parsedId) ||
                    EF.Functions.Like(s.FullName, $"%{kw}%") ||
                    (s.Parent != null && EF.Functions.Like(s.Parent.FullName, $"%{kw}%")) ||
                    (s.Parent != null && s.Parent.Account != null && s.Parent.Account.PhoneNumber != null &&
                        EF.Functions.Like(s.Parent.Account.PhoneNumber, $"%{kw}%")));
            }

            return query
                .OrderByDescending(s => s.Id)
                .Select(s => new StudentResponseDto
                {
                    Id = s.Id,
                    ParentAccountId = s.Parent != null ? s.Parent.AccountId : 0,
                    ParentName = s.Parent != null ? s.Parent.FullName : "",
                    ParentPhone = s.Parent != null && s.Parent.Account != null ? s.Parent.Account.PhoneNumber ?? "" : "",
                    FullName = s.FullName,
                    Avatar = s.Avatar,
                    TotalPoints = s.TotalPoints,
                    Stars = s.Stars,
                    StreakDays = s.StreakDays
                });
        }

        public async Task<StudentResponseDto?> GetStudentByIdAsync(int id)
        {
            var s = await _context.StudentProfiles
                .Include(s => s.Parent)
                .ThenInclude(p => p.Account)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (s == null) return null;

            return new StudentResponseDto
            {
                Id = s.Id,
                ParentAccountId = s.Parent != null ? s.Parent.AccountId : 0,
                ParentName = s.Parent != null ? s.Parent.FullName : "",
                ParentPhone = s.Parent != null && s.Parent.Account != null ? s.Parent.Account.PhoneNumber ?? "" : "",
                FullName = s.FullName,
                Avatar = s.Avatar,
                TotalPoints = s.TotalPoints,
                Stars = s.Stars,
                StreakDays = s.StreakDays
            };
        }

        public async Task<StudentResponseDto> CreateStudentAsync(CreateStudentDto dto)
        {
            // Trong hệ thống mới, CreateStudentDto truyền ParentAccountId
            // Nhưng bảng StudentProfile cần lưu ParentId (Khóa chính của ParentProfile)
            var parentProfile = await _context.ParentProfiles
                                              .Include(p => p.Account)
                                              .FirstOrDefaultAsync(p => p.AccountId == dto.ParentAccountId);

            if (parentProfile == null)
            {
                throw new ArgumentException($"Không tìm thấy Hồ sơ Phụ huynh có AccountId = {dto.ParentAccountId}");
            }

            var student = new StudentProfile
            {
                ParentId = parentProfile.Id, // Lưu id của ParentProfile
                FullName = dto.FullName.Trim(),
                Avatar = dto.Avatar,
                TotalPoints = 0,
                Stars = 0,
                StreakDays = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentProfiles.Add(student);
            await _context.SaveChangesAsync();

            return await GetStudentByIdAsync(student.Id) ?? new StudentResponseDto
            {
                Id = student.Id,
                ParentAccountId = parentProfile.AccountId,
                FullName = student.FullName,
                Avatar = student.Avatar
            };
        }

        public async Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto)
        {
            var student = await _context.StudentProfiles.FindAsync(id);
            if (student == null) return false;

            student.FullName = dto.FullName.Trim();
            if (!string.IsNullOrEmpty(dto.Avatar)) student.Avatar = dto.Avatar;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.StudentProfiles.FindAsync(id);
            if (student == null) return false;

            _context.StudentProfiles.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}