/**
 * Semantic HTML contract for initial-view generation and runtime rendering.
 * Server-side HTML generators and the runtime Svelte client use the same names
 * so stored HTML and client renderers stay in sync. No raw CSS class names in DB.
 *
 * @see docs/implementations/application-definition/application-definition-html-impl-plan.md
 */

/** HTML attribute used to identify component type. Use only these values in generated HTML. */
export const HTML_ATTR_COMPONENT = 'data-component' as const;

/** HTML attribute for slot/role within a component (e.g. root vs sub). */
export const HTML_ATTR_SLOT = 'data-slot' as const;

/** Optional HTML attribute for entity-scoped views: entity id (e.g. for entity-list, entity-form). */
export const HTML_ATTR_ENTITY_ID = 'data-entity-id' as const;

/**
 * Semantic component types for initial-view HTML.
 * Composition order: root nav → sub nav → main content (see IMPL plan).
 */
export const SemanticComponentType = {
  /** Root-level navigation item */
  NavigationRoot: 'navigation-root',
  /** Child/sub navigation item */
  NavigationSub: 'navigation-sub',
  /** Page shell / placeholder for a page */
  PageShell: 'page-shell',
  /** Main content area container */
  MainContent: 'main-content',
  /** Generic placeholder (e.g. for empty or loading) */
  Placeholder: 'placeholder',
  /** Dashboard shell (future) */
  DashboardShell: 'dashboard-shell',
  /** Entity list container (list/search view per entity; optional data-entity-id). */
  EntityList: 'entity-list',
  /** Entity form container (create/edit view per entity; optional data-entity-id). */
  EntityForm: 'entity-form',
  /** Form field wrapper from PropertyHtmlGenerator. */
  FormField: 'form-field',
} as const;

export type SemanticComponentTypeValue =
  (typeof SemanticComponentType)[keyof typeof SemanticComponentType];

/** Attribute names as a tuple for iteration or validation */
export const SEMANTIC_HTML_ATTRIBUTES = [
  HTML_ATTR_COMPONENT,
  HTML_ATTR_SLOT,
  HTML_ATTR_ENTITY_ID,
] as const;
