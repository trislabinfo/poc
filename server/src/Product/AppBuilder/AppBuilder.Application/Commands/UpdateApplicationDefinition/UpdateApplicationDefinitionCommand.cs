using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateAppDefinition;

public sealed record UpdateAppDefinitionCommand(Guid Id, string Name, string Description)
    : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
