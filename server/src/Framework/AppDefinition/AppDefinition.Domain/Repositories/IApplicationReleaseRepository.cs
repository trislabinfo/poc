using AppDefinition.Domain.Entities.Lifecycle;
using BuildingBlocks.Kernel.Persistence;

namespace AppDefinition.Domain.Repositories;

public interface IApplicationReleaseRepository : IRepository<ApplicationRelease, Guid>
{
    Task<List<ApplicationRelease>> GetByAppDefinitionIdAsync(Guid AppDefinitionId, CancellationToken cancellationToken = default);
    Task<ApplicationRelease?> GetActiveReleaseAsync(Guid AppDefinitionId, CancellationToken cancellationToken = default);
    Task<ApplicationRelease?> GetByVersionAsync(Guid AppDefinitionId, int major, int minor, int patch, CancellationToken cancellationToken = default);
}
