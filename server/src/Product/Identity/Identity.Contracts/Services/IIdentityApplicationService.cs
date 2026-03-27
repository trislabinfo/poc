using BuildingBlocks.Kernel.Results;

namespace Identity.Contracts.Services;

/// <summary>
/// Application service for Identity module. Used by other modules (e.g. Tenant) to create or delete users.
/// Implementation varies by topology: in-process (MediatR) in Monolith, HTTP in Distributed/Microservices.
/// </summary>
public interface IIdentityApplicationService
{
    /// <summary>
    /// Creates a new user. Idempotent by (tenantId, email): if user already exists for that tenant and email, returns existing user id.
    /// </summary>
    Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user (used for compensating actions when tenant creation rolls back).
    /// </summary>
    Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
