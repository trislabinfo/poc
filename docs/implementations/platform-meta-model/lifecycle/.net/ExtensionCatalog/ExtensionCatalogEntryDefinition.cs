using System.Text.Json.Serialization;

namespace PlatformMetaModel.Lifecycle.ExtensionCatalog;

/// <summary>
/// Extension catalog entry: an extension release offered in the extension catalog for applications to reference.
/// </summary>
public class ExtensionCatalogEntryDefinition
{
    /// <summary>Unique extension catalog entry identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Extension id.</summary>
    public required string ExtensionId { get; set; }

    /// <summary>Extension release id (or use version tag e.g. latest; resolution is platform-specific).</summary>
    public required string LatestExtensionReleaseId { get; set; }

    /// <summary>Display name in extension catalog.</summary>
    public required string Name { get; set; }

    public string? Description { get; set; }

    public IList<string>? Tags { get; set; }
}
