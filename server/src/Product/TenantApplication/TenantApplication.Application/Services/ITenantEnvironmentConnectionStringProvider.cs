namespace TenantApplication.Application.Services;

/// <summary>
/// Provides the connection string for a tenant application environment.
/// Uses stored value if present; otherwise derives from default connection with Database = {tenantSlug}-{appSlug}-{environment}.
/// </summary>
public interface ITenantEnvironmentConnectionStringProvider
{
    /// <summary>
    /// Returns the connection string: <paramref name="storedConnectionString"/> if not null/empty;
    /// otherwise builds from default connection with Database = <paramref name="databaseName"/> or {tenantSlug}-{appSlug}-{environmentName}.
    /// </summary>
    string? GetConnectionString(string? storedConnectionString, string? databaseName, string tenantSlug, string appSlug, string environmentName);
}
