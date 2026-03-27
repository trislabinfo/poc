using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace BuildingBlocks.Web.Middleware;

/// <summary>
/// Global exception handler that returns standardized error responses.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // PostgreSQL 42P01: relation "schema.table" does not exist — usually means migrations not applied
        var (statusCode, message) = IsMissingRelationError(exception)
            ? (HttpStatusCode.ServiceUnavailable,
                "Database schema not initialized. Run the MigrationRunner for your topology (e.g. Monolith) with the same connection string so Tenant and other module tables exist.")
            : (HttpStatusCode.InternalServerError, "An error occurred while processing your request.");

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = message,
            Details = exception.Message,
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    /// <summary>Detects PostgreSQL "relation does not exist" (42P01) without depending on Npgsql.</summary>
    private static bool IsMissingRelationError(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            var msg = e.Message ?? "";
            if (msg.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                && (msg.Contains("relation", StringComparison.OrdinalIgnoreCase) || msg.Contains("42P01")))
                return true;
            if (e.GetType().FullName?.Contains("Npgsql.PostgresException", StringComparison.Ordinal) == true
                && msg.Contains("42P01", StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
