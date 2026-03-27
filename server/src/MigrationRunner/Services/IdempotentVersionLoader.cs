using FluentMigrator.Runner;
using FluentMigrator.Runner.Versioning;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MigrationRunner.Services;

/// <summary>
/// Version loader that does not create the version table or index (they are created idempotently by
/// <see cref="VersionTableEnsurer"/>). Only loads version info from the existing table and updates it.
/// </summary>
internal sealed class IdempotentVersionLoader : IVersionLoader
{
    private readonly string _connectionString;
    private readonly IVersionTableMetaData _metaData;

    public IdempotentVersionLoader(IConfiguration configuration, IVersionTableMetaData metaData)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        _metaData = metaData;
        VersionInfo = new VersionInfo();
    }

    public bool AlreadyCreatedVersionSchema => true;
    public bool AlreadyCreatedVersionTable => true;
    public IMigrationRunner Runner { get; set; } = null!;
    public IVersionInfo VersionInfo { get; set; }
    public IVersionTableMetaData VersionTableMetaData => _metaData;

    public void LoadVersionInfo()
    {
        var schema = string.IsNullOrEmpty(_metaData.SchemaName) ? "public" : _metaData.SchemaName;
        var qualifiedTable = $"\"{schema}\".\"{_metaData.TableName}\"";
        var versionCol = _metaData.ColumnName;

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT \"{versionCol}\" FROM {qualifiedTable}";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var version = reader.GetInt64(0);
            VersionInfo.AddAppliedMigration(version);
        }
    }

    public void DeleteVersion(long version)
    {
        var schema = string.IsNullOrEmpty(_metaData.SchemaName) ? "public" : _metaData.SchemaName;
        var qualifiedTable = $"\"{schema}\".\"{_metaData.TableName}\"";
        var versionCol = _metaData.ColumnName;

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM {qualifiedTable} WHERE \"{versionCol}\" = @v";
        cmd.Parameters.AddWithValue("v", version);
        cmd.ExecuteNonQuery();
    }

    public IVersionTableMetaData GetVersionTableMetaData() => _metaData;

    public void RemoveVersionTable()
    {
        var schema = string.IsNullOrEmpty(_metaData.SchemaName) ? "public" : _metaData.SchemaName;
        var qualifiedTable = $"\"{schema}\".\"{_metaData.TableName}\"";

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {qualifiedTable}";
        cmd.ExecuteNonQuery();
    }

    public void UpdateVersionInfo(long version) => UpdateVersionInfo(version, string.Empty);

    public void UpdateVersionInfo(long version, string description)
    {
        var schema = string.IsNullOrEmpty(_metaData.SchemaName) ? "public" : _metaData.SchemaName;
        var qualifiedTable = $"\"{schema}\".\"{_metaData.TableName}\"";

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            INSERT INTO {qualifiedTable} ("{_metaData.ColumnName}", "{_metaData.DescriptionColumnName}", "{_metaData.AppliedOnColumnName}")
            VALUES (@v, @d, @t)
            """;
        cmd.Parameters.AddWithValue("v", version);
        cmd.Parameters.AddWithValue("d", description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("t", DateTime.UtcNow);
        cmd.ExecuteNonQuery();

        VersionInfo.AddAppliedMigration(version);
    }
}
