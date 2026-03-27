using System.Text.Json;

namespace AppDefinition.HtmlGeneration;

/// <summary>
/// Generates HTML fragment for a relation in form/list context. MVP: minimal placeholder.
/// </summary>
public static class RelationHtmlGenerator
{
    /// <summary>Generates a minimal placeholder for a relation field.</summary>
    public static string Generate(JsonElement relation)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<div data-relation=\"true\"");
        if (relation.TryGetProperty("id", out var idEl))
            sb.Append(" data-id=\"").Append(Escape(idEl.GetString() ?? "")).Append("\"");
        sb.Append("></div>");
        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
