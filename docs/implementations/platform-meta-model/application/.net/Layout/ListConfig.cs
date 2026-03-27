using System.Text.Json.Serialization;

namespace PlatformMetaModel.Layout;

/// <summary>List page configuration (columns, filter, sort, page size).</summary>
public class ListConfig
{
    public IList<string>? Columns { get; set; }

    public IList<string>? FilterFields { get; set; }

    public IList<string>? QuickSearchFields { get; set; }

    public IList<SortSpec>? DefaultSort { get; set; }

    /// <summary>Default page size for list.</summary>
    public int PageSize { get; set; } = 25;
}

/// <summary>Sort field and direction.</summary>
public class SortSpec
{
    public required string Field { get; set; }

    public SortDirection Direction { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection
{
    Asc,
    Desc
}
