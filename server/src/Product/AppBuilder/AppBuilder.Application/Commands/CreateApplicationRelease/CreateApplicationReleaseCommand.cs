using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateApplicationRelease;

public sealed record CreateApplicationReleaseCommand(
    Guid AppDefinitionId,
    int Major,
    int Minor,
    int Patch,
    string ReleaseNotes,
    Guid ReleasedBy)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, IAppBuilderCommand;
