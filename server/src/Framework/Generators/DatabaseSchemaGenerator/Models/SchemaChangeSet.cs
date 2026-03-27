namespace Capabilities.DatabaseSchema.Models;

/// <summary>Represents changes between two database schemas.</summary>
public sealed class SchemaChangeSet
{
    public List<TableChange> TableChanges { get; set; } = [];
    public List<ColumnChange> ColumnChanges { get; set; } = [];
    public List<ForeignKeyChange> ForeignKeyChanges { get; set; } = [];
}

/// <summary>Represents a change to a table.</summary>
public sealed class TableChange
{
    public TableChangeType ChangeType { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

/// <summary>Represents a change to a column.</summary>
public sealed class ColumnChange
{
    public ColumnChangeType ChangeType { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string? NewSqlDataType { get; set; }
    public bool? NewIsNullable { get; set; }
    public string? OldColumnName { get; set; }
}

/// <summary>Represents a change to a foreign key.</summary>
public sealed class ForeignKeyChange
{
    public ForeignKeyChangeType ChangeType { get; set; }
    public string ForeignKeyName { get; set; } = string.Empty;
    public string SourceTableName { get; set; } = string.Empty;
    public string TargetTableName { get; set; } = string.Empty;
}

public enum TableChangeType
{
    Added,
    Removed
}

public enum ColumnChangeType
{
    Added,
    Removed,
    Modified,
    Renamed
}

public enum ForeignKeyChangeType
{
    Added,
    Removed
}
