using BuildingBlocks.Web.AdminNavigation;

namespace AppBuilder.Web;

/// <summary>
/// Host navigation entries for the AppBuilder chat UI.
/// </summary>
public sealed class AppBuilderChatAdminNavigationProvider : IAdminNavigationProvider
{
    public IEnumerable<AdminNavItem> GetNavigationItems()
    {
        yield return new AdminNavItem(
            Key: "appbuilder-chat",
            Title: "App Builder Chat",
            Href: "/admin/appbuilder/chat",
            Order: 40);
    }
}

