using BuildingBlocks.Kernel.Persistence;
using TenantEntity = Tenant.Domain.Entities.Tenant;

namespace Tenant.Domain.Repositories;

public interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    Task<TenantEntity?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
}
