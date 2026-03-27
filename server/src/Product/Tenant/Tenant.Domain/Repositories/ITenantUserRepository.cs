using BuildingBlocks.Kernel.Persistence;
using Tenant.Domain.Entities;

namespace Tenant.Domain.Repositories;

/// <summary>
/// Repository for tenant users.
/// </summary>
public interface ITenantUserRepository : IRepository<TenantUser, Guid>
{
    Task<IReadOnlyList<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
