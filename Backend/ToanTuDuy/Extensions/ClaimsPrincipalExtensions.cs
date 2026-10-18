using System.Security.Claims;
using ToanTuDuy.Exceptions;

namespace ToanTuDuy.Extensions
{
    /// <summary>
    /// Đọc thông tin định danh từ token của request hiện tại, tránh parse claim lặp lại
    /// ở mọi controller/service.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        public static int GetAccountId(this ClaimsPrincipal user)
        {
            var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(raw, out var accountId))
            {
                throw ApiException.Unauthorized("Token không hợp lệ hoặc đã hết hạn.");
            }

            return accountId;
        }

        public static string GetRole(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
