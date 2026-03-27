namespace TenantApplication.Domain.Repositories;

/// <summary>Repository for TenantApplication aggregate (tenant-scoped applications).</summary>
public interface ITenantApplicationRepository
{
    Task<Entities.TenantApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.TenantApplication?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default);
    Task<List<Entities.TenantApplication>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Entities.TenantApplication?> GetByTenantAndApplicationAsync(Guid tenantId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsForTenantAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.TenantApplication app, CancellationToken cancellationToken = default);
    void Update(Entities.TenantApplication app);
    void Remove(Entities.TenantApplication app);
}
