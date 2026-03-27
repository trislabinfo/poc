namespace PlatformMetaModel.Extension;

/// <summary>
/// Declared dependency on another extension. Enables cross-extension references using namespaced ids.
/// Load order is topological; cycles are rejected.
/// </summary>
public class ExtensionDependency
{
    public required string ExtensionId { get; set; }

    /// <summary>Semantic version or range (e.g. 1.0.0 or ^1.0.0).</summary>
    public required string Version { get; set; }
}
