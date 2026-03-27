using System.Text.Json.Serialization;
using PlatformMetaModel.Lifecycle.ApplicationRelease;
using PlatformMetaModel.Lifecycle.Common;

namespace PlatformMetaModel.Lifecycle.ExtensionRelease;

/// <summary>
/// Immutable extension release. Release metadata only; content is stored as ExtensionReleaseArtifactDefinition rows.
/// Created when releasing extensions from an application and added to the extension catalog.
/// </summary>
public class ExtensionReleaseDefinition : AuditDefinition
{
    /// <summary>Unique extension release identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Extension this release belongs to.</summary>
    public required string ExtensionId { get; set; }

    /// <summary>Semantic version: major.minor.patch with optional prerelease and build.</summary>
    public required string Version { get; set; }

    [JsonPropertyName("validationStatus")]
    public required ReleaseStatus ValidationStatus { get; set; }

    /// <summary>Resolved versions of extensions in dependsOn for this release.</summary>
    public IList<ResolvedExtensionDependency>? ResolvedDependencyVersions { get; set; }
}
