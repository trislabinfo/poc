using Tenant.Application.DTOs;
using TenantEntity = Tenant.Domain.Entities.Tenant;

namespace Tenant.Application.Mappers;

public static class TenantMapper
{
    public static TenantDto ToDto(TenantEntity tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        return new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.CreatedAt,
            tenant.UpdatedAt);
    }

    public static TenantWithUsersDto ToTenantWithUsersDto(TenantEntity tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        var users = tenant.Users
            .Select(u => new TenantUserDto(u.Id, u.UserId, u.IsTenantOwner))
            .ToList();
        return new TenantWithUsersDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.CreatedAt,
            tenant.UpdatedAt,
            users);
    }
}
