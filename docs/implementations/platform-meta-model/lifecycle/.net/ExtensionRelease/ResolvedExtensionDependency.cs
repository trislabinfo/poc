namespace PlatformMetaModel.Lifecycle.ExtensionRelease;

/// <summary>
/// Resolved extension dependency at release time; stored in extension release.
/// </summary>
public class ResolvedExtensionDependency
{
    public required string ExtensionId { get; set; }

    /// <summary>Semantic version (exact at release time).</summary>
    public required string Version { get; set; }
}
