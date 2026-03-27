using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreatePageDefinition;

public sealed record CreatePageDefinitionCommand(CreatePageRequest Request)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, IAppBuilderCommand;
