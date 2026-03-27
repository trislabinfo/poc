using TenantApplication.Domain.Entities;
using TenantApplication.Domain.Enums;

namespace TenantApplication.Domain.Repositories;

/// <summary>Repository for TenantApplicationEnvironment (deployment environments).</summary>
public interface ITenantApplicationEnvironmentRepository
{
    Task<TenantApplicationEnvironment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantApplicationEnvironment?> GetByTenantAppAndEnvironmentAsync(Guid tenantApplicationId, EnvironmentType environmentType, CancellationToken cancellationToken = default);
    Task<List<TenantApplicationEnvironment>> GetByTenantApplicationAsync(Guid tenantApplicationId, CancellationToken cancellationToken = default);
    Task AddAsync(TenantApplicationEnvironment env, CancellationToken cancellationToken = default);
    void Update(TenantApplicationEnvironment env);
}
