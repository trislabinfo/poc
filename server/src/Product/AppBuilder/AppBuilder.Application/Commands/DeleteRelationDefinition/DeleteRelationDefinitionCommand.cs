using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteRelationDefinition;

public sealed record DeleteRelationDefinitionCommand(Guid RelationId) : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
