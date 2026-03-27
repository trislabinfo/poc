using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetEntityDefinition;

public sealed class GetEntityDefinitionQueryHandler
    : IApplicationRequestHandler<GetEntityDefinitionQuery, Result<EntityDefinitionDto>>
{
    private readonly IEntityDefinitionRepository _repository;

    public GetEntityDefinitionQueryHandler(IEntityDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<EntityDefinitionDto>> HandleAsync(
        GetEntityDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.EntityId, cancellationToken);
        if (entity == null)
            return Result<EntityDefinitionDto>.Failure(
                Error.NotFound("AppBuilder.EntityNotFound", "Entity definition not found."));
        return Result<EntityDefinitionDto>.Success(EntityDefinitionMapper.ToDto(entity));
    }
}
