using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Identity.Application.DTOs;
using Identity.Application.Mappers;
using Identity.Domain.Repositories;

namespace Identity.Application.Queries.Users.GetUserById;

public sealed class GetUserByIdQueryHandler : IApplicationRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> HandleAsync(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(
            new Specifications.UserByIdSpec(request.UserId),
            cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Failure(
                Error.NotFound("Identity.User.NotFound", "User not found."));
        }

        return Result<UserDto>.Success(UserMapper.ToDto(user));
    }
}
