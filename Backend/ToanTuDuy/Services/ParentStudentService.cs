using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class ParentStudentService : IParentStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParentStudentService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- NGHỆP VỤ PHỤ HUYNH ---
        public async Task<IEnumerable<ParentResponseDto>> GetAllParentsAsync(string? keyword = null)
        {
            var query = _context.ParentProfiles
                .Include(p => p.Account)
                .Include(p => p.StudentProfiles)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(p => p.FullName.ToLower().Contains(kw) ||
                                         (p.Account.PhoneNumber != null && p.Account.PhoneNumber.Contains(kw)));
            }

            return await query.Select(p => new ParentResponseDto
            {
                Id = p.Id,
                AccountId = p.AccountId,
                FullName = p.FullName,
                Phone = p.Account.PhoneNumber ?? "",
                // Pin được ánh xạ qua PinHash theo Entity mới
                Pin = p.Pin,
                Avatar = p.Avatar,
                IsActive = p.Account.IsActive,
                Students = p.StudentProfiles.Select(s => new StudentResponseDto
                {
                    Id = s.Id,
                    // ParentAccountId được đổi thành ParentId trong StudentResponseDto nếu bạn muốn khớp, 
                    // nhưng nếu DTO vẫn dùng ParentAccountId, ta map sang ParentId của Entity
                    ParentAccountId = s.ParentId,
                    ParentName = p.FullName,
                    ParentPhone = p.Account.PhoneNumber ?? "",
                    FullName = s.FullName,
                    Avatar = s.Avatar,
                    TotalPoints = s.TotalPoints,
                    Stars = s.Stars,
                    StreakDays = s.StreakDays
                }).ToList()
            }).ToListAsync();
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
            var query = _context.StudentProfiles
                .Include(s => s.Parent)
                .ThenInclude(p => p.Account)
                .AsQueryable();

            // Nếu DTO đang truyền vào ParentAccountId (của ApplicationUser)
            // Ta cần lấy ParentProfile có AccountId tương ứng
            if (parentAccountId.HasValue && parentAccountId.Value > 0)
            {
                query = query.Where(s => s.Parent != null && s.Parent.AccountId == parentAccountId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(kw) ||
                                         (s.Parent != null && s.Parent.FullName.ToLower().Contains(kw)));
            }

            return await query.Select(s => new StudentResponseDto
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
            }).ToListAsync();
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