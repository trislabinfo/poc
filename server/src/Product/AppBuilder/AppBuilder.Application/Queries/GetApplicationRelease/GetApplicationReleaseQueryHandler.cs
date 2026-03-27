using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetApplicationRelease;

public sealed class GetApplicationReleaseQueryHandler
    : IApplicationRequestHandler<GetApplicationReleaseQuery, Result<ApplicationReleaseDto>>
{
    private readonly IApplicationReleaseRepository _repository;

    public GetApplicationReleaseQueryHandler(IApplicationReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ApplicationReleaseDto>> HandleAsync(
        GetApplicationReleaseQuery request,
        CancellationToken cancellationToken)
    {
        var release = await _repository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result<ApplicationReleaseDto>.Failure(
                BuildingBlocks.Kernel.Results.Error.NotFound("AppBuilder.ReleaseNotFound", "Release not found."));
        return Result<ApplicationReleaseDto>.Success(ApplicationReleaseMapper.ToDto(release));
    }
}
