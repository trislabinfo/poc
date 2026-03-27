using AppBuilder.Domain.Repositories;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Capabilities.DatabaseSchema.Abstractions;
using System.Text.Json;

namespace AppBuilder.Application.Commands.CreateApplicationRelease;

public sealed class CreateApplicationReleaseCommandHandler
    : IApplicationRequestHandler<CreateApplicationReleaseCommand, Result<Guid>>
{
    private readonly IAppDefinitionRepository _appRepository;
    private readonly IApplicationReleaseRepository _releaseRepository;
    private readonly IEntityDefinitionRepository _entityRepository;
    private readonly IPropertyDefinitionRepository _propertyRepository;
    private readonly IRelationDefinitionRepository _relationRepository;
    private readonly INavigationDefinitionRepository _navigationRepository;
    private readonly IPageDefinitionRepository _pageRepository;
    private readonly IDataSourceDefinitionRepository _dataSourceRepository;
    private readonly ISchemaDeriver _schemaDeriver;
    private readonly IDdlScriptGenerator _ddlScriptGenerator;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateApplicationReleaseCommandHandler(
        IAppDefinitionRepository appRepository,
        IApplicationReleaseRepository releaseRepository,
        IEntityDefinitionRepository entityRepository,
        IPropertyDefinitionRepository propertyRepository,
        IRelationDefinitionRepository relationRepository,
        INavigationDefinitionRepository navigationRepository,
        IPageDefinitionRepository pageRepository,
        IDataSourceDefinitionRepository dataSourceRepository,
        ISchemaDeriver schemaDeriver,
        IDdlScriptGenerator ddlScriptGenerator,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _appRepository = appRepository;
        _releaseRepository = releaseRepository;
        _entityRepository = entityRepository;
        _propertyRepository = propertyRepository;
        _relationRepository = relationRepository;
        _navigationRepository = navigationRepository;
        _pageRepository = pageRepository;
        _dataSourceRepository = dataSourceRepository;
        _schemaDeriver = schemaDeriver;
        _ddlScriptGenerator = ddlScriptGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateApplicationReleaseCommand request, CancellationToken cancellationToken)
    {
        var app = await _appRepository.GetByIdAsync(request.AppDefinitionId, cancellationToken);
        if (app == null)
            return Result<Guid>.Failure(Error.NotFound("AppBuilder.ApplicationNotFound", "Application definition not found."));

        var version = $"{request.Major}.{request.Minor}.{request.Patch}";
        var navList = await _navigationRepository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);
        var pageList = await _pageRepository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);
        var dataSourceList = await _dataSourceRepository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);
        var entityList = await _entityRepository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);

        var entitySnapshots = new List<object>();
        var propertiesByEntityId = new Dictionary<Guid, List<PropertyDefinition>>();
        foreach (var entity in entityList)
        {
            var properties = await _propertyRepository.GetByEntityDefinitionIdAsync(entity.Id, cancellationToken);
            propertiesByEntityId[entity.Id] = properties;
            entitySnapshots.Add(new { Entity = entity, Properties = properties });
        }
        var relationSnapshots = new List<RelationDefinition>();
        foreach (var entity in entityList)
        {
            var rels = await _relationRepository.GetBySourceEntityIdAsync(entity.Id, cancellationToken);
            relationSnapshots.AddRange(rels);
        }

        // Derive schema from entities/properties/relations
        var schema = await _schemaDeriver.DeriveSchemaAsync(entityList, propertiesByEntityId, relationSnapshots, cancellationToken);
        var schemaJson = JsonSerializer.Serialize(schema);

        // Generate complete DDL scripts for the schema
        var ddlScript = await _ddlScriptGenerator.GenerateDdlScriptAsync(schema, version, cancellationToken);
        var ddlScriptsJson = JsonSerializer.Serialize(ddlScript);

        var navigationJson = JsonSerializer.Serialize(navList);
        var pageJson = JsonSerializer.Serialize(pageList);
        var dataSourceJson = JsonSerializer.Serialize(dataSourceList);
        var entityJson = JsonSerializer.Serialize(entitySnapshots);
        // Relation data could be included in entity snapshot; for simplicity we append relations
        var relationJson = JsonSerializer.Serialize(relationSnapshots);
        var fullEntityJson = JsonSerializer.Serialize(new { Entities = entitySnapshots, Relations = relationSnapshots });

        var releaseResult = app.CreateRelease(
            version,
            request.Major,
            request.Minor,
            request.Patch,
            request.ReleaseNotes ?? string.Empty,
            navigationJson,
            pageJson,
            dataSourceJson,
            fullEntityJson,
            schemaJson,
            ddlScriptsJson,
            request.ReleasedBy,
            _dateTimeProvider);

        if (releaseResult.IsFailure)
            return Result<Guid>.Failure(releaseResult.Error);

        var release = releaseResult.Value;
        await _releaseRepository.AddAsync(release, cancellationToken);
        _appRepository.Update(app);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(release.Id);
    }
}
