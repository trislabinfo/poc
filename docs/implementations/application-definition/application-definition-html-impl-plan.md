# Application Definition — HTML generation and Builder/TenantApplication: Implementation Plan

**Document Purpose:** Implementation plan for adding **generated HTML** storage and **HTML rendering support** in the runtime client. After this implementation, the team will manually test the full flow: app creation → installation → release → deploy → runtime (with HTML). This plan is for approval before implementation start.

**Parent document:** [Application Definition — HTML generation and Builder/TenantApplication updates](application-definition-html-and-builder-plan.md)

**Audience:** Architects, backend and frontend developers.

**Status:** Implemented — Step 1 ✅ through Step 7 ✅. Manual E2E testing and optional enhancements (entity view on nav click, unit tests) remain.

**Related:** [Runtime — Full Implementation Plan](../client/runtime-all-impl-plan.md), [Compatibility and Versioning Framework](../client/compatibility-and-versioning-framework.md)

---

## Table of Contents

1. [Pre-implementation decisions and clarifications](#pre-implementation-decisions-and-clarifications)
2. [Summary](#summary)
3. [Scope](#scope)
4. [Current state: what exists vs what must be created](#current-state-what-exists-vs-what-must-be-created)
5. [Implementation order overview](#implementation-order-overview)
6. [Semantic types and shared contract](#semantic-types-and-shared-contract)
7. [Step 1: Semantic types / shared contract](#step-1-semantic-types--shared-contract)
8. [Step 2: ApplicationDefinition storage for HTML](#step-2-applicationdefinition-storage-for-html)
9. [Step 3: HTML generators (per-definition, testable)](#step-3-html-generators-per-definition-testable)
10. [Step 4: AppBuilder release pipeline integration](#step-4-appbuilder-release-pipeline-integration)
11. [Step 5: TenantApplication HTML support](#step-5-tenantapplication-html-support)
12. [Step 6: BFF — initial view as HTML](#step-6-bff--initial-view-as-html)
13. [Step 7: Runtime client — HTML rendering and fallback](#step-7-runtime-client--html-rendering-and-fallback)
14. [Storage and API design](#storage-and-api-design)
15. [Release pipeline integration](#release-pipeline-integration)
16. [MVP: what's easy to miss](#mvp-whats-easy-to-miss)
17. [Manual test: end-to-end flow](#manual-test-end-to-end-flow)
18. [Create vs update checklist](#create-vs-update-checklist)
19. [References](#references)

---

## Pre-implementation decisions and clarifications

Define or resolve the following **before** starting implementation so the team does not block on open questions.

| # | Topic | Decision / clarification |
|---|--------|---------------------------|
| **1** | **Tenant release storage** | **AppBuilder schema** and **TenantApplication schema** both use **ApplicationDefinition** (same `ApplicationRelease` entity from ApplicationDefinition.Domain). Each module defines its **own entity configuration** in its **Infrastructure** layer (AppBuilder.Infrastructure, TenantApplication.Infrastructure) mapping the same entity to its schema. The HTML column is added to the `ApplicationRelease` entity; each module’s migration adds the column to its own schema’s release table (e.g. `appbuilder.application_releases`, tenantapplication’s equivalent). |
| **2** | **Generator implementation location** | Generators and the initial-view composer live **inside the ApplicationDefinition module** (e.g. `ApplicationDefinition.HtmlGeneration` or under `ApplicationDefinition.Application`). AppBuilder and TenantApplication reference ApplicationDefinition and call the composer when releasing. |
| **3** | **Semantic contract on the server** | The server must hold the semantic contract in a **contracts project**. Define semantic component type names and attribute names (e.g. `data-component`, `data-slot`) in **ApplicationDefinition.Contracts** (or the solution’s shared contracts project) so generators and client contract stay in sync. Client `client/packages/contracts` remains the single source for the full contract; server contracts project holds the minimal set needed for HTML generation. |
| **4** | **BFF API for HTML** | **Always return HTML** for the initial view. The BFF endpoint that serves the initial view (e.g. snapshot or initial-view) returns HTML when available; no format query parameter or JSON/HTML variant. If HTML is not available (e.g. legacy release), return 404 or document the fallback (client then uses JSON snapshot path). |
| **5** | **HTML generation failure during release** | If HTML generation **fails**, **block the release**: the app **cannot be released**. Do not save the release with null HTML. Fail the release process and return an error to the caller so the issue can be fixed before releasing. |
| **6** | **When to run HTML generation (trigger)** | Run composers **when the release is approved** (approve release flow). Hook into **ApproveReleaseCommandHandler** (and TenantApplication equivalent), not "create release". Hook into the “release” / “publish” / “finalize” flow that marks the release as the one to serve. |
| **7** | **Generator input shape** | Generators **generate new HTML from the release’s JSON only**. No need to check or diff against the previous version; we are creating a new release. Input: current release’s `NavigationJson`, `PageJson`, etc. (as strings or parsed inside the generator). |
| **8** | **MVP scope for Entity/Property/Relation generators** | **Start with navigation and page only.** For MVP implement **NavigationHtmlGenerator** and **PageHtmlGenerator** (and the composer that uses them). Entity/Property/Relation generators can be added later; composer only calls navigation and page for MVP. |
| **9** | **Composition order** | Use a **simple, fixed order**: **root nav item(s)** first, then **sub nav items**, then **main content area** (e.g. page placeholders). Document this order in Step 3 and in the composer so client and tests share one expectation. |
| **10** | **Storage column type** | Use **nvarchar(max)** for the `initial_view_html` column (SQL Server). For other DB engines use the equivalent (e.g. text/clob for a single HTML fragment). |
| **11** | **Option B: Pre-generate all view HTML at release** | **Chosen.** Pre-generate and store **all** HTML displayed on the UI at release time. **Initial view** = navigation + default view (dashboard) → `initial_view_html`. **Entity-driven views** = list + form **per entity** → table `release_entity_views`. Runtime fetches pre-generated HTML for entity + view type when user navigates (e.g. Customers, Add customer). |
| **12** | **Storage for entity views** | **Chosen: option (b).** Separate table **`release_entity_views`**: columns release_id, entity_id, view_type (list \| form), html. One row per entity per view type per release. Add in existing migration in AppBuilder and TenantApplication. No column on ApplicationRelease. |
| **13** | **Navigation → entity link** | Nav node may have optional **entityId**, **viewType** ("list" \| "form"). Client uses them to request entity view HTML. Generator can emit e.g. `data-entity-id`, `data-view-type`. |
| **14** | **MVP semantic types for entity list/form** | Add to semantic contract: **entity-list**, **entity-form**, **form-field** (and optional **data-entity-id**). Used by EntityHtmlGenerator and PropertyHtmlGenerator. |

---

## Summary

- The runtime currently works with **JSON snapshot** (resolve → snapshot → compatibility → client render from JSON). This plan adds **HTML generation** (server) and **HTML rendering support** (runtime client) for fast first paint and every definition displayed on the UI.
- **ApplicationDefinition** storage: **initial view** → `initial_view_html`; **entity views** (list + form per entity) → table **`release_entity_views`**. The runtime API returns this HTML as “initial view” when requested.
- **AppBuilder** and **TenantApplication** generate HTML **when the release is approved** (ApproveReleaseCommandHandler and equivalent) and persist it; if generation fails, **approval is blocked**. Both use the same ApplicationDefinition composers (initial-view + entity-views) and semantic contract (ApplicationDefinition.Contracts).
- **Runtime client** requests initial view HTML for first paint; on navigation to an entity (e.g. Customers, Add customer), requests and renders pre-generated entity list/form HTML; fallback to JSON when HTML is missing or fails.

---

## Scope

| Area | Planned direction | Notes |
|------|------------------|--------|
| **ApplicationDefinition storage** | **Initial view:** `initial_view_html`. **Entity views:** table `release_entity_views`. All generated at approval. | Step 2 (done) + Step 2b (release_entity_views); update existing migrations. |
| **AppBuilder** | When release is **approved**, run both composers (initial-view + entity-views); persist initial_view_html and rows in **release_entity_views**. On failure, block approval. | Generators inside ApplicationDefinition; each module has own entity config (Infrastructure). |
| **TenantApplication** | Same: when tenant **approves** release, run both composers, persist initial_view_html and rows in release_entity_views; block on failure. Return initial view and entity view HTML via API/BFF. | Same ApplicationRelease entity; own schema and entity configuration. |
| **Semantic types / contract** | Single source of truth for component types and mapping to structure/CSS. Server: **ApplicationDefinition.Contracts**; client: `client/packages/contracts`. Generators and runtime client use same names. | No raw CSS class names stored in DB. |
| **Dashboard elements** | Dashboard shell as static HTML; per-user elements at request time; element content fetched separately or streamed. | Align with “shell + loaders, then fetch element content” in runtime plan. Can be minimal for MVP. |

---

## Current state: what exists vs what must be created

| Artifact | Exists? | Action |
|----------|---------|--------|
| **ApplicationRelease** (ApplicationDefinition.Domain) | Yes | **UPDATE** — add InitialViewHtml ✅; Step 2b: entity list/form HTML in table **release_entity_views** (no new property on ApplicationRelease). |
| **application_releases** table (appbuilder schema) | Yes | **UPDATE** — add initial_view_html ✅; add table **release_entity_views** (Step 2b) in existing migration. |
| **ApplicationSnapshotDto** | Yes | **UPDATE** (optional) — add optional HTML field for responses that include HTML; or use separate endpoint/DTO for “initial view as HTML”. |
| **Runtime BFF** GET snapshot | Yes | **UPDATE** — add support for returning HTML when requested (e.g. Accept header or query flag). |
| **Runtime client** (resolve → snapshot → compatibility → render from JSON) | Yes | **UPDATE** — add path to request/consume initial view as HTML; fallback to JSON render. |
| **client/packages/contracts** | Yes | **UPDATE** — add semantic types / component-type mapping (shared with server generator). |
| **Semantic types / component mapping** | No | **CREATE** — shared contract (e.g. in contracts or design package). |
| **HTML generators (per-definition)** | No | **CREATE** — one generator per type (navigation, page, entity, property, relation) + composer; each testable in isolation. |
| **Release pipeline HTML trigger** | No | **CREATE** — hook in AppBuilder (and TenantApplication if tenant releases) to run generator and persist HTML on release. |
| **TenantApplication release storage for HTML** | Same entity | AppBuilder and TenantApplication both use ApplicationDefinition’s `ApplicationRelease`; each module has its own entity configuration (Infrastructure). Add HTML column to entity; each module’s migration adds column to its schema’s table. |

**Key locations (for implementation):**

| Item | Location |
|------|----------|
| ApplicationRelease entity | `ApplicationDefinition.Domain/Entities/ApplicationRelease.cs` |
| application_releases migration | `AppBuilder.Migrations/Migrations/Schema/20260215002000_CreateApplicationReleasesTable.cs` (update in place per project policy) |
| Snapshot mapper | `ApplicationDefinition.Application/Mappers/ApplicationReleaseMapper.cs` |
| HTML generators + composer | **ApplicationDefinition** module (e.g. `ApplicationDefinition.HtmlGeneration` or Application) |
| Semantic contract (server) | `ApplicationDefinition.Contracts` |
| BFF snapshot / initial-view | `AppRuntime.BFF/Controllers/RuntimeBffController.cs` |
| AppBuilder release flow | Handler/service that performs **release** action (not just create) |
| TenantApplication migrations | TenantApplication.Migrations — update existing release table in its schema |
| Runtime client | `client/apps/runtime/src/` (loaders, routes, renderer) |

---

## Implementation order overview

| Step | What | Creates | Updates |
|------|------|---------|--------|
| 1 ✅ | Semantic types / shared contract | Semantic component types + mapping (client + ApplicationDefinition.Contracts) | client/packages/contracts |
| 2 ✅ | ApplicationDefinition storage for HTML | — | Migration (initial_view_html), ApplicationRelease entity, both entity configurations |
| 2b ✅ | Storage for entity views | — | Add **table** `release_entity_views`; both migrations + repository |
| 3 ✅ | HTML generators (per-definition) | Navigation, Page, Entity (list/form), Property, Relation generators; **initial-view composer** + **entity-views composer** inside ApplicationDefinition | — |
| 4 ✅ | AppBuilder release pipeline | — | **When release is approved**: call both composers, persist InitialViewHtml + rows in release_entity_views; on failure block approval |
| 5 ✅ | TenantApplication HTML support | — | Same: persist initial_view_html + rows in release_entity_views; API to return initial view and entity view HTML |
| 6 ✅ | BFF — initial view as HTML | — | BFF returns **initial view** HTML and **entity view** HTML (by entityId + viewType); 404 when not found |
| 7 ✅ | Runtime client — HTML rendering | — | Loaders, request initial view HTML; render; fallback to JSON |

---

## Semantic types and shared contract

**Goal:** One source of truth for “component type” and “structure/CSS mapping” so that:

- The **server-side HTML generator** produces HTML using the same semantic names (e.g. `data-component="navigation"`, `data-component="page-shell"`).
- The **runtime Svelte client** uses the same names for rendering and for hydrating or matching elements.
- No raw CSS class names are stored in the database; only semantic identifiers.

**Deliverables:**

- Define semantic component types (e.g. navigation, page-shell, dashboard-shell, placeholder) and their expected DOM structure or attributes.
- Document or export the mapping (e.g. TypeScript types + constants in `client/packages/contracts` or `client/packages/design`; backend may consume a shared spec or duplicate the minimal set needed for HTML generation).
- Both generator and client use the same attribute names (e.g. `data-component`, `data-slot`, optional `data-entity-id`) so stored HTML and client stay in sync. **MVP semantic types for entity views (decision #14):** entity-list, entity-form, form-field (and optional data-entity-id). Implemented in semantic-html.ts, SemanticHtmlConstants.cs, semantic-html-contract.md.

**Out of scope for contract itself:** Full design system; only the minimal set needed for “initial view” HTML (navigation + shell).

---

## Step 1: Semantic types / shared contract ✅ Done

**Goal:** Establish the shared contract so Step 3 (generators) and Step 7 (client) can implement against it. The **server** must hold the semantic contract in a **contracts project** (see [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #3).

**Creates:**

- **Client:** Semantic component type constants and types in `client/packages/contracts` (or design).
- **Server:** Same minimal set (component type names, attribute names e.g. `data-component`, `data-slot`) in **ApplicationDefinition.Contracts** (or the solution’s contracts project) so generators can use them without referencing the client.
- Optional: small markdown or JSON spec describing component types and expected HTML structure.

**Updates:**

- None (or add export from contracts/design if new file).

**Dependencies:** None.

**How to test (manual):** Types build; documentation or spec is reviewable; generator and client can later reference the same names.

---

## Step 2: ApplicationDefinition storage for HTML ✅ Done

**Goal:** Persist generated HTML per release so BFF and TenantApplication can return it.

**Creates:** Nothing new.

**Updates:**

- **Migration:** Update the **existing** migration that creates `application_releases` in **AppBuilder.Migrations** (e.g. `20260215002000_CreateApplicationReleasesTable.cs`): add column `initial_view_html` as **nvarchar(max)** (SQL Server) or equivalent. Do the same in **TenantApplication** migrations for its schema’s release table. Do not add new migrations (project policy: update existing when DB is recreated).
- **Domain:** `ApplicationRelease` — add property e.g. `InitialViewHtml` (nullable string).
- **Entity configuration:** Each module (AppBuilder.Infrastructure, TenantApplication.Infrastructure) defines its own entity configuration for `ApplicationRelease`; add the new column mapping in both.
- **Repository / persistence:** Ensure Create and Update paths can set the new field; no new repository interface required if using existing save.
- **DTOs:** Decide whether `ApplicationSnapshotDto` gets an optional `InitialViewHtml` or a separate DTO is used for “initial view” response; document in Step 6.

**Dependencies:** None (can be done before generator exists; column can be null).

**How to test (manual):** Migration runs; entity has property; can write/read via existing release create/update (e.g. set HTML in a test or script).

---

## Step 2b: Storage for entity views (table release_entity_views) ✅ Done

**Goal:** Persist pre-generated **entity view HTML** (list + form per entity) per release in a **separate table** `release_entity_views` (decision #12) so the BFF can return it when the user navigates to e.g. Customers list or Add customer form.

**Creates:**

- **Table** `release_entity_views` in the same schema as the release table (appbuilder, tenantapplication): columns release_id, entity_id, view_type (list \| form), html (text/nvarchar(max)). One row per entity per view type per release.
- **Repository** (or equivalent) to save and load rows: by release_id; and by release_id + entity_id + view_type for BFF lookup.
- **Migration (AppBuilder + TenantApplication):** Update the **existing** migration that creates the release table: add creation of table `release_entity_views` with columns as above. Same policy: do not add a new migration file.

**Updates:**

- **Entity configuration:** No new property on `ApplicationRelease`; the new table is separate. Each module (AppBuilder.Infrastructure, TenantApplication.Infrastructure) may need a new entity for the table and its configuration, or a dedicated repository that uses the same DbContext/schema.

**Dependencies:** None (can be done in parallel with or after Step 2).

**How to test (manual):** Migration runs; can insert/select rows by release_id and by release_id + entity_id + view_type.

---

## Step 3: HTML generators (per-definition, testable) ✅ Done

**Goal:** Produce “initial view” HTML from application schema using the semantic contract. **Each definition type has its own HTML generator** so responsibilities are separated and each generator is **independently testable**.

**Design:**

- **One generator per definition type:** navigation, page (MVP); entity, property, relation later. Each generator has a single responsibility: map one kind of schema fragment to HTML using the semantic contract from **ApplicationDefinition.Contracts** (see [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #3).
- **Testability:** Every generator is a pure or near-pure function (or small service) with well-defined input (current release’s JSON only; no previous version) and output (HTML string). Unit tests pass known input and assert on generated HTML (e.g. `data-component`, structure, no raw CSS class names).
- **Orchestrator / composer:** A thin “initial view composer” takes the release’s snapshot JSON, calls the generators, and assembles the final HTML in a **fixed order**: **root nav item(s)** → **sub nav items** → **main content area** (e.g. page placeholders). See [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #9.

**Creates (inside ApplicationDefinition module):**

- **Per-definition HTML generators** (e.g. under `ApplicationDefinition.HtmlGeneration` or `ApplicationDefinition.Application`):
  - **NavigationHtmlGenerator** — input: navigation JSON; output: navigation fragment HTML (root nav, then sub nav; semantic attributes only).
  - **PageHtmlGenerator** — input: page definition(s) or PageJson; output: page shell / placeholder HTML per page.
  - *(MVP only: navigation + page. Entity/Property/Relation generators later; see [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #8.)*
- **Initial-view composer** — input: release’s NavigationJson, PageJson (new release only; no previous version). Composes in order: root nav → sub nav → main content. Both AppBuilder and TenantApplication call this when **releasing** (see decision #6).
- **Location:** **Inside the ApplicationDefinition module**; AppBuilder and TenantApplication reference ApplicationDefinition and use the composer. See [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #2.

**Updates:** None to storage or client at this step.

**Dependencies:** Step 1 (semantic contract). Semantic constants live in **ApplicationDefinition.Contracts**. Input: current release’s JSON only (no previous version). See [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #7, #8.

**Testing (required):**

- **Unit tests per generator:** For each generator, tests with fixed input (e.g. minimal valid navigation JSON, one page definition, one entity) and assert: output is valid HTML; contains expected semantic attributes (e.g. `data-component="navigation"`); no raw CSS class names in output; structure matches contract.
- **Unit test for composer:** Given fixed snapshot JSON (or fixture), assert composed HTML contains outputs from the relevant generators in the expected order/structure.
- **How to test (manual):** Run the unit test suite for all generators and composer; optional: console or integration test that passes sample release JSON and prints/inspects the full HTML.

---

## Step 4: AppBuilder release pipeline integration ✅ Done

**Goal:** **When the app is released** (release action), run the initial-view composer and persist HTML. If HTML generation fails, **block the release** — the app cannot be released (see [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #5, #6).

**Creates:**

- Integration in the **release** flow (not at “create release”): when the user/system performs the release action, call the initial-view composer with the release’s JSON, set `InitialViewHtml` on the release entity, then persist. If the composer or any generator fails, fail the release and return an error.

**Updates:**

- AppBuilder: in the handler or service that **releases** the app (e.g. finalize release, publish, or the step that marks the release as active/served), call the ApplicationDefinition composer with the release’s snapshot JSON. Set `InitialViewHtml` and save. On generator failure, do not save; return error and block the release.

**Dependencies:** Step 2 (initial_view_html), Step 2b (release_entity_views table), Step 3 (all generators + both composers).

**How to test (manual):** Trigger **approve release** via AppBuilder API; confirm `initial_view_html` is set and `release_entity_views` has rows. Simulate generator failure and confirm approval is not completed (error returned).

---

## Step 5: TenantApplication HTML support ✅ Done

**Goal:** Tenant-specific releases store and return generated HTML. TenantApplication uses the **same** `ApplicationRelease` entity (ApplicationDefinition); it has its **own entity configuration** in TenantApplication.Infrastructure and its own schema table. When a tenant **releases** an app, run the same composer (from ApplicationDefinition) and persist HTML. On failure, block the release (same as AppBuilder).

**Creates:**

- TenantApplication migration: add `initial_view_html` (nvarchar(max) or equivalent) to its release table in its schema (see [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #1).

**Updates:**

- TenantApplication.Infrastructure: entity configuration for `ApplicationRelease` — map the new `InitialViewHtml` column.
- TenantApplication: in the **release** flow (when tenant releases the app), call the ApplicationDefinition initial-view composer, set `InitialViewHtml`, persist. On generator failure, block the release.
- TenantApplication snapshot (or “initial view”) API to return HTML when requested (or delegate to BFF; see Step 6).

**Dependencies:** Step 2, Step 3, Step 4 (pattern from AppBuilder).

**How to test (manual):** Create a tenant release; verify HTML is stored and can be retrieved (via TenantApplication or BFF).

---

## Step 6: BFF — initial view as HTML ✅ Done

**Goal:** The BFF **always returns HTML** for the initial view when available (see [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #4). No format parameter; the initial-view/snapshot response is HTML.

**Creates:**

- Endpoint (or snapshot route) that returns the initial view as **text/html**: e.g. `GET /api/runtime/initial-view?applicationReleaseId=` or snapshot returning HTML. Response is the stored `InitialViewHtml` string. If HTML is not available (e.g. legacy release), return 404 or document fallback so the client can use the JSON snapshot path.

**Updates:**

- BFF: (1) Resolve snapshot (AppBuilder or TenantApplication by release ID). (2) For initial-view request: return `InitialViewHtml` as text/html. (3) For entity-view request: look up release_entity_views by release_id + entity_id + view_type; return that row's html as text/html.

**Dependencies:** Step 2, Step 4, Step 5 (HTML must be stored and readable by BFF).

**How to test (manual):** Call BFF initial-view (or snapshot) endpoint with `applicationReleaseId`; expect 200 with `text/html` body when HTML is stored. For release without HTML (e.g. legacy), expect 404; client falls back to JSON snapshot.

---

## Step 7: Runtime client — HTML rendering and fallback ✅ Done

**Goal:** Runtime client requests initial view as HTML when beneficial, renders it for fast first paint, and falls back to existing JSON + client renderer when HTML is missing or fails.

**Creates:**

- Optional: loader or API helper that requests HTML (e.g. same BFF URL with format=html or Accept header).
- Render path: inject HTML into a root container (e.g. `innerHTML` or slot) and/or use it for first paint; ensure semantic attributes match client expectations (hydration or progressive enhancement can be later).

**Updates:**

- Runtime client: in the resolve → snapshot flow, add branch: request “initial view as HTML” (e.g. first load); on success, render HTML into shell; on failure or missing HTML, use existing JSON snapshot + client renderer.
- Use semantic types from Step 1 so client structure matches server-generated HTML (e.g. same `data-component` values).

**Dependencies:** Step 1 (contract), Step 6 (BFF returns HTML).

**How to test (manual):** Open runtime URL for an app that has HTML; confirm first paint shows server-rendered HTML. For an app without HTML or on error, confirm fallback to JSON render. No CORS or console errors.

---

## Storage and API design

**Storage:**

- **Where:** Column on the release table in each schema. AppBuilder has `appbuilder.application_releases.initial_view_html`; TenantApplication has its own release table in its schema with the same column. Same `ApplicationRelease` entity; each module’s migration and entity configuration in Infrastructure (see [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #1).
- **Entity views (decision #12):** Table **release_entity_views**: columns release_id, entity_id, view_type (list \| form), html. One row per entity per view type per release. Add in existing migration in both modules.
- **Format (initial_view_html):** Single HTML string (navigation + shell fragment). Column type: **nvarchar(max)** (SQL Server) or equivalent. Semantic attributes only. See [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #10.
- **Migration:** Update the **existing** migration(s) that create the release table in AppBuilder and in TenantApplication; do not add new migrations (see [Runtime plan — Database migrations](../client/runtime-all-impl-plan.md#database-migrations)).

**API:**

- **Initial view:** `GET /api/runtime/initial-view?applicationReleaseId=` returns `text/html` when `InitialViewHtml` is present. If not available, return 404; client uses JSON snapshot path (decision #4). **Entity view:** `GET /api/runtime/view?applicationReleaseId=&entityId=&viewType=list|form` returns `text/html` for the requested entity list or form from table `release_entity_views`. If not found, return 404. Document in BFF and API doc.

---

## Release pipeline integration

- **When HTML is generated:** **When the release is approved** (approve release flow; decision #6). Composer runs in the “release” / “publish” / “finalize” flow.
- **Who runs the composers:** ApplicationDefinition initial-view and entity-views composers (Step 3) are called from AppBuilder (Step 4) and TenantApplication (Step 5) when they approve the release. Same input: the release’s NavigationJson, PageJson (new release only; no previous version).
- **Failure:** If HTML generation fails, **block the approval**; return error (decision #5).
- **Tenant vs platform:** Both use the same ApplicationRelease entity and the same two composers; each module has its own schema, entity configuration, and migrations for initial_view_html and table release_entity_views.

---

## MVP: what's easy to miss

| Item | Why it matters |
|------|----------------|
| **Semantic contract first** | Generator and client must use the same names; implement Step 1 before Step 3 and Step 7. |
| **Fallback when HTML missing** | Existing releases or errors must still work; client must fall back to JSON snapshot + renderer. |
| **Migration policy** | Update existing migration for `application_releases`; do not add a new migration. Sync entity, configuration, and all references. |
| **Seed/demo release with HTML** | For manual E2E test, at least one release must have `initial_view_html` populated (run generator or seed script). |
| **BFF always returns HTML** | Initial-view endpoint returns HTML when available; no format param. Client uses this for first paint; if 404, client falls back to JSON snapshot. See [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #4. |
| **Error handling** | If HTML generation fails during **approve release**, **block the approval**; return error. Do not save with null HTML. See [Pre-implementation decisions](#pre-implementation-decisions-and-clarifications) #5. |

---

## Manual test: end-to-end flow

After implementation, verify:

1. **App creation** — Create an application in AppBuilder (or use existing).
2. **Installation** — Install app to a tenant (if applicable).
3. **Release** — Approve a release with navigation/pages/entities; confirm initial_view_html is set and release_entity_views has rows (e.g. one entity list + one entity form).
4. **Deploy** — No separate deploy step required for MVP if “release” is the unit; ensure active release is the one with HTML.
5. **Runtime** — Open runtime URL (e.g. `http://localhost:5173/<tenantSlug>/<appSlug>/production`); confirm first paint uses server-rendered HTML when available.
6. **Fallback** — Use an app/release without HTML or simulate error; confirm client shows JSON-rendered app and clear error when appropriate.

**Prerequisites:** DB migrated, seed or demo data with at least one release that has `initial_view_html` set and at least one row in `release_entity_views`, BFF and runtime client running with correct config. Document tenant slug, app slug, release ID, and one entityId for testers.

---

## Create vs update checklist

**CREATE**

- [x] Semantic component types and mapping: **client** in `client/packages/contracts`; **server** in **ApplicationDefinition.Contracts**. ✅
- [x] Per-definition HTML generators **inside ApplicationDefinition module**: Navigation (3.1), Page (3.2), Entity list/form (3.3, 3.4), Property (3.5), Relation (3.6). Initial-view composer (3.7) and entity-views composer (3.8). ✅ (Unit tests for each generator and both composers optional.)
- [x] BFF support for returning HTML: GET `/api/runtime/initial-view`, GET `/api/runtime/view?entityId=&viewType=list|form`. ✅
- [x] Runtime client path: request initial view as HTML and render it; fallback to JSON. ✅

**UPDATE**

- [x] Existing migration for `application_releases` (add `initial_view_html`); same for TenantApplication; then entity, both entity configurations. ✅
- [x] ApplicationRelease entity — add InitialViewHtml + SetInitialViewHtml(). ✅
- [x] Storage for entity views (Step 2b): **table** `release_entity_views` (release_id, entity_id, view_type, html); add in existing migration in both modules; repository to save/load by release and by release+entity+viewType. ✅
- [x] AppBuilder **approve release** flow — call **both** composers (initial-view + entity-views), persist InitialViewHtml and rows in release_entity_views; on failure block approval. ✅
- [x] TenantApplication — add table `release_entity_views` in migration; in **approve release** flow call both composers, persist initial_view_html and rows in release_entity_views; on failure block approval. Return initial view and entity view HTML when requested. ✅
- [x] Runtime BFF — initial-view and entity view endpoints return HTML when available; 404 if not. No format param. ✅
- [x] Runtime client — loaders and render path for HTML; use semantic contract; fallback to JSON render. ✅
- [x] `client/packages/contracts` (or design) — export semantic types used by generator and client. ✅

**MVP-only**

- [ ] Seed or demo release with HTML populated for manual E2E.
- [ ] Document: BFF always returns HTML for initial view when available; client fallback to JSON snapshot on 404.

---

## References

- [Application Definition — HTML generation and Builder/TenantApplication updates](application-definition-html-and-builder-plan.md) — Parent scope and summary.
- [Runtime — Full Implementation Plan](../client/runtime-all-impl-plan.md) — BFF, snapshot, runtime client; database migration policy.
- [Compatibility and Versioning Framework](../client/compatibility-and-versioning-framework.md) — Contract-first; schema version.
- [07-DB-MIGRATION-FLOW](../../ai-context/07-DB-MIGRATION-FLOW.md) — Migration runner and order (if present).
