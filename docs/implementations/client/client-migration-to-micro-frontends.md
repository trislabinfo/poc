# Client Migration to Micro-Frontends

**Document Purpose:** Step-by-step guide for migrating **any** Datarizen client application (Builder, Dashboard, Runtime, or a future fourth app) to a micro-frontend architecture when scaling demands it. All three apps are designed to be **micro-frontend ready**; this doc describes how to perform the migration for whichever app you choose.

**Audience:** Frontend developers, architects, DevOps engineers.

**Status:** Planning / Future Implementation

**Context:** Datarizen uses a **three-application model** (Builder, Dashboard, Runtime), extensible to a **fourth** (or more), plus a **shared contracts package**. See [Solution Structure](../../ai-context/02-SOLUTION-STRUCTURE.md). Each app is a modular monolith with clear boundaries and entry points so that **any** of them can adopt micro-frontends without a rewrite.

---

## Table of Contents

1. [Overview](#overview)
2. [Which Application Are You Migrating?](#which-application-are-you-migrating)
3. [Shared Contracts and Versioning](#shared-contracts-and-versioning)
4. [Prerequisites Checklist](#prerequisites-checklist)
5. [Phase 1: Prepare the Application](#phase-1-prepare-the-application)
6. [Phase 2: Extract First Remote](#phase-2-extract-first-remote)
7. [Phase 3: Extract Remaining Remotes](#phase-3-extract-remaining-remotes)
8. [Phase 4: Independent Deployment](#phase-4-independent-deployment)
9. [Adding a Fourth (or More) Application](#adding-a-fourth-or-more-application)
10. [Testing Strategy](#testing-strategy)
11. [Rollback Plan](#rollback-plan)
12. [Troubleshooting](#troubleshooting)
13. [References](#references)

---

## Overview

### Architecture Alignment

- **Builder** (`/clients/builder`): Modular monolith by **features** (canvas, workflow-editor, ai-assistant, marketplace). Can become shell + **feature remotes**.
- **Dashboard** (`/clients/dashboard`): Modular monolith by **backend modules** (tenant, identity, user, feature, appBuilder, tenantApplication). Can become shell + **module remotes**.
- **Runtime** (`/clients/runtime`): Modular monolith by **boundaries** (renderer, loaders, optional plugins). Can become shell + **renderer/plugin remotes** if needed.

Any of the three can adopt MF independently. The **same migration pattern** (prepare → extract first remote → extract rest → independent deploy) applies to each; only the **units you extract** differ (features vs modules vs renderer/plugins).

### Migration Goals (Any App)

- Enable independent deployment of slices (features, modules, or runtime boundaries).
- Support multiple teams or independent release cycles per slice.
- Reduce bundle size via lazy-loaded remotes.
- Keep the app runnable as a single deploy (no remotes) during rollout and for rollback.
- For Builder and Runtime: keep shared contracts (`packages/contracts`) versioned and compatible.

### Technology Stack

- **Build Tool:** Vite
- **Module Federation:** `@originjs/vite-plugin-federation`
- **Framework:** Svelte / SvelteKit
- **State Management:** Svelte stores
- **Shared Contracts:** `packages/contracts` (Builder and Runtime)

---

## Which Application Are You Migrating?

Choose one (or plan for all three over time). The phases below are written generically; map “module/feature/boundary” to your app as follows.

### Builder

- **Units to extract:** **Features** (e.g. workflow-editor, ai-assistant, marketplace, component-palette).
- **Shell:** Builder app becomes host; loads remotes from e.g. `clients/builder-remotes/{feature-name}` or CDN.
- **When to migrate:** 3+ teams on Builder, plugin/marketplace, or independent feature releases.
- **Contracts:** Builder remotes and Runtime must share same major version of `packages/contracts`.

### Dashboard

- **Units to extract:** **Backend modules** (e.g. tenant, identity, user, feature, appBuilder, tenantApplication).
- **Shell:** Dashboard app becomes host; loads remotes from e.g. `clients/dashboard-remotes/{module-name}` or CDN.
- **When to migrate:** Multiple teams owning different dashboard areas, or independent module deployments.
- **Contracts:** Dashboard does not use `packages/contracts` for app schema; only Builder and Runtime do. Shared deps are Svelte, auth, API client, etc.

### Runtime

- **Units to extract:** **Boundaries** such as renderer, loaders, or plugin bundles (if you add pluggable renderers or runtime extensions).
- **Shell:** Runtime app becomes host; loads remotes from e.g. `clients/runtime-remotes/{boundary-name}` or CDN.
- **When to migrate:** Plugin renderers, multiple runtime modes, or very large runtime bundle split by concern.
- **Contracts:** Runtime remotes must use same major version of `packages/contracts` as Builder (and shell). Runtime also depends on Runtime BFF (resolve, structure, compatibility, execution); compatibility and versioning for all feature types follow the [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md). See [Runtime Client Implementation Plan](runtime-client-impl-plan.md) and [Runtime Server Implementation Plan](runtime-server-impl-plan.md) for backend requirements.

### New (Fourth) Application

- Add under `/clients/{appName}` (e.g. `/clients/admin`, `/clients/analytics`). Structure it as a **modular monolith** with clear slices and `index.ts` entry points per slice. Optionally add `/src/shell` from the start. When scaling demands it, use the same phases to turn it into shell + remotes. See [Adding a Fourth (or More) Application](#adding-a-fourth-or-more-application).

---

## Shared Contracts and Versioning

Builder and Runtime depend on **`packages/contracts`** (component schema, layout JSON, actions, validation). When introducing remotes in **Builder** or **Runtime**:

- **Contract package** must be versioned (e.g. semver); shell and remotes (and Runtime if Builder is migrating) must align on the same major version.
- **Breaking contract changes** require coordinated releases or backward-compatible evolution.
- Document compatibility in `packages/contracts/README.md` and release notes.

Dashboard does not consume app-definition contracts; it shares only Svelte, auth, and API client. See [Solution Structure - Shared Contracts Package](../../ai-context/02-SOLUTION-STRUCTURE.md#shared-contracts-package-packagescontracts).

---

## Prerequisites Checklist

Before starting MF migration for **any** app:

- [ ] Three-app model in place (see [02-SOLUTION-STRUCTURE](../../ai-context/02-SOLUTION-STRUCTURE.md)); app to migrate is one of Builder, Dashboard, or Runtime (or a new fourth).
- [ ] Target app is a modular monolith with **clear boundaries** (Builder: features; Dashboard: modules; Runtime: renderer/loaders/plugins).
- [ ] Each boundary has an **entry point** (e.g. `index.ts`) and does not depend on other boundaries; shared code is in `/shared` or `/lib`.
- [ ] App build and tests pass.
- [ ] If app is Builder or Runtime: shared contracts package exists and is used; version compatibility is understood.

---

## Phase 1: Prepare the Application

**Goal:** Make the app ready to extract slices as remotes (clear boundaries, entry points, optional shell infra, shared deps).

**Duration:** 1–2 weeks

Apply to **Builder**, **Dashboard**, or **Runtime** as appropriate.

### Step 1.1: Verify Boundary Structure

- **Builder:** Each feature under `/src/features/{featureName}` has `index.ts`, and no cross-feature imports.
- **Dashboard:** Each module under `/src/modules/{moduleName}` has `index.ts`, and no cross-module imports.
- **Runtime:** Renderer, loaders (and any plugin boundaries) have `index.ts` and clear borders; no cross-boundary imports.

**Action Items:**
- [ ] Add or verify `index.ts` per slice.
- [ ] Ensure shared code lives only in `shared/` or `lib/`.
- [ ] Document public API (components, routes, stores) per slice.

### Step 1.2: Optional Shell Infrastructure

Add `/src/shell` when you are ready to load remotes:

```
/src/shell
  /module-loader
    loadModule.ts            # Load remote or local slice
    moduleRegistry.ts        # Slice registry
  /routing
    router.ts
    route-guards.ts
  /layout
    ShellLayout.svelte
    Navigation.svelte
```

**Action Items:**
- [ ] Implement `loadLocalModule(sliceName)` so the app can run without remotes.
- [ ] Use shell only when proceeding to Phase 2.

### Step 1.3: Define Shared Dependencies for Federation

List and pin dependencies shared between shell and remotes (e.g. Svelte, SvelteKit; for Builder/Runtime also `@datarizen/contracts`). Document and pin versions.

**Action Items:**
- [ ] List shared dependencies; pin in app `package.json`.
- [ ] For Builder/Runtime: ensure `packages/contracts` is a shared dependency where needed.

### Step 1.4: Test App as Single Deploy

**Action Items:**
- [ ] Run all tests; verify build and bundle size.
- [ ] Confirm no regressions.

**Completion Criteria:**
- ✅ Boundaries clear and entry points in place.
- ✅ Optional shell added if proceeding to Phase 2.
- ✅ Shared dependencies documented and pinned.
- ✅ App builds and tests pass.

---

## Phase 2: Extract First Remote

**Goal:** Extract one slice as a remote and load it from the app (which becomes the shell).

**Duration:** 1–2 weeks

**Example below uses Builder (workflow-editor);** for Dashboard use a module (e.g. tenant); for Runtime use a boundary (e.g. renderer or a plugin). Replace paths and names accordingly.

### Step 2.1: Create Remote Project

Create a new app that will expose the slice via Module Federation.

**Builder example:**
```
/clients/builder-remotes
  /workflow-editor
    package.json
    vite.config.ts
    tsconfig.json
    /src
      /features/workflow-editor
      bootstrap.ts
      remote-entry.ts
```

**Dashboard example:**
```
/clients/dashboard-remotes
  /tenant
    package.json
    vite.config.ts
    /src
      /modules/tenant
      bootstrap.ts
      remote-entry.ts
```

**Runtime example:**
```
/clients/runtime-remotes
  /renderer
    package.json
    vite.config.ts
    /src
      /renderer
      bootstrap.ts
      remote-entry.ts
```

**Action Items:**
- [ ] Create project; copy slice code from host app.
- [ ] Fix imports (shared deps, contracts if applicable); no host-internal paths.

### Step 2.2: Configure Module Federation (Remote)

In the remote’s `vite.config.ts`:

```typescript
import { defineConfig } from 'vite';
import { sveltekit } from '@sveltejs/kit/vite';
import { federation } from '@originjs/vite-plugin-federation';

export default defineConfig({
  plugins: [
    sveltekit(),
    federation({
      name: 'builder-workflow-editor',   // or dashboard-tenant, runtime-renderer, etc.
      filename: 'remoteEntry.js',
      exposes: {
        './WorkflowEditor': './src/features/workflow-editor/index.ts',
        './WorkflowEditorRoutes': './src/features/workflow-editor/routes.ts',
      },
      shared: {
        'svelte': { singleton: true, requiredVersion: false },
        '@sveltejs/kit': { singleton: true, requiredVersion: false },
        '@datarizen/contracts': { singleton: true },  // omit for Dashboard
      },
    }),
  ],
  build: { target: 'esnext', minify: false, cssCodeSplit: false },
});
```

**Action Items:**
- [ ] Install `@originjs/vite-plugin-federation`; set `exposes` and `shared` to match host.
- [ ] Build and verify `remoteEntry.js` is produced.

### Step 2.3: Bootstrap and Remote Entry

- `bootstrap.ts`: Register the slice in a module registry and return public API.
- `remote-entry.ts`: Re-export slice and bootstrap for the host.

**Action Items:**
- [ ] Implement bootstrap and remote-entry; test remote build.

### Step 2.4: Update Host (Shell) to Load Remote

In the **host app** (Builder, Dashboard, or Runtime), add Module Federation as host:

```typescript
// e.g. clients/builder/vite.config.ts
federation({
  name: 'builder-shell',
  remotes: {
    workflowEditor: 'http://localhost:3001/assets/remoteEntry.js',
    // prod: 'https://cdn.datarizen.com/builder-remotes/workflow-editor/latest/remoteEntry.js',
  },
  shared: {
    'svelte': { singleton: true },
    '@sveltejs/kit': { singleton: true },
    '@datarizen/contracts': { singleton: true },
  },
});
```

**Action Items:**
- [ ] Add federation plugin to host; implement dynamic load in `shell/module-loader`.
- [ ] Register remote routes in host router when loaded.
- [ ] Keep ability to load the slice **locally** when remote URL is not set.

### Step 2.5: Test First Remote

**Action Items:**
- [ ] Run remote and host; verify slice loads, routing, shared state.
- [ ] For Builder/Runtime: verify contracts version match.

**Completion Criteria:**
- ✅ First slice runs as remote; host can fall back to local.
- ✅ Shared deps are singletons; no duplicate Svelte/contracts.

---

## Phase 3: Extract Remaining Remotes

**Goal:** Extract other slices as remotes using the same pattern.

**Duration:** 2–4 weeks (depends on number of slices)

### Steps per Slice

For each additional slice:

- [ ] Create remote project under `clients/{app}-remotes/{slice-name}`.
- [ ] Copy slice code; fix imports and shared deps.
- [ ] Add federation config (exposes, shared); add bootstrap and remote-entry.
- [ ] Register remote in host (remotes config + loader + routes).
- [ ] Test; keep optional local fallback.
- [ ] Optionally remove slice from host repo and rely on remote only.

### Shell Configuration Example (Multiple Remotes)

**Builder:**
```typescript
remotes: {
  workflowEditor: isDev ? 'http://localhost:3001/assets/remoteEntry.js' : '...',
  aiAssistant:    isDev ? 'http://localhost:3002/assets/remoteEntry.js' : '...',
  marketplace:     isDev ? 'http://localhost:3003/assets/remoteEntry.js' : '...',
},
```

**Dashboard:**
```typescript
remotes: {
  tenant:   isDev ? 'http://localhost:3011/assets/remoteEntry.js' : '...',
  identity: isDev ? 'http://localhost:3012/assets/remoteEntry.js' : '...',
  user:     isDev ? 'http://localhost:3013/assets/remoteEntry.js' : '...',
  // ...
},
```

**Completion Criteria:**
- ✅ All intended slices can run as remotes; host loads them; tests pass.
- ✅ Contracts (Builder/Runtime) consistent across shell and remotes.

---

## Phase 4: Independent Deployment

**Goal:** Deploy each remote to CDN (or equivalent) and have host load them by URL.

**Duration:** 1–2 weeks

### Step 4.1: CI/CD Per Remote

- Each remote has its own build and deploy pipeline; deploy to versioned paths (e.g. `.../workflow-editor/1.0.0/`).

**Action Items:**
- [ ] Add workflow per remote; deploy to CDN/static host; use semver or similar.

### Step 4.2: Host Production Config

- Host reads remote URLs from config or env (e.g. `BUILDER_REMOTES_WORKFLOW_EDITOR_URL`). Document rollback (switch URL to previous version).

**Action Items:**
- [ ] Env-based or config-based remote URLs; document rollback; CORS and caching correct.

### Step 4.3: Contracts and Monitoring (Builder/Runtime)

- Builder/Runtime: pin `@datarizen/contracts`; document Runtime compatibility when contracts change.
- Monitor remote load failures and performance; use feature flags or config to disable a remote and fall back to local.

**Completion Criteria:**
- ✅ Remotes deploy independently; host loads from CDN; versioning and rollback documented; monitoring in place.

---

## Adding a Fourth (or More) Application

To add a new client application (e.g. admin, analytics, white-label portal):

1. **Create** `/clients/{appName}` with the same **modular, MF-ready** structure as Builder/Dashboard/Runtime:
   - Clear slices (features or modules) with `index.ts` entry points.
   - No cross-slice imports; shared code in `/shared` or `/lib`.
   - Optional `/src/shell` (module-loader, routing, layout) from the start.
2. **Document** what the app does and which boundaries could become remotes later.
3. **When scaling demands it,** use the same migration phases (prepare → extract first remote → extract rest → independent deploy). Remotes can live under e.g. `clients/{appName}-remotes/{slice-name}`.

This keeps the codebase ready for MF on **any** app without committing to MF until needed.

---

## Testing Strategy

- **Unit:** Per slice (components, stores, API clients); host shell (loader, routing).
- **Integration:** Host loads one or more remotes; navigation and shared state.
- **E2E:** Critical flows with remotes enabled.
- **Contracts (Builder/Runtime):** Ensure shell, remotes, and Runtime use same contracts version (e.g. tests in `packages/contracts`).

---

## Rollback Plan

### Roll Back a Single Remote

- Point host config to previous version of that remote’s URL, or disable the remote and use local slice if still present. No need to redeploy other apps.

### Roll Back Entire App to Single Deploy

- Remove or empty `remotes` in the app’s federation config; ensure all slices are again available locally; rebuild and redeploy that app only. Other apps (Builder, Dashboard, Runtime) are unchanged.

---

## Troubleshooting

- **Remote fails to load:** Check URL, CORS, `remoteEntry.js` path; verify shared dependency versions match host.
- **Contracts mismatch (Builder/Runtime):** Same major version of `@datarizen/contracts` in shell, remotes, and Runtime; no duplicate package.
- **Routes not showing:** Remote bootstrap must register routes with host router; host must load remote before rendering.
- **Build failures:** Align Vite and federation plugin versions; ensure `exposes` paths match actual files.

---

## Success Criteria

Migration is complete for the chosen app when:

- ✅ App can run as shell + remotes or as single deploy (configurable).
- ✅ At least one slice is deployed as a remote and loaded from CDN.
- ✅ For Builder/Runtime: shared contracts versioned and compatible.
- ✅ Tests pass; monitoring and rollback are in place.
- ✅ Documentation (this doc and [02-SOLUTION-STRUCTURE](../../ai-context/02-SOLUTION-STRUCTURE.md)) is updated.

---

## References

- [Solution Structure (02-SOLUTION-STRUCTURE.md)](../../ai-context/02-SOLUTION-STRUCTURE.md) — Three-app model (extensible to four), shared contracts, MF-ready design for all apps.
- [Runtime Client Implementation Plan](runtime-client-impl-plan.md) — Implementation plan for the runtime client (contract-first, loaders, renderer, versioning).
- [Runtime Server Implementation Plan](runtime-server-impl-plan.md) — Backend API requirements and implementation plan for runtime support.
- [Compatibility and Versioning Framework](compatibility-and-versioning-framework.md) — One pattern for compatibility and versioning across all feature types; schema version + support matrix + client adapters.
- [Vite Plugin Federation](https://github.com/originjs/vite-plugin-federation)
- [Micro-Frontends Guide](https://micro-frontends.org/)
