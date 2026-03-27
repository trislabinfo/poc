namespace BuildingBlocks.Web.AdminNavigation;

/// <summary>
/// A navigation item contributed by a frontend module (e.g. User.Web).
/// </summary>
public sealed record AdminNavItem(
    string Key,
    string Title,
    string Href,
    int Order = 0,
    string? ParentKey = null);

