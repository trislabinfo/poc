using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListPagesByApplication;

public sealed record ListPagesByApplicationQuery(Guid AppDefinitionId)
    : IApplicationRequest<Result<List<PageDefinitionDto>>>;
