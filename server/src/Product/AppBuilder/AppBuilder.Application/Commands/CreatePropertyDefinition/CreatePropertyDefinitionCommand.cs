using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreatePropertyDefinition;

public sealed record CreatePropertyDefinitionCommand(CreatePropertyRequest Request)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, IAppBuilderCommand;
