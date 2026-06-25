using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SmartCoffeeBuilder.API.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // traceId giúp đối chiếu log với response trả về cho client.
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception [{TraceId}] on {Method} {Path}{QueryString} → {ExceptionType}: {Message}\n{InnerChain}",
            traceId,
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.QueryString,
            exception.GetType().FullName,
            exception.Message,
            BuildInnerChain(exception));

        var (statusCode, title) = exception switch
        {
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        httpContext.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };
        problem.Extensions["traceId"] = traceId;

        // Chỉ lộ chi tiết nội bộ (stack trace, inner exception) khi ở môi trường dev.
        if (_env.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().FullName;
            problem.Extensions["innerException"] = exception.InnerException?.Message;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    // Gộp toàn bộ chuỗi InnerException thành một chuỗi dễ đọc cho log.
    private static string BuildInnerChain(Exception exception)
    {
        var sb = new StringBuilder();
        var inner = exception.InnerException;
        var depth = 1;
        while (inner is not null)
        {
            sb.AppendLine($"  Inner[{depth}] {inner.GetType().FullName}: {inner.Message}");
            inner = inner.InnerException;
            depth++;
        }
        return sb.Length == 0 ? "  (no inner exception)" : sb.ToString().TrimEnd();
    }
}
