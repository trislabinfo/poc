using System.Text.Json.Serialization;

namespace PlatformMetaModel.Entity;

/// <summary>
/// Relation definition; defined under entity definition.
/// </summary>
public class RelationDefinition
{
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required RelationType Type { get; set; }

    /// <summary>Target entity ID.</summary>
    public required string Target { get; set; }

    /// <summary>Property name in target entity that references back.</summary>
    public string? Inverse { get; set; }

    public bool Required { get; set; }

    public bool TenantScoped { get; set; } = true;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RelationType
{
    [JsonPropertyName("one-to-one")]
    OneToOne,

    [JsonPropertyName("one-to-many")]
    OneToMany,

    [JsonPropertyName("many-to-one")]
    ManyToOne,

    [JsonPropertyName("many-to-many")]
    ManyToMany
}
