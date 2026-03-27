using AppDefinition.Domain.Entities.Application;
using BuildingBlocks.Kernel.Persistence;

namespace AppDefinition.Domain.Repositories;

public interface IRelationDefinitionRepository : IRepository<RelationDefinition, Guid>
{
    Task<List<RelationDefinition>> GetBySourceEntityIdAsync(Guid sourceEntityId, CancellationToken cancellationToken = default);
    Task<List<RelationDefinition>> GetByTargetEntityIdAsync(Guid targetEntityId, CancellationToken cancellationToken = default);
}
