using BuildingBlocks.Web.AdminNavigation;

namespace Tenant.Web;

/// <summary>
/// Contributes tenant management entry to the control plan navigation.
/// </summary>
public sealed class TenantAdminNavigationProvider : IAdminNavigationProvider
{
    public IEnumerable<AdminNavItem> GetNavigationItems()
    {
        // Dashboard -> Tenants group -> Search / Add-Edit
        yield return new AdminNavItem(Key: "tenants",
            Title: "Tenants",
            Href: "/admin/tenants",
            Order: 20,
            ParentKey: "dashboard");
        yield return new AdminNavItem(Key: "tenants-search", Title: "Search", Href: "/admin/tenants/search", ParentKey: "tenants", Order: 10);
        yield return new AdminNavItem(Key: "tenants-add-edit", Title: "Add/Edit", Href: "/admin/tenants/add-edit", ParentKey: "tenants", Order: 20);
    }
}

