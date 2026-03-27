using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateDataSourceDefinition;

public sealed record CreateDataSourceDefinitionCommand(CreateDataSourceRequest Request)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, IAppBuilderCommand;
