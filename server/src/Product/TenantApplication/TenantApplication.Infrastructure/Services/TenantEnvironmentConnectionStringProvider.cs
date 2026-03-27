using Microsoft.Extensions.Configuration;
using Npgsql;
using TenantApplication.Application.Services;

namespace TenantApplication.Infrastructure.Services;

/// <summary>
/// Provides environment connection string: stored value or derived from default connection with Database = {tenantSlug}-{appSlug}-{environment}.
/// Uses same connection key resolution as the rest of the app (DefaultConnection, dr-development-db, dr-development).
/// </summary>
public sealed class TenantEnvironmentConnectionStringProvider : ITenantEnvironmentConnectionStringProvider
{
    private readonly IConfiguration _configuration;

    public TenantEnvironmentConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string? GetConnectionString(string? storedConnectionString, string? databaseName, string tenantSlug, string appSlug, string environmentName)
    {
        if (!string.IsNullOrWhiteSpace(storedConnectionString))
            return storedConnectionString;

        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? _configuration.GetConnectionString("dr-development-db")
            ?? _configuration.GetConnectionString("dr-development");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
            return null;

        var dbName = !string.IsNullOrWhiteSpace(databaseName)
            ? databaseName
            : $"{tenantSlug}-{appSlug}-{NormalizeEnvironmentName(environmentName)}";

        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = dbName
        };
        return builder.ToString();
    }

    private static string NormalizeEnvironmentName(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName)) return "production";
        return environmentName.Trim().ToLowerInvariant().Replace(" ", "-");
    }
}
