using AppDefinition.Domain.Entities.Application;
using BuildingBlocks.Kernel.Persistence;

namespace AppDefinition.Domain.Repositories;

public interface IEntityDefinitionRepository : IRepository<EntityDefinition, Guid>
{
    Task<List<EntityDefinition>> GetByAppDefinitionIdAsync(Guid AppDefinitionId, CancellationToken cancellationToken = default);
    Task<EntityDefinition?> GetByNameAsync(Guid AppDefinitionId, string name, CancellationToken cancellationToken = default);
}
