using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeactivateRelease;

public sealed record DeactivateReleaseCommand(Guid ReleaseId) : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
