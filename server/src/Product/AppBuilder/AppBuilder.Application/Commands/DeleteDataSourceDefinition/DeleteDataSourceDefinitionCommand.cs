using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteDataSourceDefinition;

public sealed record DeleteDataSourceDefinitionCommand(Guid DataSourceId) : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
