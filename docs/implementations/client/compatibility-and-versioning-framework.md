# Compatibility and Versioning Framework

**Document Purpose:** One general, reusable pattern for compatibility and versioning across **all** current and future no-code platform features (datasources, workflows, entity validation rules, navigation, pages, and any new engines). Ensures the runtime client UI, Runtime API, and application definitions stay aligned without reinventing the wheel per feature type. Enterprise-ready and contract-first.

**Audience:** Architects, backend and frontend developers.

**Status:** Planning

**Related:** [Runtime Server Implementation Plan](runtime-server-impl-plan.md), [Runtime Client Implementation Plan](runtime-client-impl-plan.md), [Solution Structure - Runtime](../../ai-context/02-SOLUTION-STRUCTURE.md)

---

## Table of Contents

1. [Overview](#overview)
2. [General pattern: definition + engine + client](#general-pattern-definition--engine--client)
3. [Schema version + support matrix](#schema-version--support-matrix)
4. [Single compatibility check](#single-compatibility-check)
5. [Contract-first and compatibility API](#contract-first-and-compatibility-api)
6. [Adding a new feature type](#adding-a-new-feature-type)
7. [Backward compatibility policy](#backward-compatibility-policy)
8. [References](#references)

---

## Overview

Every feature type on the platform (datasource, workflow, entity validation rules, navigation, page, and future engines) follows the **same** compatibility and versioning pattern. This avoids one-off logic per feature and keeps the runtime client UI, Runtime API execution, and application definitions in sync across versions (v1, v2, v3, …).

**Principles:**

- **One pattern** for all feature types: definition schema version + engine support matrix + client adapters.
- **One compatibility check** for the whole application release: no per-feature compatibility endpoints or ad-hoc rules.
- **Contract-first:** Shared definition schemas and compatibility contract; Builder, Runtime API, and runtime client consume the same contracts and version them together.
- **Enterprise-ready:** Explicit support matrix, documented backward-compatibility policy, and clear upgrade paths.

---

## General pattern: definition + engine + client

For **every** feature type (datasource, workflow, validation rules, navigation, page, etc.):

| Layer | Responsibility | Versioning |
|-------|----------------|------------|
| **Definition** (AppBuilder / TenantApplication) | Author and store the “what” (e.g. datasource config, workflow definition, validation rules). | **Definition schema version** per feature type (or shared snapshot schema version). |
| **Engine** (Runtime API / AppRuntime) | Execute the “what” (e.g. run datasource, run workflow, evaluate validation). | **Engine version** with a list of **supported definition schema versions**. |
| **Client** (runtime client, and Builder where applicable) | Render and interact (UI, run triggers, show errors). | **Adapter or renderer per definition schema version** (or one renderer that handles multiple versions). |

**Compatibility rule (same for all feature types):** For every feature instance in an application release, the **current runtime** must have an **engine version** that supports that instance’s **definition schema version**, and the **runtime client** must have a **renderer or adapter** for that schema version. If either is missing, the release is **incompatible** (clear error, no silent break).

---

## Schema version + support matrix

### Definition schema version (per feature type)

- Each feature type has a **schema** (e.g. JSON schema or TypeScript/contract types): datasource schema, workflow schema, validation-rules schema, etc.
- The schema is **versioned** (e.g. `datasourceSchemaVersion: 1`, `workflowSchemaVersion: 1`, `validationRulesSchemaVersion: 1`). Use integers (1, 2, 3) or semver; integers are sufficient for “v1, v2, v3.”
- Every **application release snapshot** stores, for each feature instance (or per section), the **schema version** that applies:
  - **Option A:** One **snapshot schema version** for the whole snapshot (simpler; any change to any definition shape bumps it).
  - **Option B (recommended):** **Per–feature-type schema versions** (e.g. `datasourceSchemaVersion`, `workflowSchemaVersion`) so datasource can evolve independently of workflow.
- New capabilities = new schema version; **existing releases keep their stored version**. Definitions are immutable per release.

### Engine support matrix (Runtime API)

- Each **engine** (DataSourceEngine, WorkflowEngine, ValidationRulesEngine, NavigationEngine, PageEngine, etc.) has **engine versions** (e.g. 1.0, 2.0).
- Each engine version declares: **supported definition schema versions** for its feature type (e.g. DataSourceEngine 2.0 supports `[1, 2]`).
- The **compatibility check** (see [Single compatibility check](#single-compatibility-check)) uses this matrix for **all** feature types: for each instance in the snapshot, it checks that the current RuntimeVersion has an engine that supports that instance’s (feature type, schema version). No separate logic per feature.

### Client adapters

- The runtime client receives **schema version(s)** in the snapshot or in the compatibility response.
- For each feature type, the client has **adapters or renderers per schema version** (or a single renderer that branches on version).
- **Correct UI** (e.g. workflow UI, datasource config, validation messages) is guaranteed by: **same schema version → same adapter**. No ad-hoc branching per feature beyond “which adapter for this version.”

---

## Single compatibility check

- **One** compatibility operation applies to the **entire** application release and all feature types.
- **Input:** `applicationReleaseId`, optional `runtimeVersionId`, optional `clientCapabilities` (e.g. list of supported schema versions per feature type the client can render).
- **Output:** `isCompatible`, and optionally **per–feature-type or per-instance** details: `supportedSchemaVersions`, `unsupportedInstances` (e.g. “workflow id X uses schema 3, not supported”), and upgrade hints.
- **Algorithm (same for all feature types):**
  1. Load the release snapshot (and any metadata with schema versions).
  2. For each feature instance in the snapshot: (feature type, definition schema version).
  3. For that feature type, check the current RuntimeVersion’s support matrix: “Is there an engine that supports this schema version?”
  4. If **clientCapabilities** are provided, optionally check that the client can render each instance’s schema version.
  5. If any instance is unsupported by runtime (or by client when checked) → **incompatible**.
- **Execution** is version-agnostic at the API surface: the client sends “execute this (release, feature type, instance id).” The Runtime API loads the definition from the snapshot, reads its schema version, selects the correct engine version, and runs it. Same flow for datasource, workflow, validation, etc.

**Implementation note (current state):** The compatibility checker in AppRuntime is currently a **stub** (in-memory; returns compatible by default). A full implementation will use the **support matrix** (engine version ↔ supported definition schema versions) and optionally **runtime version**; that data may be stored in **DB** (e.g. in the `appruntime` schema) or in **config/code** — TBD. AppRuntime already has an **AppRuntime.Migrations** project; any support-matrix or runtime-version tables would live there.

---

## Contract-first and compatibility API

- **Definition schemas** (datasource, workflow, validation rules, etc.) live in a shared place (e.g. `packages/contracts` or OpenAPI + JSON Schema). Builder, Runtime API, and runtime client consume the same contracts.
- **Compatibility** request/response and **execution APIs** (e.g. execute datasource, run workflow, evaluate validation) are specified in the same contract set. Version the contracts (e.g. semver); document “Runtime 2.x supports definition schema versions 1 and 2 for datasource, workflow, validation.”
- **Build and CI:** Validate that Runtime API, BFF, and runtime client use the same contract versions where applicable; avoid runtime drift.

---

## Adding a new feature type

To add a new feature (e.g. entity validation rules) **without** reinventing compatibility:

1. **Define the definition schema** and set the initial schema version (e.g. `validationRulesSchemaVersion: 1`).
2. **AppBuilder / TenantApplication:** Add persistence and include this definition type in the application release snapshot (same pattern as datasource or workflow).
3. **Runtime API (AppRuntime):** Add the new engine (e.g. ValidationRulesEngine), register an engine version with `supportedDefinitionSchemaVersions: [1]`, and plug it into the **same** compatibility matrix and execution routing.
4. **Compatibility:** Extend the matrix to include the new feature type; the existing “for each instance, check engine support for its schema version” algorithm applies unchanged.
5. **Runtime client:** Add adapter(s) for the new feature type’s schema version(s) and use the schema version from the snapshot or compatibility response to select the correct UI.

No new compatibility pattern is required—only a new schema, new engine, and new client adapter, all following this framework.

---

## Backward compatibility policy

Apply one policy across the platform:

- **Engines:** New engine versions **should** support at least the previous definition schema version(s) (backward compatible). Dropping support is a **major** change and must be explicitly documented and communicated.
- **Runtime client:** New client versions typically support **old and new** schema versions (adapters for v1, v2). If an old client loads an app that uses a newer schema version, show a clear “upgrade client” or “unsupported” message instead of rendering incorrectly.
- **Contracts:** Contract versioning (e.g. semver in `packages/contracts`) is documented; “Runtime 2.x supports snapshot/definition schema 1.x and 2.x” is stated in release notes and in the compatibility API response when applicable.

---

## References

- [Runtime Server Implementation Plan](runtime-server-impl-plan.md) — BFF and Runtime API; compatibility and execution endpoints.
- [Runtime Client Implementation Plan](runtime-client-impl-plan.md) — How the client uses compatibility and schema versions; adapters.
- [Solution Structure - Runtime](../../ai-context/02-SOLUTION-STRUCTURE.md) — Runtime app and backend dependencies.
- [AppRuntime Architecture](../latest-plan/appruntime-architecture.md) — Resolution flow, component loading.
- [AppRuntime Contracts](../latest-plan/appruntime-contracts.md) — ICompatibilityCheckService, RuntimeVersionDto, support matrix concepts.
