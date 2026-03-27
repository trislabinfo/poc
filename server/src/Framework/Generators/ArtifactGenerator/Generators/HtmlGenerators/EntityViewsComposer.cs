using AppDefinition.Domain.Entities.Application;
using System.Text.Json;

namespace AppDefinition.HtmlGeneration;

/// <summary>
/// Composes entity view HTML (list + form per entity) from release EntityJson.
/// Returns a list of ReleaseEntityView to persist (caller sets ReleaseId when saving).
/// </summary>
public static class EntityViewsComposer
{
    /// <summary>
    /// Parses EntityJson (expects { Entities: [ { Entity: {...}, Properties: [...] } ], Relations: [...] }) and generates
    /// one list view and one form view per entity. Returns ReleaseEntityView instances without ReleaseId set; caller must set it.
    /// </summary>
    public static IReadOnlyList<ReleaseEntityView> Compose(string? entityJson, Guid releaseId)
    {
        if (string.IsNullOrWhiteSpace(entityJson))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(entityJson);
            var root = doc.RootElement;
            var list = new List<ReleaseEntityView>();

            JsonElement entitiesArray = default;
            if (root.TryGetProperty("Entities", out var entitiesProp))
                entitiesArray = entitiesProp;
            else if (root.ValueKind == JsonValueKind.Array)
                entitiesArray = root;
            else
                return list;

            JsonElement relationsArray = default;
            root.TryGetProperty("Relations", out relationsArray);

            foreach (var item in entitiesArray.EnumerateArray())
            {
                if (!item.TryGetProperty("Entity", out var entityEl))
                    continue;

                Guid entityId;
                if (entityEl.TryGetProperty("Id", out var idEl))
                {
                    if (idEl.ValueKind == JsonValueKind.String && Guid.TryParse(idEl.GetString(), out var parsed))
                        entityId = parsed;
                    else
                        try { entityId = idEl.GetGuid(); } catch { entityId = Guid.Empty; }
                }
                else
                    entityId = Guid.Empty;
                if (entityId == Guid.Empty)
                    continue;

                var entityIdStr = entityId.ToString();
                var displayName = entityEl.TryGetProperty("DisplayName", out var dn) ? dn.GetString() : null;

                JsonElement? props = null;
                if (item.TryGetProperty("Properties", out var propsEl))
                    props = propsEl;

                // List view
                var listHtml = EntityHtmlGenerator.GenerateListView(entityIdStr, displayName);
                list.Add(ReleaseEntityView.Create(releaseId, entityId, ViewTypes.List, listHtml));

                // Form view (with properties/relations if available)
                var formHtml = EntityHtmlGenerator.GenerateFormView(entityIdStr, displayName, props, relationsArray.ValueKind == JsonValueKind.Array ? relationsArray : null);
                list.Add(ReleaseEntityView.Create(releaseId, entityId, ViewTypes.Form, formHtml));
            }

            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
