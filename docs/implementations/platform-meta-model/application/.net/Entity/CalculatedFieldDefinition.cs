using System.Text.Json.Serialization;

namespace PlatformMetaModel.Entity;

/// <summary>Calculated field definition.</summary>
public class CalculatedFieldDefinition
{
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required CalculatedFieldType Type { get; set; }

    public required string Expression { get; set; }

    public bool Persist { get; set; } = true;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CalculatedFieldType
{
    String,
    Int,
    Long,
    Decimal,
    Date,
    Datetime,
    Boolean
}
