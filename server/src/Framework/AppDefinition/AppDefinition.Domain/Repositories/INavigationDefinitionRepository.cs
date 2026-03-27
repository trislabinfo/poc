using AppDefinition.Domain.Entities.Application;
using BuildingBlocks.Kernel.Persistence;

namespace AppDefinition.Domain.Repositories;

public interface INavigationDefinitionRepository : IRepository<NavigationDefinition, Guid>
{
    Task<List<NavigationDefinition>> GetByAppDefinitionIdAsync(Guid AppDefinitionId, CancellationToken cancellationToken = default);
}
