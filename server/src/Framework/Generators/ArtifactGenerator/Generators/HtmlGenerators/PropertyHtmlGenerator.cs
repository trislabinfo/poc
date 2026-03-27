using AppDefinition.Contracts.SemanticHtml;
using System.Text.Json;

namespace AppDefinition.HtmlGeneration;

/// <summary>
/// Generates form-field HTML fragment from a property definition (semantic form-field).
/// </summary>
public static class PropertyHtmlGenerator
{
    /// <summary>Generates a form-field wrapper HTML fragment for one property. Returns minimal semantic HTML.</summary>
    public static string Generate(JsonElement property)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<div ");
        sb.Append(SemanticHtmlConstants.HtmlAttrComponent).Append("=\"").Append(SemanticHtmlConstants.ComponentTypes.FormField).Append("\"");
        if (property.TryGetProperty("id", out var idEl))
            sb.Append(" data-id=\"").Append(Escape(idEl.GetString() ?? "")).Append("\"");
        if (property.TryGetProperty("name", out var nameEl))
            sb.Append(" data-name=\"").Append(Escape(nameEl.GetString() ?? "")).Append("\"");
        sb.Append("></div>");
        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
