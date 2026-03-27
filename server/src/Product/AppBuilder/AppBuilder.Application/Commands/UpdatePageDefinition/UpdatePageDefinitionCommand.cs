using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdatePageDefinition;

public sealed record UpdatePageDefinitionCommand(Guid PageId, UpdatePageRequest Request)
    : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
