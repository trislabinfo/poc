using AppDefinition.Domain.Entities.Application;

namespace TenantApplication.Domain.Repositories;

/// <summary>Reads current definition snapshot (entities, nav, pages, datasources) for a tenant application to build a release.</summary>
public interface ITenantDefinitionSnapshotReader
{
    Task<(string NavigationJson, string PageJson, string DataSourceJson, string EntityJson)> GetSnapshotAsync(
        Guid tenantApplicationId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets entities, properties, and relations for schema derivation.</summary>
    Task<(IReadOnlyList<EntityDefinition> Entities, Dictionary<Guid, List<PropertyDefinition>> PropertiesByEntityId, IReadOnlyList<RelationDefinition> Relations)> GetSchemaDataAsync(
        Guid tenantApplicationId,
        CancellationToken cancellationToken = default);
}
