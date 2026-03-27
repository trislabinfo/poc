using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.MigrationExecution;

/// <summary>Executes migration scripts against environment databases.</summary>
public interface IMigrationExecutor
{
    /// <summary>
    /// Executes a migration script against the target environment's database.
    /// </summary>
    /// <param name="migrationId">ID of the migration to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ExecuteMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default);
}
