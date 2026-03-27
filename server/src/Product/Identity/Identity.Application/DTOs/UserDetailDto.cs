namespace Identity.Application.DTOs;

public sealed record UserDetailDto(
    Guid Id,
    Guid DefaultTenantId,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<RoleDto> Roles,
    IReadOnlyList<PermissionDto> Permissions);
