# Dashboard

SaaS multi-tenant dashboard (billing, orgs, tenant settings, RBAC). Built with SvelteKit and Svelte 5.

## Run locally

```bash
pnpm install
pnpm run dev
```

Or from repo root: `pnpm run dev:dashboard`. The app typically runs at http://localhost:5174.

Set `VITE_API_BASE_URL` to the API gateway (e.g. `https://localhost:8443`) in `.env.development` or `.env` so API calls (including the runtime testing flow) work.

## Runtime testing flow

Open **[/test-runtime](/test-runtime)** to run the full lifecycle manually: create tenant, application definition, release, install tenant application, approve, create environment, deploy. Then use the "Open runtime" link to verify the runtime client (including HTML initial view and JSON fallback). See [Runtime Testing Flow Implementation Plan](../../../docs/implementations/client-dashboard/runtime-testing-flow-impl-plan.md).
