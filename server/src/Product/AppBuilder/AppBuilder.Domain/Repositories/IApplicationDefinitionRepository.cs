using BuildingBlocks.Kernel.Persistence;

namespace AppBuilder.Domain.Repositories;

public interface IAppDefinitionRepository : IRepository<AppDefinition.Domain.Entities.Application.AppDefinition, Guid>
{
    Task<List<AppDefinition.Domain.Entities.Application.AppDefinition>> ListAsync(CancellationToken cancellationToken = default);
    Task<AppDefinition.Domain.Entities.Application.AppDefinition?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
}
