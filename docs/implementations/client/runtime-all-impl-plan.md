# Runtime — Full Implementation Plan

**Document Purpose:** Single guide for implementing the full runtime functionality: frontend scaffold (all three clients and shared packages), backend AppRuntime module creation, Runtime BFF, and runtime client. Ensures nothing is missed; defines what to **create** vs **update** and in which order. Use this doc as the master checklist for development.

**Audience:** Developers, tech leads, architects, AI coding agents.

**How to use this document:** Follow the steps in order (1 → 9). After completing each step, run the **How to test (manual)** instructions for that step to verify the minimum valuable outcome before moving on; if a step cannot be fully tested in isolation, the note in that section explains what to defer. Use the **Create vs update checklist** at the end to avoid missing artifacts. For detailed API contracts, DTOs, and CORS/auth, see [Runtime Server Implementation Plan](runtime-server-impl-plan.md). For client phases (contracts, loaders, renderer, versioning), see [Runtime Client Implementation Plan](runtime-client-impl-plan.md). For compatibility and versioning rules, see [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md). The **Key backend types and locations** table and **BFF API quick reference** below give implementation-ready references without leaving this doc.

**Status:** In progress — Steps 1–6 and 8 completed; Step 7 skipped (future when auth added).

**Implementation step status:** 1 ✅ | 2 ✅ | 3 ✅ | 4 ✅ | 5 ✅ | 6 ✅ | 7 ⏭ skipped | 8 ✅ | 9 —

**Related:** [Runtime Server Implementation Plan](runtime-server-impl-plan.md), [Runtime Client Implementation Plan](runtime-client-impl-plan.md), [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md), [Solution Structure - Runtime](../../ai-context/02-SOLUTION-STRUCTURE.md)

---

## Summary

The runtime allows end users to run no-code applications: the **runtime client** (browser) calls only the **Runtime BFF**, which delegates to **TenantApplication API** (resolve, tenant snapshot), **AppBuilder** (platform snapshot), and **Runtime API (AppRuntime)** (compatibility, datasource/engine execution). Compatibility and versioning follow one pattern for all feature types (see [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md)).

**Implementation order:** (1) Scaffold frontend: three client apps and shared packages (contracts, design). (2) Create the **AppRuntime** module in `server/src/Product` (it does not exist today). (3) Expose backend capabilities (TenantApplication resolve/snapshot, AppBuilder snapshot, AppRuntime compatibility). (4) Implement Runtime BFF. (5) Implement runtime client (contracts, loaders, compatibility, renderer). (6) Runtime execution (datasource, engines). (7) Auth at BFF. (8) Versioning/adapters. (9) Testing and observability.

**Runtime module and engines:** The AppRuntime module (API, Application, Infrastructure) is responsible for **all runtime engines** (datasource, workflow, rule engine, etc.). AppRuntime already has **AppRuntime.Migrations** for the runtime schema (e.g. `appruntime`); any future compatibility/support-matrix tables would live there.

**MVP scope:** The **minimum viable product** is: **Steps 1–5** working end-to-end — open a URL in the runtime client → resolve → snapshot → compatibility → **render one app** (navigation + at least one page). For MVP you may **minimize or defer:** Step 6 (execution) — stub or one datasource only; Step 7 (auth) — optional for local dev (no auth or public resolve); Step 8 — support a **single schema version** only; Step 9 — **manual testing** only, no automated E2E required. Delivering Steps 1–5 with the additions below is enough to demonstrate the runtime pipeline.

**Frontend stack:** All client applications (Builder, Dashboard, Runtime) are built with **Svelte** and **SvelteKit**. Use SvelteKit for routing, SSR/SSG if needed, and project structure; use Vite (SvelteKit’s default) for build and dev server. Shared packages (`client/packages/contracts`, `client/packages/design`) are consumed by all three apps under `client/apps/`. This plan uses Svelte/SvelteKit terminology (e.g. `+page.svelte`, `+layout.svelte`, `src/routes`) where relevant.

## Building and running client apps

- **From repo root (monorepo):** Install dependencies once: `pnpm install` (or `npm install` if using npm workspaces). Build shared packages first: `pnpm run build --filter @datarizen/contracts` and `--filter @datarizen/design` (or equivalent workspace names). Then run a client in dev mode: `pnpm run dev --filter runtime` (or `cd client/apps/runtime && pnpm run dev`). Repeat for `builder` and `dashboard` as needed.
- **From a single app:** `cd client/apps/runtime && pnpm install && pnpm run dev`. SvelteKit dev server typically runs at `http://localhost:5173` (or next available port). Ensure `client/packages/contracts` and `client/packages/design` are built and linked (workspace dependency) so the app resolves them.
- **Production build:** From root: `pnpm run build` (if root script builds all) or `pnpm run build --filter runtime` for the runtime app only. From app: `cd client/apps/runtime && pnpm run build`. Output is in `build/` or `.svelte-kit/output` per SvelteKit config.
- **Run production build locally:** `cd client/apps/runtime && pnpm run preview` (SvelteKit preview). Use this to verify the built app before deployment.

**First-time run (MVP):** To run the full stack once Steps 1–5 are in place: (1) Ensure database is running and **migrations applied** (e.g. `dotnet run --project server/src/MigrationRunner` or your migration process). (2) **Seed or create minimal data:** at least one tenant, one application definition, and one **application release** with snapshot (navigation, pages) so resolve and snapshot return real data — use a seed script, migration, or manual API/DB setup; document in the repo (e.g. `docs/development/seed-runtime-demo.md` or README). (3) Start the **backend host** that includes BFF + TenantApplication + AppBuilder + AppRuntime (single host recommended for MVP). (4) Set **BFF configuration** (appsettings): if modules are in the same process, no URLs needed; if separate hosts, set base URLs for TenantApplication, AppBuilder, AppRuntime so the BFF can call them. (5) Start the **runtime client** with `VITE_API_BASE_URL` pointing to the BFF. (6) Open in browser: `http://localhost:5173/<tenantSlug>/<appSlug>/production` (or your route) and confirm the app loads and renders.

---

## Minimum viable product: what’s easy to miss

These items are not always obvious from the step list but are needed for a working MVP.

| Item | Why it matters for MVP |
|------|------------------------|
| **Seed / demo data** | Resolve and snapshot need at least one tenant, one app, one release with snapshot JSON. Without it, Step 5 testing shows only 404 or empty. Add a seed script, a development-only migration, or clear instructions to create one tenant + app + release (and document the slugs/IDs for testing). |
| **BFF configuration** | The BFF must know how to call TenantApplication, AppBuilder, and AppRuntime. When all run in **one host**, use in-process calls (no config). When they run in **separate hosts**, configure base URLs (e.g. `Services:TenantApplication:BaseUrl`, `Services:AppBuilder:BaseUrl`, `Services:AppRuntime:BaseUrl`) in the BFF’s appsettings or environment. Document the chosen approach. |
| **Single host for MVP** | Running BFF + TenantApplication + AppBuilder + AppRuntime in a single host avoids gateway routing and CORS between backend services; only the runtime client needs CORS from the BFF. Prefer this for MVP. |
| **Loading state in runtime client** | While resolve, snapshot, and compatibility run, the UI should show a loading indicator (e.g. “Loading app…”). Otherwise users see a blank screen and may assume a failure. |
| **.env.example** | Commit `client/apps/runtime/.env.example` with `VITE_API_BASE_URL=http://localhost:5xxx` (and any auth-related vars added in Step 7). New developers then know which variables to set. |
| **Minimal component set for MVP** | The renderer needs at least a minimal component set to show something. For MVP, implement only what’s required to render one demo app (e.g. layout container, text, maybe button). Full set (DataGrid, Form, etc.) can follow. |
| **Error messages** | When resolve returns 404, snapshot fails, or compatibility says incompatible, the runtime client must show a **clear, user-facing message** (not a raw error or blank). Include this in Step 5 deliverable. |

