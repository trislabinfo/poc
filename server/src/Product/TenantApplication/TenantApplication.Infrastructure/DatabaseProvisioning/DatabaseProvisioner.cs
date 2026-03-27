using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using TenantApplication.Domain.DatabaseProvisioning;

namespace TenantApplication.Infrastructure.DatabaseProvisioning;

/// <summary>Provisions PostgreSQL databases for tenant application environments.</summary>
public sealed class DatabaseProvisioner : IDatabaseProvisioner
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseProvisioner> _logger;

    public DatabaseProvisioner(IConfiguration configuration, ILogger<DatabaseProvisioner> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreateDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        // Same key resolution as rest of app (standalone and Aspire)
        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? _configuration.GetConnectionString("dr-development-db")
            ?? _configuration.GetConnectionString("dr-development")
            ?? throw new InvalidOperationException("DefaultConnection (or Aspire dr-development-db) connection string not found.");

        // Parse connection string to get server details
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
        var serverHost = builder.Host;
        var serverPort = builder.Port;
        var userId = builder.Username;
        var password = builder.Password;
        var defaultDatabase = builder.Database ?? "postgres";

        // Connect to default database (postgres) to create the new database
        var adminConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = serverHost,
            Port = serverPort,
            Username = userId,
            Password = password,
            Database = defaultDatabase
        }.ToString();

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Check if database already exists
        var checkDbCommand = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'",
            connection);
        var dbExists = await checkDbCommand.ExecuteScalarAsync(cancellationToken) != null;

        if (dbExists)
        {
            _logger.LogWarning("Database {DatabaseName} already exists.", databaseName);
        }
        else
        {
            // Create the database
            var createDbCommand = new NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\"",
                connection);
            await createDbCommand.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Created database {DatabaseName}.", databaseName);
        }

        // Return connection string for the new database
        var newConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = serverHost,
            Port = serverPort,
            Username = userId,
            Password = password,
            Database = databaseName
        }.ToString();

        return newConnectionString;
    }
}
