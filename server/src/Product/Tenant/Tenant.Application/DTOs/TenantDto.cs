namespace Tenant.Application.DTOs;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
