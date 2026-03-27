namespace Capabilities.DatabaseSchema.Models;

/// <summary>Represents a foreign key relationship.</summary>
public sealed class ForeignKeySchema
{
    public string Name { get; set; } = string.Empty;
    public string SourceTableName { get; set; } = string.Empty;
    public string SourceColumnName { get; set; } = string.Empty;
    public string TargetTableName { get; set; } = string.Empty;
    public string TargetColumnName { get; set; } = string.Empty;
    public bool CascadeDelete { get; set; }
}
