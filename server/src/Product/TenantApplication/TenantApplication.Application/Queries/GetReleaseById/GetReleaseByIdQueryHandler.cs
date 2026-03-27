using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Queries.GetReleaseById;

public sealed class GetReleaseByIdQueryHandler
    : IApplicationRequestHandler<GetReleaseByIdQuery, Result<ApplicationReleaseDto>>
{
    private readonly ITenantApplicationReleaseRepository _repository;

    public GetReleaseByIdQueryHandler(ITenantApplicationReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ApplicationReleaseDto>> HandleAsync(
        GetReleaseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var release = await _repository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result<ApplicationReleaseDto>.Failure(
                Error.NotFound("TenantApplication.ReleaseNotFound", "Release not found."));

        if (release.AppDefinitionId != request.TenantApplicationId)
            return Result<ApplicationReleaseDto>.Failure(
                Error.Validation("TenantApplication.ReleaseMismatch", "Release does not belong to this tenant application."));

        return Result<ApplicationReleaseDto>.Success(ApplicationReleaseMapper.ToDto(release));
    }
}
