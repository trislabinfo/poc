using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateRelationDefinition;

public sealed record UpdateRelationDefinitionCommand(Guid RelationId, UpdateRelationRequest Request)
    : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
