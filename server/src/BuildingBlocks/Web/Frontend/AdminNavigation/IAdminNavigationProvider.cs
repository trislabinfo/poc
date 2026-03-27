namespace BuildingBlocks.Web.AdminNavigation;

/// <summary>
/// Implemented by frontend modules to contribute admin navigation items.
/// </summary>
public interface IAdminNavigationProvider
{
    IEnumerable<AdminNavItem> GetNavigationItems();
}

