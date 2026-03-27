namespace Tenant.Application.Commands.CreateTenantWithUsers;

/// <summary>
/// User data for create-tenant-with-users request.
/// </summary>
public sealed record UserData(
    string Email,
    string DisplayName,
    string? Password,
    bool IsTenantOwner);
