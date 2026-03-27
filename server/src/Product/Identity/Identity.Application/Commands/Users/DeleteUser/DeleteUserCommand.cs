using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
namespace Identity.Application.Commands.Users.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId)
    : IApplicationRequest<Result>, ITransactionalCommand, IIdentityCommand;
