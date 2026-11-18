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
            var accounts = await BuildAccountQuery(keyword, role: null, isActive: null, User.GetAccountId())
                .ToListAsync();

            return Ok(accounts);
        }

        [HttpGet("paged")]
        [ProducesResponseType(typeof(AdminAccountPagedResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminAccountPagedResponse>> GetAccountsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
            [FromQuery] string? keyword = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var paged = await BuildAccountQuery(keyword, role, isActive, User.GetAccountId())
                .ToPagedResultAsync(page, pageSize, cancellationToken: cancellationToken);

            var activeCount = await _context.Users
                .AsNoTracking()
                .CountAsync(account => account.IsActive, cancellationToken);
            var totalUsers = await _context.Users
                .AsNoTracking()
                .CountAsync(cancellationToken);

            return Ok(new AdminAccountPagedResponse
            {
                Items = paged.Items,
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                ActiveCount = activeCount,
                BlockedCount = totalUsers - activeCount
            });
        }

        private IQueryable<AdminAccountResponse> BuildAccountQuery(
            string? keyword,
            string? role,
            bool? isActive,
            int currentAccountId)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var search = keyword.Trim();
                query = query.Where(account =>
                    (account.UserName != null && EF.Functions.Like(account.UserName, $"%{search}%")) ||
                    (account.Email != null && EF.Functions.Like(account.Email, $"%{search}%")) ||
                    (account.PhoneNumber != null && EF.Functions.Like(account.PhoneNumber, $"%{search}%")) ||
                    (account.Role == "Parent" && account.ParentProfile != null &&
                        EF.Functions.Like(account.ParentProfile.FullName, $"%{search}%")) ||
                    (account.Role == "Teacher" && account.TeacherProfile != null &&
                        EF.Functions.Like(account.TeacherProfile.FullName, $"%{search}%")) ||
                    (account.Role == "Admin" && account.AdminProfile != null &&
                        EF.Functions.Like(account.AdminProfile.FullName, $"%{search}%")));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                var normalizedRole = role.Trim();
                query = query.Where(account => account.Role == normalizedRole);
            }

            if (isActive.HasValue)
            {
                query = query.Where(account => account.IsActive == isActive.Value);
            }

            return query
                .OrderByDescending(account => account.CreatedAt)
                .ThenByDescending(account => account.Id)
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
                    IsCurrentUser = account.Id == currentAccountId,
                    CreatedAt = account.CreatedAt
                });
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
