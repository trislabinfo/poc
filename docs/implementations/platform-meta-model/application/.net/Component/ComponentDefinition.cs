using System.Text.Json.Serialization;

namespace PlatformMetaModel.Component;

/// <summary>
/// Registered UI component in the component registry. Components are referenced by componentId in layout nodes.
/// </summary>
public class ComponentDefinition
{
    /// <summary>Unique component identifier (e.g. "TextInput", "DataTable").</summary>
    public required string Id { get; set; }

    /// <summary>Component category: Field (input), Layout (container), or Data (display).</summary>
    [JsonPropertyName("category")]
    public required ComponentCategory Category { get; set; }

    /// <summary>Semantic version of the component.</summary>
    public string? Version { get; set; }

    /// <summary>Optional JSON schema describing component properties.</summary>
    public Dictionary<string, ComponentPropertyDefinition>? PropsSchema { get; set; }
}

/// <summary>Component category enumeration.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ComponentCategory
{
    Field,
    Layout,
    Data
}
