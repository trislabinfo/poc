using AppBuilder.Infrastructure.Data;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class NavigationDefinitionRepository : Repository<NavigationDefinition, Guid>, INavigationDefinitionRepository
{
    private readonly AppBuilderDbContext _context;

    public NavigationDefinitionRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<NavigationDefinition>> GetByAppDefinitionIdAsync(
        Guid AppDefinitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.NavigationDefinitions
            .Where(x => x.AppDefinitionId == AppDefinitionId)
            .ToListAsync(cancellationToken);
    }
}
