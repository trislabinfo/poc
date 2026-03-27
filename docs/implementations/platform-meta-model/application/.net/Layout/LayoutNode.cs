using System.Text.Json.Serialization;
using PlatformMetaModel.Validation;

namespace PlatformMetaModel.Layout;

/// <summary>
/// Layout node: structural (Section, Row, Tabs, Tab) or data-bound (Field, DataTable).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SectionNode), "Section")]
[JsonDerivedType(typeof(RowNode), "Row")]
[JsonDerivedType(typeof(TabsNode), "Tabs")]
[JsonDerivedType(typeof(TabNode), "Tab")]
[JsonDerivedType(typeof(FieldNode), "Field")]
[JsonDerivedType(typeof(DataTableNode), "DataTable")]
public abstract class LayoutNode
{
    public string? Id { get; set; }

    public string? VisibleFrom { get; set; }

    public string? HiddenFrom { get; set; }

    public IList<LayoutNode>? Children { get; set; }
}

/// <summary>Section container.</summary>
public class SectionNode : LayoutNode
{
    public string? Title { get; set; }
    public bool? Collapsible { get; set; }
    public bool? DefaultCollapsed { get; set; }
}

/// <summary>Row container.</summary>
public class RowNode : LayoutNode
{
    public int? Columns { get; set; }
    public object? Gap { get; set; } // string | number
}

/// <summary>Tabs container.</summary>
public class TabsNode : LayoutNode
{
    public TabsOrientation? Orientation { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TabsOrientation
{
    Top,
    Left,
    Right,
    Bottom
}

/// <summary>Single tab.</summary>
public class TabNode : LayoutNode
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

/// <summary>
/// Field node: basic input (TextInput, NumberInput, etc.) or Autocomplete (requires DataSourceId, OperationId).
/// </summary>
public class FieldNode : LayoutNode
{
    public required string Field { get; set; }
    public required string ComponentId { get; set; }
    /// <summary>Required for Autocomplete component.</summary>
    public string? DataSourceId { get; set; }
    public string? OperationId { get; set; }
    public bool? Required { get; set; }
    public string? Label { get; set; }
    public string? LabelKey { get; set; }
    public string? Placeholder { get; set; }
    public string? PlaceholderKey { get; set; }
    public string? HelpKey { get; set; }
    public bool? Readonly { get; set; }
    public IList<ValidationRuleDefinition>? Validation { get; set; }
    public string? DateFormat { get; set; }
    public string? NumberFormat { get; set; }
}

/// <summary>Data table (entity relation or data-source-backed).</summary>
public class DataTableNode : LayoutNode
{
    public string? Field { get; set; }
    public string? DataSourceId { get; set; }
    public string? OperationId { get; set; }
    public IList<DataTableColumn>? Column { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>Column definition for DataTable.</summary>
public class DataTableColumn
{
    public string? Field { get; set; }
    public string? Label { get; set; }
    public object? Width { get; set; } // string | number
}
