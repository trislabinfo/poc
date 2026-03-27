namespace Tenant.Application.DTOs;

public sealed record TenantWithUsersDto(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<TenantUserDto> Users);
