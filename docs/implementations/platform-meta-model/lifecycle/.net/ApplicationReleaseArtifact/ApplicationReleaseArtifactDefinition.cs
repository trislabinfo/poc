using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformMetaModel.Lifecycle.ApplicationReleaseArtifact;

/// <summary>
/// One row per piece of an application release. Stored in a separate resource/table from ApplicationReleaseDefinition.
/// Full release snapshot can be assembled on demand.
/// </summary>
public class ApplicationReleaseArtifactDefinition
{
    /// <summary>Application release this artifact belongs to.</summary>
    public required string ApplicationReleaseId { get; set; }

    /// <summary>Type of definition this artifact row holds.</summary>
    [JsonPropertyName("artifactType")]
    public required ApplicationReleaseArtifactType ArtifactType { get; set; }

    /// <summary>Id of the definition: applicationId for artifactType application; extensionId for artifactType extension; entity id, page id, etc. for other types.</summary>
    public required string DefinitionId { get; set; }

    /// <summary>Set for artifacts that belong to an in-app extension. Null for app-owned content and for artifactType application or extension.</summary>
    public string? ExtensionId { get; set; }

    /// <summary>The definition JSON. Validates against the corresponding application meta model definition.</summary>
    public required JsonElement Snapshot { get; set; }
}

/// <summary>Type of definition this artifact row holds.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationReleaseArtifactType
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
