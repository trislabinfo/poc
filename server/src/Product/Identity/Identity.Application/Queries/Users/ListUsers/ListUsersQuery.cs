using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Identity.Application.DTOs;
namespace Identity.Application.Queries.Users.ListUsers;

public sealed record ListUsersQuery(Guid? DefaultTenantId, int Page = 1, int PageSize = 10)
    : IApplicationRequest<Result<PagedResponse<UserDto>>>;
