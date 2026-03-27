using TenantApplication.Domain.Entities;

namespace TenantApplication.Domain.Repositories;

/// <summary>Repository for TenantApplicationMigration (schema migrations between releases).</summary>
public interface ITenantApplicationMigrationRepository
{
    Task<TenantApplicationMigration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TenantApplicationMigration>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<TenantApplicationMigration?> GetPendingMigrationAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task AddAsync(TenantApplicationMigration migration, CancellationToken cancellationToken = default);
    void Update(TenantApplicationMigration migration);
}
