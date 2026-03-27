using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Web.Middleware;

/// <summary>
/// Resolves tenant from request (header, subdomain, or claim) and stores in HttpContext.Items.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = ResolveTenantId(context);

        if (tenantId.HasValue)
        {
            context.Items["TenantId"] = tenantId.Value;
            _logger.LogDebug("Resolved TenantId: {TenantId}", tenantId.Value);
        }

        await _next(context);
    }

    private static Guid? ResolveTenantId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) &&
            Guid.TryParse(headerValue, out var tenantId))
        {
            return tenantId;
        }

        var tenantClaim = context.User.FindFirst("tenant_id");
        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var claimTenantId))
        {
            return claimTenantId;
        }

        return null;
    }
}
