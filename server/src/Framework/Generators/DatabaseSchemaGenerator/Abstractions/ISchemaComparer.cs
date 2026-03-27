namespace Capabilities.DatabaseSchema.Abstractions;

/// <summary>Compares two database schemas and produces a change set.</summary>
public interface ISchemaComparer
{
    /// <summary>
    /// Compares two schemas and returns the differences.
    /// </summary>
    /// <param name="sourceSchema">Source schema (current state).</param>
    /// <param name="targetSchema">Target schema (desired state).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Change set describing differences.</returns>
    Task<Models.SchemaChangeSet> CompareAsync(
        Models.DatabaseSchema sourceSchema,
        Models.DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares actual database schema with target schema.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="targetSchema">Target schema (desired state).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Change set describing differences.</returns>
    Task<Models.SchemaChangeSet> CompareWithDatabaseAsync(
        string connectionString,
        Models.DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default);
}
