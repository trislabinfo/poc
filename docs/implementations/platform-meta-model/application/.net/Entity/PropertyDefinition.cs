using System.Text.Json;
using System.Text.Json.Serialization;
using PlatformMetaModel.Validation;

namespace PlatformMetaModel.Entity;

/// <summary>
/// Property definition; defined under entity definition.
/// </summary>
public class PropertyDefinition
{
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required PropertyType Type { get; set; }

    public bool Required { get; set; }

    public bool Readonly { get; set; }

    public JsonElement? Default { get; set; }

    public int? Length { get; set; }

    public int? Precision { get; set; }

    public int? Scale { get; set; }

    /// <summary>Expression for calculated value.</summary>
    public string? Expression { get; set; }

    /// <summary>Whether calculated value is stored in DB.</summary>
    public bool Persist { get; set; }

    /// <summary>Validation rules for this property; applied when property is edited in forms.</summary>
    public IList<ValidationRuleDefinition>? Validation { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PropertyType
{
    String,
    Uuid,
    Int,
    Decimal,
    Datetime,
    Boolean,
    Relation,
    Binary
}
