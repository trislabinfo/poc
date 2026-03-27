namespace MigrationRunner.Services;

/// <summary>
/// Orchestrates running (or rolling back) migrations for all modules in dependency order.
/// </summary>
public interface IMigrationOrchestrator
{
    /// <summary>
    /// Runs all migrations for the current topology in dependency order.
    /// </summary>
    /// <param name="dryRun">If true, only report what would be run without executing.</param>
    Task MigrateAsync(bool dryRun, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the last migration batch (or one step per module).
    /// </summary>
    /// <param name="dryRun">If true, only report what would be rolled back.</param>
    Task RollbackAsync(bool dryRun, CancellationToken cancellationToken = default);
}
