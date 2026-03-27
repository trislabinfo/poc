using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListEntitiesByApplication;

public sealed record ListEntitiesByApplicationQuery(Guid AppDefinitionId)
    : IApplicationRequest<Result<List<EntityDefinitionDto>>>;
