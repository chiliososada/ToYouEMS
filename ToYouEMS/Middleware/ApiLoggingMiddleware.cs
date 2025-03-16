using System.Diagnostics;

namespace ToYouEMS.Middleware
{
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiLoggingMiddleware> _logger;

        public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 开始计时
            var stopwatch = Stopwatch.StartNew();

            // 记录请求信息
            var request = context.Request;
            var requestPath = $"{request.Method} {request.Path}{request.QueryString}";
            var requestIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // 获取用户信息(如果已登录)
            string userId = context.User.FindFirst("sub")?.Value;
            string userType = context.User.FindFirst("userType")?.Value;
            string userInfo = string.IsNullOrEmpty(userId) ? "未登录用户" : $"用户ID: {userId}, 类型: {userType}";

            // 记录日志
            _logger.LogInformation($"收到请求: {requestPath} - 来自IP: {requestIp} - {userInfo}");
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 收到请求: {requestPath} - 来自IP: {requestIp} - {userInfo}");

            try
            {
                // 调用下一个中间件
                await _next(context);

                // 记录响应信息
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsed = stopwatch.ElapsedMilliseconds;

                string statusMessage = statusCode < 400 ? "成功" : "失败";

                _logger.LogInformation($"请求{statusMessage}: {statusCode} - {requestPath} - 耗时: {elapsed}ms");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 请求{statusMessage}: {statusCode} - {requestPath} - 耗时: {elapsed}ms");
            }
            catch (Exception ex)
            {
                // 记录异常
                stopwatch.Stop();
                var elapsed = stopwatch.ElapsedMilliseconds;

                _logger.LogError(ex, $"请求异常: {requestPath} - 耗时: {elapsed}ms - 错误: {ex.Message}");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 请求异常: {requestPath} - 耗时: {elapsed}ms - 错误: {ex.Message}");

                // 重新抛出异常，让异常处理中间件处理
                throw;
            }
        }
    }

    // 扩展方法，方便在Program.cs中使用
    public static class ApiLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiLoggingMiddleware>();
        }
    }
}
