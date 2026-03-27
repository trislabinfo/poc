using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListPropertiesByEntity;

public sealed record ListPropertiesByEntityQuery(Guid EntityDefinitionId)
    : IApplicationRequest<Result<List<PropertyDefinitionDto>>>;
