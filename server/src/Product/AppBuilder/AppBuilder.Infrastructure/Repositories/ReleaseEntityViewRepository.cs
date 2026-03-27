using AppBuilder.Infrastructure.Data;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

/// <summary>Repository for release entity views (appbuilder.release_entity_views).</summary>
public sealed class ReleaseEntityViewRepository : IReleaseEntityViewRepository
{
    private readonly AppBuilderDbContext _context;

    public ReleaseEntityViewRepository(AppBuilderDbContext context)
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
        // Caller (e.g. ApproveReleaseCommandHandler) calls Unit of Work SaveChanges to persist in same transaction.
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
