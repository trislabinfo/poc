namespace PlatformMetaModel.CodeTable;

/// <summary>
/// Code table definition: reference/lookup data (e.g. statuses, types).
/// Defined only inside application definition.
/// </summary>
public class CodeTableDefinition
{
    /// <summary>Unique code table identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Human-readable name.</summary>
    public required string DisplayName { get; set; }

    /// <summary>Whether the code table is tenant-specific.</summary>
    public bool TenantScoped { get; set; }

    /// <summary>Static list of code table rows.</summary>
    public IList<CodeTableItem>? Items { get; set; }
}

/// <summary>Single row in a code table.</summary>
public class CodeTableItem
{
    /// <summary>Code value.</summary>
    public required string Code { get; set; }

    /// <summary>Display value.</summary>
    public required string Value { get; set; }

    /// <summary>Optional sort order.</summary>
    public int? SortOrder { get; set; }
}
