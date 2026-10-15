using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Extensions;
using ToanTuDuy.Services;

namespace ToanTuDuy.Filters
{
    /// <summary>
    /// Xác thực PIN phụ huynh lấy từ header <c>X-Pin</c> trước khi action chạy.
    /// Chạy SAU khi <c>[Authorize]</c> đã xác thực JWT, nên luôn có accountId.
    /// </summary>
    public class PinFilter : IAsyncActionFilter
    {
        public const string HeaderName = "X-Pin";

        private readonly IPinService _pinService;

        public PinFilter(IPinService pinService)
        {
            _pinService = pinService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var pin = context.HttpContext.Request.Headers[HeaderName].ToString();

            if (string.IsNullOrWhiteSpace(pin))
            {
                throw ApiException.BadRequest($"Thiếu mã PIN. Gửi kèm header '{HeaderName}'.");
            }

            var accountId = context.HttpContext.User.GetAccountId();
            var result = await _pinService.VerifyAsync(accountId, pin);

            if (!result.Valid)
            {
                throw ApiException.Forbidden("Mã PIN không đúng.");
            }

            await next();
        }
    }
}
