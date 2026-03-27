using AppDefinition.Domain.Entities.Application;
using BuildingBlocks.Kernel.Persistence;

namespace AppDefinition.Domain.Repositories;

public interface IPropertyDefinitionRepository : IRepository<PropertyDefinition, Guid>
{
    Task<List<PropertyDefinition>> GetByEntityDefinitionIdAsync(Guid entityDefinitionId, CancellationToken cancellationToken = default);
    Task<PropertyDefinition?> GetByNameAsync(Guid entityDefinitionId, string name, CancellationToken cancellationToken = default);
}
