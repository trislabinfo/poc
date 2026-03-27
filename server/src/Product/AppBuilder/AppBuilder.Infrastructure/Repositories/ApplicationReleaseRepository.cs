using AppBuilder.Infrastructure.Data;
using AppDefinition.Domain.Entities.Lifecycle;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class ApplicationReleaseRepository : Repository<ApplicationRelease, Guid>, IApplicationReleaseRepository
{
    private readonly AppBuilderDbContext _context;

    public ApplicationReleaseRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<ApplicationRelease>> GetByAppDefinitionIdAsync(
        Guid AppDefinitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationReleases
            .Where(x => x.AppDefinitionId == AppDefinitionId)
            .OrderByDescending(x => x.ReleasedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicationRelease?> GetActiveReleaseAsync(
        Guid AppDefinitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationReleases
            .Where(x => x.AppDefinitionId == AppDefinitionId && x.IsActive)
            .OrderByDescending(x => x.ReleasedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ApplicationRelease?> GetByVersionAsync(
        Guid AppDefinitionId,
        int major,
        int minor,
        int patch,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationReleases
            .FirstOrDefaultAsync(
                x => x.AppDefinitionId == AppDefinitionId
                     && x.Major == major && x.Minor == minor && x.Patch == patch,
                cancellationToken);
    }
}
