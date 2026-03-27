using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListEntitiesByApplication;

public sealed class ListEntitiesByApplicationQueryHandler
    : IApplicationRequestHandler<ListEntitiesByApplicationQuery, Result<List<EntityDefinitionDto>>>
{
    private readonly IEntityDefinitionRepository _repository;

    public ListEntitiesByApplicationQueryHandler(IEntityDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<EntityDefinitionDto>>> HandleAsync(
        ListEntitiesByApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);
        var dtos = list.Select(EntityDefinitionMapper.ToDto).ToList();
        return Result<List<EntityDefinitionDto>>.Success(dtos);
    }
}
