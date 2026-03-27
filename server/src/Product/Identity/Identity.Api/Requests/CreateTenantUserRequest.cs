namespace Identity.Api.Requests;

/// <summary>
/// Request body for POST /api/identity/create-tenant-user (called by Tenant service when creating tenant users).
/// </summary>
public sealed record CreateTenantUserRequest(
    Guid TenantId,
    string? TenantName,
    string Email,
    string DisplayName,
    string? Password,
    bool IsTenantOwner);
