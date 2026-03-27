using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListNavigationsByApplication;

public sealed record ListNavigationsByApplicationQuery(Guid AppDefinitionId)
    : IApplicationRequest<Result<List<NavigationDefinitionDto>>>;
