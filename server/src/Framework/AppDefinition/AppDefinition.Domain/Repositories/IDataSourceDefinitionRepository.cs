using AppDefinition.Domain.Entities.Application;
using BuildingBlocks.Kernel.Persistence;

namespace AppDefinition.Domain.Repositories;

public interface IDataSourceDefinitionRepository : IRepository<DataSourceDefinition, Guid>
{
    Task<List<DataSourceDefinition>> GetByAppDefinitionIdAsync(Guid AppDefinitionId, CancellationToken cancellationToken = default);
}
