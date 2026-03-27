namespace Capabilities.DatabaseSchema.Models;

/// <summary>Represents a database column schema.</summary>
public sealed class ColumnSchema
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SqlDataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? DefaultValue { get; set; }
    public int Order { get; set; }
}
