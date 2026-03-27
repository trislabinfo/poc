using AppBuilder.Infrastructure.Data;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class PropertyDefinitionRepository : Repository<PropertyDefinition, Guid>, IPropertyDefinitionRepository
{
    private readonly AppBuilderDbContext _context;

    public PropertyDefinitionRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<PropertyDefinition>> GetByEntityDefinitionIdAsync(Guid entityDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyDefinitions
            .Where(x => x.EntityDefinitionId == entityDefinitionId)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyDefinition?> GetByNameAsync(Guid entityDefinitionId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyDefinitions
            .FirstOrDefaultAsync(x => x.EntityDefinitionId == entityDefinitionId && x.Name == name, cancellationToken);
    }
}
