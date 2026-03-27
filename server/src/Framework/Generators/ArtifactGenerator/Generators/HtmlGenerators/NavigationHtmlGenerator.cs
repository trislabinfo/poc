using AppDefinition.Contracts.SemanticHtml;
using System.Text.Json;

namespace AppDefinition.HtmlGeneration;

/// <summary>
/// Generates navigation fragment HTML from navigation JSON using the semantic contract.
/// Output: root nav items first, then sub nav items (data-component="navigation-root" / "navigation-sub").
/// Supports optional entityId and viewType on nodes (decision #13).
/// </summary>
public static class NavigationHtmlGenerator
{
    private const string DataViewType = "data-view-type";

    /// <summary>Generates navigation HTML from navigation JSON string. Returns empty string if JSON is null/empty or invalid.</summary>
    public static string Generate(string? navigationJson)
    {
        if (string.IsNullOrWhiteSpace(navigationJson))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(navigationJson);
            var root = doc.RootElement;
            var sb = new System.Text.StringBuilder();

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var node in root.EnumerateArray())
                    AppendNavNode(sb, node, isRoot: true);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                var rootChildren = root.TryGetProperty("children", out var c) ? c : (root.TryGetProperty("Children", out var c2) ? c2 : default);
                if (rootChildren.ValueKind == JsonValueKind.Array)
                {
                    foreach (var node in rootChildren.EnumerateArray())
                        AppendNavNode(sb, node, isRoot: true);
                }
                else if (root.TryGetProperty("nodes", out var nodes))
                {
                    foreach (var node in nodes.EnumerateArray())
                        AppendNavNode(sb, node, isRoot: true);
                }
                else
                    AppendNavNode(sb, root, isRoot: true);
            }

            return sb.ToString();
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static void AppendNavNode(System.Text.StringBuilder sb, JsonElement node, bool isRoot)
    {
        var component = isRoot ? SemanticHtmlConstants.ComponentTypes.NavigationRoot : SemanticHtmlConstants.ComponentTypes.NavigationSub;
        sb.Append("<div ");
        sb.Append(SemanticHtmlConstants.HtmlAttrComponent).Append("=\"").Append(component).Append("\"");

        var id = GetString(node, "id") ?? GetString(node, "Id");
        if (!string.IsNullOrEmpty(id))
            sb.Append(" data-id=\"").Append(Escape(id)).Append("\"");
        var label = GetString(node, "label") ?? GetString(node, "name") ?? GetString(node, "Name");
        if (!string.IsNullOrEmpty(label))
            sb.Append(" data-label=\"").Append(Escape(label)).Append("\"");
        var path = GetString(node, "path") ?? GetString(node, "Path") ?? GetPathFromConfiguration(GetString(node, "configurationJson") ?? GetString(node, "ConfigurationJson"));
        if (!string.IsNullOrEmpty(path))
            sb.Append(" data-path=\"").Append(Escape(path)).Append("\"");
        if (node.TryGetProperty("entityId", out var entityIdEl))
            sb.Append(" ").Append(SemanticHtmlConstants.HtmlAttrEntityId).Append("=\"").Append(Escape(entityIdEl.GetString() ?? "")).Append("\"");
        if (node.TryGetProperty("viewType", out var viewTypeEl))
            sb.Append(" ").Append(DataViewType).Append("=\"").Append(Escape(viewTypeEl.GetString() ?? "")).Append("\"");

        sb.Append(">");

        var childrenProp = node.TryGetProperty("children", out var children) ? children : (node.TryGetProperty("Children", out var children2) ? children2 : default);
        if (childrenProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in childrenProp.EnumerateArray())
                AppendNavNode(sb, child, isRoot: false);
        }

        sb.Append("</div>");
    }

    private static string? GetString(JsonElement node, string propertyName)
    {
        if (node.TryGetProperty(propertyName, out var el))
            return el.GetString();
        return null;
    }

    private static string? GetPathFromConfiguration(string? configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(configurationJson);
            var root = doc.RootElement;
            return root.TryGetProperty("path", out var pathEl) ? pathEl.GetString() : (root.TryGetProperty("Path", out var pathEl2) ? pathEl2.GetString() : null);
        }
        catch (JsonException) { /* ignore */ }
        return null;
    }

    private static string Escape(string s)
    {
        return s.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}
