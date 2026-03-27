# Runtime Server Implementation Plan

**Document Purpose:** Backend API requirements and implementation plan to support the **runtime client** (browser). The runtime client calls a single backend—the **Runtime BFF**—which exposes resolve, snapshot, compatibility, and runtime execution APIs. The BFF calls the **TenantApplication API** for app resolution (and, with AppBuilder, for snapshot); it calls the **Runtime API** (AppRuntime) for compatibility and for all other runtime-related executions (e.g. datasource execution, engines). Authentication is implemented at the Runtime BFF; internal modules are not called directly by the browser.

**Audience:** Backend developers, architects.

**Status:** Planning

**Related:** [Runtime — Full Implementation Plan](runtime-all-impl-plan.md) (master checklist), [Runtime Client Implementation Plan](runtime-client-impl-plan.md), [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md), [AppRuntime Architecture](../../latest-plan/appruntime-architecture.md)

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Required APIs for the Runtime Client](#required-apis-for-the-runtime-client)
4. [API 1: Resolve Application by URL](#api-1-resolve-application-by-url)
5. [API 2: Get Application Snapshot (Structure)](#api-2-get-application-snapshot-structure)
6. [API 3: Compatibility Check](#api-3-compatibility-check)
7. [Runtime execution (datasource, engines)](#runtime-execution-datasource-engines)
8. [Optional Improvements](#optional-improvements)
9. [CORS and Authentication](#cors-and-authentication)
10. [References](#references)

---

## Overview

**Application resolution is always performed by the TenantApplication module; AppBuilder never resolves applications.** AppBuilder only provides application structure (snapshot) for a given release ID. The runtime client obtains that release ID from the **Runtime BFF**, which calls TenantApplication (resolve by URL).

The runtime client runs in the browser and calls **only the Runtime BFF**. The Runtime BFF exposes REST APIs and delegates as follows (Pattern 1):

1. **Resolve** — BFF calls **TenantApplication API** (tenant slug, app slug, environment) → returns `ApplicationReleaseId` + merged configuration.
2. **Get snapshot** — BFF calls **TenantApplication API** or **AppBuilder** (for the release ID from resolve) → returns application structure (navigation, pages, data sources, entities).
3. **Compatibility check** — BFF calls **Runtime API** (AppRuntime) (application release ID, optional runtime version) → returns whether the current runtime can execute this release.
4. **Runtime execution** — BFF calls **Runtime API** (AppRuntime) for datasource execution, navigation/page/datasource engines, and other runtime execution. The runtime client does not call AppRuntime directly; all such requests go through the BFF.

Existing backend modules implement the logic via contracts (e.g. `IApplicationResolverService` in TenantApplication, snapshot services in AppBuilder/TenantApplication, `ICompatibilityCheckService` and execution engines in AppRuntime). This plan focuses on implementing the **Runtime BFF** that exposes these capabilities as a single API surface to the runtime client, and on CORS and authentication at the BFF.

---

## Architecture

**Flow:** Runtime client (browser) → **Runtime BFF** (single origin, auth) → **TenantApplication API** (resolve, snapshot when tenant-owned) and **Runtime API** (AppRuntime) (compatibility, datasource execution, engines). Internal module endpoints are not exposed to the browser.

```
Runtime client (browser)
    → Runtime BFF  [auth, CORS]
        → TenantApplication API   (resolve by URL; snapshot for tenant releases)
        → AppBuilder              (snapshot for platform releases)
        → Runtime API (AppRuntime) (compatibility; datasource execution; engines)
```

- **Runtime BFF:** Dedicated backend host or API path (e.g. `/api/runtime/*`) that is the only backend the runtime client calls. It validates authentication, derives tenant/user context, and calls the TenantApplication API (resolve, snapshot when tenant-owned), AppBuilder (snapshot for platform releases), and the Runtime API (AppRuntime) for compatibility and runtime execution. Internal module endpoints are not exposed to the browser.
- **TenantApplication API:** BFF calls it for app resolution (`IApplicationResolverService.ResolveByUrlAsync`) and for snapshot when the release is tenant-owned. Resolution is always performed by TenantApplication; AppBuilder never resolves.
- **AppBuilder:** BFF calls it for snapshot by release ID when the release is a platform release.
- **Runtime API (AppRuntime):** BFF calls it for compatibility (`ICompatibilityCheckService.CheckCompatibilityAsync`) and for all other runtime-related execution (datasource execution, navigation/page/datasource engines). AppRuntime is the backend module for runtime execution; the BFF does not perform execution itself.

---

## Required APIs for the Runtime Client

All of the following are **Runtime BFF** endpoints. The runtime client calls only these; it does not call TenantApplication, AppBuilder, or AppRuntime directly.

| # | Capability        | BFF route (example)        | BFF delegates to                    | Purpose for runtime client        |
|---|-------------------|----------------------------|-------------------------------------|-----------------------------------|
| 1 | Resolve by URL    | `GET /api/runtime/resolve` | TenantApplication API               | Get ApplicationReleaseId + config |
| 2 | Get snapshot      | `GET /api/runtime/snapshot`| TenantApplication API or AppBuilder| Get application structure         |
| 3 | Compatibility     | `GET /api/runtime/compatibility` | Runtime API (AppRuntime)        | Verify runtime can run this release |
| 4 | Runtime execution | e.g. `/api/runtime/...`    | Runtime API (AppRuntime)            | Datasource execution, engines     |

Runtime execution (datasource execution, engines) is delegated by the BFF to the Runtime API (AppRuntime). Exact routes and contracts are defined in the AppRuntime module; the BFF exposes a single surface to the client and forwards to AppRuntime.

---

## API 1: Resolve Application by URL

**Purpose:** Runtime client has URL (e.g. `/{tenantSlug}/{appSlug}/{environment}`). It calls the Runtime BFF, which returns the resolved tenant application and the **ApplicationReleaseId** and **merged configuration** for that environment. Resolution is performed by TenantApplication only; the BFF delegates to it.

**Contract (existing):** `TenantApplication.Contracts.IApplicationResolverService.ResolveByUrlAsync(tenantSlug, appSlug, environment)` → `Result<ResolvedApplicationDto>`.

**ResolvedApplicationDto (existing):** TenantId, TenantSlug, TenantApplicationId, ApplicationId, AppSlug, EnvironmentId, EnvironmentType, **ApplicationReleaseId**, **EnvironmentConfiguration** (merged config JSON).

**Runtime BFF API:**

- **Method/route:** e.g. `GET /api/runtime/resolve` with query params `tenantSlug`, `appSlug`, `environment` (optional; default production).
- **Response:** 200 OK with body matching `ResolvedApplicationDto` (or a minimal DTO that includes at least `ApplicationReleaseId` and `EnvironmentConfiguration`). 404 when not found; 403 when tenant/app not active or not allowed.
- **Implementation:** BFF receives the request, optionally validates auth and derives tenant context, then calls TenantApplication (in-process or HTTP) `IApplicationResolverService.ResolveByUrlAsync` and returns the result.

**Implementation tasks:**

- [ ] Add Runtime BFF endpoint: `GET /api/runtime/resolve?tenantSlug=&appSlug=&environment=`.
- [ ] BFF calls TenantApplication `IApplicationResolverService.ResolveByUrlAsync` and returns DTO or mapped response.
- [ ] Document route, query params, and response shape for the runtime client.
- [ ] Ensure TenantApplication exposes the resolve capability (service or internal API) so the BFF can call it. If using separate hosts, BFF calls TenantApplication host over HTTP; gateway routes internal traffic to TenantApplication.

---

## API 2: Get Application Snapshot (Structure)

**Purpose:** Runtime client has `ApplicationReleaseId` from resolve. It calls the Runtime BFF to get the **application structure** (navigation, pages, data sources, entities) for that release. The BFF delegates to AppBuilder (platform releases) or TenantApplication (tenant custom/forked releases).

**Contract (existing):** AppBuilder (or TenantApplication for tenant releases) exposes snapshot retrieval (e.g. `GetSnapshotAsync(applicationReleaseId)`). See `AppRuntime.Contracts` / `ApplicationSnapshotDto` and AppBuilder release snapshot concepts in docs.

**Runtime BFF API:**

- **Method/route:** e.g. `GET /api/runtime/snapshot?applicationReleaseId={guid}` (or `GET /api/runtime/releases/{applicationReleaseId}/snapshot`).
- **Response:** 200 OK with body matching the snapshot DTO used by the runtime (e.g. `ApplicationSnapshotDto`: navigation, pages, dataSources, optional entities). 404 when release not found; 403 when caller has no access.
- **Implementation:** BFF receives the request, optionally validates auth, then determines release ownership (platform vs tenant) and calls AppBuilder or TenantApplication to get the snapshot. Alternatively, BFF calls a single internal endpoint that resolves release ownership and returns the snapshot from the correct module.

**Implementation tasks:**

- [ ] Add Runtime BFF endpoint: e.g. `GET /api/runtime/snapshot?applicationReleaseId=` (or path-based variant).
- [ ] BFF calls AppBuilder or TenantApplication (depending on release ownership) to get snapshot; response shape must align with runtime client contracts and `packages/contracts`. Include **schema version** in response if multiple snapshot versions are supported (see [Optional Improvements](#optional-improvements)).
- [ ] Document route and response for the runtime client.
- [ ] Ensure AppBuilder and TenantApplication expose snapshot retrieval (service or internal API) so the BFF can call them.

---

## API 3: Compatibility Check

**Purpose:** Before rendering, the runtime client must know whether the **current runtime version** can execute the resolved application release. The Runtime BFF delegates to AppRuntime. Compatibility follows the [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md): one check for all feature types (datasource, workflow, validation rules, etc.) using a definition schema version + engine support matrix.

**Contract (existing):** `AppRuntime.Contracts.ICompatibilityCheckService.CheckCompatibilityAsync(applicationReleaseId, optional runtimeVersionId)` → `Result<CompatibilityCheckResultDto>`.

**Runtime BFF API:**

- **Method/route:** e.g. `GET /api/runtime/compatibility?applicationReleaseId={guid}` and optional `runtimeVersionId={guid}` (or use default runtime version).
- **Response:** 200 OK with body indicating compatible (boolean), optional message, optional schema version or runtime version info. 404 when release or runtime version not found.
- **Implementation:** BFF receives the request, optionally validates auth, then calls AppRuntime `ICompatibilityCheckService.CheckCompatibilityAsync` and returns the result.

**Implementation tasks:**

- [ ] Add Runtime BFF endpoint: e.g. `GET /api/runtime/compatibility?applicationReleaseId=&runtimeVersionId=` (optional).
- [ ] BFF calls AppRuntime `ICompatibilityCheckService.CheckCompatibilityAsync` and returns a DTO (e.g. compatible, message, optional schemaVersion).
- [ ] Document route and response for the runtime client.
- [ ] Ensure AppRuntime exposes the compatibility service (in-process or internal API) so the BFF can call it. If using separate hosts, BFF calls AppRuntime host over HTTP.

---

## Runtime execution (datasource, engines)

**Purpose:** At runtime, the client may need to execute datasources, run navigation/page/datasource engines, or perform other execution that the AppRuntime module owns. All such requests from the runtime client go to the Runtime BFF, which forwards them to the **Runtime API** (AppRuntime). The BFF does not execute; it delegates to AppRuntime.

**Implementation:** Define execution endpoints (e.g. datasource execute, engine invocations) in the AppRuntime module. The Runtime BFF exposes corresponding routes under `/api/runtime/...` and forwards requests to the Runtime API (in-process or HTTP). Contracts, routes, and response shapes are defined in the AppRuntime module and related docs.

**Implementation tasks:**

- [ ] Define Runtime API (AppRuntime) execution endpoints (datasource, engines) per AppRuntime module plan.
- [ ] Add Runtime BFF proxy/forward endpoints for runtime execution that delegate to the Runtime API.
- [ ] Document execution routes and contracts for the runtime client.

---

## Optional Improvements

- **Bootstrap endpoint:** A single BFF endpoint that performs resolve + load snapshot + compatibility check and returns a combined payload. Reduces round-trips for the runtime client. Implementation: e.g. `GET /api/runtime/bootstrap?tenantSlug=&appSlug=&environment=` (or POST with body); BFF orchestrates calls to TenantApplication, AppBuilder/TenantApplication, and AppRuntime and returns one response.
- **Schema version in snapshot response:** Align with [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md): include per–feature-type schema version(s) (or a single snapshot schema version) in the snapshot so the runtime client can select the correct adapter. Backend stores or derives schema version per release and includes it in the snapshot DTO.
- **Runtime version in response:** Compatibility API could return the current (or default) runtime version and its supported range (e.g. min/max application version) so the client can show clearer upgrade messages.

---

## CORS and Authentication

- **CORS:** Only the **Runtime BFF** is called by the browser. Configure CORS on the Runtime BFF host (or the gateway that fronts it) for the runtime client origin(s) (e.g. `app.datarizen.com` or tenant-specific subdomain). Internal modules (TenantApplication, AppBuilder, AppRuntime) are not called directly by the client and do not need CORS for the runtime client origin.
- **Authentication:** Implement authentication at the **Runtime BFF**. The runtime client sends a session cookie or token (e.g. end-user JWT) to the BFF. The BFF validates the token/session, derives tenant and user context, and forwards requests to internal modules (which may receive tenant/user context via headers or server-side identity; they do not need to validate the browser token). For public apps, tenant and environment can be derived from the URL (tenant slug, app slug, environment); the resolve endpoint may be callable with minimal or no auth when the app is public. Document the auth model: which endpoints require auth, how tenant is determined (URL vs header vs token), and how the BFF passes context to internal services.

**Implementation tasks:**

- [ ] Configure CORS for runtime client origin(s) on the Runtime BFF (or gateway fronting the BFF).
- [ ] Implement and document authentication at the Runtime BFF (cookie/token validation, tenant/user derivation, public vs authenticated endpoints).
- [ ] Document auth model for resolve, snapshot, and compatibility endpoints (anonymous vs authenticated, tenant from URL vs header).
- [ ] If tenant is from URL only for public apps, ensure the resolve endpoint does not rely on a tenant header that the browser cannot send on first load.

---

## References

- [Runtime Client Implementation Plan](runtime-client-impl-plan.md) — How the client uses the Runtime BFF APIs.
- [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md) — One pattern for all feature types; schema version + support matrix; backward-compatibility policy.
- [AppRuntime Architecture](../latest-plan/appruntime-architecture.md) — Resolution flow, data sources (AppBuilder vs TenantApplication).
- [AppRuntime Contracts](../latest-plan/appruntime-contracts.md) — ICompatibilityCheckService, DTOs.
- [URL Routing and Environment Deployment](../architectura/url-routing-and-environment-deployment.md) — IApplicationResolverService, ResolvedApplicationDto.
- [Release, Installation, Deployment & Migration](../latest-plan/release-installation-deployment-migration.md) — Release and deployment lifecycle.
