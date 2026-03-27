using AppDefinition.Domain.Entities.Lifecycle;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Capabilities.DatabaseSchema.Abstractions;
using System.Text.Json;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.CreateTenantApplicationRelease;

public sealed class CreateTenantApplicationReleaseCommandHandler
    : IApplicationRequestHandler<CreateTenantApplicationReleaseCommand, Result<Guid>>
{
    private readonly ITenantApplicationRepository _appRepository;
    private readonly ITenantApplicationReleaseRepository _releaseRepository;
    private readonly ITenantDefinitionSnapshotReader _snapshotReader;
    private readonly ISchemaDeriver _schemaDeriver;
    private readonly IDdlScriptGenerator _ddlScriptGenerator;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTenantApplicationReleaseCommandHandler(
        ITenantApplicationRepository appRepository,
        ITenantApplicationReleaseRepository releaseRepository,
        ITenantDefinitionSnapshotReader snapshotReader,
        ISchemaDeriver schemaDeriver,
        IDdlScriptGenerator ddlScriptGenerator,
        ITenantApplicationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _appRepository = appRepository;
        _releaseRepository = releaseRepository;
        _snapshotReader = snapshotReader;
        _schemaDeriver = schemaDeriver;
        _ddlScriptGenerator = ddlScriptGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateTenantApplicationReleaseCommand request, CancellationToken cancellationToken)
    {
        var app = await _appRepository.GetByIdAsync(request.TenantApplicationId, cancellationToken);
        if (app == null)
            return Result<Guid>.Failure(Error.NotFound("TenantApplication.NotFound", "Tenant application not found."));

        var (navJson, pageJson, dataSourceJson, entityJson) = await _snapshotReader.GetSnapshotAsync(request.TenantApplicationId, cancellationToken);
        var (entities, propertiesByEntityId, relations) = await _snapshotReader.GetSchemaDataAsync(request.TenantApplicationId, cancellationToken);

        // Derive schema from entities/properties/relations
        var schema = await _schemaDeriver.DeriveSchemaAsync(entities, propertiesByEntityId, relations, cancellationToken);
        var schemaJson = JsonSerializer.Serialize(schema);

        var version = $"{request.Major}.{request.Minor}.{request.Patch}";

        // Generate complete DDL scripts for the schema
        var ddlScript = await _ddlScriptGenerator.GenerateDdlScriptAsync(schema, version, cancellationToken);
        var ddlScriptsJson = JsonSerializer.Serialize(ddlScript);

        var releaseResult = ApplicationRelease.Create(
            request.TenantApplicationId,
            version,
            request.Major,
            request.Minor,
            request.Patch,
            request.ReleaseNotes ?? string.Empty,
            navJson,
            pageJson,
            dataSourceJson,
            entityJson,
            schemaJson,
            ddlScriptsJson,
            request.ReleasedBy,
            _dateTimeProvider);

        if (releaseResult.IsFailure) return Result<Guid>.Failure(releaseResult.Error);

        var release = releaseResult.Value;
        await _releaseRepository.AddAsync(release, cancellationToken);
        app.SetReleaseInfo(release.Id, null, request.Major, request.Minor, request.Patch);
        _appRepository.Update(app);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(release.Id);
    }
}
