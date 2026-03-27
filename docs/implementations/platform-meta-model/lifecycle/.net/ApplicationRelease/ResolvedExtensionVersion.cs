namespace PlatformMetaModel.Lifecycle.ApplicationRelease;

/// <summary>
/// Resolved extension id and version at release time; used when loading extension content for this release.
/// </summary>
public class ResolvedExtensionVersion
{
    public required string ExtensionId { get; set; }

    /// <summary>Semantic version (exact at release time).</summary>
    public required string Version { get; set; }
}
