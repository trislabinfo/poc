using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListNavigationsByApplication;

public sealed class ListNavigationsByApplicationQueryHandler
    : IApplicationRequestHandler<ListNavigationsByApplicationQuery, Result<List<NavigationDefinitionDto>>>
{
    private readonly INavigationDefinitionRepository _repository;

    public ListNavigationsByApplicationQueryHandler(INavigationDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<NavigationDefinitionDto>>> HandleAsync(
        ListNavigationsByApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);
        var dtos = list.Select(NavigationDefinitionMapper.ToDto).ToList();
        return Result<List<NavigationDefinitionDto>>.Success(dtos);
    }
}
