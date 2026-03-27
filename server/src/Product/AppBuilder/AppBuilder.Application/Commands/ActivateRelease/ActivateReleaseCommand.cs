using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.ActivateRelease;

public sealed record ActivateReleaseCommand(Guid ReleaseId) : IApplicationRequest<Result>, ITransactionalCommand, IAppBuilderCommand;
