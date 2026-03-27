using System.Text.Json.Serialization;

namespace PlatformMetaModel.Lifecycle.Deploy;

/// <summary>Deployment of a tenant application release to an environment.</summary>
public class DeployDefinition
{
    /// <summary>Unique deployment identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Tenant application release being deployed.</summary>
    public required string TenantApplicationReleaseId { get; set; }

    /// <summary>Target environment.</summary>
    public required string EnvironmentId { get; set; }

    [JsonPropertyName("status")]
    public required DeployStatus Status { get; set; }

    /// <summary>When deployment completed (for succeeded).</summary>
    public string? DeployedAt { get; set; }

    /// <summary>Optional reference to compiled artifact (e.g. storage path or build id).</summary>
    public string? ArtifactRef { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeployStatus
{
    Pending,
    InProgress,
    Succeeded,
    Failed
}
