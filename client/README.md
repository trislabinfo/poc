# Client (frontend)

All frontend code lives under this root, mirroring the **server** layout. Same best practice: one root per domain.

- **`apps/`** — Deployable applications (builder, dashboard, runtime). Each is a SvelteKit app.
- **`packages/`** — Shared packages used by the apps:
  - **contracts** — Shared types (ResolvedApplication, ApplicationSnapshot, CompatibilityResult, etc.). Builder and Runtime depend on it.
  - **design** — Design tokens and base styles.

**Prerequisites:** Node.js 18+ and npm or pnpm. From repo root, workspace is defined in `pnpm-workspace.yaml` (`client/apps/*`, `client/packages/*`).

---

## Build

Build **packages first** (contracts and design), then the apps. Apps depend on the built output of the packages.

### Option A: From repo root (pnpm)

```bash
# From repo root (c:\dr\poc9.1)
pnpm install
pnpm run build:packages          # builds @datarizen/contracts and @datarizen/design
pnpm run build                   # builds packages + all three apps
```

To build a single app only (after packages are built):

```bash
pnpm run build --filter builder
pnpm run build --filter dashboard
pnpm run build --filter runtime
```

### Option B: Using npm (per folder)

```bash
# 1. Build packages
cd client/packages/contracts
npm install && npm run build

cd ../design
npm install && npm run build

# 2. Build an app (e.g. runtime)
cd ../../apps/runtime
npm install && npx svelte-kit sync && npx vite build
```

Repeat step 2 for `apps/builder` and `apps/dashboard` if needed.

---

## Run (development server)

Run **one** app at a time (each uses its own port, e.g. 5173, 5174, 5175).

### Single app

**From repo root (pnpm):**

```bash
pnpm run dev:builder    # App Builder
pnpm run dev:dashboard  # Dashboard
pnpm run dev:runtime    # Runtime
```

**From app folder:**

```bash
cd client/apps/runtime
npm install && npx svelte-kit sync && npx vite dev
# Open http://localhost:5173 (or the port shown)
```

### Run all three apps in parallel

**Option 1: From repo root (recommended)**

One-time setup: install root devDependencies and each app’s dependencies (and build packages if you haven’t yet):

```bash
# From repo root
npm install
cd client/apps/builder   && npm install && cd ../..
cd client/apps/dashboard && npm install && cd ../..
cd client/apps/runtime   && npm install && cd ../..
```

Then run all three dev servers in parallel:

```bash
npm run dev:all
```

Builder, dashboard, and runtime each start on their own port (e.g. 5173, 5174, 5175). Stop with `Ctrl+C`.

**Option 2: Three terminals**

Open three terminals and run one app in each:

| Terminal | Command              | Typical URL            |
|----------|----------------------|------------------------|
| 1        | `pnpm run dev:builder`   | http://localhost:5173 |
| 2        | `pnpm run dev:dashboard` | http://localhost:5174 |
| 3        | `pnpm run dev:runtime`   | http://localhost:5175 |

Ports may differ if one is already in use; check the output in each terminal.

---

## Preview (production build locally)

After building an app:

```bash
cd client/apps/runtime
npx vite preview
# Open the URL shown (e.g. http://localhost:4173)
```

---

See [Runtime — Full Implementation Plan](../docs/implementations/client/runtime-all-impl-plan.md) for the full flow and BFF setup.
