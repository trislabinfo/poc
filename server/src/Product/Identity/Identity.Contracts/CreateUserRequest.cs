namespace Identity.Contracts;

/// <summary>
/// Request to create a user, used by Tenant (and other modules) when calling IIdentityApplicationService.
/// </summary>
public sealed record CreateUserRequest(
    Guid TenantId,
    string Email,
    string DisplayName,
    string? Password,
    bool IsTenantOwner);
