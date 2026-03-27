namespace Capabilities.DatabaseSchema.Models;

/// <summary>Represents a database table schema.</summary>
public sealed class TableSchema
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ColumnSchema> Columns { get; set; } = [];
    public string? PrimaryKeyColumnName { get; set; }
}
