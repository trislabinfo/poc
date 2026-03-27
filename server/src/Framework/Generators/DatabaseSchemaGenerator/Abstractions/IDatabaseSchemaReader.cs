namespace Capabilities.DatabaseSchema.Abstractions;

/// <summary>Reads database schema from an actual database.</summary>
public interface IDatabaseSchemaReader
{
    /// <summary>
    /// Reads the current schema from a database.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Database schema model representing the actual database structure.</returns>
    Task<Models.DatabaseSchema> ReadSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}
