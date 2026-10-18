namespace ToanTuDuy.Exceptions
{
    /// <summary>
    /// Exception nghiệp vụ mang sẵn HTTP status code. Ném loại này ở tầng Service
    /// để <see cref="ToanTuDuy.Middleware.GlobalExceptionHandler"/> map thẳng ra response,
    /// tránh phải try/catch và tự set status ở từng action.
    /// </summary>
    public class ApiException : Exception
    {
        public int StatusCode { get; }
        public string? Details { get; }

        public ApiException(int statusCode, string message, string? details = null)
            : base(message)
        {
            StatusCode = statusCode;
            Details = details;
        }

        public static ApiException BadRequest(string message) => new(400, message);
        public static ApiException Unauthorized(string message) => new(401, message);
        public static ApiException Forbidden(string message) => new(403, message);
        public static ApiException NotFound(string message) => new(404, message);
        public static ApiException Conflict(string message) => new(409, message);
    }
}
