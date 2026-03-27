using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListRelationsByTargetEntity;

public sealed class ListRelationsByTargetEntityQueryHandler
    : IApplicationRequestHandler<ListRelationsByTargetEntityQuery, Result<List<RelationDefinitionDto>>>
{
    private readonly IRelationDefinitionRepository _repository;

    public ListRelationsByTargetEntityQueryHandler(IRelationDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<RelationDefinitionDto>>> HandleAsync(
        ListRelationsByTargetEntityQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByTargetEntityIdAsync(request.TargetEntityId, cancellationToken);
        var dtos = list.Select(RelationDefinitionMapper.ToDto).ToList();
        return Result<List<RelationDefinitionDto>>.Success(dtos);
    }
}
