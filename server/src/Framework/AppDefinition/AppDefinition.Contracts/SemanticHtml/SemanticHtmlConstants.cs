namespace AppDefinition.Contracts.SemanticHtml;

/// <summary>
/// Semantic HTML contract for initial-view generation.
/// Must stay in sync with client/packages/contracts (semantic-html.ts).
/// No raw CSS class names in DB; use only these attribute names and component type values.
/// </summary>
/// See: docs/implementations/application-definition/application-definition-html-impl-plan.md
public static class SemanticHtmlConstants
{
    /// <summary>HTML attribute for component type. Use only values from <see cref="ComponentTypes"/>.</summary>
    public const string HtmlAttrComponent = "data-component";

    /// <summary>HTML attribute for slot/role within a component (e.g. root vs sub).</summary>
    public const string HtmlAttrSlot = "data-slot";

    /// <summary>Optional HTML attribute for entity-scoped views: entity id (e.g. entity-list, entity-form).</summary>
    public const string HtmlAttrEntityId = "data-entity-id";

    /// <summary>Semantic component types for initial-view HTML. Composition order: root nav → sub nav → main content.</summary>
    public static class ComponentTypes
    {
        /// <summary>Root-level navigation item.</summary>
        public const string NavigationRoot = "navigation-root";

        /// <summary>Child/sub navigation item.</summary>
        public const string NavigationSub = "navigation-sub";

        /// <summary>Page shell / placeholder for a page.</summary>
        public const string PageShell = "page-shell";

        /// <summary>Main content area container.</summary>
        public const string MainContent = "main-content";

        /// <summary>Generic placeholder (e.g. empty or loading).</summary>
        public const string Placeholder = "placeholder";

        /// <summary>Dashboard shell (future).</summary>
        public const string DashboardShell = "dashboard-shell";

        /// <summary>Entity list container (list/search view per entity; optional data-entity-id).</summary>
        public const string EntityList = "entity-list";

        /// <summary>Entity form container (create/edit view per entity; optional data-entity-id).</summary>
        public const string EntityForm = "entity-form";

        /// <summary>Form field wrapper from PropertyHtmlGenerator.</summary>
        public const string FormField = "form-field";
    }
}
