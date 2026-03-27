using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
namespace Tenant.Application.Commands.CreateTenant;

public sealed record CreateTenantCommand(string Name, string Slug)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, ITenantCommand;
