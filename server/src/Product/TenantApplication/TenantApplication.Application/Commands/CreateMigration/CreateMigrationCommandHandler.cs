using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Capabilities.DatabaseSchema.Abstractions;
using Capabilities.DatabaseSchema.Models;
using System.Text.Json;
using TenantApplication.Domain.Entities;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.CreateMigration;

public sealed class CreateMigrationCommandHandler
    : IApplicationRequestHandler<CreateMigrationCommand, Result<Guid>>
{
    private readonly ITenantApplicationEnvironmentRepository _envRepository;
    private readonly ITenantApplicationReleaseRepository _releaseRepository;
    private readonly ITenantApplicationMigrationRepository _migrationRepository;
    private readonly ISchemaComparer _schemaComparer;
    private readonly IMigrationScriptGenerator _scriptGenerator;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;

    public CreateMigrationCommandHandler(
        ITenantApplicationEnvironmentRepository envRepository,
        ITenantApplicationReleaseRepository releaseRepository,
        ITenantApplicationMigrationRepository migrationRepository,
        ISchemaComparer schemaComparer,
        IMigrationScriptGenerator scriptGenerator,
        ITenantApplicationUnitOfWork unitOfWork)
    {
        _envRepository = envRepository;
        _releaseRepository = releaseRepository;
        _migrationRepository = migrationRepository;
        _schemaComparer = schemaComparer;
        _scriptGenerator = scriptGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(CreateMigrationCommand request, CancellationToken cancellationToken)
    {
        var env = await _envRepository.GetByIdAsync(request.TenantApplicationEnvironmentId, cancellationToken);
        if (env == null)
            return Result<Guid>.Failure(Error.NotFound("TenantApplication.EnvironmentNotFound", "Environment not found."));

        // Load releases to get schemas
        var toRelease = await _releaseRepository.GetByIdAsync(request.ToReleaseId, cancellationToken);
        if (toRelease == null)
            return Result<Guid>.Failure(Error.NotFound("TenantApplication.ReleaseNotFound", "Target release not found."));

        DatabaseSchema? fromSchema = null;
        if (request.FromReleaseId.HasValue)
        {
            var fromRelease = await _releaseRepository.GetByIdAsync(request.FromReleaseId.Value, cancellationToken);
            if (fromRelease == null)
                return Result<Guid>.Failure(Error.NotFound("TenantApplication.ReleaseNotFound", "Source release not found."));

            if (!string.IsNullOrWhiteSpace(fromRelease.SchemaJson) && fromRelease.SchemaJson != "{}")
            {
                fromSchema = JsonSerializer.Deserialize<DatabaseSchema>(fromRelease.SchemaJson);
            }
        }

        // If no from release, start with empty schema
        fromSchema ??= new DatabaseSchema();

        // Deserialize to release schema
        DatabaseSchema toSchema;
        if (!string.IsNullOrWhiteSpace(toRelease.SchemaJson) && toRelease.SchemaJson != "{}")
        {
            toSchema = JsonSerializer.Deserialize<DatabaseSchema>(toRelease.SchemaJson)
                ?? throw new InvalidOperationException("Failed to deserialize target release schema.");
        }
        else
        {
            return Result<Guid>.Failure(Error.Validation("TenantApplication.NoSchema", "Target release does not have a schema. Create a release first."));
        }

        // Generate migration script
        string migrationScript;
        if (!string.IsNullOrWhiteSpace(request.MigrationScriptJson) && request.MigrationScriptJson != "{}")
        {
            // User provided custom script
            migrationScript = request.MigrationScriptJson;
        }
        else
        {
            // Auto-generate script from schema comparison
            var changeSet = await _schemaComparer.CompareAsync(fromSchema, toSchema, cancellationToken);
            migrationScript = await _scriptGenerator.GenerateMigrationScriptAsync(changeSet, toSchema, cancellationToken);
        }

        var result = TenantApplicationMigration.Create(
            request.TenantApplicationEnvironmentId,
            request.FromReleaseId,
            request.ToReleaseId,
            migrationScript);
        if (result.IsFailure) return Result<Guid>.Failure(result.Error);

        var migration = result.Value;
        await _migrationRepository.AddAsync(migration, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(migration.Id);
    }
}
