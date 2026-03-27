using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteNavigationDefinition;

public sealed record DeleteNavigationDefinitionCommand(Guid NavigationId) : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
