using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateRelationDefinition;

public sealed record CreateRelationDefinitionCommand(CreateRelationRequest Request)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, IAppBuilderCommand;
