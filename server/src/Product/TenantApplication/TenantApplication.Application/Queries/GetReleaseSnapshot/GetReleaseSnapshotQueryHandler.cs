using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Queries.GetReleaseSnapshot;

public sealed class GetReleaseSnapshotQueryHandler
    : IApplicationRequestHandler<GetReleaseSnapshotQuery, Result<ApplicationSnapshotDto>>
{
    private readonly ITenantApplicationReleaseRepository _repository;

    public GetReleaseSnapshotQueryHandler(ITenantApplicationReleaseRepository repository)
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
                Error.NotFound("TenantApplication.ReleaseNotFound", "Release not found."));
        return Result<ApplicationSnapshotDto>.Success(ApplicationReleaseMapper.ToSnapshotDto(release));
    }
}
