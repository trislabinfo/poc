namespace Identity.Api.Responses;

/// <summary>
/// Response body for POST /api/identity/create-tenant-user (201 Created).
/// </summary>
public sealed record CreateTenantUserResponse(Guid UserId);
