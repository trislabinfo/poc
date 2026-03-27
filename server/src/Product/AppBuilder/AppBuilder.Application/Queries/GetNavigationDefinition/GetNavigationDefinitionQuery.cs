using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetNavigationDefinition;

public sealed record GetNavigationDefinitionQuery(Guid NavigationId) : IApplicationRequest<Result<NavigationDefinitionDto>>;
