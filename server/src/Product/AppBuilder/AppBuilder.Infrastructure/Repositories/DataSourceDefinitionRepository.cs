using AppBuilder.Infrastructure.Data;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class DataSourceDefinitionRepository : Repository<DataSourceDefinition, Guid>, IDataSourceDefinitionRepository
{
    private readonly AppBuilderDbContext _context;

    public DataSourceDefinitionRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<DataSourceDefinition>> GetByAppDefinitionIdAsync(Guid AppDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _context.DataSourceDefinitions
            .Where(x => x.AppDefinitionId == AppDefinitionId)
            .ToListAsync(cancellationToken);
    }
}
