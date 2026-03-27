using BuildingBlocks.Infrastructure.Persistence;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RoleRepository : Repository<Role, Guid>, IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<Role?> GetByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name && r.TenantId == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }
}
