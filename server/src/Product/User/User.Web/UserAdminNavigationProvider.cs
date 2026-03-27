using BuildingBlocks.Web.AdminNavigation;

namespace User.Web;

/// <summary>
/// Contributes the "Users" admin navigation entry.
/// </summary>
public sealed class UserAdminNavigationProvider : IAdminNavigationProvider
{
    public IEnumerable<AdminNavItem> GetNavigationItems()
    {
        // Dashboard -> Users group -> Search / Add-Edit
        yield return new AdminNavItem(Key: "users",
            Title: "Users",
            Href: "/admin/users",
            Order: 10,
            ParentKey: "dashboard");
        yield return new AdminNavItem(Key: "users-search", Title: "Search", Href: "/admin/users/search", ParentKey: "users", Order: 10);
        yield return new AdminNavItem(Key: "users-add-edit", Title: "Add/Edit", Href: "/admin/users/add-edit", ParentKey: "users", Order: 20);
    }
}

