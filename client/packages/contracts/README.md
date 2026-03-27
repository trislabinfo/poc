# @datarizen/contracts

Shared TypeScript types and contracts used by **Builder** and **Runtime** clients. Keeps application definition shape and runtime rendering in sync.

## Contents

- **ResolvedApplication** — shape returned by Runtime BFF resolve (applicationReleaseId, tenant, environment config).
- **ApplicationSnapshot** — navigation, pages, dataSources, entities; optional `schemaVersion` for compatibility.
- **CompatibilityResult** — result of compatibility check (isCompatible, optional hints).
- **NavigationNode**, **PageDefinition**, **ComponentDefinition**, **DataSourceDefinition**, **EntityDefinition** — structure types.

## Versioning

- Package uses **semver**. Breaking changes to exported types require a major bump.
- **Schema version** (in snapshot/compatibility) is separate: it denotes the definition schema version for adapters; see [Compatibility and Versioning Framework](../../docs/implementations/client/compatibility-and-versioning-framework.md).

## Build

```bash
pnpm install
pnpm run build
```

Output is in `dist/`. Consumers should depend on `@datarizen/contracts` as a workspace dependency and build it before building clients.
