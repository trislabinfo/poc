using System.Text.Json.Serialization;

namespace PlatformMetaModel.Navigation;

/// <summary>
/// Navigation definition: defines a navigation container by type and its root items.
/// </summary>
public class NavigationDefinition
{
    /// <summary>Unique identifier for this navigation.</summary>
    public required string Id { get; set; }

    [JsonPropertyName("type")]
    public required NavigationType Type { get; set; }

    /// <summary>Root navigation items (tree).</summary>
    public IList<NavigationItemDefinition>? Item { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NavigationType
{
    SidebarLeft,
    SidebarRight,
    Hamburger,
    TopBar,
    BottomBar
}

/// <summary>Navigation item; show/hide based on user role or permissions.</summary>
public class NavigationItemDefinition
{
    public string? Id { get; set; }
    public string? Label { get; set; }
    /// <summary>Page ID.</summary>
    public string? Page { get; set; }
    /// <summary>Role IDs; item visible only if user has at least one of these roles.</summary>
    public IList<string>? AllowedRole { get; set; }
    /// <summary>Item visible only if user has at least one of these permissions.</summary>
    public IList<AllowedPermission>? AllowedPermission { get; set; }
    public IList<NavigationItemDefinition>? Children { get; set; }
    public string? TenantId { get; set; }
}

/// <summary>Permission required to show a navigation item.</summary>
public class AllowedPermission
{
    public required ResourceType ResourceType { get; set; }
    public required string ResourceId { get; set; }
    public required PermissionAction Action { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResourceType
{
    Entity,
    Page,
    Workflow
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PermissionAction
{
    Create,
    Read,
    Update,
    Delete,
    Execute
}
