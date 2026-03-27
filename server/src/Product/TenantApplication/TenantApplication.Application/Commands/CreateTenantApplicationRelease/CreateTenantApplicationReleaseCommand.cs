using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.CreateTenantApplicationRelease;

public sealed record CreateTenantApplicationReleaseCommand(
    Guid TenantApplicationId,
    int Major,
    int Minor,
    int Patch,
    string ReleaseNotes,
    Guid ReleasedBy)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, ITenantApplicationCommand;
