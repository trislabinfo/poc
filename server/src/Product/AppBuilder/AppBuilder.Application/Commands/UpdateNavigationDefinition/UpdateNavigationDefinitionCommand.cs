using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateNavigationDefinition;

public sealed record UpdateNavigationDefinitionCommand(Guid NavigationId, UpdateNavigationRequest Request)
    : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
