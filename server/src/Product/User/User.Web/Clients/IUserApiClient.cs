using BuildingBlocks.Web.Rest;
using User.Web.Models;

namespace User.Web.Clients;

public interface IUserApiClient
{
    Task<RestCallResult<IReadOnlyList<UserDto>>> GetAllUsersAsync(
        CancellationToken cancellationToken = default);

    Task<RestCallResult<UserDto>> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

