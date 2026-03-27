using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Identity.Application.Commands.Users.CreateUser;
using Identity.Application.Commands.Users.DeleteUser;
using Identity.Contracts;
using Identity.Contracts.Services;

namespace Identity.Application.Services;

/// <summary>
/// In-process implementation of IIdentityApplicationService using request dispatch.
/// Used in Monolith (or any host where both Tenant and Identity modules are loaded).
/// </summary>
public sealed class IdentityApplicationService : IIdentityApplicationService
{
    private readonly IRequestDispatcher _requestDispatcher;

    public IdentityApplicationService(IRequestDispatcher requestDispatcher)
    {
        _requestDispatcher = requestDispatcher;
    }

    public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateUserCommand(
            request.TenantId,
            request.Email,
            request.DisplayName,
            request.Password ?? string.Empty);
        return await _requestDispatcher.SendAsync(command, cancellationToken);
    }

    public async Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new DeleteUserCommand(userId);
        return await _requestDispatcher.SendAsync(command, cancellationToken);
    }
}
