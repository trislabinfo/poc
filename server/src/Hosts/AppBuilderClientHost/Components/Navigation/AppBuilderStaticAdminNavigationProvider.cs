using BuildingBlocks.Web.AdminNavigation;

namespace AppBuilderClientHost.Components.Navigation;

/// <summary>
/// Host-owned admin navigation entries that are not yet contributed by feature-specific frontend modules.
/// </summary>
public sealed class AppBuilderStaticAdminNavigationProvider : IAdminNavigationProvider
{
    public IEnumerable<AdminNavItem> GetNavigationItems()
    {
        yield return new AdminNavItem(Key: "overview", Title: "Overview", Href: "/admin", Order: 0);
        yield return new AdminNavItem(Key: "application-definitions", Title: "Application Definitions", Href: "/admin/application-definitions", Order: 30);
    }
}

