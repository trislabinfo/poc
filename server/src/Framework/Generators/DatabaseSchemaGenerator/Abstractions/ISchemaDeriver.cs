using AppDefinition.Domain.Entities.Application;

namespace Capabilities.DatabaseSchema.Abstractions;

/// <summary>Derives database schema (tables/columns/foreign keys) from entity/property/relation definitions.</summary>
public interface ISchemaDeriver
{
    /// <summary>
    /// Derives a database schema from entity definitions with their properties and relations.
    /// </summary>
    /// <param name="entities">Entity definitions.</param>
    /// <param name="propertiesByEntityId">Properties grouped by entity ID.</param>
    /// <param name="relations">Relation definitions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Database schema model.</returns>
    Task<Models.DatabaseSchema> DeriveSchemaAsync(
        IReadOnlyList<EntityDefinition> entities,
        Dictionary<Guid, List<PropertyDefinition>> propertiesByEntityId,
        IReadOnlyList<RelationDefinition> relations,
        CancellationToken cancellationToken = default);
}
