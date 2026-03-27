using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MigrationRunner.Services;

/// <summary>
/// Ensures the FluentMigrator version table and its unique index exist using idempotent SQL
/// (CREATE TABLE IF NOT EXISTS, CREATE UNIQUE INDEX IF NOT EXISTS) so re-running migrations does not fail.
/// </summary>
internal static class VersionTableEnsurer
{
    public static void EnsureVersionTableAndIndex(
        string connectionString,
        IVersionTableMetaData meta,
        ILogger? logger = null)
    {
        var schema = string.IsNullOrEmpty(meta.SchemaName) ? "public" : meta.SchemaName;
        var tableName = meta.TableName;
        var qualifiedTable = $"\"{schema}\".\"{tableName}\"";
        var versionCol = $"\"{meta.ColumnName}\"";
        var descCol = $"\"{meta.DescriptionColumnName}\"";
        var appliedOnCol = $"\"{meta.AppliedOnColumnName}\"";

        var createTableSql = meta.CreateWithPrimaryKey
            ? $"CREATE TABLE IF NOT EXISTS {qualifiedTable} ({versionCol} bigint NOT NULL, {descCol} varchar(255) NULL, {appliedOnCol} timestamp NULL, PRIMARY KEY ({versionCol}))"
            : $"CREATE TABLE IF NOT EXISTS {qualifiedTable} ({versionCol} bigint NOT NULL, {descCol} varchar(255) NULL, {appliedOnCol} timestamp NULL)";

        var indexName = meta.UniqueIndexName;
        var createIndexSql = $"CREATE UNIQUE INDEX IF NOT EXISTS \"{indexName}\" ON {qualifiedTable} ({versionCol} ASC)";

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = createTableSql;
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = createIndexSql;
            cmd.ExecuteNonQuery();
        }

        logger?.LogDebug("Ensured version table {Table} and index {Index}.", qualifiedTable, indexName);
    }
}
