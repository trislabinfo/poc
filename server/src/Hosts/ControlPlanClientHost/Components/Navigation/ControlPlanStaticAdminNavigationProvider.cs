using BuildingBlocks.Web.AdminNavigation;

namespace ControlPlanClientHost.Components.Navigation;

/// <summary>
/// Host-owned admin navigation entries that are not yet contributed by feature-specific frontend modules.
/// </summary>
public sealed class ControlPlanStaticAdminNavigationProvider : IAdminNavigationProvider
{
    public IEnumerable<AdminNavItem> GetNavigationItems()
    {
        // Host-owned "Dashboard" section. Feature modules contribute items under it.
        yield return new AdminNavItem(
            Key: "dashboard",
            Title: "Dashboard",
            Href: "/admin",
            Order: 0);
    }
}

