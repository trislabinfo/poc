using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Identity.Domain.Repositories;

namespace Identity.Application.Commands.Users.DeleteUser;

public sealed class DeleteUserCommandHandler : IApplicationRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public DeleteUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(Error.NotFound("Identity.User", "User not found."));

        _userRepository.Delete(user);
        return Result.Success();
    }
}
