namespace PlatformMetaModel.Breakpoint;

/// <summary>
/// Viewport breakpoint for responsive layout; referenced by id in layout nodes (visibleFrom, hiddenFrom).
/// </summary>
public class BreakpointDefinition
{
    /// <summary>Breakpoint identifier (e.g. small, medium, large, xlarge).</summary>
    public required string Id { get; set; }

    /// <summary>Min viewport width in px. Omit or 0 for smallest breakpoint.</summary>
    public int? MinWidth { get; set; }

    /// <summary>Max viewport width in px. Omit for largest breakpoint (no max).</summary>
    public int? MaxWidth { get; set; }
}
