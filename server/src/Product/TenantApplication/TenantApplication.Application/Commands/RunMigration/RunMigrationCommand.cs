using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.RunMigration;

public sealed record RunMigrationCommand(
    Guid TenantApplicationId,
    Guid EnvironmentId,
    Guid MigrationId) : IApplicationRequest<Result>, ITransactionalCommand, ITenantApplicationCommand;
