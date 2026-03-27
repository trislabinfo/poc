# Runtime Client Implementation Plan

**Document Purpose:** Implementation plan for the Datarizen **runtime client** (`/clients/runtime`)—the end-user application renderer that loads application structure and tenant configuration from the backend and renders the UI. Aligns with enterprise no-code platform best practices: contract-first development, clear backend dependencies, multi-tenant versioning, and compatibility handling.

**Audience:** Frontend developers, architects.

**Status:** Planning

**Related:** [Runtime — Full Implementation Plan](runtime-all-impl-plan.md) (master checklist), [Solution Structure - Runtime](../../ai-context/02-SOLUTION-STRUCTURE.md), [Runtime Server Implementation Plan](runtime-server-impl-plan.md), [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md)

---

## Table of Contents

1. [Overview](#overview)
2. [Backend Dependencies](#backend-dependencies)
3. [Architecture](#architecture)
4. [Phase 1: Contracts and Types](#phase-1-contracts-and-types)
5. [Phase 2: Loaders](#phase-2-loaders)
6. [Phase 3: Compatibility Check](#phase-3-compatibility-check)
7. [Phase 4: Renderer](#phase-4-renderer)
8. [Phase 5: Versioning and Multi-Tenant Support](#phase-5-versioning-and-multi-tenant-support)
9. [Phase 6: Testing and Observability](#phase-6-testing-and-observability)
10. [References](#references)

---

## Overview

The runtime client is a **consumer** of application definitions, not the owner. The client calls **only the Runtime BFF**. The Runtime BFF delegates to TenantApplication (resolve), AppBuilder or TenantApplication (snapshot), and AppRuntime (compatibility). Application resolution is always done by TenantApplication; AppBuilder never resolves. The client:

1. **Resolves** which application and configuration to load (Runtime BFF: tenant + app slug + environment → `ApplicationReleaseId` + configuration).
2. **Loads** application structure (Runtime BFF: snapshot for the `ApplicationReleaseId` from step 1).
3. **Checks compatibility** (Runtime BFF: current runtime can execute this release).
4. **Renders** the application using shared contracts (schema, components, layout).

**Principles:**

- **Contract-first:** All shapes (resolved app, snapshot, configuration) align with `packages/contracts` and backend DTOs.
- **Thin client:** No business logic for “what an application is”; only load, validate, and render.
- **Multi-tenant by design:** Each request is tenant-scoped via URL or context; backend returns the release and config for that tenant and environment.
- **Versioning:** Follow the [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md): support a range of definition schema versions per feature type via compatibility result and adapters so different tenants can run different app versions; one pattern for datasource, workflow, validation rules, and future features.

---

## Backend Dependencies

**Applications are always resolved by the TenantApplication module; AppBuilder never performs resolution.** The runtime client calls **only the Runtime BFF**. The BFF delegates to TenantApplication (resolve), AppBuilder or TenantApplication (snapshot), and AppRuntime (compatibility).

| Client calls | BFF delegates to | Purpose |
|--------------|------------------|---------|
| **Runtime BFF** — resolve | TenantApplication API | Get `ApplicationReleaseId` + merged tenant **configuration** |
| **Runtime BFF** — snapshot | TenantApplication API or AppBuilder | Get **snapshot** (navigation, pages, data sources, entities) for the release ID from resolve |
| **Runtime BFF** — compatibility | Runtime API (AppRuntime) | Verify runtime can execute this release before rendering |
| **Runtime BFF** — execution | Runtime API (AppRuntime) | Datasource execution, engines (navigation, page, datasource) |

The client does **not** implement resolution or snapshot logic; it only calls the Runtime BFF APIs and maps responses to contract types. All runtime execution (datasource, engines) is also requested via the BFF, which delegates to the Runtime API (AppRuntime). See [Runtime Server Implementation Plan](runtime-server-impl-plan.md) for BFF API details.

---

## Architecture

### High-level flow

```
URL (e.g. /acme-corp/crm/production)
    → ResolveLoader (Runtime BFF API)
    → ResolvedApplicationDto { applicationReleaseId, configuration }
    → StructureLoader (Runtime BFF API)
    → ApplicationSnapshotDto { navigation, pages, dataSources, ... }
    → CompatibilityLoader (Runtime BFF API)
    → compatible: boolean (+ optional schema version)
    → Renderer (contract-based)
    → Rendered UI
```

### Folder structure (aligned with 02-SOLUTION-STRUCTURE)

```
/clients/runtime
  package.json
  vite.config.ts
  tsconfig.json
  .env.development
  .env.production

  /src
    main.ts
    App.svelte

    /loaders
      index.ts
      resolveLoader.ts       # Runtime BFF: resolve by URL
      structureLoader.ts     # Runtime BFF: get snapshot
      compatibilityLoader.ts # Runtime BFF: compatibility check
    /renderer
      index.ts
      /engine               # Render engine (layout, pages, components)
      /components           # Runtime component set (from contracts)
    /shared
      /components
      /utils
      /api                  # HTTP client, base URL, auth/tenant headers
    /styles
```

---

## Phase 1: Contracts and Types

**Goal:** Establish a single source of truth for application structure and configuration shapes; align with backend and Builder.

**Tasks:**

1. **Ensure `packages/contracts` exists** and exports (aligned with [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md)):
   - Schema types: component schema, layout JSON, page schema, data source schema, workflow schema, validation rules schema (as they are introduced).
   - Action and validation types used by both Builder and Runtime.
   - Schema **version** field(s)—per–feature-type or snapshot-wide—so the runtime can select the correct adapter for each feature type.

2. **Define runtime-specific types** (in Runtime or in contracts) for:
   - **ResolvedApplication:** Maps from `ResolvedApplicationDto` (tenantId, applicationReleaseId, environmentConfiguration, etc.).
   - **ApplicationSnapshot:** Maps from `ApplicationSnapshotDto` (navigation, pages, dataSources, etc.); must match backend and contracts; include schema version(s) when present.
   - **CompatibilityResult:** compatible (boolean), optional runtimeVersion, optional per–feature-type or snapshot schema version(s), optional unsupportedInstances/upgrade hints (per framework).

3. **Document contract versioning:** Same major version of `packages/contracts` for Builder and Runtime; document supported definition schema versions per feature type (e.g. “Runtime 2.x supports datasource schema 1 and 2, workflow schema 1”).

**Deliverables:**

- [ ] `packages/contracts` with schema and shared types; versioned (e.g. semver).
- [ ] Runtime types for resolved app, snapshot, and compatibility result.
- [ ] README or doc for contract versioning and supported schema versions.

---

## Phase 2: Loaders

**Goal:** Implement API clients that fetch resolve result, snapshot, and compatibility from the backend.

**Tasks:**

1. **ResolveLoader**
   - Input: URL-derived context (tenantSlug, appSlug, environment) or a single “resolve” URL.
   - Call Runtime BFF resolve API (see [Runtime Server Implementation Plan](runtime-server-impl-plan.md)): e.g. `GET /api/runtime/resolve?tenantSlug=...&appSlug=...&environment=...`.
   - Map response to `ResolvedApplication` (contract type).
   - Handle errors (404, 403, network); expose as typed Result or throw with clear messages.

2. **StructureLoader**
   - Input: `applicationReleaseId` (from resolve).
   - Call Runtime BFF snapshot API: e.g. `GET /api/runtime/snapshot?applicationReleaseId=...` (or path-based equivalent).
   - Map response to `ApplicationSnapshot` (contract type).
   - If backend returns a **schema version** field, use it for adapter selection (Phase 5).

3. **StructureLoader**
   - The Runtime BFF returns the same snapshot contract for both platform and tenant custom/forked releases; the client calls a single BFF snapshot endpoint. No client-side branching by release ownership.

4. **Shared API client**
   - Base URL from env (e.g. `VITE_API_BASE_URL`).
   - Attach tenant/auth headers as required (e.g. from resolve or parent app).
   - CORS and error handling in one place.

**Deliverables:**

- [ ] `resolveLoader.ts` with Runtime BFF resolve-by-URL API call.
- [ ] `structureLoader.ts` with Runtime BFF get-snapshot API call.
- [ ] Shared API client and env configuration.
- [ ] Unit tests for loaders (e.g. with mocked fetch).

---

## Phase 3: Compatibility Check

**Goal:** Before rendering, ensure the current runtime can execute the resolved application release.

**Tasks:**

1. **CompatibilityLoader**
   - Input: `applicationReleaseId`, optional `runtimeVersionId` (or use current/default).
   - Call Runtime BFF compatibility API (see [Runtime Server Implementation Plan](runtime-server-impl-plan.md)): e.g. `GET /api/runtime/compatibility?applicationReleaseId=...` or equivalent.
   - Map response to `CompatibilityResult` (compatible, optional schema version, optional message).

2. **Integration in flow**
   - After resolve and load structure, call compatibility check.
   - If not compatible: show user-facing message (e.g. “This application requires a newer runtime” or “Upgrade the application”); do not render.
   - If compatible: proceed to render; optionally use returned schema version to select an adapter (Phase 5).

**Deliverables:**

- [ ] `compatibilityLoader.ts` with compatibility API call.
- [ ] Flow in main/App: resolve → structure → compatibility → render or error.
- [ ] User-visible error state when incompatible.

---

## Phase 4: Renderer

**Goal:** Render the application from snapshot + configuration using contract types and a component set.

**Tasks:**

1. **Render engine**
   - Input: `ApplicationSnapshot` + tenant `Configuration` (from resolve).
   - Use contract types for navigation, pages, data sources.
   - Implement: layout/navigation renderer, page renderer, component renderer (from `/renderer/components`), wired to snapshot and config.

2. **Component set**
   - Runtime component set (Svelte components) that map to contract component types (e.g. Button, DataGrid, Form). These are the “runtime” implementations of the components the Builder defines; they must respect contract props and events.

3. **Configuration injection**
   - Merge tenant configuration into component props or context (e.g. theme, feature flags, API base URLs). Configuration comes from resolve result only (no extra fetch).

4. **Performance**
   - Lazy-load heavy components or routes if needed; keep initial bundle small. Consider code-splitting by page or section.

**Deliverables:**

- [ ] Renderer that consumes `ApplicationSnapshot` + `Configuration`.
- [ ] Runtime component set aligned with contract component types.
- [ ] Configuration merged into render context/props.
- [ ] Basic E2E: resolve → load → render for one sample app.

---

## Phase 5: Versioning and Multi-Tenant Support

**Goal:** Support multiple tenants on different application versions and multiple definition schema versions per feature type, following the [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md).

**Tasks:**

1. **Multi-tenant by URL/context**
   - Tenant (and environment) come from URL or parent context; every loader call is tenant-scoped. Backend returns release and config for that tenant. No client-side tenant list or switching; single-tenant per load.

2. **Per-tenant release pinning**
   - Backend (TenantApplication) stores per-tenant, per-environment `ApplicationReleaseId`. Client does not choose release; it only uses the one returned by resolve. Different tenants can run different app versions simultaneously.

3. **Schema version and adapters (per framework)**
   - Backend includes **schema version(s)** in the snapshot or compatibility response (per–feature-type or snapshot-wide). Runtime client uses these to select the **correct adapter or renderer** per feature type (datasource, workflow, validation rules, etc.).
   - Implement adapters for each supported definition schema version; same pattern for all feature types. If the client does not support a schema version present in the snapshot, show a clear “upgrade client” or “unsupported” message.
   - Document supported schema versions and backward-compatibility policy per framework.

4. **Compatibility**
   - Single compatibility API (per framework): client calls it and respects the result. Backend uses the engine support matrix for all feature types; no client-side compatibility matrix. Client may optionally send `clientCapabilities` (supported schema versions) so the backend can return richer upgrade hints.

**Deliverables:**

- [ ] Documentation: how multi-tenant and versioning work from the client’s perspective; reference the Compatibility and Versioning Framework.
- [ ] Schema version field(s) in types and adapter(s) per feature type for supported definition schema versions.
- [ ] No client-side compatibility matrix; rely on backend and framework.

---

## Phase 6: Testing and Observability

**Goal:** Reliable runtime client with clear failure modes and metrics.

**Tasks:**

1. **Unit tests**
   - Loaders: mocked HTTP; assert request URL/body and response mapping to contract types.
   - Renderer: snapshot + config in; assert structure (e.g. correct components, no crash).

2. **Integration tests**
   - Against real or mock backend: resolve → structure → compatibility → render for at least one release. Use test tenant and test app.

3. **E2E**
   - Full flow: open URL → resolve → load → compatibility → render; assert visible UI and key interactions.

4. **Error handling**
   - Network errors, 404/403, incompatible release: clear user message and optional reporting (e.g. Sentry). Do not expose internal details.

5. **Observability**
   - Optional: log or metric for resolve/structure/compatibility duration and success/failure; optional RUM for “time to first render.”

**Deliverables:**

- [ ] Unit tests for loaders and renderer.
- [ ] Integration test(s) for full load flow.
- [ ] E2E for at least one app.
- [ ] Error handling and optional observability.

---

## References

- [Solution Structure - Runtime](../../ai-context/02-SOLUTION-STRUCTURE.md) — Runtime app structure and backend dependencies.
- [Runtime Server Implementation Plan](runtime-server-impl-plan.md) — Backend API requirements and implementation plan.
- [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md) — One pattern for all feature types; schema version + support matrix; client adapters; backward-compatibility policy.
- [AppRuntime Architecture](../latest-plan/appruntime-architecture.md) — Resolution flow, compatibility, data sources.
- [Release, Installation, Deployment & Migration](../latest-plan/release-installation-deployment-migration.md) — Release and deployment lifecycle.
- [AppRuntime Contracts](../latest-plan/appruntime-contracts.md) — CompatibilityCheckService, RuntimeVersionDto, DTOs.
