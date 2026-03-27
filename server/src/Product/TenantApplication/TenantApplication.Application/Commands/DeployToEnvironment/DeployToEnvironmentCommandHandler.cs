using AppDefinition.Domain.Entities.Lifecycle;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Capabilities.DatabaseSchema.Abstractions;
using Capabilities.DatabaseSchema.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;
using TenantApplication.Domain.Entities;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.DeployToEnvironment;

public sealed class DeployToEnvironmentCommandHandler
    : IApplicationRequestHandler<DeployToEnvironmentCommand, Result<Guid?>>
{
    private readonly ITenantApplicationEnvironmentRepository _envRepository;
    private readonly ITenantApplicationReleaseRepository _releaseRepository;
    private readonly ITenantApplicationMigrationRepository _migrationRepository;
    private readonly ISchemaComparer _schemaComparer;
    private readonly IDatabaseSchemaReader _databaseSchemaReader;
    private readonly IMigrationScriptGenerator _migrationScriptGenerator;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DeployToEnvironmentCommandHandler> _logger;

    public DeployToEnvironmentCommandHandler(
        ITenantApplicationEnvironmentRepository envRepository,
        ITenantApplicationReleaseRepository releaseRepository,
        ITenantApplicationMigrationRepository migrationRepository,
        ISchemaComparer schemaComparer,
        IDatabaseSchemaReader databaseSchemaReader,
        IMigrationScriptGenerator migrationScriptGenerator,
        ITenantApplicationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<DeployToEnvironmentCommandHandler> logger)
    {
        _envRepository = envRepository;
        _releaseRepository = releaseRepository;
        _migrationRepository = migrationRepository;
        _schemaComparer = schemaComparer;
        _databaseSchemaReader = databaseSchemaReader;
        _migrationScriptGenerator = migrationScriptGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<Guid?>> HandleAsync(DeployToEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var env = await _envRepository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (env == null)
        {
            _logger.LogWarning("Deploy: environment not found. EnvironmentId={EnvironmentId}. Use the Id from Step 7 (Create environment) response.", request.EnvironmentId);
            return Result<Guid?>.Failure(Error.NotFound("TenantApplication.EnvironmentNotFound", "Environment not found. Use the environment Id from Step 7 (Create environment) response."));
        }
        if (env.TenantApplicationId != request.TenantApplicationId)
            return Result<Guid?>.Failure(Error.Validation("TenantApplication.EnvironmentMismatch", "Environment does not belong to this tenant application."));

        var targetRelease = await _releaseRepository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (targetRelease == null)
        {
            _logger.LogWarning("Deploy: release not found. ReleaseId={ReleaseId}. Use the release Id from Step 5 (Create tenant application release) response, not the platform release Id from Step 3.", request.ReleaseId);
            return Result<Guid?>.Failure(Error.NotFound("TenantApplication.ReleaseNotFound", "Release not found. Use the release Id from Step 5 response (tenant application release), not the platform release Id from Step 3."));
        }
        if (targetRelease.AppDefinitionId != request.TenantApplicationId)
            return Result<Guid?>.Failure(Error.Validation("TenantApplication.ReleaseMismatch", "Release does not belong to this tenant application."));

        // Check if release DDL scripts are approved
        if (targetRelease.DdlScriptsStatus != DdlScriptStatus.Approved)
            return Result<Guid?>.Failure(Error.Validation("TenantApplication.ReleaseNotApproved", "Release DDL scripts must be approved before deployment."));

        // Check if environment has existing deployment.
        // Idempotency: first run applies DDL and sets ApplicationReleaseId; subsequent runs do not re-run initial DDL
        // but create a migration record for review (schema compare → migration script → return MigrationId).
        if (env.ApplicationReleaseId.HasValue)
        {
            // Compare actual DB schema with target release schema
            if (string.IsNullOrWhiteSpace(env.ConnectionString))
                return Result<Guid?>.Failure(Error.Validation("TenantApplication.NoConnectionString", "Environment does not have a connection string."));

            try
            {
                // Read actual database schema
                var actualSchema = await _databaseSchemaReader.ReadSchemaAsync(env.ConnectionString, cancellationToken);

                // Load target release schema from SchemaJson
                DatabaseSchema targetSchema;
                if (!string.IsNullOrWhiteSpace(targetRelease.SchemaJson) && targetRelease.SchemaJson != "{}")
                {
                    targetSchema = JsonSerializer.Deserialize<DatabaseSchema>(targetRelease.SchemaJson)
                        ?? throw new InvalidOperationException("Failed to deserialize target release schema.");
                }
                else
                {
                    return Result<Guid?>.Failure(Error.Validation("TenantApplication.NoSchema", "Target release does not have a schema."));
                }

                // Compare schemas and generate diff
                var changeSet = await _schemaComparer.CompareAsync(actualSchema, targetSchema, cancellationToken);

                // Generate migration script from diff
                var migrationScript = await _migrationScriptGenerator.GenerateMigrationScriptAsync(changeSet, targetSchema, cancellationToken);

                // Create migration record
                var migrationResult = TenantApplicationMigration.Create(
                    env.Id,
                    env.ApplicationReleaseId,
                    request.ReleaseId,
                    migrationScript);

                if (migrationResult.IsFailure)
                    return Result<Guid?>.Failure(migrationResult.Error);

                var migration = migrationResult.Value;
                await _migrationRepository.AddAsync(migration, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Created migration {MigrationId} for environment {EnvironmentId} from release {FromReleaseId} to {ToReleaseId}.",
                    migration.Id, env.Id, env.ApplicationReleaseId, request.ReleaseId);

                // Return migration ID for review (deployment is not complete yet)
                return Result<Guid?>.Success(migration.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compare schemas for deployment.");
                return Result<Guid?>.Failure(Error.Failure("TenantApplication.SchemaComparisonFailed", $"Schema comparison failed: {ex.Message}"));
            }
        }
        else
        {
            // First deployment - apply complete DDL scripts directly
            if (string.IsNullOrWhiteSpace(env.ConnectionString))
                return Result<Guid?>.Failure(Error.Validation("TenantApplication.NoConnectionString", "Environment does not have a connection string."));

            try
            {
                // Load DDL scripts from release
                if (string.IsNullOrWhiteSpace(targetRelease.DdlScriptsJson) || targetRelease.DdlScriptsJson == "{}")
                    return Result<Guid?>.Failure(Error.Validation("TenantApplication.NoDdlScripts", "Release does not have DDL scripts."));

                var ddlScript = JsonSerializer.Deserialize<DdlScript>(targetRelease.DdlScriptsJson);
                if (ddlScript == null || string.IsNullOrWhiteSpace(ddlScript.CompleteScript))
                    return Result<Guid?>.Failure(Error.Validation("TenantApplication.InvalidDdlScripts", "Release DDL scripts are invalid."));

                var completeScript = ddlScript.CompleteScript.Trim();
                if (string.IsNullOrWhiteSpace(completeScript))
                    return Result<Guid?>.Failure(Error.Validation("TenantApplication.InvalidDdlScripts", "Release DDL CompleteScript is empty."));

                // Execute DDL scripts (full script in one command; PostgreSQL/Npgsql support multiple statements)
                await using var connection = new NpgsqlConnection(env.ConnectionString);
                await connection.OpenAsync(cancellationToken);

                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                try
                {
                    await using var command = new NpgsqlCommand(completeScript, connection, transaction);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogInformation(
                        "Applied initial DDL scripts to environment {EnvironmentId} (database: {DatabaseName}).",
                        env.Id,
                        env.DatabaseName ?? "(unknown)");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }

                // Update environment's deployed release
                env.DeployRelease(request.ReleaseId, request.Version, request.DeployedBy, _dateTimeProvider);
                _envRepository.Update(env);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid?>.Success(null); // No migration needed for first deployment
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply DDL scripts for first deployment.");
                return Result<Guid?>.Failure(Error.Failure("TenantApplication.DdlScriptExecutionFailed", $"DDL script execution failed: {ex.Message}"));
            }
        }
    }
}
