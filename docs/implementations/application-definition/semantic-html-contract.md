# Semantic HTML contract (initial view)

**Purpose:** Single source of truth for HTML attribute names and component type values used in generated initial-view HTML and in the runtime client. Keeps server-generated HTML and client renderers in sync.

**Used by:** ApplicationDefinition HTML generators (server), runtime Svelte client (client).  
**See:** [Application Definition — HTML implementation plan](application-definition-html-impl-plan.md) Step 1.

---

## Attributes

| Attribute        | Value / usage |
|-----------------|----------------|
| `data-component` | Semantic component type (see below). **Only** these values may be stored in DB. |
| `data-slot`      | Optional slot/role within a component (e.g. root vs sub). |
| `data-entity-id` | Optional; entity id for entity-list / entity-form (MVP). |

---

## Component types (values for `data-component`)

| Value              | Meaning |
|--------------------|--------|
| `navigation-root`  | Root-level navigation item. |
| `navigation-sub`   | Child/sub navigation item. |
| `page-shell`       | Page shell / placeholder for a page. |
| `main-content`     | Main content area container. |
| `placeholder`      | Generic placeholder (empty or loading). |
| `dashboard-shell`  | Dashboard shell (future). |
| `entity-list`      | Entity list container (list/search view; optional `data-entity-id`). MVP. |
| `entity-form`      | Entity form container (create/edit view; optional `data-entity-id`). MVP. |
| `form-field`       | Form field wrapper from PropertyHtmlGenerator. MVP. |

---

## Composition order (initial view)

Generated HTML is composed in this order:

1. **Root nav item(s)**
2. **Sub nav items**
3. **Main content area** (e.g. page placeholders)

---

## Implementation

- **Client:** `client/packages/contracts` — `semantic-html.ts` (exported from `index.ts`).
- **Server:** `ApplicationDefinition.Contracts` — `SemanticHtml/SemanticHtmlConstants.cs`.

Keep server constants in sync with the client when adding or changing types.
