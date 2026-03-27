using Ardalis.Specification;

using Identity.Domain.Entities;

namespace Identity.Application.Specifications;

public sealed class UserByIdSpec : Specification<User>
{
    public UserByIdSpec(Guid id)
    {
        Query.Where(u => u.Id == id);
    }
}

public sealed class UserByEmailSpec : Specification<User>
{
    public UserByEmailSpec(string emailValue)
    {
        Query.Where(u => u.Email.Value == emailValue);
    }
}

/// <summary>
/// Active users, optionally filtered by default tenant.
/// </summary>
public sealed class ActiveUsersSpec : Specification<User>
{
    public ActiveUsersSpec(Guid? defaultTenantId = null)
    {
        Query.Where(u => u.IsActive && (!defaultTenantId.HasValue || defaultTenantId.Value == Guid.Empty || u.DefaultTenantId == defaultTenantId.Value));
        Query.OrderBy(u => u.DisplayName);
    }
}

/// <summary>
/// All users ordered by display name (for list queries).
/// </summary>
public sealed class UsersOrderedByDisplayNameSpec : Specification<User>
{
    public UsersOrderedByDisplayNameSpec(Guid? defaultTenantId = null)
    {
        Query.Where(u => !defaultTenantId.HasValue || defaultTenantId.Value == Guid.Empty || u.DefaultTenantId == defaultTenantId.Value);
        Query.OrderBy(u => u.DisplayName);
    }
}
