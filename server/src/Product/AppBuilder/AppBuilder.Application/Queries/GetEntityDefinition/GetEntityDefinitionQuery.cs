using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetEntityDefinition;

public sealed record GetEntityDefinitionQuery(Guid EntityId) : IApplicationRequest<Result<EntityDefinitionDto>>;
