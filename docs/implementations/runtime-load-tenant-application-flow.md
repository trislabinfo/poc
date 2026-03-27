# How the runtime loads the tenant application (entities, relations, navigation)

## Deployment topology (Monolith)

- **Runtime client** = one deployed application; **Runtime BFF** = separate app only for runtime client; **Runtime BFF talks to Monolith** (all server modules in one deployed app). There is no separate HTTP call from “Runtime API” to “Tenant Application API” in the current design: the BFF hosts the TenantApplication (and AppBuilder) modules and resolves/snapshot in-process.

```
Runtime client  →  Runtime BFF (aggregates, optimizes)  →  Monolith or microservices
```

## How the tenant application is loaded (resolve + snapshot)

1. **Resolve**  
   Client calls `GET /api/runtime/resolve?tenantSlug=jan&appSlug=1&environment=Development`.  
   BFF uses **IApplicationResolverService.ResolveByUrlAsync** (TenantApplication, in-process).  
   Returns `applicationReleaseId` (and server-side connection string, not sent to client).

2. **Snapshot (entities, relations, navigation)**  
   Client calls `GET /api/runtime/snapshot?applicationReleaseId=<id>`.  
   BFF does **not** call a Tenant Application HTTP API. It dispatches in-process:
   - **GetReleaseSnapshotQuery(applicationReleaseId)** to **AppBuilder** first; if 404,
   - **GetReleaseSnapshotQuery(applicationReleaseId)** to **TenantApplication**.

3. **TenantApplication snapshot**  
   - **GetReleaseSnapshotQueryHandler** (TenantApplication.Application) loads the release from **ITenantApplicationReleaseRepository** (table `tenantapplication.tenant_application_releases`).
   - The release is the shared **ApplicationRelease** entity with: **NavigationJson**, **PageJson**, **DataSourceJson**, **EntityJson** (and schema version).
   - **ApplicationReleaseMapper.ToSnapshotDto(release)** maps to **ApplicationSnapshotDto**:
     - `NavigationJson` ← release.NavigationJson  
     - `PageJson` ← release.PageJson  
     - `DataSourceJson` ← release.DataSourceJson  
     - `EntityJson` ← release.EntityJson  
   - BFF returns this DTO to the client (JSON: `navigationJson`, `pageJson`, etc.).

4. **Runtime client**  
   - **structureLoader** GETs `/api/runtime/snapshot?applicationReleaseId=...`, receives **ApplicationSnapshotDto**.
   - Parses `navigationJson` (and other JSON fields) and builds **ApplicationSnapshot** with `navigation`, `pages`, `dataSources`, `entities`.
   - **RuntimeRenderer** renders the left navigation from `snapshot.navigation` and main content from snapshot.

So **navigation (and entities, relations, pages, data sources) are already loaded by the BFF from the tenant application via the in-process GetReleaseSnapshotQuery and sent to the client in the snapshot response.** Relations are part of the entity definitions in **EntityJson** (or schema); the client gets them in the same snapshot.

## Where tenant navigation comes from

- On **install** (Step 4), **CopyDefinitionsFromPlatformReleaseService** copies navigation definitions from the platform release into the tenant app (e.g. `tenant_navigation_definitions`).
- When a **tenant application release** is created/approved, its **NavigationJson** is populated from those tenant navigation definitions (or from the platform snapshot at copy time).
- **GetReleaseSnapshotQuery** reads the release row and returns **NavigationJson** in the snapshot; the client paints it in the browser.

## If you introduce a separate Runtime API service

If in the future a **Runtime API** host runs without the TenantApplication module and must load the tenant application over HTTP:

- That host would need to call the **Tenant Application API** (e.g. `GET /api/tenantapplication/.../releases/{releaseId}/snapshot` or whatever the Tenant Application host exposes) to get **ApplicationSnapshotDto** (including **navigationJson**).
- The Runtime API would then return that snapshot (including navigation) to the BFF or client so the runtime client can still paint navigation from the snapshot.

Current setup: **Runtime BFF** has TenantApplication in-process, so it does **not** call the Tenant Application API; it uses **GetReleaseSnapshotQuery** and already returns navigation (and the rest of the snapshot) to the runtime client for painting in the browser.
