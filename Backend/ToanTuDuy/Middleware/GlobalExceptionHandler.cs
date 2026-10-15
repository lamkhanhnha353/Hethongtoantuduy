using Microsoft.AspNetCore.Diagnostics;
using ToanTuDuy.Exceptions;

namespace ToanTuDuy.Middleware
{
    /// <summary>
    /// Bắt mọi exception chưa được xử lý và trả về một envelope lỗi thống nhất:
    /// <c>{ code, message, details? }</c> — đúng định dạng mà client đang đọc.
    /// Nhờ vậy controller không cần try/catch.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, message, details) = Map(exception);

            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Lỗi hệ thống khi xử lý {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
            }
            else
            {
                _logger.LogWarning("Lỗi nghiệp vụ {StatusCode}: {Message}",
                    statusCode, message);
            }

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(
                new { code = statusCode, message, details },
                cancellationToken);

            return true;
        }

        private static (int StatusCode, string Message, string? Details) Map(Exception exception) =>
            exception switch
            {
                ApiException api => (api.StatusCode, api.Message, api.Details),

                // Exception chuẩn .NET: giữ tương thích cho các service hiện có.
                UnauthorizedAccessException => (401, exception.Message, null),
                ArgumentException => (400, exception.Message, null),
                InvalidOperationException => (400, exception.Message, null),
                KeyNotFoundException => (404, exception.Message, null),

                _ => (500, "Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.", exception.Message),
            };
    }
}
