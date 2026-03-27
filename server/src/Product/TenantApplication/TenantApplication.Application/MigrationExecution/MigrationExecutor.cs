using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Microsoft.Extensions.Logging;
using Npgsql;
using TenantApplication.Domain.Enums;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.MigrationExecution;

/// <summary>Executes PostgreSQL migration scripts against environment databases.</summary>
public sealed class MigrationExecutor : IMigrationExecutor
{
    private readonly ITenantApplicationMigrationRepository _migrationRepository;
    private readonly ITenantApplicationEnvironmentRepository _environmentRepository;
    private readonly ITenantApplicationReleaseRepository _releaseRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<MigrationExecutor> _logger;

    public MigrationExecutor(
        ITenantApplicationMigrationRepository migrationRepository,
        ITenantApplicationEnvironmentRepository environmentRepository,
        ITenantApplicationReleaseRepository releaseRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<MigrationExecutor> logger)
    {
        _migrationRepository = migrationRepository;
        _environmentRepository = environmentRepository;
        _releaseRepository = releaseRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result> ExecuteMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default)
    {
        var migration = await _migrationRepository.GetByIdAsync(migrationId, cancellationToken);
        if (migration == null)
            return Result.Failure(Error.NotFound("TenantApplication.MigrationNotFound", "Migration not found."));

        if (migration.Status != MigrationStatus.Approved)
            return Result.Failure(Error.Validation("TenantApplication.MigrationNotApproved", "Only approved migrations can be executed."));

        // Mark as executing
        migration.MarkExecuting(_dateTimeProvider);
        _migrationRepository.Update(migration);

        var release = await _releaseRepository.GetByIdAsync(migration.ToReleaseId, cancellationToken);
        if (release == null)
            return Result.Failure(Error.NotFound("TenantApplication.ReleaseNotFound", "Target release not found."));

        var environment = await _environmentRepository.GetByIdAsync(migration.TenantApplicationEnvironmentId, cancellationToken);
        if (environment == null)
            return Result.Failure(Error.NotFound("TenantApplication.EnvironmentNotFound", "Environment not found."));

        if (string.IsNullOrWhiteSpace(environment.ConnectionString))
            return Result.Failure(Error.Validation("TenantApplication.NoConnectionString", "Environment does not have a connection string."));

        if (string.IsNullOrWhiteSpace(migration.MigrationScriptJson) || migration.MigrationScriptJson == "{}")
            return Result.Failure(Error.Validation("TenantApplication.NoMigrationScript", "Migration does not have a script to execute."));

        try
        {
            // Execute SQL script
            await using var connection = new NpgsqlConnection(environment.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Split script into individual statements (simple approach - split by semicolon)
            var statements = migration.MigrationScriptJson
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) && !s.StartsWith("--", StringComparison.Ordinal))
                .ToList();

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var statement in statements)
                {
                    if (string.IsNullOrWhiteSpace(statement) || statement.StartsWith("--", StringComparison.Ordinal))
                        continue;

                    await using var command = new NpgsqlCommand(statement, connection, transaction);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    _logger.LogDebug("Executed migration statement: {Statement}", statement.Substring(0, Math.Min(100, statement.Length)));
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Successfully executed migration {MigrationId}.", migrationId);

                // Mark migration as completed
                migration.MarkCompleted(_dateTimeProvider);
                _migrationRepository.Update(migration);

                // Update environment's deployed release
                environment.DeployRelease(migration.ToReleaseId, release.Version, migration.ApprovedBy ?? Guid.Empty, _dateTimeProvider);
                _environmentRepository.Update(environment);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute migration {MigrationId}.", migrationId);
            migration.MarkFailed(ex.Message, _dateTimeProvider);
            _migrationRepository.Update(migration);
            return Result.Failure(Error.Failure("TenantApplication.MigrationExecutionFailed", $"Migration execution failed: {ex.Message}"));
        }

        return Result.Success();
    }
}
