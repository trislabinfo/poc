using AppBuilder.Infrastructure.Data;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class PageDefinitionRepository : Repository<PageDefinition, Guid>, IPageDefinitionRepository
{
    private readonly AppBuilderDbContext _context;

    public PageDefinitionRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<PageDefinition>> GetByAppDefinitionIdAsync(Guid AppDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _context.PageDefinitions
            .Where(x => x.AppDefinitionId == AppDefinitionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PageDefinition?> GetByRouteAsync(Guid AppDefinitionId, string route, CancellationToken cancellationToken = default)
    {
        return await _context.PageDefinitions
            .FirstOrDefaultAsync(x => x.AppDefinitionId == AppDefinitionId && x.Route == route, cancellationToken);
    }
}
