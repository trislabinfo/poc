using AppDefinition.Contracts.SemanticHtml;
using System.Text.Json;

namespace AppDefinition.HtmlGeneration;

/// <summary>
/// Generates page shell / placeholder HTML from page JSON using the semantic contract.
/// Output: one page-shell per page (data-component="page-shell").
/// </summary>
public static class PageHtmlGenerator
{
    /// <summary>Generates page shells HTML from page JSON string. Returns empty string if JSON is null/empty or invalid.</summary>
    public static string Generate(string? pageJson)
    {
        if (string.IsNullOrWhiteSpace(pageJson))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(pageJson);
            var root = doc.RootElement;
            var sb = new System.Text.StringBuilder();

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var page in root.EnumerateArray())
                    AppendPageShell(sb, page);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("pages", out var pages))
                {
                    foreach (var page in pages.EnumerateArray())
                        AppendPageShell(sb, page);
                }
                else
                    AppendPageShell(sb, root);
            }

            return sb.ToString();
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static void AppendPageShell(System.Text.StringBuilder sb, JsonElement page)
    {
        sb.Append("<div ");
        sb.Append(SemanticHtmlConstants.HtmlAttrComponent).Append("=\"").Append(SemanticHtmlConstants.ComponentTypes.PageShell).Append("\"");

        if (page.TryGetProperty("id", out var idEl))
            sb.Append(" data-id=\"").Append(Escape(idEl.GetString() ?? "")).Append("\"");
        if (page.TryGetProperty("name", out var nameEl))
            sb.Append(" data-name=\"").Append(Escape(nameEl.GetString() ?? "")).Append("\"");

        sb.Append("></div>");
    }

    private static string Escape(string s)
    {
        return s.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}
