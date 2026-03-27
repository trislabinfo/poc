using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Identity.Application.DTOs;
using Identity.Application.Mappers;
using Identity.Application.Specifications;
using Identity.Domain.Repositories;

namespace Identity.Application.Queries.Users.ListUsers;

public sealed class ListUsersQueryHandler : IApplicationRequestHandler<ListUsersQuery, Result<PagedResponse<UserDto>>>
{
    private readonly IUserRepository _userRepository;

    public ListUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResponse<UserDto>>> HandleAsync(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        var spec = new UsersOrderedByDisplayNameSpec(request.DefaultTenantId);
        var paged = await _userRepository.GetByPaginationAsync(spec, request.Page, request.PageSize, cancellationToken);
        var dtos = paged.Items.Select(UserMapper.ToDto).ToList();
        var response = new PagedResponse<UserDto>(dtos, paged.PageNumber, paged.PageSize, paged.TotalCount);
        return Result<PagedResponse<UserDto>>.Success(response);
    }
}