---

## Database migrations

**Policy: update existing migrations, do not add new ones when the DB is recreated.**  
If the database will be recreated (e.g. local dev, greenfield), any schema change must be made by **editing the existing migration file(s)** that create or alter the affected table. **Do not** add a new migration for that change — the recreated DB has no history, so the updated migration is the single source of truth.

**When you change a migration (or add a new one for a new module), keep everything in sync:**

| Layer | What to update |
|-------|----------------|
| **Domain model** | Entity class: add/remove/rename properties so they match the table columns. |
| **Entity configuration** | EF Core `IEntityTypeConfiguration<T>` (or FluentMigrator table definition) so the mapping matches the entity and the migration. |
| **References** | Repositories (queries, inserts, updates), DTOs, mappers, API responses — any code that reads or writes the changed column(s). |

**AppRuntime (new module):** AppRuntime has no existing migrations. **CREATE** new migration(s) in `AppRuntime.Migrations` for the AppRuntime schema (e.g. `appruntime` schema, `runtime_versions`, component support tables). If the repo already contains placeholder or draft AppRuntime migration files, **UPDATE** those files in place instead of adding more. Align with the project’s migration tool (e.g. FluentMigrator or EF Core) and with [AppRuntime Migrations](../latest-plan/appruntime-migrations.md) if present.

**Existing modules (ApplicationDefinition, AppBuilder, TenantApplication):** If the runtime work requires **new columns or tables** (e.g. `SchemaVersion` on the application release table, or a new table for support matrix), **locate the existing migration** that creates or last alters that table and **UPDATE that migration file** — add the column/table there. Do **not** create a new migration. Then **UPDATE** the domain entity, entity configuration, and all references (repository, DTOs, mappers, snapshot/compatibility responses) as in the table above.

**Migration runner:** Ensure the MigrationRunner (or startup migration execution) includes the AppRuntime migrations module and runs migrations in the correct order (dependencies first). See [07-DB-MIGRATION-FLOW](../../ai-context/07-DB-MIGRATION-FLOW.md) for the project’s migration flow.

---

## Table of Contents

