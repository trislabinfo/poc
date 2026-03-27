using BuildingBlocks.Kernel.Results;

namespace Tenant.Contracts.Services;

/// <summary>
/// Application service for resolving tenant by slug or id. Used by other modules (e.g. TenantApplication) to resolve tenant context.
/// Implementation varies by topology: in-process in Monolith, HTTP in Microservices.
/// </summary>
public interface ITenantResolverService
{
    /// <summary>
    /// Gets tenant info by slug, or null if not found.
    /// </summary>
    Task<Result<TenantInfoDto?>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant info by id, or null if not found.
    /// </summary>
    Task<Result<TenantInfoDto?>> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
