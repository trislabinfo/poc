using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Domain.Repositories;
using Tenant.Infrastructure.Data;

namespace Tenant.Infrastructure.Repositories;

public class TenantUserRepository : Repository<TenantUser, Guid>, ITenantUserRepository
{
    private readonly TenantDbContext _context;

    public TenantUserRepository(TenantDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantUsers
            .Where(tu => tu.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }
}
