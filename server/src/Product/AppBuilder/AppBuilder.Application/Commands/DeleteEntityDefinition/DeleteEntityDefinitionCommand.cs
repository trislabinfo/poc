using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteEntityDefinition;

public sealed record DeleteEntityDefinitionCommand(Guid EntityId) : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
