using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
namespace Identity.Application.Commands.Users.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string DisplayName)
    : IApplicationRequest<Result>, ITransactionalCommand, IIdentityCommand;
