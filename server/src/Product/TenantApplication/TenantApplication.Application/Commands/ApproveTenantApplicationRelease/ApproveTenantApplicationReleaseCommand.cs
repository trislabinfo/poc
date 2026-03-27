using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.ApproveTenantApplicationRelease;

public sealed record ApproveTenantApplicationReleaseCommand(
    Guid TenantApplicationId,
    Guid ReleaseId,
    Guid ApprovedBy)
    : IApplicationRequest<Result>, ITransactionalCommand, ITenantApplicationCommand;
