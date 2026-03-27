using System.Text.Json.Serialization;

namespace PlatformMetaModel.Lifecycle.Environment;

/// <summary>
/// Tenant application environment (e.g. dev, staging, prod). Holds environment-specific config for deployment.
/// </summary>
public class EnvironmentDefinition
{
    /// <summary>Unique environment identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Tenant application this environment belongs to (optional if environment is tenant-scoped only).</summary>
    public string? TenantApplicationId { get; set; }

    /// <summary>Display name.</summary>
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required EnvironmentType Type { get; set; }

    /// <summary>Environment-specific config: connection strings, base URLs, feature flags, etc.</summary>
    public Dictionary<string, object>? Config { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnvironmentType
{
    Dev,
    Staging,
    Prod
}
