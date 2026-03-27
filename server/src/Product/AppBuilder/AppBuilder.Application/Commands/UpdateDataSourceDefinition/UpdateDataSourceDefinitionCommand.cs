using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateDataSourceDefinition;

public sealed record UpdateDataSourceDefinitionCommand(Guid DataSourceId, UpdateDataSourceRequest Request)
    : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
