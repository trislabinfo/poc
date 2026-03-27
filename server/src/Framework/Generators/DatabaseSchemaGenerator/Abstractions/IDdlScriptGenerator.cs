namespace Capabilities.DatabaseSchema.Abstractions;

/// <summary>Generates complete DDL scripts for a database schema.</summary>
public interface IDdlScriptGenerator
{
    /// <summary>
    /// Generates complete DDL scripts for a schema (CREATE TABLE, CREATE INDEX, FOREIGN KEYS, etc.).
    /// </summary>
    /// <param name="schema">Database schema to generate DDL for.</param>
    /// <param name="version">Version identifier for the script.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>DDL script containing all statements needed to create the schema.</returns>
    Task<Models.DdlScript> GenerateDdlScriptAsync(
        Models.DatabaseSchema schema,
        string version,
        CancellationToken cancellationToken = default);
}
