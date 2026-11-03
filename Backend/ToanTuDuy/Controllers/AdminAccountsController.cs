using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Extensions;
using ToanTuDuy.Models;
using ToanTuDuy.Services;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/admin/accounts")]
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordResetService _passwordResetService;

        public AdminAccountsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPasswordResetService passwordResetService)
        {
            _context = context;
            _userManager = userManager;
            _passwordResetService = passwordResetService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminAccountResponse>>> GetAccounts(
            [FromQuery] string? keyword)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var search = keyword.Trim().ToLower();
                query = query.Where(account =>
                    (account.UserName != null && account.UserName.ToLower().Contains(search)) ||
                    (account.Email != null && account.Email.ToLower().Contains(search)) ||
                    (account.PhoneNumber != null && account.PhoneNumber.Contains(search)) ||
                    (account.Role == "Parent" && account.ParentProfile != null &&
                        account.ParentProfile.FullName.ToLower().Contains(search)) ||
                    (account.Role == "Teacher" && account.TeacherProfile != null &&
                        account.TeacherProfile.FullName.ToLower().Contains(search)) ||
                    (account.Role == "Admin" && account.AdminProfile != null &&
                        account.AdminProfile.FullName.ToLower().Contains(search)));
            }

            var accounts = await query
                .OrderByDescending(account => account.CreatedAt)
                .Select(account => new AdminAccountResponse
                {
                    Id = account.Id,
                    FullName = account.Role == "Parent"
                        ? account.ParentProfile != null ? account.ParentProfile.FullName : account.UserName ?? string.Empty
                        : account.Role == "Teacher"
                            ? account.TeacherProfile != null ? account.TeacherProfile.FullName : account.UserName ?? string.Empty
                            : account.Role == "Admin"
                                ? account.AdminProfile != null ? account.AdminProfile.FullName : account.UserName ?? string.Empty
                                : account.UserName ?? string.Empty,
                    Email = account.Email,
                    EmailConfirmed = account.EmailConfirmed,
                    Phone = account.PhoneNumber,
                    Role = account.Role,
                    IsActive = account.IsActive,
                    CreatedAt = account.CreatedAt
                })
                .ToListAsync();

            var currentAccountId = User.GetAccountId();
            foreach (var account in accounts)
            {
                account.IsCurrentUser = account.Id == currentAccountId;
            }

            return Ok(accounts);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateAccountStatusRequest request)
        {
            var account = await _userManager.FindByIdAsync(id.ToString());
            if (account == null)
            {
                throw ApiException.NotFound("Không tìm thấy tài khoản.");
            }

            var isActive = request.IsActive!.Value;
            if (id == User.GetAccountId() && !isActive)
            {
                throw ApiException.Conflict("Bạn không thể tự khóa tài khoản Admin đang đăng nhập.");
            }

            account.IsActive = isActive;
            account.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
            account.RefreshToken = null;

            var result = await _userManager.UpdateSecurityStampAsync(account);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw ApiException.BadRequest($"Không thể cập nhật trạng thái tài khoản: {errors}");
            }

            return Ok(new
            {
                message = isActive
                    ? "Đã mở khóa tài khoản."
                    : "Đã khóa tài khoản và thu hồi các phiên đăng nhập.",
                isActive
            });
        }

        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var response = await _passwordResetService.ResetPasswordForAdminAsync(id);
            return Ok(response);
        }
    }
}
