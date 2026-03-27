using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetReleaseSnapshot;

public sealed class GetReleaseSnapshotQueryHandler
    : IApplicationRequestHandler<GetReleaseSnapshotQuery, Result<ApplicationSnapshotDto>>
{
    private readonly IApplicationReleaseRepository _repository;

    public GetReleaseSnapshotQueryHandler(IApplicationReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ApplicationSnapshotDto>> HandleAsync(
        GetReleaseSnapshotQuery request,
        CancellationToken cancellationToken)
    {
        var release = await _repository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result<ApplicationSnapshotDto>.Failure(
                Error.NotFound("AppBuilder.ReleaseNotFound", "Release not found."));
        return Result<ApplicationSnapshotDto>.Success(ApplicationReleaseMapper.ToSnapshotDto(release));
    }
}
