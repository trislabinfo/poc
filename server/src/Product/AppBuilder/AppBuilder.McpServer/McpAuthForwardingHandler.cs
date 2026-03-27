using Microsoft.AspNetCore.Http;

namespace AppBuilder.McpServer;

public sealed class McpAuthForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public McpAuthForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authorization) &&
                !string.IsNullOrWhiteSpace(authorization))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorization.ToString());
            }

            if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId) &&
                !string.IsNullOrWhiteSpace(tenantId))
            {
                request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.ToString());
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}

