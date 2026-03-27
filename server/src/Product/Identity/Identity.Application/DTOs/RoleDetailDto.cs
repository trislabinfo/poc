namespace Identity.Application.DTOs;

public sealed record RoleDetailDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    bool IsSystemRole,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<PermissionDto> Permissions);
