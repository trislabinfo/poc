using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.DeleteEnvironment;

public sealed record DeleteEnvironmentCommand(Guid TenantApplicationId, Guid EnvironmentId) : IApplicationRequest<Result>, ITransactionalCommand, ITenantApplicationCommand;
