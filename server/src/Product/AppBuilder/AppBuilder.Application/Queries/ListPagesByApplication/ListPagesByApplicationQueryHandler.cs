using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListPagesByApplication;

public sealed class ListPagesByApplicationQueryHandler
    : IApplicationRequestHandler<ListPagesByApplicationQuery, Result<List<PageDefinitionDto>>>
{
    private readonly IPageDefinitionRepository _repository;

    public ListPagesByApplicationQueryHandler(IPageDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<PageDefinitionDto>>> HandleAsync(
        ListPagesByApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);
        var dtos = list.Select(PageDefinitionMapper.ToDto).ToList();
        return Result<List<PageDefinitionDto>>.Success(dtos);
    }
}
