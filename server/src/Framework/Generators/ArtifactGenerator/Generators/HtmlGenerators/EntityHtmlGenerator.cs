using AppDefinition.Contracts.SemanticHtml;
using System.Text.Json;

namespace AppDefinition.HtmlGeneration;

/// <summary>
/// Generates entity list view and entity form view HTML using semantic contract (entity-list, entity-form, form-field).
/// </summary>
public static class EntityHtmlGenerator
{
    /// <summary>Generates list view HTML for one entity (entity-list container with optional data-entity-id).</summary>
    public static string GenerateListView(string entityId, string? displayName)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<div ");
        sb.Append(SemanticHtmlConstants.HtmlAttrComponent).Append("=\"").Append(SemanticHtmlConstants.ComponentTypes.EntityList).Append("\" ");
        sb.Append(SemanticHtmlConstants.HtmlAttrEntityId).Append("=\"").Append(Escape(entityId)).Append("\"");
        if (!string.IsNullOrEmpty(displayName))
            sb.Append(" data-display-name=\"").Append(Escape(displayName)).Append("\"");
        sb.Append("></div>");
        return sb.ToString();
    }

    /// <summary>Generates form view HTML for one entity (entity-form container; optionally includes property/relation placeholders).</summary>
    public static string GenerateFormView(string entityId, string? displayName, JsonElement? propertiesArray = null, JsonElement? relationsArray = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<div ");
        sb.Append(SemanticHtmlConstants.HtmlAttrComponent).Append("=\"").Append(SemanticHtmlConstants.ComponentTypes.EntityForm).Append("\" ");
        sb.Append(SemanticHtmlConstants.HtmlAttrEntityId).Append("=\"").Append(Escape(entityId)).Append("\"");
        if (!string.IsNullOrEmpty(displayName))
            sb.Append(" data-display-name=\"").Append(Escape(displayName)).Append("\"");
        sb.Append(">");

        if (propertiesArray.HasValue && propertiesArray.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var prop in propertiesArray.Value.EnumerateArray())
                sb.Append(PropertyHtmlGenerator.Generate(prop));
        }
        if (relationsArray.HasValue && relationsArray.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var rel in relationsArray.Value.EnumerateArray())
                sb.Append(RelationHtmlGenerator.Generate(rel));
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
