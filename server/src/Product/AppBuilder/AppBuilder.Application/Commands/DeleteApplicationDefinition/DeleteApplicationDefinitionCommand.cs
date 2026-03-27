using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteAppDefinition;

public sealed record DeleteAppDefinitionCommand(Guid Id)
    : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
