using System.Text.Json;

namespace PlatformMetaModel.Layout;

/// <summary>Override for a field on a page (readonly, visible, default).</summary>
public class FieldOverride
{
    public required string Field { get; set; }

    /// <summary>Expression for readonly at runtime.</summary>
    public string? Readonly { get; set; }

    /// <summary>Expression for visibility at runtime.</summary>
    public string? Visible { get; set; }

    public JsonElement? Default { get; set; }
}
