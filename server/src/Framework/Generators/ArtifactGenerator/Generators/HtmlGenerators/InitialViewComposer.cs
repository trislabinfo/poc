using AppDefinition.Contracts.SemanticHtml;

namespace AppDefinition.HtmlGeneration;

/// <summary>
/// Composes the initial view HTML from release snapshot JSON.
/// Order: root nav → sub nav → main content (dashboard shell + page shells). See plan decision #9.
/// </summary>
public static class InitialViewComposer
{
    /// <summary>
    /// Returns true when stored HTML has nav structure but no labels (e.g. generated before PascalCase support).
    /// Caller should regenerate from NavigationJson + PageJson when this is true.
    /// </summary>
    public static bool IsStaleNavHtml(string? storedHtml)
    {
        if (string.IsNullOrEmpty(storedHtml)) return false;
        return storedHtml.Contains("navigation-root", StringComparison.OrdinalIgnoreCase)
            && !storedHtml.Contains("data-label", StringComparison.Ordinal);
    }

    /// <summary>
    /// Composes full initial-view HTML from navigation and page JSON.
    /// Uses semantic contract only (no raw CSS class names).
    /// </summary>
    /// <param name="navigationJson">Release navigation JSON.</param>
    /// <param name="pageJson">Release page JSON.</param>
    /// <returns>Single HTML string for initial view, or null if generation fails.</returns>
    public static string? Compose(string? navigationJson, string? pageJson)
    {
        try
        {
            var navHtml = NavigationHtmlGenerator.Generate(navigationJson);
            var pageHtml = PageHtmlGenerator.Generate(pageJson);

            var sb = new System.Text.StringBuilder();
            sb.Append("<div ")
                .Append(SemanticHtmlConstants.HtmlAttrComponent).Append("=\"").Append(SemanticHtmlConstants.ComponentTypes.MainContent).Append("\" ")
                .Append(SemanticHtmlConstants.HtmlAttrSlot).Append("=\"main\">");

            // Dashboard shell placeholder (MVP: minimal)
            sb.Append("<div ")
                .Append(SemanticHtmlConstants.HtmlAttrComponent).Append("=\"").Append(SemanticHtmlConstants.ComponentTypes.DashboardShell).Append("\"></div>");

            if (!string.IsNullOrEmpty(pageHtml))
                sb.Append(pageHtml);

            sb.Append("</div>");

            // Wrap: nav first, then main content
            var mainContent = sb.ToString();
            sb.Clear();
            if (!string.IsNullOrEmpty(navHtml))
                sb.Append(navHtml);
            sb.Append(mainContent);

            return sb.ToString();
        }
        catch
        {
            return null;
        }
    }
}
