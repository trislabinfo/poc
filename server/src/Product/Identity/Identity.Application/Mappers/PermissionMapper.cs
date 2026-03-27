using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Mappers;

public static class PermissionMapper
{
    public static PermissionDto ToDto(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return new PermissionDto(
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Description,
            permission.Module,
            permission.CreatedAt);
    }
}
