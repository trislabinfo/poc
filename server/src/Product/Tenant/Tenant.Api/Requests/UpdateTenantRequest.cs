namespace Tenant.Api.Requests;

/// <summary>
/// Request body for updating a tenant (used when Update is implemented).
/// </summary>
public sealed record UpdateTenantRequest(string Name, string Slug);
