using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Services;

/// <summary>
/// Resolves application by URL (tenant slug, app slug, environment). Used by Runtime BFF.
/// </summary>
public interface IApplicationResolverService
{
    Task<Result<ResolvedApplicationDto>> ResolveByUrlAsync(
        string tenantSlug,
        string appSlug,
        string environment,
        CancellationToken cancellationToken = default);
}
