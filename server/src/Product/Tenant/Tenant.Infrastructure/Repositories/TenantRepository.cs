using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Repositories;
using Tenant.Infrastructure.Data;

namespace Tenant.Infrastructure.Repositories;

public class TenantRepository : Repository<Tenant.Domain.Entities.Tenant, Guid>, ITenantRepository
{
    private readonly TenantDbContext _context;

    public TenantRepository(TenantDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<Tenant.Domain.Entities.Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AnyAsync(t => t.Slug == slug, cancellationToken);
    }
}
