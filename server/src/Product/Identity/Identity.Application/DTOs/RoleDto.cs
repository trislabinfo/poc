namespace Identity.Application.DTOs;

public sealed record RoleDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    bool IsSystemRole,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
