using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using TenantApplication.Infrastructure.Data;

namespace TenantApplication.Infrastructure.Repositories;

/// <summary>Repository for release entity views (tenantapplication.release_entity_views).</summary>
public sealed class ReleaseEntityViewRepository : IReleaseEntityViewRepository
{
    private readonly TenantApplicationDbContext _context;

    public ReleaseEntityViewRepository(TenantApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SetForReleaseAsync(Guid releaseId, IReadOnlyList<ReleaseEntityView> views, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ReleaseEntityViews
            .Where(v => v.ReleaseId == releaseId)
            .ToListAsync(cancellationToken);
        _context.ReleaseEntityViews.RemoveRange(existing);
        if (views.Count > 0)
            await _context.ReleaseEntityViews.AddRangeAsync(views, cancellationToken);
        // Caller calls Unit of Work or SaveChanges to persist in same transaction.
    }

    public async Task<IReadOnlyList<ReleaseEntityView>> GetByReleaseIdAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return await _context.ReleaseEntityViews
            .Where(v => v.ReleaseId == releaseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReleaseEntityView?> GetAsync(Guid releaseId, Guid entityId, string viewType, CancellationToken cancellationToken = default)
    {
        return await _context.ReleaseEntityViews
            .FirstOrDefaultAsync(
                v => v.ReleaseId == releaseId && v.EntityId == entityId && v.ViewType == viewType,
                cancellationToken);
    }
}
