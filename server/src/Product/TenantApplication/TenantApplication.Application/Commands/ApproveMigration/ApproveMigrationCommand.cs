using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.ApproveMigration;

public sealed record ApproveMigrationCommand(
    Guid TenantApplicationId,
    Guid EnvironmentId,
    Guid MigrationId,
    Guid ApprovedBy)
    : IApplicationRequest<Result>, ITransactionalCommand, ITenantApplicationCommand;
