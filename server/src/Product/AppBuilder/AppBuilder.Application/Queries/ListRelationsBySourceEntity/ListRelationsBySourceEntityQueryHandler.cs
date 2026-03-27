using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListRelationsBySourceEntity;

public sealed class ListRelationsBySourceEntityQueryHandler
    : IApplicationRequestHandler<ListRelationsBySourceEntityQuery, Result<List<RelationDefinitionDto>>>
{
    private readonly IRelationDefinitionRepository _repository;

    public ListRelationsBySourceEntityQueryHandler(IRelationDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<RelationDefinitionDto>>> HandleAsync(
        ListRelationsBySourceEntityQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetBySourceEntityIdAsync(request.SourceEntityId, cancellationToken);
        var dtos = list.Select(RelationDefinitionMapper.ToDto).ToList();
        return Result<List<RelationDefinitionDto>>.Success(dtos);
    }
}
