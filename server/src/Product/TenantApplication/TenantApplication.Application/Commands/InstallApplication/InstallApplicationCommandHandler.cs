using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Application.Services;
using TenantApplication.Domain.Repositories;
using Entity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Application.Commands.InstallApplication;

public sealed class InstallApplicationCommandHandler
    : IApplicationRequestHandler<InstallApplicationCommand, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;
    private readonly ICopyDefinitionsFromPlatformReleaseService _copyDefinitionsService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InstallApplicationCommandHandler(
        ITenantApplicationRepository repository,
        ITenantApplicationUnitOfWork unitOfWork,
        ICopyDefinitionsFromPlatformReleaseService copyDefinitionsService,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _copyDefinitionsService = copyDefinitionsService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TenantApplicationDto>> HandleAsync(InstallApplicationCommand request, CancellationToken cancellationToken)
    {
        var slugExists = await _repository.SlugExistsForTenantAsync(request.TenantId, request.Slug.Trim().ToLowerInvariant(), cancellationToken);
        if (slugExists)
            return Result<TenantApplicationDto>.Failure(Error.Conflict("TenantApplication.SlugExists", "An application with this slug already exists for this tenant."));

        var result = Entity.InstallFromPlatform(
            request.TenantId,
            request.ApplicationReleaseId,
            request.Name,
            request.Slug,
            _dateTimeProvider);

        if (result.IsFailure) return Result<TenantApplicationDto>.Failure(result.Error);

        var app = result.Value;
        if (request.ConfigurationJson != null)
            app.UpdateConfiguration(request.ConfigurationJson, _dateTimeProvider);

        await _repository.AddAsync(app, cancellationToken);

        var copyResult = await _copyDefinitionsService.CopyAsync(app.Id, request.ApplicationReleaseId, cancellationToken);
        if (copyResult.IsFailure)
            return Result<TenantApplicationDto>.Failure(copyResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TenantApplicationDto>.Success(Map(app));
    }

    private static TenantApplicationDto Map(Entity a) => new(
        a.Id,
        a.TenantId,
        a.ApplicationReleaseId,
        a.ApplicationId,
        a.Major,
        a.Minor,
        a.Patch,
        a.Name,
        a.Slug,
        a.Description,
        a.IsCustom,
        a.SourceApplicationReleaseId,
        a.Status,
        a.ConfigurationJson,
        a.InstalledAt,
        a.ActivatedAt,
        a.CreatedAt);
}
