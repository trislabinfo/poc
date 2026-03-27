namespace Tenant.Api.Requests;

public sealed record UserDataRequest(
    string Email,
    string DisplayName,
    string? Password,
    bool IsTenantOwner);
