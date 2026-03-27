namespace Capabilities.DatabaseSchema.Abstractions;

/// <summary>Generates SQL migration scripts from schema change sets.</summary>
public interface IMigrationScriptGenerator
{
    /// <summary>
    /// Generates a SQL migration script from a change set.
    /// </summary>
    /// <param name="changeSet">Schema changes to apply.</param>
    /// <param name="targetSchema">Target schema (needed to create tables with columns).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SQL script as string.</returns>
    Task<string> GenerateMigrationScriptAsync(
        Models.SchemaChangeSet changeSet,
        Models.DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default);
}
