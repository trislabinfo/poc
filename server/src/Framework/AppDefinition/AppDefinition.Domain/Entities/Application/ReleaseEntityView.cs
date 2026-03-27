namespace AppDefinition.Domain.Entities.Application;

/// <summary>
/// Pre-generated HTML for an entity view (list or form) per release.
/// Stored in release_entity_views table (AppBuilder and TenantApplication schemas).
/// Composite key: ReleaseId, EntityId, ViewType.
/// </summary>
public sealed class ReleaseEntityView
{
    public Guid ReleaseId { get; private set; }
    public Guid EntityId { get; private set; }
    /// <summary>View type: "list" or "form".</summary>
    public string ViewType { get; private set; } = string.Empty;
    public string Html { get; private set; } = string.Empty;

    private ReleaseEntityView() { }

    public static ReleaseEntityView Create(Guid releaseId, Guid entityId, string viewType, string html)
    {
        if (releaseId == Guid.Empty)
            throw new ArgumentException("Release ID is required.", nameof(releaseId));
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(viewType))
            throw new ArgumentException("View type is required.", nameof(viewType));
        if (viewType != ViewTypes.List && viewType != ViewTypes.Form)
            throw new ArgumentException($"View type must be '{ViewTypes.List}' or '{ViewTypes.Form}'.", nameof(viewType));

        return new ReleaseEntityView
        {
            ReleaseId = releaseId,
            EntityId = entityId,
            ViewType = viewType,
            Html = html ?? string.Empty
        };
    }
}

/// <summary>View type constants for entity views (list | form).</summary>
public static class ViewTypes
{
    public const string List = "list";
    public const string Form = "form";
}
