using BuildingBlocks.Web.Rest;
using User.Web.Models;

namespace User.Web.Clients;

public sealed class UserApiClient : RestApiClientBase, IUserApiClient
{
    public UserApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public Task<RestCallResult<IReadOnlyList<UserDto>>> GetAllUsersAsync(
        CancellationToken cancellationToken = default)
    {
        // User.Api currently returns raw JSON (no ApiResponse envelope), but RestApiClientBase supports it.
        return GetAsync<IReadOnlyList<UserDto>>("api/user", cancellationToken);
    }

    public Task<RestCallResult<UserDto>> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<UserDto>($"api/user/{id}", cancellationToken);
    }
}

