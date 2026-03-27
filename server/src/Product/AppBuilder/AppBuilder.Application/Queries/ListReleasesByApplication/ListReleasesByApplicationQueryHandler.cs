using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListReleasesByApplication;

public sealed class ListReleasesByApplicationQueryHandler
    : IApplicationRequestHandler<ListReleasesByApplicationQuery, Result<List<ApplicationReleaseDto>>>
{
    private readonly IApplicationReleaseRepository _repository;

    public ListReleasesByApplicationQueryHandler(IApplicationReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ApplicationReleaseDto>>> HandleAsync(
        ListReleasesByApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);
        var dtos = list.Select(ApplicationReleaseMapper.ToDto).ToList();
        return Result<List<ApplicationReleaseDto>>.Success(dtos);
    }
}
