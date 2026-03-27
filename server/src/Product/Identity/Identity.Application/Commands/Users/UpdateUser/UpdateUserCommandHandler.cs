using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Identity.Application.Specifications;
using Identity.Domain.Repositories;

namespace Identity.Application.Commands.Users.UpdateUser;

public sealed class UpdateUserCommandHandler : IApplicationRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly BuildingBlocks.Kernel.Domain.IDateTimeProvider _dateTimeProvider;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        BuildingBlocks.Kernel.Domain.IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(
            new UserByIdSpec(request.UserId),
            cancellationToken);

        if (user is null)
        {
            return Result.Failure(
                Error.NotFound("Identity.User.NotFound", "User not found."));
        }

        var result = user.Update(request.DisplayName, _dateTimeProvider);
        if (result.IsFailure)
        {
            return result;
        }

        _userRepository.Update(user);
        return Result.Success();
    }
}
