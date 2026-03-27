using Ardalis.Specification;

using Identity.Domain.Entities;

namespace Identity.Application.Specifications;

public sealed class PermissionByIdSpec : Specification<Permission>
{
    public PermissionByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id);
    }
}

public sealed class PermissionByCodeSpec : Specification<Permission>
{
    public PermissionByCodeSpec(string code)
    {
        Query.Where(p => p.Code == code);
    }
}

/// <summary>
/// Permissions by module, ordered by code.
/// </summary>
public sealed class PermissionsByModuleSpec : Specification<Permission>
{
    public PermissionsByModuleSpec(string module)
    {
        Query.Where(p => p.Module == module)
            .OrderBy(p => p.Code);
    }
}

/// <summary>
/// All permissions ordered by module then code.
/// </summary>
public sealed class AllPermissionsOrderedSpec : Specification<Permission>
{
    public AllPermissionsOrderedSpec()
    {
        Query.OrderBy(p => p.Module)
            .ThenBy(p => p.Code);
    }
}
