using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserDto(
            user.Id,
            user.DefaultTenantId,
            user.Email.Value,
            user.DisplayName,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
    }

    public static UserDetailDto ToDetailDto(
        User user,
        IReadOnlyList<RoleDto> roles,
        IReadOnlyList<PermissionDto> permissions)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserDetailDto(
            user.Id,
            user.DefaultTenantId,
            user.Email.Value,
            user.DisplayName,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            roles ?? Array.Empty<RoleDto>(),
            permissions ?? Array.Empty<PermissionDto>());
    }
}
