namespace Tenant.Contracts;

/// <summary>
/// Minimal tenant info for cross-module resolution (e.g. TenantApplication resolving tenant by slug).
/// </summary>
public sealed record TenantInfoDto(Guid Id, string Slug);
