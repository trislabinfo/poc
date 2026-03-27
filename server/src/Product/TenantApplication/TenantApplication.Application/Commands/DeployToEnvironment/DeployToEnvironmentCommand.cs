using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.DeployToEnvironment;

public sealed record DeployToEnvironmentCommand(
    Guid TenantApplicationId,
    Guid EnvironmentId,
    Guid ReleaseId,
    string Version,
    Guid DeployedBy)
    : IApplicationRequest<Result<Guid?>>, ITransactionalCommand, ITenantApplicationCommand; // Returns migration ID if migration was created, null if deployment completed immediately
