using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
namespace Identity.Application.Commands.Users.CreateUser;

public sealed record CreateUserCommand(
    Guid DefaultTenantId,
    string Email,
    string DisplayName,
    string Password)
    : IApplicationRequest<Result<Guid>>, ITransactionalCommand, IIdentityCommand;
