using AppDefinition.Domain.Entities.Lifecycle;
using Microsoft.EntityFrameworkCore;
using TenantApplication.Domain.Repositories;
using TenantApplication.Infrastructure.Data;

namespace TenantApplication.Infrastructure.Repositories;

public sealed class TenantApplicationReleaseRepository : ITenantApplicationReleaseRepository
{
    private readonly TenantApplicationDbContext _context;

    public TenantApplicationReleaseRepository(TenantApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationRelease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationReleases.FindAsync([id], cancellationToken);
    }

    public async Task<List<ApplicationRelease>> GetByTenantApplicationIdAsync(Guid tenantApplicationId, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationReleases
            .Where(x => x.AppDefinitionId == tenantApplicationId)
            .OrderByDescending(x => x.ReleasedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ApplicationRelease release, CancellationToken cancellationToken = default)
    {
        await _context.ApplicationReleases.AddAsync(release, cancellationToken);
    }

    public void Update(ApplicationRelease release)
    {
        _context.ApplicationReleases.Update(release);
    }
}
