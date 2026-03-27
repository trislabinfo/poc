using System.Text.Json.Serialization;
using PlatformMetaModel.Lifecycle.Common;

namespace PlatformMetaModel.Lifecycle.ApplicationRelease;

/// <summary>
/// Immutable application release (platform). Release metadata only; content is stored as
/// ApplicationReleaseArtifactDefinition rows. Full snapshot can be assembled on demand.
/// </summary>
public class ApplicationReleaseDefinition : AuditDefinition
{
    /// <summary>Unique release identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Application this release belongs to.</summary>
    public required string ApplicationId { get; set; }

    /// <summary>Semantic version: major.minor.patch with optional prerelease and build.</summary>
    public required string Version { get; set; }

    /// <summary>Validation result of the snapshot.</summary>
    [JsonPropertyName("validationStatus")]
    public required ReleaseStatus ReleaseStatus { get; set; }

    /// <summary>Resolved extension versions used in this release.</summary>
    public IList<ResolvedExtensionVersion>? ResolvedExtensionVersions { get; set; }
}
