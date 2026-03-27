using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Repositories;
using TenantApplicationEntity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Application.Commands.CreateCustomApplication;

public sealed class CreateCustomApplicationCommandHandler
    : IApplicationRequestHandler<CreateCustomApplicationCommand, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCustomApplicationCommandHandler(
        ITenantApplicationRepository repository,
        ITenantApplicationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TenantApplicationDto>> HandleAsync(CreateCustomApplicationCommand request, CancellationToken cancellationToken)
    {
        var slugExists = await _repository.SlugExistsForTenantAsync(request.TenantId, request.Slug.Trim().ToLowerInvariant(), cancellationToken);
        if (slugExists)
            return Result<TenantApplicationDto>.Failure(Error.Conflict("TenantApplication.SlugExists", "An application with this slug already exists for this tenant."));

        var result = TenantApplicationEntity.CreateCustom(
            request.TenantId,
            request.Name,
            request.Slug,
            request.Description,
            _dateTimeProvider);

        if (result.IsFailure) return Result<TenantApplicationDto>.Failure(result.Error);

        var app = result.Value;
        await _repository.AddAsync(app, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TenantApplicationDto>.Success(Map(app));
    }

    private static TenantApplicationDto Map(TenantApplicationEntity a) => new(
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
