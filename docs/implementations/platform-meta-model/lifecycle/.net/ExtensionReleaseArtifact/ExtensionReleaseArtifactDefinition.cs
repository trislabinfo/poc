using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformMetaModel.Lifecycle.ExtensionReleaseArtifact;

/// <summary>
/// One row per piece of an extension release. Stored in a separate resource/table from ExtensionReleaseDefinition.
/// Full release snapshot can be assembled on demand.
/// </summary>
public class ExtensionReleaseArtifactDefinition
{
    /// <summary>Extension release this artifact belongs to.</summary>
    public required string ExtensionReleaseId { get; set; }

    [JsonPropertyName("artifactType")]
    public required ExtensionReleaseArtifactType ArtifactType { get; set; }

    /// <summary>Id of the definition: extensionId for artifactType extension; entity id, page id, etc. for other types.</summary>
    public required string DefinitionId { get; set; }

    /// <summary>The definition JSON. Validates against the corresponding extension meta model.</summary>
    public required JsonElement Snapshot { get; set; }
}

/// <summary>Type of definition this artifact row holds.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExtensionReleaseArtifactType
{
    Extension,
    Entity,
    Page,
    Navigation,
    Workflow,
    Role,
    Permission,
    CodeTable,
    DataSource
}
