using AppBuilder.Infrastructure.Data;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class RelationDefinitionRepository : Repository<RelationDefinition, Guid>, IRelationDefinitionRepository
{
    private readonly AppBuilderDbContext _context;

    public RelationDefinitionRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<RelationDefinition>> GetBySourceEntityIdAsync(Guid sourceEntityId, CancellationToken cancellationToken = default)
    {
        return await _context.RelationDefinitions
            .Where(x => x.SourceEntityId == sourceEntityId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RelationDefinition>> GetByTargetEntityIdAsync(Guid targetEntityId, CancellationToken cancellationToken = default)
    {
        return await _context.RelationDefinitions
            .Where(x => x.TargetEntityId == targetEntityId)
            .ToListAsync(cancellationToken);
    }
}
