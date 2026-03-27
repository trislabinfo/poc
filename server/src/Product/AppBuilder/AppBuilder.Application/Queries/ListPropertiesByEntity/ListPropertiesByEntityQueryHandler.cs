using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListPropertiesByEntity;

public sealed class ListPropertiesByEntityQueryHandler
    : IApplicationRequestHandler<ListPropertiesByEntityQuery, Result<List<PropertyDefinitionDto>>>
{
    private readonly IPropertyDefinitionRepository _repository;

    public ListPropertiesByEntityQueryHandler(IPropertyDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<PropertyDefinitionDto>>> HandleAsync(
        ListPropertiesByEntityQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByEntityDefinitionIdAsync(request.EntityDefinitionId, cancellationToken);
        var dtos = list.Select(PropertyDefinitionMapper.ToDto).ToList();
        return Result<List<PropertyDefinitionDto>>.Success(dtos);
    }
}
