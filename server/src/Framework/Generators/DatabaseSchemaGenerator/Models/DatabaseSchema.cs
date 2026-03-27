namespace Capabilities.DatabaseSchema.Models;

/// <summary>Complete database schema model (tables, columns, foreign keys).</summary>
public sealed class DatabaseSchema
{
    public List<TableSchema> Tables { get; set; } = [];
    public List<ForeignKeySchema> ForeignKeys { get; set; } = [];
}
