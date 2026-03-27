using Ardalis.Specification;

using Identity.Domain.Entities;

namespace Identity.Application.Specifications;

public sealed class RoleByIdSpec : Specification<Role>
{
    public RoleByIdSpec(Guid id)
    {
        Query.Where(r => r.Id == id);
    }
}

public sealed class RoleByNameSpec : Specification<Role>
{
    public RoleByNameSpec(string name, Guid tenantId)
    {
        Query.Where(r => r.TenantId == tenantId && r.Name == name);
    }
}

/// <summary>
/// Roles for a tenant, ordered by name.
/// </summary>
public sealed class RolesByTenantSpec : Specification<Role>
{
    public RolesByTenantSpec(Guid tenantId)
    {
        Query.Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name);
    }
}

/// <summary>
/// Active roles for a tenant.
/// </summary>
public sealed class ActiveRolesByTenantSpec : Specification<Role>
{
    public ActiveRolesByTenantSpec(Guid tenantId)
    {
        Query.Where(r => r.TenantId == tenantId && r.IsActive)
            .OrderBy(r => r.Name);
    }
}
