using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Queries.ListReleasesByTenantApplication;

public sealed class ListReleasesByTenantApplicationQueryHandler
    : IApplicationRequestHandler<ListReleasesByTenantApplicationQuery, Result<IReadOnlyList<ApplicationReleaseDto>>>
{
    private readonly ITenantApplicationReleaseRepository _repository;

    public ListReleasesByTenantApplicationQueryHandler(ITenantApplicationReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ApplicationReleaseDto>>> HandleAsync(
        ListReleasesByTenantApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByTenantApplicationIdAsync(request.TenantApplicationId, cancellationToken);
        var dtos = list.Select(ApplicationReleaseMapper.ToDto).ToList();
        return Result<IReadOnlyList<ApplicationReleaseDto>>.Success(dtos);
    }
}
