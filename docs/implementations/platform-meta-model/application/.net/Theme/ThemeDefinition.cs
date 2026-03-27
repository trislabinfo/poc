namespace PlatformMetaModel.Theme;

/// <summary>
/// Theme definition; defined only inside application definition.
/// </summary>
public class ThemeDefinition
{
    public required string Id { get; set; }

    public ThemeColors? Colors { get; set; }

    public ThemeTypography? Typography { get; set; }
}

/// <summary>Theme color tokens.</summary>
public class ThemeColors
{
    public string? Primary { get; set; }
    public string? Secondary { get; set; }
    public string? Background { get; set; }
    public string? Text { get; set; }
}

/// <summary>Theme typography.</summary>
public class ThemeTypography
{
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
}
