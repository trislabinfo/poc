using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformMetaModel.Lifecycle.TenantApplicationReleaseArtifact;

/// <summary>
/// One row per piece of the effective tenant application release. Stored in a separate resource/table from
/// TenantApplicationReleaseDefinition. Built by copying app release artifacts and merging tenant overrides.
/// </summary>
public class TenantApplicationReleaseArtifactDefinition
{
    /// <summary>Tenant application release this artifact belongs to.</summary>
    public required string TenantApplicationReleaseId { get; set; }

    [JsonPropertyName("artifactType")]
    public required TenantApplicationReleaseArtifactType ArtifactType { get; set; }

    /// <summary>Id of the definition: applicationId for artifactType application; extensionId for artifactType extension; entity id, page id, etc. for other types.</summary>
    public required string DefinitionId { get; set; }

    /// <summary>Set for content that came from an extension (namespaced). Null for app-owned content and for artifactType application or extension.</summary>
    public string? ExtensionId { get; set; }

    /// <summary>The effective definition JSON (base + tenant overrides merged). Validates against the application meta model.</summary>
    public required JsonElement Snapshot { get; set; }
}

/// <summary>Same set as ApplicationReleaseArtifactType. Each row holds the effective definition (base + tenant overrides merged).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantApplicationReleaseArtifactType
{
    Application,
    Entity,
    Page,
    Navigation,
    Workflow,
    Role,
    Permission,
    CodeTable,
    Theme,
    DataSource,
    Breakpoint,
    Extension
}
