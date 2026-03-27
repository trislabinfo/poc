using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetRelationDefinition;

public sealed class GetRelationDefinitionQueryHandler
    : IApplicationRequestHandler<GetRelationDefinitionQuery, Result<RelationDefinitionDto>>
{
    private readonly IRelationDefinitionRepository _repository;

    public GetRelationDefinitionQueryHandler(IRelationDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<RelationDefinitionDto>> HandleAsync(
        GetRelationDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.RelationId, cancellationToken);
        if (entity == null)
            return Result<RelationDefinitionDto>.Failure(
                Error.NotFound("AppBuilder.RelationNotFound", "Relation definition not found."));
        return Result<RelationDefinitionDto>.Success(RelationDefinitionMapper.ToDto(entity));
    }
}
