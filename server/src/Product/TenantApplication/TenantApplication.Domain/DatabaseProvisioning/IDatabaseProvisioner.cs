namespace TenantApplication.Domain.DatabaseProvisioning;

/// <summary>Provisions databases for tenant application environments.</summary>
public interface IDatabaseProvisioner
{
    /// <summary>
    /// Creates a new database for an environment.
    /// </summary>
    /// <param name="databaseName">Name of the database to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection string for the created database.</returns>
    Task<string> CreateDatabaseAsync(string databaseName, CancellationToken cancellationToken = default);
}
