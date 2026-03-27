# Application Definition — HTML generation and Builder/TenantApplication updates

**Document Purpose:** Plan for storing **generated HTML** in ApplicationDefinition (and related storage) and for updates to **AppBuilder** and **TenantApplication** to support HTML generation. This is **outside** the scope of [Runtime — Full Implementation Plan](../client/runtime-all-impl-plan.md); it is a future implementation to be refined when work starts.

**Audience:** Architects, backend and frontend developers.

**Status:** Planning (future implementation; details to be refined).

**Implementation plan (for approval before impl start):** [Application Definition — HTML implementation plan](application-definition-html-impl-plan.md)

**Related:** [Runtime — Full Implementation Plan](../client/runtime-all-impl-plan.md), [Compatibility and Versioning Framework](../client/compatibility-and-versioning-framework.md)

---

## Summary

- **ApplicationDefinition** (and/or release/snapshot storage) will be extended to **store generated HTML** (e.g. navigation, dashboard shell) produced from the application schema at release time. This enables fast first paint: the runtime API can return HTML directly instead of only JSON.
- **AppBuilder** and **TenantApplication** modules need updates to **support HTML generation**: when an application is released and the schema is created, generate HTML from the schema and persist it (e.g. in a DB table or column). Both modules may participate in the release pipeline that produces and stores this HTML.
- **Semantic types (contract):** Definitions used to generate HTML (entity, relations, properties, component types) must be based on **semantic types** from a shared contract — not raw CSS class names. The **runtime Svelte client** and the **server-side HTML generator** both consume the **same mapping** (same contract) so that stored HTML and client renderers stay in sync (e.g. no “class ABC” in DB while the client was renamed to “XYZ”).

---

## Scope (to be refined)

| Area | Planned direction | Notes |
|------|------------------|--------|
| **ApplicationDefinition storage** | Add storage for generated HTML (e.g. navigation HTML, dashboard shell HTML) per release or snapshot. | Table/column design and migration to be defined. |
| **AppBuilder** | Participate in release pipeline: when a release is created/finalized, trigger or perform HTML generation from the application schema; persist result. | May call a shared HTML generator or contain generator logic; contract (semantic types) must be shared with Runtime client. |
| **TenantApplication** | Support HTML for tenant-specific releases (e.g. tenant snapshot + generated HTML). Return HTML from API when the runtime requests “initial view” as HTML. | May aggregate platform + tenant HTML; dashboard elements may be shell + loaders first, then element content separately. |
| **Semantic types / contract** | Single source of truth for component types and their mapping to structure/CSS. Builder, Runtime client, and server-side HTML generator all use it. | Shared package or design system; no raw CSS class names stored in DB. |
| **Dashboard elements** | Dashboard shell can be static HTML; per-user elements (permissions, preferences) may be resolved at request time; element content (data-driven) fetched separately or streamed. | Align with “shell + loaders, then fetch element content” approach in runtime plan. |

---

## References

- [Runtime — Full Implementation Plan](../client/runtime-all-impl-plan.md) — Runtime BFF, client, optional “initial view as HTML” and dashboard loaders.
- [Compatibility and Versioning Framework](../client/compatibility-and-versioning-framework.md) — Contract-first; schema version and support matrix.
