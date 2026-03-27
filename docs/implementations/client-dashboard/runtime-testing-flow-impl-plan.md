# Dashboard — Runtime Testing Flow Implementation Plan

**Document Purpose:** Implementation plan for a manual runtime testing flow page in the client dashboard app. The page provides expandable step cards (create tenant, create application definition, install, release, approve, environment, deploy) with inputs, execute buttons, and response areas so the full lifecycle can be run and verified before testing the runtime client (including HTML initial view and JSON fallback).

**Audience:** Frontend developers, tech leads, AI coding agents.

**Status:** Implemented — dashboard API client, `/test-runtime` page with step cards 1–9, flow state, and README in place.

**Related:** [Solution Structure — Dashboard](../../ai-context/02-SOLUTION-STRUCTURE.md#dashboard-application-clientappsdashboard), [Runtime All Implementation Plan](../client/runtime-all-impl-plan.md), [Client Migration to Micro-Frontends](../client/client-migration-to-micro-frontends.md), [Application Definition HTML Implementation Plan](../application-definition/application-definition-html-impl-plan.md)

---

## Table of Contents

1. [Dashboard app architecture](#dashboard-app-architecture)
2. [Dashboard client execution via Aspire](#dashboard-client-execution-via-aspire)
3. [Runtime testing flow — goal and scope](#runtime-testing-flow--goal-and-scope)
4. [Steps to cover](#steps-to-cover)
5. [Page structure and UI](#page-structure-and-ui)
6. [State and data flow](#state-and-data-flow)
7. [API client and configuration](#api-client-and-configuration)
8. [Design and UX alignment](#design-and-ux-alignment)
9. [Implementation checklist](#implementation-checklist)
10. [Out of scope](#out-of-scope)

---

## Dashboard app architecture

### Role in the solution

The **dashboard** is one of three deployable client apps under `client/apps/` (with **builder** and **runtime**). In the docs it is the “SaaS multi-tenant dashboard” for billing, orgs, tenant settings, and RBAC. The runtime testing flow is a feature page within this app.

### Tech stack (aligned with runtime)

- **Framework:** SvelteKit, Svelte 5, Vite, TypeScript.
- **Shared packages:** `@datarizen/design` (tokens) — same as runtime; `@datarizen/contracts` only if the dashboard ever needs app-definition types.
- **Layout:** Root layout imports `@datarizen/design/tokens.css` and uses a single `.app` wrapper with `min-height: 100vh` and `font-family: system-ui, sans-serif` — same pattern as runtime’s `+layout.svelte`.

### Intended structure (from docs)

From [02-SOLUTION-STRUCTURE](../../ai-context/02-SOLUTION-STRUCTURE.md):

- **Modules** (mirroring backend): `tenant`, `identity`, `user`, `feature`, `appBuilder`, `tenantApplication` — each with `index.ts`, `api`, `components`, `stores`, `routes`; MF-ready.
- **Features:** User-facing pages (billing, org-settings, apps, and the testing flow).
- **Shared:** `apiClient`, auth, stores, types, shared UI (same idea as runtime’s `$lib/shared`).
- **Shell:** Optional, for a future micro-frontend host (module loader, routing, layout).

**Current state:** The dashboard is minimal today: a single `+page.svelte`, `+layout.svelte`, and `lib/index.ts`. There are no `modules/`, `features/`, or `shared/` folders yet. The testing flow can be added as the first real “feature” and can live at a single route (e.g. `/test-runtime`) without requiring the full module structure upfront.

### Design principles (same as runtime)

- **Design tokens:** Use `--datarizen-*` for colors, spacing, radius (e.g. `var(--datarizen-primary)`, `var(--datarizen-spacing-lg)`), as in runtime and the current dashboard `+page.svelte`.
- **API access:** One base URL (e.g. `VITE_API_BASE_URL` pointing at the API gateway). All backend calls go through a small **shared API client** with `getApiBaseUrl()`, `apiGet`, `apiPost` (same pattern as `client/apps/runtime/src/lib/shared/apiClient.ts`). Dashboard will call gateway paths like `/api/tenant`, `/api/appbuilder`, `/api/tenantapplication` (same host as Bruno).
- **Thin UI layer:** Components/pages handle layout and user input; API calls and response handling live in the shared client or small loader/action modules, not buried in big components.
- **Svelte 5:** Use `$state`, `$effect`, `$props` for the new page so patterns match the runtime app.
- **Boundaries:** No cross-module/cross-feature imports; shared code lives in `$lib/shared` or `$lib` (when you add modules/features later, the testing flow can stay in a single feature or under `routes`).

### API and deployment

- The dashboard is expected to call the **same API gateway** as the runtime (and as Bruno): e.g. `https://localhost:8443`. The gateway routes to Tenant, AppBuilder, TenantApplication, etc.
- There is no separate “Dashboard BFF” in the docs; the testing page will use the same base URL as the runtime, with the same env-driven config (`VITE_API_BASE_URL`), and will call the existing backend APIs directly (with auth inherited, e.g. cookie or header, as in Bruno).

---

## Dashboard client execution via Aspire

**Current state:** The dashboard **client** (Svelte app in `client/apps/dashboard`) is **not** run by the Aspire AppHost in any topology. It is started manually (e.g. `pnpm run dev --filter dashboard` or `npm run dev:dashboard` from repo root). The AppHost only runs .NET projects (Monolith, DistributedApp, or Microservices). In **DistributedApp**, a **controlpanel** backend host (`MultiAppControlPanelHost`) is added—it serves Tenant, Identity, TenantApplication APIs; the gateway routes to it. That host does **not** serve the dashboard SPA; it is an API-only host. In **Microservices**, there is no controlpanel project; the gateway routes to separate services (identity, tenant, appbuilder, tenantapplication, runtimebff).

**How the dashboard fits per topology:**

| Topology        | Dashboard backend APIs                         | Dashboard client (SPA) run how                          |
|-----------------|--------------------------------------------------|---------------------------------------------------------|
| **Monolith**    | Same host as all modules                         | Separate: `pnpm run dev --filter dashboard`             |
| **DistributedApp** | `controlpanel` host (MultiAppControlPanelHost) | Separate: `pnpm run dev --filter dashboard`             |
| **Microservices**  | No single “controlpanel”; gateway → tenant, identity, appbuilder, tenantapplication, runtimebff | Separate: `pnpm run dev --filter dashboard`; `VITE_API_BASE_URL` = gateway |

In all cases the dashboard client must set `VITE_API_BASE_URL` to the **gateway** URL (e.g. `https://localhost:8443` when using gateway HTTPS) so the testing flow and any future dashboard API calls go through the gateway.

**Including the dashboard client in Aspire (optional):** To run the dashboard SPA as part of the Aspire app (e.g. for one-command local dev or for consistency with other frontends):

- Use .NET Aspire’s **Node/npm integration** (e.g. `AddNpmApp`) or the **Aspire Node.js Community Toolkit** (e.g. `AddPnpmApp`) in `server/src/AppHost/Program.cs`.
- Add the dashboard resource in the same topology block(s) where you want it (e.g. for **Microservices** and optionally **DistributedApp**):
  - **Working directory:** Path to the dashboard app (e.g. `../../client/apps/dashboard` relative to AppHost, or a path that resolves from the AppHost project).
  - **Script:** `"dev"` for Vite dev server (hot reload) or `"preview"` if serving a pre-built app.
  - **HTTP endpoint:** Use `WithHttpEndpoint(port: 5xxxx, env: "PORT")` (or the env var the Vite dev server respects) so Aspire assigns a port and the dashboard is reachable from the Aspire dashboard.
  - **Gateway URL for the dashboard:** Pass the gateway URL into the dashboard so it can call the APIs. Use `WithEnvironment("VITE_API_BASE_URL", gatewayUrl)` where `gatewayUrl` is a reference to the gateway resource’s HTTPS endpoint (e.g. `gateway.GetEndpoint("gatewayHttps")` or the appropriate Aspire endpoint API). That way the dashboard client runs with the correct API base URL for the current topology.
- **Publish (e.g. Kubernetes):** For `aspire publish -e k8s`, the dashboard can be built into static assets and served by a small web host or a dedicated container; running a Node dev server in production is not typical. So “execution via Aspire” in this plan refers mainly to **local development** (AppHost starts the dashboard dev server). Production deployment of the dashboard SPA is a separate concern (static hosting, CDN, or container serving built files).

**Implementation checklist (optional):**

- [ ] Add dashboard client as a Node/pnpm resource in AppHost for the desired topology (e.g. Microservices, and optionally DistributedApp).
- [ ] Set working directory to `client/apps/dashboard`, script to `dev`, and HTTP endpoint with port/env.
- [ ] Pass gateway URL into the dashboard via `VITE_API_BASE_URL` using a reference to the gateway’s endpoint.
- [ ] Document in this doc or in AppHost README how to start the full stack (including dashboard) via Aspire and how to open the dashboard and `/test-runtime` in the browser.

---

## Runtime testing flow — goal and scope

**Goal:** One dashboard page that walks through the full lifecycle (tenant → app definition → release → install → tenant release → approve → environment → deploy) with one **expandable card per step**. Each card has inputs (and/or prefilled values from previous steps), an “Execute” button, and a response area. After the flow is done, the user can open the runtime URL and verify the app (including HTML initial view and JSON fallback).

**Scope:** Manual testing only. No automated E2E in this plan. Authentication is assumed (user already logged in; auth inherited like Bruno).

---

## Steps to cover

| Step | Card title | API | Needs from previous | Returns (for next steps) |
|------|------------|-----|---------------------|---------------------------|
| 1 | Create tenant | `POST /api/tenant/with-users` | — | `tenantId`, `slug` (and tenant payload) |
| 2 | Create application definition | `POST /api/appbuilder/application-definitions` | — | `applicationDefinitionId` or `id` |
| 3 | Create (platform) release | `POST /api/appbuilder/applications/{applicationDefinitionId}/releases` | Step 2: app def id | `releaseId` (platform release id) |
| 4 | Install tenant application | `POST /api/tenantapplication/tenants/{tenantId}/applications/install` | Step 1: `tenantId`; Step 3: `releaseId` | `tenantApplicationId` or `id` |
| 5 | Create tenant application release | `POST .../tenants/{tenantId}/applications/{tenantApplicationId}/releases` | Step 1: `tenantId`; Step 4: `tenantApplicationId` | `tenantReleaseId` (and optionally version) |
| 6 | Approve tenant release | `POST .../releases/{tenantReleaseId}/approve` | Step 1, 4, 5: tenant + app + release ids | — |
| 7 | Create environment | `POST .../applications/{tenantApplicationId}/environments` | Step 1, 4: tenant + app ids | `environmentId` |
| 8 | Deploy | `POST .../environments/{environmentId}/deploy` | Step 1, 4, 5, 7: tenant, app, env, and **tenant release** id; body: `ReleaseId`, `Version` | — |
| 9 | Open runtime | — | Step 1: `tenantSlug`; Step 4: `appSlug`; Step 7: env name (e.g. "Development") | Display/link: `/{tenantSlug}/{appSlug}/{environment}` |

**Step 9** is informational: show the runtime URL and a link that opens the runtime app (same origin or configured runtime origin) so the user can manually verify the runtime (including HTML initial view and JSON fallback).

**Reference payloads (Bruno):**

- **Step 1:** `{ "Name": "...", "Slug": "...", "Users": [{ "Email", "DisplayName", "Password", "IsTenantOwner": true }] }`
- **Step 2:** `{ "name", "description", "slug", "ispublic" }`
- **Step 3:** `{}`
- **Step 4:** `{ "ApplicationReleaseId": "<platform release id>", "Name": "...", "Slug": "..." }`
- **Step 5:** `{}` (or minimal body per API)
- **Step 6:** `{}`
- **Step 7:** `{ "Name": "Development" }`
- **Step 8:** `{ "ReleaseId": "<tenant release id>", "Version": "0.0.0" }`

---

## Page structure and UI

- **Route:** `/test-runtime` (e.g. `src/routes/test-runtime/+page.svelte`). Alternative: `src/routes/testing/flow/+page.svelte` for URL `/testing/flow`.
- **Single page** containing:
  - Page title and short description.
  - One **expandable/collapsible card** per step (e.g. “Step 1 – Create tenant”, “Step 2 – Create application definition”, … “Step 9 – Open runtime”).
- **Per card (Steps 1–8):**
  - **Title:** “Step N – [action]”.
  - **Expand/collapse:** Only one card expanded at a time (or allow multiple; product choice).
  - **Inputs:**  
    - Where the API expects path parameters (e.g. `tenantId`, `applicationDefinitionId`), provide **input fields** (prefilled from “Last response” when possible).  
    - Where the API expects a body, provide a **text area** (JSON) and/or structured fields (e.g. name, slug, environment name).  
  - **Execute button:** Calls the API with current inputs and shows loading state.
  - **Response area:** Read-only text area (or formatted JSON) showing the last response (and optionally status code). Store this in page state so the next step can use it.
- **Step 9 card:** No execute; show computed runtime URL and an “Open runtime” link (and optionally tenant slug / app slug / environment as copyable text).

---

## State and data flow

- **Page state:** Keep the last **response (and status)** per step (e.g. `step1Response`, `step2Response`, …) so:
  - The UI can parse IDs from the response and **prefill** the next step’s path/body inputs (e.g. `tenantId`, `applicationDefinitionId`, `releaseId`, `tenantApplicationId`, `tenantReleaseId`, `environmentId`).
- **Optional:** A single “flow state” object that each step reads/writes (e.g. `tenantId`, `applicationDefinitionId`, `platformReleaseId`, `tenantApplicationId`, `tenantReleaseId`, `environmentId`, `tenantSlug`, `appSlug`, `environmentName`) so step N can always derive its inputs from previous steps and you don’t duplicate parsing logic.
- **Parsing:** Either parse IDs from the stored JSON (e.g. `response.id`, `response.tenantId`) in the page or in a small helper; keep it in one place so backend response shape changes are easy to handle.

---

## API client and configuration

- **Add** `src/lib/shared/apiClient.ts` in the dashboard (same interface as runtime: `getApiBaseUrl()`, `apiGet<T>(path)`, `apiPost<T, B>(path, body)`), reading `VITE_API_BASE_URL`.
- Use this client for all step requests. If the gateway uses the same auth as Bruno (e.g. cookie), use `credentials: 'include'` (or the project’s standard) so the testing flow works when the user is already logged in.
- **Config:** Add or document `.env.development` (and `.env.example`) with `VITE_API_BASE_URL` pointing at the same gateway as runtime (e.g. `https://localhost:8443`).

---

## Design and UX alignment

- Use **design tokens** for spacing, colors, borders, radius (e.g. cards, buttons, inputs, response area): `--datarizen-spacing-*`, `--datarizen-primary`, `--datarizen-border`, `--datarizen-radius-*`, etc.
- Use semantic HTML and clear labels; keep the same font and layout style as the rest of the dashboard (and runtime).
- Optional: “Copy ID” buttons next to each important ID in the response or in the flow state to paste into another step or external tool.
- **Errors:** On non-2xx, show status and response body in the response area and do not overwrite “last successful” flow state so the user can correct and retry.
- **Deploy body:** Confirm with backend/Bruno whether deploy expects the **tenant release id** (from step 5) as `ReleaseId` and what `Version` format is; document in the card or placeholder.

---

## Implementation checklist

1. **Dashboard shared API client**
   - [x] Create `client/apps/dashboard/src/lib/shared/apiClient.ts` with `getApiBaseUrl()`, `apiGet`, `apiPost` (mirror runtime; includes `credentials: 'include'`).
   - [x] Add `.env.development` and `.env.example` with `VITE_API_BASE_URL`.

2. **Route and page**
   - [x] Create `client/apps/dashboard/src/routes/test-runtime/+page.svelte`.
   - [x] Implement page title and description.

3. **Step cards (1–8)**
   - [x] Implement expandable/collapsible card component or inline markup per step.
   - [x] For each step: title, inputs (path params + body), Execute button, response area.
   - [x] Wire Execute to API client using path and body from inputs.
   - [x] Store last response (and status) per step in page state.
   - [x] Prefill next step’s inputs from previous step’s response (by parsing IDs / flow state).

4. **Step 9 — Open runtime**
   - [x] Compute runtime URL from flow state (tenant slug, app slug, environment name).
   - [x] Display URL and “Open runtime” link (default http://localhost:5175; override via VITE_RUNTIME_BASE_URL).

5. **Flow state (optional)**
   - [x] Introduce a single flow-state object and parse all IDs into it from step responses so step N reads from it for path/body defaults.

6. **Styling and tokens**
   - [x] Apply design tokens to cards, buttons, inputs, response areas.
   - [x] Ensure layout and typography match dashboard and runtime (tokens, font).

7. **Documentation**
   - [x] Add dashboard README with run instructions and link to this doc; add "Test runtime flow" link on dashboard home to `/test-runtime`.

8. **Dashboard client via Aspire (optional)**
   - [x] Add builder, dashboard, and runtime client apps to AppHost for **Microservices** topology using `Aspire.Hosting.JavaScript` (`AddJavaScriptApp` + `WithPnpm()`); gateway has `WithHttpEndpoint`/`WithHttpsEndpoint`; each client gets `VITE_API_BASE_URL` from `gateway.GetEndpoint("https")`; clients listen on PORT (5173, 5174, 5175). See AppHost README.

---

## Out of scope

- Full dashboard module/feature folder structure (can be introduced later; this page can live under `routes`).
- Authentication UI (assume user is already authenticated; auth is inherited like Bruno).
- Automated E2E tests for this flow (manual testing only for now).
- Changing backend APIs or gateway routes; this plan assumes existing endpoints and payloads (as in Bruno).
