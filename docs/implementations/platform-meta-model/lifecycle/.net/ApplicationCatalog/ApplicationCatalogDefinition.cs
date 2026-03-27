namespace PlatformMetaModel.Lifecycle.ApplicationCatalog;

/// <summary>
/// Application catalog item: an application release (or application) offered for tenant installation.
/// </summary>
public class ApplicationCatalogDefinition
{
    /// <summary>Unique catalog item identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Application id.</summary>
    public required string ApplicationId { get; set; }

    /// <summary>Application release id (or use version tag e.g. latest; resolution is platform-specific).</summary>
    public required string LatestApplicationReleaseId { get; set; }

    /// <summary>Display name in catalog.</summary>
    public required string Name { get; set; }

    public string? Description { get; set; }

    public IList<string>? Tags { get; set; }
}
