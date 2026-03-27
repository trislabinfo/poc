using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeletePropertyDefinition;

public sealed record DeletePropertyDefinitionCommand(Guid PropertyId) : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