1. [Current state: what exists vs what must be created](#current-state-what-exists-vs-what-must-be-created)
2. [Building and running client apps](#building-and-running-client-apps) (includes First-time run for MVP)
3. [Minimum viable product: what's easy to miss](#minimum-viable-product-whats-easy-to-miss)
4. [Database migrations](#database-migrations)
5. [Implementation order overview](#implementation-order-overview)
6. [Step 1: Frontend scaffold — clients and shared packages](#step-1-frontend-scaffold--clients-and-shared-packages)
7. [Step 2: Create AppRuntime module (server)](#step-2-create-appruntime-module-server)
8. [Step 3: Backend capabilities for BFF](#step-3-backend-capabilities-for-bff)
9. [Step 4: Runtime BFF](#step-4-runtime-bff)
10. [Step 5: Runtime client — contracts, loaders, compatibility, renderer](#step-5-runtime-client--contracts-loaders-compatibility-renderer)
11. [Step 6: Runtime execution (datasource, engines)](#step-6-runtime-execution-datasource-engines)
12. [Step 7: Authentication at BFF](#step-7-authentication-at-bff)
13. [Step 8: Compatibility and versioning](#step-8-compatibility-and-versioning)
14. [Step 9: Testing and observability](#step-9-testing-and-observability)
15. [Optional / later improvements](#optional--later-improvements)
16. [Create vs update checklist](#create-vs-update-checklist)
17. [References](#references)

---

## Current state: what exists vs what must be created

| Artifact | Exists? | Action |
|----------|---------|--------|
| **server/src/Product/** | Yes | Tenant, TenantApplication, Identity, User, Feature, AppBuilder, ApplicationDefinition. |
| **server/src/Product/AppRuntime** | **Yes** ✅ | Created (Step 2). Domain, Application, Contracts, Infrastructure, Api, Migrations, Module; AppRuntimeServiceHost for microservices. |
| **server/src/Hosts/MultiAppRuntimeHost** | Yes | **UPDATE** — loads AppRuntime module. |
| **Runtime BFF** (host or API path) | **Yes** ✅ | AppRuntime.BFF + RuntimeBFFHost + MultiAppRuntimeHost; RuntimeBFFHost in microservices topology (Step 4). Step 6: POST /api/runtime/datasource/execute. |
| **client/apps/builder** | Yes ✅ | Created (Step 1). |
| **client/apps/dashboard** | Yes ✅ | Created (Step 1). |
| **client/apps/runtime** | Yes ✅ | Created (Step 1). Step 5: loaders, shared API client, env, minimal renderer (nav + placeholder), resolve → snapshot → compatibility → render flow. Step 6: apiPost, executeDatasource(), renderer “Run” per datasource. |
| **client/packages/contracts** | Yes ✅ | Created (Step 1). |
| **client/packages/design** (or shared UI lib) | Yes ✅ | Created (Step 1). |
| **TenantApplication** resolve API | Yes ✅ | **CREATE** (Step 3) — GET api/tenantapplication/resolve; IApplicationResolverService + ResolvedApplicationDto. |
| **ApplicationDefinition** ApplicationSnapshotDto | Yes ✅ | **CREATE** (Step 3) — in ApplicationDefinition.Contracts + mapper in ApplicationDefinition.Application. |
| **AppBuilder** snapshot by release ID | Yes ✅ | **CREATE** (Step 3) — GetReleaseSnapshotQuery + GET api/appbuilder/releases/{id}/snapshot. |
| **TenantApplication** snapshot by tenant release ID | Yes ✅ | **CREATE** (Step 3) — GET api/tenantapplication/releases/{id}/snapshot. |

**Key backend types and locations (for implementation):**

| Type / interface | Location (or to create) | Notes |
|------------------|-------------------------|--------|
| `ApplicationRelease` | `ApplicationDefinition.Domain` (entity) | Has NavigationJson, PageJson, DataSourceJson, EntityJson, SchemaJson. |
| `ApplicationReleaseDto` | `ApplicationDefinition.Contracts` | Current DTO does not include snapshot JSON; do not use for runtime snapshot response. |
| `ApplicationSnapshotDto` | ✅ `ApplicationDefinition.Contracts.DTOs` | Snapshot response (navigation, pages, dataSources, entities, optional SchemaVersion). |
| `IApplicationReleaseRepository` | `ApplicationDefinition.Domain`; impl in `AppBuilder.Infrastructure` | GetByIdAsync; used in AppBuilder GetReleaseSnapshotQuery. |
| `ResolvedApplicationDto` | ✅ `TenantApplication.Application.DTOs` | TenantId, TenantSlug, ApplicationReleaseId, EnvironmentConfiguration, IsTenantRelease. |
| `IApplicationResolverService` | ✅ `TenantApplication.Application.Services` | ResolveByUrlAsync(tenantSlug, appSlug, environment). |
| `ICompatibilityCheckService`, `CompatibilityCheckResultDto` | ✅ `AppRuntime.Contracts` | See [AppRuntime Contracts](../latest-plan/appruntime-contracts.md). |

---

## Implementation order overview

| Step | What | Creates | Updates |
|------|------|---------|--------|
| 1 | Frontend scaffold | client/apps/builder, client/apps/dashboard, client/apps/runtime; client/packages/contracts; client/packages/design (or equivalent) | — |
| 2 | AppRuntime module | server/src/Product/AppRuntime (Domain, Application, Infrastructure, Contracts, Api, Migrations, Module) | Datarizen.sln, host(s) |
| 3 | Backend capabilities | — | ApplicationDefinition (ApplicationSnapshotDto + mapper), TenantApplication (resolve + snapshot endpoint), AppBuilder (snapshot query + endpoint), AppRuntime (compatibility API) |
| 4 | Runtime BFF | BFF host or API area | Gateway/host config |
| 5 | Runtime client | Loaders, renderer, flow | client/packages/contracts types |
| 6 | Runtime execution | AppRuntime execution endpoints, BFF proxy | — |
| 7 | Auth at BFF | Auth middleware, doc | — |
| 8 | Versioning/adapters | Schema version in snapshot/compatibility; client adapters | — |
| 9 | Testing and observability | Tests, error handling, optional metrics | — |

---

## Step 1: Frontend scaffold — clients and shared packages ✅ Done

**Goal:** All three client applications and shared packages exist so the runtime (and Builder/Dashboard) can be developed without missing dependencies.

**1.1 Create `client/packages/contracts`**

- **CREATE** `client/packages/contracts` (e.g. TypeScript package, build with tsc or vite library mode).
- Export shared types: application structure (navigation, pages, data sources), component schema, layout JSON, resolved app shape, snapshot shape. Add schema version field(s) when needed (see [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md)).
- Version with semver; document contract versioning and supported schema versions.
- **Deliverables:** `client/packages/contracts` with package.json, tsconfig, exports; README for versioning.

**1.2 Create shared design package (optional but recommended)**

- **CREATE** `client/packages/design` (or `client/packages/ui`) — shared design system: tokens, base components, styles. Used by Builder, Dashboard, and Runtime for consistent UI.
- **Deliverables:** `client/packages/design` with package.json, tokens/components, build.

**1.3 Create client app scaffolds**

- **CREATE** `client/apps/builder` — App Builder (visual editor). SvelteKit structure: package.json, svelte.config.js, tsconfig, `src/routes/` (+layout.svelte, +page.svelte), `src/lib/` (features, shared, components), styles. Depends on `client/packages/contracts`, `client/packages/design`. See [02-SOLUTION-STRUCTURE](../../ai-context/02-SOLUTION-STRUCTURE.md).
- **CREATE** `client/apps/dashboard` — SaaS multi-tenant dashboard. Same SvelteKit pattern: routes, lib, shared packages.
- **CREATE** `client/apps/runtime` — End-user app renderer. SvelteKit structure: `src/routes/` (e.g. `[tenantSlug]/[appSlug]/[environment]/+page.svelte` for URL params), `src/loaders/`, `src/renderer/`, `src/lib/` (shared), styles. Depends on `client/packages/contracts`, `client/packages/design`. Align with [Runtime Client Implementation Plan](runtime-client-impl-plan.md) folder structure. **Microfrontend-ready:** use clear boundaries (loaders, renderer) with entry points (`index.ts` per boundary), no cross-boundary imports (shared code in `src/lib/` or `src/shared/`), and optional `src/shell/` (module-loader, routing, layout) for when Runtime becomes a host; see Step 5.5 and [Client Migration to Micro-Frontends](client-migration-to-micro-frontends.md).
- **Deliverables:** All three apps runnable via `pnpm run dev` (SvelteKit dev server); repo root workspace config so `client/apps/*` and `client/packages/*` are linked.

**1.4 Root / tooling**

- **CREATE** or **UPDATE** repo root: workspace config so `client/apps/*` and `client/packages/*` are linked; shared scripts (build, test, lint) if desired.

**How to test (manual):** (1) From repo root run `pnpm install`. (2) Build packages: `pnpm run build` in `client/packages/contracts` and `client/packages/design` (or use workspace filter from root). (3) Run each app: `cd client/apps/runtime && pnpm run dev` — browser should open (e.g. http://localhost:5173) and show the app placeholder (e.g. SvelteKit welcome or a minimal "Runtime" page). Repeat for `client/apps/builder` and `client/apps/dashboard` on different ports. (4) Confirm no console errors and that shared packages are resolved (no "module not found" for `@datarizen/contracts` or design).

---

## Step 2: Create AppRuntime module (server) ✅ Done

**Goal:** The AppRuntime module exists under `server/src/Product` so the BFF can call compatibility and, later, execution. Without this module, Step 3 cannot expose AppRuntime compatibility.

**2.1 Create AppRuntime project structure**

- **CREATE** `server/src/Product/AppRuntime/` with Clean Architecture layers (aligned with existing Product modules):
  - **AppRuntime.Domain** — Entities (e.g. RuntimeVersion, RuntimeInstance if needed), interfaces (ICompatibilityChecker or similar). No dependency on other Product projects except shared kernel/contracts if used.
  - **AppRuntime.Application** — Commands/queries, ICompatibilityCheckService implementation (calls Domain), DTOs. References Domain, Contracts.
  - **AppRuntime.Contracts** — ICompatibilityCheckService, CompatibilityCheckResultDto, RuntimeVersionDto, and any execution contracts. Referenced by BFF and other modules.
  - **AppRuntime.Infrastructure** — Persistence (e.g. runtime_versions, support matrix), implementations of Domain interfaces. References Domain, Application.
  - **AppRuntime.Api** — Controllers: compatibility endpoint (e.g. GET compatibility), later execution endpoints. References Application, Contracts.
  - **AppRuntime.Migrations** — Migrations for AppRuntime schema (e.g. `appruntime` schema, runtime_versions, component support). Use the project’s migration tool (FluentMigrator or EF Core). References Infrastructure.
  - **AppRuntime.Module** — DI registration, module composition (e.g. AddAppRuntime()). References other AppRuntime projects.
- **CREATE** solution entries for all AppRuntime projects; add them under the Product solution folder in **`server/Datarizen.sln`**.

**2.2 AppRuntime database migrations**

- **CREATE** migration(s) in **AppRuntime.Migrations** for the AppRuntime schema and tables (e.g. runtime_versions, component support). AppRuntime is a new module — there are no existing migrations to update. If the repo already has placeholder AppRuntime migration files, **UPDATE those files in place**; do not add a new migration for the same schema (see [Database migrations](#database-migrations)).
- When adding or editing migrations, **keep in sync:** (1) **Domain** entities, (2) **entity configuration** (DbContext / IEntityTypeConfiguration or FluentMigrator), (3) **references** (repositories, DTOs, mappers).
- **UPDATE** MigrationRunner (or appsettings) so AppRuntime migrations are included and run in the correct order. See [07-DB-MIGRATION-FLOW](../../ai-context/07-DB-MIGRATION-FLOW.md).

**2.3 Implement compatibility**

- **CREATE** compatibility check logic: input `applicationReleaseId` (and optional `runtimeVersionId`); load release snapshot or metadata; determine definition schema version(s) per feature type; check engine support matrix; return CompatibilityCheckResultDto (IsCompatible, optional MissingComponentTypes, IncompatibleVersions, ErrorMessage). See [AppRuntime Contracts](../latest-plan/appruntime-contracts.md) and [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md).
- **Dependency:** AppRuntime must obtain release snapshot or schema version metadata to run the compatibility check. Either (a) call the same snapshot API that the BFF uses (AppBuilder or TenantApplication by release ID), or (b) add a minimal internal contract (e.g. IReleaseMetadataProvider) that returns schema version(s) for a release ID, implemented by calling AppBuilder/TenantApplication. Ensure AppRuntime.Application (or Api) can resolve release ownership (platform vs tenant) and call the correct module.
- **CREATE** AppRuntime.Api controller (e.g. `GET /api/appruntime/compatibility` or under a base route) that calls ICompatibilityCheckService and returns the DTO. This is the internal API the BFF will call (in-process or HTTP).

**2.4 Register and host AppRuntime**

- **UPDATE** host(s) that should run AppRuntime: load AppRuntime.Module (e.g. MultiAppRuntimeHost or a dedicated AppRuntimeServiceHost). Ensure the host references AppRuntime.Module and registers it in the pipeline.
- **UPDATE** gateway (if used) so that internal routes to AppRuntime are configured when BFF and AppRuntime are in separate hosts.

**Deliverables:** AppRuntime module builds and runs; compatibility endpoint callable (direct or via host); solution and host updated.

**How to test (manual):** (1) Build the solution: `dotnet build server/Datarizen.sln` — AppRuntime projects must compile. (2) Run the host that loads AppRuntime (e.g. MultiAppRuntimeHost): `dotnet run --project server/src/Hosts/MultiAppRuntimeHost` (or the host you configured). (3) Call the compatibility endpoint: `curl "http://localhost:<port>/api/appruntime/compatibility?applicationReleaseId=<guid>&runtimeVersionId=<optional>"`. You may get 400 (missing/invalid params) or a JSON result (e.g. IsCompatible: false) until Step 3 provides real data — the important check is that the route exists and returns a structured response, not 404. (4) If the endpoint is on a different base path, adjust the URL; document the actual route in the API doc.

---

## Step 3: Backend capabilities for BFF ✅ Done

**Goal:** TenantApplication exposes resolve (and snapshot for tenant releases); AppBuilder (or TenantApplication) exposes snapshot by release ID; AppRuntime exposes compatibility. All are callable by the Runtime BFF (in-process or HTTP).

**3.1 TenantApplication: resolve by URL**

- **VERIFY** or **CREATE** `IApplicationResolverService.ResolveByUrlAsync(tenantSlug, appSlug, environment)` and implementation. See [Runtime Server Implementation Plan](runtime-server-impl-plan.md) API 1.
- **VERIFY** or **UPDATE** TenantApplication.Api: endpoint (e.g. `GET /api/tenantapplication/resolve`) that the BFF can call. If BFF and TenantApplication are in the same process, BFF may call the service directly; otherwise HTTP endpoint is required.
- **Deliverables:** Resolve by URL callable by BFF.

**3.2 Snapshot by release ID**

Snapshot must return application structure (navigation, pages, data sources, entities) for a given `applicationReleaseId`. The `ApplicationRelease` entity (ApplicationDefinition.Domain) already has `NavigationJson`, `PageJson`, `DataSourceJson`, `EntityJson`; the current `ApplicationReleaseDto` does not expose them. Implement in this order:

**Migrations (existing modules):** If you add a **new column** (e.g. `SchemaVersion` on the application release table) or table in ApplicationDefinition/AppBuilder/TenantApplication, **update the existing migration** that creates or alters that table — do **not** create a new migration (DB will be recreated). Then **UPDATE** the domain entity, entity configuration, and all references (repository, DTOs, mapper, snapshot response). See [Database migrations](#database-migrations).

**3.2a ApplicationDefinition — snapshot contract and mapper**

- **CREATE** in **ApplicationDefinition.Contracts**: **ApplicationSnapshotDto** (or equivalent) with fields for application structure: e.g. Navigation (or NavigationJson), Pages (or PageJson), DataSources (or DataSourceJson), Entities (or EntityJson), and optional **SchemaVersion** (per [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md)). This DTO is the shared response shape for runtime snapshot.
- **CREATE** in **ApplicationDefinition.Application** (or AppBuilder.Application): mapper from `ApplicationRelease` to `ApplicationSnapshotDto` (map the existing JSON properties; optionally parse to typed structure if contracts define it). If the mapper lives in AppBuilder to avoid ApplicationDefinition depending on a “runtime” DTO, ensure the contract is still in ApplicationDefinition.Contracts so both AppBuilder and TenantApplication can use it.
- **Deliverables:** ApplicationSnapshotDto in contracts; mapper from ApplicationRelease → ApplicationSnapshotDto.

**3.2b AppBuilder — snapshot by release ID**

- **CREATE** query (e.g. **GetReleaseSnapshotQuery**(releaseId)): load release by ID via existing `IApplicationReleaseRepository`; map to `ApplicationSnapshotDto` using the mapper from 3.2a. Return 404 when release not found.
- **CREATE** or **UPDATE** **AppBuilder.Api**: endpoint **GET api/appbuilder/releases/{id}/snapshot** (or equivalent) that returns `ApplicationSnapshotDto` for platform releases. BFF will call this for platform applicationReleaseIds.
- **Deliverables:** Snapshot by release ID callable for platform releases; response shape aligns with ApplicationSnapshotDto.

**3.2c TenantApplication — snapshot for tenant releases**

- **VERIFY** or **CREATE** equivalent of ApplicationRelease for tenant custom/forked releases (e.g. TenantApplicationRelease with snapshot JSON). If tenant releases use the same `ApplicationRelease` entity in a different schema, reuse ApplicationSnapshotDto and add a mapper.
- **CREATE** or **UPDATE** **TenantApplication.Api**: endpoint that returns snapshot by **tenant** release ID (e.g. GET api/tenantapplication/releases/{id}/snapshot) so the BFF can get snapshot for tenant-owned releases. Response shape must align with ApplicationSnapshotDto.
- **Deliverables:** Snapshot by tenant release ID callable by BFF; same ApplicationSnapshotDto shape.

**3.2 Summary:** After 3.2a–3.2c, AppBuilder (platform) and TenantApplication (tenant) both expose snapshot by release ID; BFF can call the appropriate one based on release ownership. See [Runtime Server Implementation Plan](runtime-server-impl-plan.md) API 2.

**3.3 AppRuntime: compatibility**

- **DONE in Step 2** — AppRuntime already exposes compatibility. Ensure BFF can call it (in-process when in same host, or HTTP when in separate host).
- **Deliverables:** No extra work if Step 2 is complete; otherwise ensure AppRuntime compatibility endpoint is registered and reachable from BFF.

**How to test (manual):** Test each capability in isolation. **(3.1 Resolve)** Call TenantApplication resolve (direct or via host): `GET /api/tenantapplication/resolve?tenantSlug=<slug>&appSlug=<slug>&environment=production`. Expect 200 with a body containing ApplicationReleaseId (or 404 if tenant/app not found). **(3.2 Snapshot)** Call AppBuilder: `GET /api/appbuilder/releases/<valid-release-id>/snapshot` — expect 200 and JSON with navigation, pages, dataSources, entities (or 404). If you have tenant releases, call TenantApplication snapshot with a tenant release ID and expect same shape. **(3.3 Compatibility)** Already tested in Step 2; ensure the same host that will be used by the BFF can reach AppRuntime compatibility. Use Postman or curl for all calls; verify response shapes match ApplicationSnapshotDto and ResolvedApplicationDto.

---

## Step 4: Runtime BFF ✅

**Status:** Done. AppRuntime.BFF created; RuntimeBFFHost and MultiAppRuntimeHost reference it; RuntimeBFFHost added to microservices topology.

**Goal:** A single backend surface for the runtime client: resolve, snapshot, compatibility (and later execution). BFF delegates to TenantApplication, AppBuilder, and AppRuntime; auth and CORS at BFF.

**4.1 BFF structure and topology (same pattern as other hosts)**

- **CREATE** `server/src/Product/AppRuntime/AppRuntime.BFF` — put all BFF business logic here (resolve, snapshot, compatibility orchestration; later execution proxy). Same pattern as other product modules with a dedicated BFF project.
- **CREATE** or **UPDATE** **RuntimeBFFHost** (`server/src/Hosts/RuntimeBFFHost`): reference **AppRuntime.BFF**; host only exposes `/api/runtime/*`. RuntimeBFFHost is part of the **microservices topology** (add to AppHost when using that topology).
- **UPDATE** **MultiAppRuntimeHost**: reference **AppRuntime.BFF** so that when runtime modules run in the distributed host, the BFF logic is loaded from AppRuntime.BFF. Both RuntimeBFFHost and MultiAppRuntimeHost use the same BFF implementation.
- **Note:** This structure is partially implemented in code (RuntimeBFFHost exists); ensure AppRuntime.BFF project exists and both hosts reference it; add RuntimeBFFHost to the microservices topology in AppHost.

**4.2 Create BFF endpoints**

- **CREATE** BFF endpoints (in AppRuntime.BFF or its API surface):
  - `GET /api/runtime/resolve?tenantSlug=&appSlug=&environment=` → calls TenantApplication resolve; returns ResolvedApplicationDto (or minimal DTO).
  - `GET /api/runtime/snapshot?applicationReleaseId=` → determines platform vs tenant release; calls AppBuilder or TenantApplication for snapshot; returns ApplicationSnapshotDto (include schema version when available).
  - `GET /api/runtime/compatibility?applicationReleaseId=&runtimeVersionId=` → calls AppRuntime ICompatibilityCheckService; returns CompatibilityCheckResultDto.
- **How BFF determines platform vs tenant release for snapshot:** Use one of: (a) resolve response from TenantApplication includes a flag or source (e.g. `IsTenantRelease` or `ReleaseSource`) so BFF knows which API to call; (b) BFF calls TenantApplication with the release ID to ask whether it is a tenant release; (c) BFF tries AppBuilder snapshot first and on 404 calls TenantApplication. Document the chosen approach in the BFF and in the API doc.
- **CREATE** CORS configuration for the runtime client origin(s) (e.g. allow origin of `client/apps/runtime` in dev and production origin in prod).
- **UPDATE** gateway (if used) so that `/api/runtime/*` routes to the BFF host.
- **Error responses:** Use a consistent JSON shape for errors (e.g. `{ "error": "message", "code": "NotFound" }`). Return 404 when resource not found, 403 when not allowed; document in API doc.

**4.3 BFF API quick reference (for client and BFF implementation)**

| Method | Route | Query params | Response (200) | Error status |
|--------|-------|--------------|----------------|--------------|
| GET | `/api/runtime/resolve` | `tenantSlug`, `appSlug`, `environment` (optional, default production) | ResolvedApplicationDto: ApplicationReleaseId, EnvironmentConfiguration, TenantId, TenantSlug, etc. | 404 not found, 403 not allowed |
| GET | `/api/runtime/snapshot` | `applicationReleaseId` (guid) | ApplicationSnapshotDto: navigation, pages, dataSources, entities, optional schemaVersion | 404 release not found, 403 no access |
| GET | `/api/runtime/compatibility` | `applicationReleaseId` (guid), `runtimeVersionId` (optional) | CompatibilityCheckResultDto: IsCompatible, optional MissingComponentTypes, IncompatibleVersions, ErrorMessage | 404 release or runtime version not found |
| POST | `/api/runtime/datasource/execute` | Body: DatasourceExecuteRequestDto (applicationReleaseId, datasourceId) | DatasourceExecuteResultDto: Data, optional SchemaVersion | 400 validation, 404 release or datasource not found |

**4.4 Document BFF API**

- **CREATE** or **UPDATE** API doc: routes, query params, response shapes, error codes (404, 403) for resolve, snapshot, compatibility. See [Runtime Server Implementation Plan](runtime-server-impl-plan.md).

**Deliverables:** Runtime BFF runnable; resolve, snapshot, compatibility callable by client; CORS configured; API documented.

**How to test (manual):** (1) Start the BFF host (e.g. `dotnet run --project server/src/Hosts/RuntimeBFFHost` or the host that serves `/api/runtime/*`). (2) From a terminal: `curl "http://localhost:<bff-port>/api/runtime/resolve?tenantSlug=mytenant&appSlug=myapp&environment=production"` — expect 200 + JSON (or 404 if no app). (3) Use the ApplicationReleaseId from resolve in: `curl "http://localhost:<bff-port>/api/runtime/snapshot?applicationReleaseId=<guid>"` and `curl "http://localhost:<bff-port>/api/runtime/compatibility?applicationReleaseId=<guid>"`. (4) CORS: open the runtime client (Step 5) in the browser with `VITE_API_BASE_URL` pointing to the BFF; trigger resolve/snapshot/compatibility from the app and confirm no CORS errors in the browser console. If the client is not ready, use browser DevTools → Network and a simple fetch from the console to the BFF origin to verify CORS headers.

---

## Step 5: Runtime client — contracts, loaders, compatibility, renderer ✅

**Status:** Done. Contracts aligned with backend DTOs; loaders use shared API client; URL routing, .env.example and .env.development; resolve → snapshot → compatibility flow with loading/error; minimal renderer (nav + placeholder); boundaries (loaders, renderer, shared).

**Goal:** Runtime client loads app (resolve → snapshot → compatibility) and renders it using contracts. See [Runtime Client Implementation Plan](runtime-client-impl-plan.md) for detail.

**5.1 Contracts and types**

- **UPDATE** `client/packages/contracts`: ensure ResolvedApplication, ApplicationSnapshot, CompatibilityResult (and schema version fields) are defined and exported. Align with backend DTOs.
- **CREATE** or **UPDATE** runtime-specific types in `client/apps/runtime` (or in contracts) for resolved app, snapshot, compatibility result.

**5.2 URL and environment**

- **CREATE** routing so the runtime client derives `tenantSlug`, `appSlug`, `environment` from the URL (e.g. path `/:tenantSlug/:appSlug/:environment` or `/:tenantSlug/:appSlug` with default environment). These are passed to the resolve loader.
- **CREATE** `.env.development` and `.env.production` (or equivalent) with at least: **`VITE_API_BASE_URL`** (or `PUBLIC_API_BASE_URL`) — base URL of the Runtime BFF (e.g. `http://localhost:5xxx` in dev). Add any auth-related env vars when Step 7 is implemented. **CREATE** `.env.example` in `client/apps/runtime` (e.g. `VITE_API_BASE_URL=http://localhost:5xxx`) and commit it so new developers know which variables to set.
- **CREATE** shared API client that reads base URL from env; use for all BFF calls (resolve, snapshot, compatibility, and later execution).

**5.3 Loaders**

- **CREATE** `client/apps/runtime/src/loaders/resolveLoader.ts` — calls `GET /api/runtime/resolve?tenantSlug=&appSlug=&environment=`, maps response to ResolvedApplication.
- **CREATE** `client/apps/runtime/src/loaders/structureLoader.ts` — calls `GET /api/runtime/snapshot?applicationReleaseId=` (use ApplicationReleaseId from resolve result), maps to ApplicationSnapshot.
- **CREATE** `client/apps/runtime/src/loaders/compatibilityLoader.ts` — calls `GET /api/runtime/compatibility?applicationReleaseId=`, maps to CompatibilityResult.
- **CREATE** main/App flow: parse URL → resolve (with tenantSlug, appSlug, environment) → snapshot (with applicationReleaseId) → compatibility → render or show incompatible error. Show a **loading state** (e.g. “Loading app…”) while any of these calls are in progress so the user does not see a blank screen.

**5.4 Renderer**

- **CREATE** renderer that consumes ApplicationSnapshot + tenant Configuration (from resolve). Implement layout/navigation, page, and component renderer; use runtime component set from contracts. Inject configuration into components/context.
- **CREATE** runtime component set (e.g. Svelte components) that map to contract component types (Button, DataGrid, Form, etc.).
- **Semantic types (contract):** Entity, relations, properties (and any definitions used to generate HTML or UI) must be based on **semantic types** from a shared contract — not raw CSS class names. The runtime Svelte client and any server-side HTML generator both consume the **same mapping** (same contract). See [Application Definition — HTML and Builder plan](../application-definition/application-definition-html-and-builder-plan.md) for storage of generated HTML and Builder/TenantApplication updates.

**5.5 Runtime client: microfrontend-ready requirements**

So the runtime can later become a **shell + remotes** (e.g. renderer or plugins as remotes) without a rewrite, implement the following from the start. See [Client Migration to Micro-Frontends](client-migration-to-micro-frontends.md) for the full migration guide.

- **Clear boundaries:** Structure the app by **boundaries** (loaders, renderer, optional plugins). Each boundary is a self-contained slice (e.g. `src/loaders/`, `src/renderer/`). No direct imports from one boundary into another; shared code lives in `src/shared/` or `src/lib/`.
- **Entry points:** Each boundary has an **entry point** (e.g. `loaders/index.ts`, `renderer/index.ts`) that exports its public API (functions, components, types). The main app (e.g. `App.svelte`, `main.ts`) imports from these entry points only.
- **Optional `/src/shell`:** Add a `src/shell/` folder when you are ready to support remotes: `module-loader` (load remote or local slice), `routing`, `layout`. Until migration, the app runs without loading remotes; shell code can be stubbed or unused. See [Client Migration to Micro-Frontends](client-migration-to-micro-frontends.md) Phase 1.
- **Shared dependencies:** Pin and document shared deps (Svelte, `client/packages/contracts`, etc.) so that when Runtime becomes a host, Module Federation config (exposes, shared) stays consistent. Builder and Runtime must use the same major version of `packages/contracts`.
- **Runtime as future host:** When migrating, Runtime becomes the **shell**; remotes can be renderer, loaders, or plugin bundles (e.g. `client/apps/runtime-remotes/renderer`). No change to the implementation order in this plan—only the structure is MF-ready from the start.

**Deliverables:** Runtime client loads and renders an app end-to-end (resolve → snapshot → compatibility → render); clear error when incompatible. Client structure is **microfrontend-ready** (boundaries, entry points, optional shell, pinned shared deps).

**How to test (manual):** (1) Ensure BFF is running and CORS allows the client origin. (2) Set `VITE_API_BASE_URL` (or `PUBLIC_API_BASE_URL`) in `clients/runtime/.env.development` to the BFF base URL (e.g. `http://localhost:5xxx`). (3) Run the runtime client: `cd clients/runtime && pnpm run dev`. (4) In the browser, open a URL that includes tenant and app (e.g. `http://localhost:5173/mytenant/myapp` or your route pattern). (5) Verify: resolve runs (Network tab shows GET to `/api/runtime/resolve?...`), then snapshot and compatibility; either the app renders (navigation/pages/components) or an "incompatible" / error message appears. (6) Test error cases: invalid tenant/app (404), missing env (wrong or empty base URL). Minimum valuable outcome: one known tenant/app shows a rendered layout or a clear error.

---

## Step 6: Runtime execution (datasource, engines) ✅

**Goal:** Runtime client can trigger datasource execution (and later other engines); BFF forwards to AppRuntime; AppRuntime selects engine version from release snapshot/schema version and executes.

**6.1 AppRuntime execution** ✅

- **CREATE** execution endpoints in AppRuntime.Api (e.g. datasource execute): request with applicationReleaseId + component/datasource id; load definition from snapshot; select engine version by schema version; execute; return result. See [Runtime Server Implementation Plan](runtime-server-impl-plan.md) Runtime execution section.
- **CREATE** engine registry / support matrix implementation in AppRuntime (engine version ↔ supported definition schema versions). *Deferred: stub execution returns mock data; engine selection by schema version can follow.*

**6.2 BFF proxy** ✅

- **UPDATE** Runtime BFF: add proxy/forward routes for execution (e.g. POST /api/runtime/datasource/execute or similar). BFF forwards to AppRuntime; does not execute itself.
- **Document** execution routes and request/response for the runtime client.

**6.3 Runtime client** ✅

- **UPDATE** runtime client: call execution via BFF when user triggers datasource (or other engine); pass applicationReleaseId and component id; handle response and errors.

**Deliverables:** Datasource execution (and optionally other engines) working via BFF → AppRuntime; client can trigger execution. *Implemented: contracts (IDatasourceExecutionService, IReleaseSnapshotProvider, DTOs); DatasourceExecutionService (stub: parse DataSourceJson, find by id, return mock); BffReleaseSnapshotProvider + StubReleaseSnapshotProvider; BFF POST /api/runtime/datasource/execute; AppRuntime.Api POST api/appruntime/datasource/execute; client apiPost, executeDatasource(), renderer “Run” per datasource.*

**How to test (manual):** (1) In the runtime client, open an app that has at least one datasource (or mock component that triggers execution). (2) Trigger the action that calls the execution endpoint (e.g. button or load that runs a datasource). (3) In Network tab confirm: POST (or GET) to BFF execution route (e.g. `/api/runtime/datasource/execute`) with correct payload; response 200 and expected data shape (or 4xx with clear error). (4) If no datasource exists yet: use a mock or stub in the client that calls the BFF execution URL with a test payload; verify BFF forwards to AppRuntime and returns the result. **Note:** Full end-to-end test requires a release with a real datasource definition and a working engine; minimal test is "client → BFF → AppRuntime → response" with a stub or first datasource implementation.

---

## Step 7: Authentication at BFF — ⏭ Skipped (future implementation)

**Goal:** Auth is implemented at the Runtime BFF; client sends cookie or token; BFF validates and derives tenant/user context; internal modules do not need to validate browser tokens.

**Status:** **Skipped.** To be implemented when Identity/auth (e.g. JWT or session) is available. Until then: no auth at BFF; tenant/app/environment are derived from URL and request body only; all BFF endpoints are effectively public for local/dev use.

- **CREATE** auth middleware or pipeline in Runtime BFF: validate session cookie or JWT; derive tenant and user; attach to request context.
- **UPDATE** BFF endpoints: apply auth where required; pass tenant/user to downstream calls (TenantApplication, AppRuntime) via headers or server-side context.
- **CREATE** doc: auth model (which endpoints require auth; tenant from URL vs header vs token; public apps). See [Runtime Server Implementation Plan](runtime-server-impl-plan.md) CORS and Authentication.
- **Deliverables:** Auth at BFF; auth model documented; resolve endpoint can work with minimal auth for public apps if designed.

**How to test (manual):** (1) Call a protected BFF endpoint without a cookie/token: expect 401 Unauthorized (or 403 if your design distinguishes). (2) Sign in (or obtain a valid session/JWT) and repeat the same request with the cookie or `Authorization` header: expect 200 and normal response. (3) If you have "public" apps, call resolve (or the designated public endpoint) without auth and confirm it still returns 200 for allowed cases. (4) Document in the auth model doc which endpoints are protected and how tenant/user are derived; use this as the checklist for manual verification. **Note:** If auth is not yet implemented, this step cannot be fully tested; add a note "Auth at BFF — to be tested when Step 7 is implemented" and run the above once middleware and identity are in place.

---

## Step 8: Compatibility and versioning ✅

**Goal:** Snapshot and compatibility responses include schema version(s); runtime client uses adapters per schema version so UI stays correct across definition versions. Full pattern is defined in [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md).

**Compatibility and versioning (summary):**

- **One pattern for all feature types** (datasource, workflow, validation rules, etc.): **definition schema version** (in snapshot) + **engine support matrix** (in AppRuntime) + **client adapters** (per schema version). Same rule for every feature: runtime must support the definition's schema version; client must have an adapter for it.
- **Backend:** Snapshot API and compatibility API include **schema version(s)** (per-feature-type or snapshot-wide). AppRuntime implements the **support matrix** (engine version ↔ supported definition schema versions) and uses it in the single compatibility check. Execution selects engine by schema version from the release snapshot.
- **Client:** Uses **schema version** from snapshot or compatibility response to select the correct **adapter or renderer** per feature type. If the client does not support a schema version, show a clear upgrade-client or unsupported message.
- **Backward-compatibility policy:** New engine versions should support at least the previous definition schema version(s); document when support is dropped. See [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md).

**Implementation tasks:**

- **UPDATE** snapshot API (and/or release metadata): include per–feature-type or snapshot-wide schema version in response.
- **UPDATE** compatibility API: return supported schema versions and optional unsupportedInstances/upgrade hints.
- **UPDATE** runtime client: add adapters or renderers per definition schema version; select adapter from schema version in snapshot/compatibility. If client does not support a schema version, show “upgrade client” or “unsupported” message.
- **Document** supported schema versions and backward-compatibility policy.
- **Deliverables:** Schema version in backend responses; client adapters for supported versions; doc updated. *Implemented: snapshot SchemaVersion "1.0" in mapper; CompatibilityCheckResultDto.SupportedSchemaVersions ["1.0"]; client schemaVersionAdapter + page check; unsupported version shows clear message. Single version "1.0" for MVP.*

**How to test (manual):** (1) Call snapshot and compatibility APIs and confirm the response JSON includes a schema version field (e.g. `schemaVersion` or per-feature version). (2) In the runtime client, load an app that has a supported schema version — UI should render with the correct adapter. (3) If possible, use a release with an older or unsupported schema version (or temporarily mock one): the client should show a clear "upgrade client" or "unsupported version" message instead of a broken UI. (4) Document the supported schema version(s) and deprecation policy; verify the doc matches backend and client behavior. **Note:** Full testing requires at least two schema versions or a way to simulate an unsupported version; if only one version exists, verify that version is present in responses and that the client uses it to select the adapter.

---

## Step 9: Testing and observability

**Goal:** Unit tests for loaders and renderer; integration tests for full load flow; E2E for at least one app; error handling and optional observability.

- **CREATE** unit tests: loaders (mocked HTTP), renderer (snapshot + config in, assert structure).
- **CREATE** integration tests: resolve → snapshot → compatibility → render against real or mock BFF.
- **CREATE** E2E: full flow (open URL → resolve → load → compatibility → render); assert visible UI and key interactions.
- **CREATE** error handling: network errors, 404/403, incompatible release — clear user message; optional reporting (e.g. Sentry).
- **CREATE** (optional) observability: logs or metrics for resolve/snapshot/compatibility duration and success; optional RUM for “time to first render.”
- **Deliverables:** Test suite; error handling; optional observability. See [Runtime Client Implementation Plan](runtime-client-impl-plan.md) Phase 6.

**Testing notes for developers:** Unit tests for loaders should mock `fetch` or the API client and assert correct URL (including query params), request method, and response mapping to contract types. Integration tests need a running BFF (or mock server) with resolve, snapshot, compatibility endpoints; set `VITE_API_BASE_URL` (or override) to the test BFF URL.

**How to test (manual):** (1) **Unit:** Run `pnpm run test` (or `vitest`) in `clients/runtime` — loader and renderer tests must pass. (2) **Integration:** Start the BFF (and any required backend), set env to point at it, run integration tests (e.g. `pnpm run test:integration`) — resolve → snapshot → compatibility flow should pass. (3) **E2E:** Run E2E suite (e.g. Playwright); open app URL, confirm visible UI and at least one key interaction (e.g. navigation). (4) **Error handling:** Manually break the network, use an invalid tenant/app, or return an incompatible response from a mock — confirm the UI shows a clear error message and does not crash. (5) **Observability (optional):** If implemented, check logs or metrics for resolve/snapshot/compatibility duration. **Note:** If the test suite or E2E is not yet set up, the minimum test for this step is: run whatever tests exist and manually perform (2) and (4) once.

---

## Optional / later improvements

These are not required for the initial implementation but are documented in [Runtime Server Implementation Plan](runtime-server-impl-plan.md) and can be added after MVP:

- **Bootstrap endpoint:** Single BFF endpoint `GET /api/runtime/bootstrap?tenantSlug=&appSlug=&environment=` that performs resolve + snapshot + compatibility in one server-side flow and returns a combined payload. Reduces client round-trips.
- **Runtime version in compatibility response:** Return current (or default) runtime version and supported range (e.g. min/max application version) so the client can show clearer upgrade messages.
- **Initial view as HTML:** ApplicationDefinition/TenantApplication may store and return **pre-generated HTML** (navigation, dashboard shell) for fast first paint; dashboard elements may be returned as shell + loaders first, with element content (data-driven HTML or JSON) fetched separately. See [Application Definition — HTML and Builder plan](../application-definition/application-definition-html-and-builder-plan.md). Plan to be refined when that implementation starts.

---

## Create vs update checklist

Use this list to avoid missing artifacts.

**CREATE**

- [x] `client/packages/contracts` (package, types, build) — align with backend DTOs (ResolvedApplicationDto, ApplicationSnapshotDto, CompatibilityCheckResultDto)
- [x] `client/packages/design` (or equivalent shared UI lib)
- [x] `client/apps/builder` (app scaffold)
- [x] `client/apps/dashboard` (app scaffold)
- [x] `client/apps/runtime` (app scaffold)
- [x] `server/src/Product/AppRuntime` (Domain, Application, Infrastructure, Contracts, Api, Migrations, Module)
- [x] AppRuntime migrations: schema + tables (runtime_versions, etc.); if placeholder migrations exist, update in place (see [Database migrations](#database-migrations))
- [x] AppRuntime compatibility endpoint (controller + service)
- [x] AppRuntime.BFF project under `server/src/Product/AppRuntime` (all BFF business logic)
- [x] Runtime BFF host (RuntimeBFFHost references AppRuntime.BFF; add to microservices topology)
- [x] MultiAppRuntimeHost references AppRuntime.BFF
- [x] BFF endpoints: resolve, snapshot, compatibility (and execution proxy)
- [x] Runtime client: URL routing (tenantSlug, appSlug, environment), env (VITE_API_BASE_URL), .env.example in `client/apps/runtime`, loaders (resolve, structure, compatibility), renderer, minimal component set, main flow, loading state, clear error messages (404/incompatible)
- [x] AppRuntime execution endpoints and engine selection (stub execution; engine registry deferred)
- [ ] BFF auth middleware and auth model doc (Step 7 skipped — future when Identity/auth added)
- [x] Client adapters per schema version (Step 8): schemaVersionAdapter + page check; runtime client MF-ready (boundaries, entry points) deferred
- [ ] Unit, integration, E2E tests; error handling; optional observability

**MVP-only (don’t forget)**

- [ ] Seed or demo data: at least one tenant, one app, one release with snapshot (script, migration, or doc); document tenant/app slugs for testing
- [ ] BFF appsettings (or env): if multi-host, base URLs for TenantApplication, AppBuilder, AppRuntime; if single host, document that in-process is used

**UPDATE**

- [x] Repo root / monorepo workspace (link packages and clients)
- [x] `Datarizen.sln` — add AppRuntime projects (incl. AppRuntime.BFF, RuntimeBFFHost)
- [ ] MigrationRunner (or startup): include AppRuntime migrations; run in correct order
- [ ] **When updating any existing migration:** update domain entity, entity configuration, and all references (repositories, DTOs, mappers) — do not create a new migration if DB is recreated (see [Database migrations](#database-migrations))
- [x] Host(s) — RuntimeBFFHost and MultiAppRuntimeHost reference AppRuntime.BFF; load AppRuntime.Module; add RuntimeBFFHost to AppHost microservices topology
- [ ] Gateway — route `/api/runtime/*` to BFF if separate host
- [x] **ApplicationDefinition.Contracts** — add ApplicationSnapshotDto (navigation, pages, data sources, entities, optional schema version)
- [x] **ApplicationDefinition.Application** (or AppBuilder) — add mapper ApplicationRelease → ApplicationSnapshotDto
- [ ] **If new columns/tables in ApplicationDefinition/AppBuilder/TenantApplication:** update the **existing** migration that creates/alters that table; then update domain entity, entity configuration, and references (see [Database migrations](#database-migrations))
- [x] **AppBuilder** — add GetReleaseSnapshotQuery (or equivalent); add GET api/appbuilder/releases/{id}/snapshot endpoint
- [x] **TenantApplication** — add snapshot-by–tenant-release-ID (query + GET api/tenantapplication/releases/{id}/snapshot or equivalent)
- [x] TenantApplication — resolve by URL callable by BFF (endpoint or in-process)
- [x] `client/packages/contracts` — add/align ResolvedApplication, ApplicationSnapshot, CompatibilityResult, schema version
- [x] Snapshot and compatibility responses — include schema version(s) (snapshot "1.0", compatibility SupportedSchemaVersions ["1.0"])
- [x] Runtime client — execution calls via BFF (apiPost, executeDatasource, Run in renderer); versioning adapters (Step 8)

---

## References

- [Runtime Server Implementation Plan](runtime-server-impl-plan.md) — BFF APIs, TenantApplication/AppBuilder/AppRuntime delegation, CORS and auth.
- [Runtime Client Implementation Plan](runtime-client-impl-plan.md) — Client phases, loaders, renderer, versioning, testing.
- [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md) — One pattern for all feature types; schema version + support matrix; client adapters; backward-compatibility policy.
- [Application Definition — HTML and Builder plan](../application-definition/application-definition-html-and-builder-plan.md) — Storing generated HTML in ApplicationDefinition; AppBuilder and TenantApplication updates for HTML generation; semantic types/contract for Builder, Runtime client, and server-side HTML generator. Future implementation; to be refined.
- [Client Migration to Micro-Frontends](client-migration-to-micro-frontends.md) — How to migrate Runtime (or Builder/Dashboard) to shell + remotes; MF-ready structure, Module Federation, deployment.
- [Solution Structure - Runtime](../../ai-context/02-SOLUTION-STRUCTURE.md) — Runtime app structure, backend dependencies, flow.
- [07-DB-MIGRATION-FLOW](../../ai-context/07-DB-MIGRATION-FLOW.md) — Migration runner, order, environment-specific migrations.
- [AppRuntime Architecture](../latest-plan/appruntime-architecture.md) — Resolution flow, component loading.
- [AppRuntime Contracts](../latest-plan/appruntime-contracts.md) — ICompatibilityCheckService, DTOs, support matrix concepts.
- [AppRuntime Migrations](../latest-plan/appruntime-migrations.md) — AppRuntime schema and table migrations (if present).
