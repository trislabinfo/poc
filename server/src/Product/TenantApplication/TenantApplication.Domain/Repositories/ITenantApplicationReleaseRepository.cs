using AppDefinition.Domain.Entities.Lifecycle;

namespace TenantApplication.Domain.Repositories;

/// <summary>Repository for tenant application releases (tenantapplication.tenant_application_releases).</summary>
public interface ITenantApplicationReleaseRepository
{
    Task<ApplicationRelease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ApplicationRelease>> GetByTenantApplicationIdAsync(Guid tenantApplicationId, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationRelease release, CancellationToken cancellationToken = default);
    void Update(ApplicationRelease release);
}
