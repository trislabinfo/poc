using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BuildingBlocks.Web.Middleware;

/// <summary>
/// Logs HTTP request/response for observability.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientApp = context.Request.Headers["X-Client-App"].FirstOrDefault() ?? "(none)";

        _logger.LogInformation(
            "HTTP {Method} {Path} [Client: {ClientApp}] started",
            context.Request.Method,
            context.Request.Path,
            clientApp);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "HTTP {Method} {Path} [Client: {ClientApp}] completed with {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                clientApp,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
