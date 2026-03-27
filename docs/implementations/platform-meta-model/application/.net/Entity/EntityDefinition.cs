namespace PlatformMetaModel.Entity;

/// <summary>
/// Entity definition (business object). Contains property definitions and relation definitions.
/// </summary>
public class EntityDefinition
{
    /// <summary>Unique entity identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Human-readable name.</summary>
    public required string DisplayName { get; set; }

    /// <summary>Whether the entity is tenant-specific.</summary>
    public required bool TenantScoped { get; set; }

    /// <summary>Property definitions (defined under entity definition).</summary>
    public IList<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();

    /// <summary>Relation definitions (defined under entity definition).</summary>
    public IList<RelationDefinition> Relations { get; set; } = new List<RelationDefinition>();

    /// <summary>Calculated field definitions.</summary>
    public IList<CalculatedFieldDefinition> CalculatedFields { get; set; } = new List<CalculatedFieldDefinition>();
}
