using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetPropertyDefinition;

public sealed class GetPropertyDefinitionQueryHandler
    : IApplicationRequestHandler<GetPropertyDefinitionQuery, Result<PropertyDefinitionDto>>
{
    private readonly IPropertyDefinitionRepository _repository;

    public GetPropertyDefinitionQueryHandler(IPropertyDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PropertyDefinitionDto>> HandleAsync(
        GetPropertyDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (entity == null)
            return Result<PropertyDefinitionDto>.Failure(
                Error.NotFound("AppBuilder.PropertyNotFound", "Property definition not found."));
        return Result<PropertyDefinitionDto>.Success(PropertyDefinitionMapper.ToDto(entity));
    }
}
