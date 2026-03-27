using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.UpdateMigrationScript;

public sealed record UpdateMigrationScriptCommand(
    Guid TenantApplicationId,
    Guid EnvironmentId,
    Guid MigrationId,
    string MigrationScriptJson) : IApplicationRequest<Result>, ITransactionalCommand, ITenantApplicationCommand;
