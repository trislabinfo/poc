using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeletePageDefinition;

public sealed record DeletePageDefinitionCommand(Guid PageId) : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
