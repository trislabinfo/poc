using AppDefinition.Domain.Entities.Application;
using BuildingBlocks.Kernel.Persistence;

namespace AppDefinition.Domain.Repositories;

public interface IPageDefinitionRepository : IRepository<PageDefinition, Guid>
{
    Task<List<PageDefinition>> GetByAppDefinitionIdAsync(Guid AppDefinitionId, CancellationToken cancellationToken = default);
    Task<PageDefinition?> GetByRouteAsync(Guid AppDefinitionId, string route, CancellationToken cancellationToken = default);
}
