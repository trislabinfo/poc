using AppDefinition.Domain.Entities.Application;

namespace AppDefinition.Domain.Repositories;

/// <summary>
/// Repository for release entity view HTML (list/form per entity per release).
/// Implemented in AppBuilder and TenantApplication; each writes to its own schema's release_entity_views table.
/// </summary>
public interface IReleaseEntityViewRepository
{
    /// <summary>Replaces all entity views for the given release with the provided rows.</summary>
    Task SetForReleaseAsync(Guid releaseId, IReadOnlyList<ReleaseEntityView> views, CancellationToken cancellationToken = default);

    /// <summary>Gets all entity views for the given release.</summary>
    Task<IReadOnlyList<ReleaseEntityView>> GetByReleaseIdAsync(Guid releaseId, CancellationToken cancellationToken = default);

    /// <summary>Gets the entity view HTML for the given release, entity, and view type, or null if not found.</summary>
    Task<ReleaseEntityView?> GetAsync(Guid releaseId, Guid entityId, string viewType, CancellationToken cancellationToken = default);
}
