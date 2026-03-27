using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateAppDefinition;

public sealed record CreateAppDefinitionCommand(
    string Name,
    string Description,
    string Slug,
    bool IsPublic)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, IAppBuilderCommand;
