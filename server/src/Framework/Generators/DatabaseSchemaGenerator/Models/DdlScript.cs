namespace Capabilities.DatabaseSchema.Models;

/// <summary>Represents a DDL script for database schema creation/modification.</summary>
public sealed class DdlScript
{
    public string CreateTablesScript { get; set; } = string.Empty;
    public string CreateIndexesScript { get; set; } = string.Empty;
    public string CreateForeignKeysScript { get; set; } = string.Empty;
    public string CompleteScript { get; set; } = string.Empty; // Combined script
    public DateTime GeneratedAt { get; set; }
    public string Version { get; set; } = string.Empty;
}
