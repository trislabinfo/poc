using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Queries.ListEnvironmentsByTenantApplication;

public sealed class ListEnvironmentsByTenantApplicationQueryHandler
    : IApplicationRequestHandler<ListEnvironmentsByTenantApplicationQuery, Result<IReadOnlyList<TenantApplicationEnvironmentDto>>>
{
    private readonly ITenantApplicationEnvironmentRepository _repository;

    public ListEnvironmentsByTenantApplicationQueryHandler(ITenantApplicationEnvironmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TenantApplicationEnvironmentDto>>> HandleAsync(
        ListEnvironmentsByTenantApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByTenantApplicationAsync(request.TenantApplicationId, cancellationToken);
        var dtos = list.Select(e => new TenantApplicationEnvironmentDto(
            e.Id,
            e.TenantApplicationId,
            e.Name,
            e.EnvironmentType,
            e.ApplicationReleaseId,
            e.ReleaseVersion,
            e.IsActive,
            e.DeployedAt,
            e.CreatedAt)).ToList();
        return Result<IReadOnlyList<TenantApplicationEnvironmentDto>>.Success(dtos);
    }
}
