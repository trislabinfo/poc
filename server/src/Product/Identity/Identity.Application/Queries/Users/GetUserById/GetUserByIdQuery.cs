using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Identity.Application.DTOs;
namespace Identity.Application.Queries.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IApplicationRequest<Result<UserDto>>;
