using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateNavigationDefinition;

public sealed record CreateNavigationDefinitionCommand(CreateNavigationRequest Request)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, IAppBuilderCommand;
