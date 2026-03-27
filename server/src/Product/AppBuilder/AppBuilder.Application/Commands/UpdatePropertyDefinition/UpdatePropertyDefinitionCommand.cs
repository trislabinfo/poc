using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdatePropertyDefinition;

public sealed record UpdatePropertyDefinitionCommand(Guid PropertyId, UpdatePropertyRequest Request)
    : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
