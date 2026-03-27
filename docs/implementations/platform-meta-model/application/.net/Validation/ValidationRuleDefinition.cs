using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformMetaModel.Validation;

/// <summary>
/// Single validation rule; used on entity property or field node.
/// Error message resolved via messageKey from application translations.
/// </summary>
public class ValidationRuleDefinition
{
    /// <summary>Rule kind.</summary>
    [JsonPropertyName("type")]
    public required ValidationRuleType Type { get; set; }

    /// <summary>Parameter for min/max/length/pattern (e.g. regex string or numeric bound).</summary>
    public JsonElement? Value { get; set; }

    /// <summary>Translation key for error message (e.g. validation.required).</summary>
    public string? MessageKey { get; set; }
}

/// <summary>Validation rule kind.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidationRuleType
{
    Required,
    MinLength,
    MaxLength,
    Pattern,
    Min,
    Max,
    Email,
    Custom
}
