using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateEntityDefinition;

public sealed record UpdateEntityDefinitionCommand(Guid EntityId, UpdateEntityRequest Request)
    : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
