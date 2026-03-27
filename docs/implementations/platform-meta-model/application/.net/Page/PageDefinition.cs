using System.Text.Json.Serialization;
using PlatformMetaModel.Layout;

namespace PlatformMetaModel.Page;

/// <summary>
/// Page definition; defined only inside application definition.
/// </summary>
public class PageDefinition
{
    public required string Id { get; set; }

    [JsonPropertyName("type")]
    public required PageType Type { get; set; }

    /// <summary>Entity ID for entity-based pages.</summary>
    public string? Entity { get; set; }

    /// <summary>Root layout node.</summary>
    public LayoutNode? Layout { get; set; }

    public ListConfig? ListConfig { get; set; }

    public IList<FieldOverride>? FieldOverrides { get; set; }

    public PagePermissions? Permissions { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PageType
{
    EntityList,
    EntityEdit,
    Custom
}

/// <summary>Page-level permissions.</summary>
public class PagePermissions
{
    public IList<string>? AllowedRoles { get; set; }
}
