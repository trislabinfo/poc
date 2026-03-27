using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.CreateMigration;

public sealed record CreateMigrationCommand(
    Guid TenantApplicationEnvironmentId,
    Guid? FromReleaseId,
    Guid ToReleaseId,
    string? MigrationScriptJson = null)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, ITenantApplicationCommand;
