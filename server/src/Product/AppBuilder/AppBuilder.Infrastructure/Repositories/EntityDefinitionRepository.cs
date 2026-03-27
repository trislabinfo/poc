using AppBuilder.Infrastructure.Data;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class EntityDefinitionRepository : Repository<EntityDefinition, Guid>, IEntityDefinitionRepository
{
    private readonly AppBuilderDbContext _context;

    public EntityDefinitionRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<EntityDefinition>> GetByAppDefinitionIdAsync(
        Guid AppDefinitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityDefinitions
            .Where(x => x.AppDefinitionId == AppDefinitionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<EntityDefinition?> GetByNameAsync(
        Guid AppDefinitionId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityDefinitions
            .FirstOrDefaultAsync(
                x => x.AppDefinitionId == AppDefinitionId && x.Name == name,
                cancellationToken);
    }
}
