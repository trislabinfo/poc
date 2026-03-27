using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Mappers;

public static class RoleMapper
{
    public static RoleDto ToDto(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RoleDto(
            role.Id,
            role.TenantId,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt);
    }

    public static RoleDetailDto ToDetailDto(Role role, IReadOnlyList<PermissionDto> permissions)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RoleDetailDto(
            role.Id,
            role.TenantId,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt,
            permissions ?? Array.Empty<PermissionDto>());
    }
}
