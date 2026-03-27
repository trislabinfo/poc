namespace Datarizen.Identity.Migrations.Helpers;

/// <summary>Seed row for identity.permissions.</summary>
public sealed record PermissionSeedDto(Guid Id, string Name, string Resource, string Action);

/// <summary>Seed row for identity.roles; PermissionIds are used for role_permissions.</summary>
public sealed record RoleSeedDto(Guid Id, string Name, string Description, List<Guid> PermissionIds);

/// <summary>Seed row for identity.users; RoleIds are used for user_roles. PasswordHash is stored in identity.credentials.</summary>
public sealed record UserSeedDto(
    Guid Id,
    string Email,
    Guid DefaultTenantId,
    string DisplayName,
    string PasswordHash,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<Guid> RoleIds);
