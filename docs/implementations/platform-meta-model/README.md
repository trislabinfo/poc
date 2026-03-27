# Platform Meta-Model for No-Code Enterprise Application Definition

This document describes the **platform meta-model** used by Datarizen for defining enterprise-grade applications in a no-code way. The meta-model is the formal schema that governs what can be expressed in an **Application Definition** (Authoring JSON) and how it is validated, compiled, and deployed.

## Purpose

- **Meta-model**: Defines the rules, types, and structure of application definitions (the “schema of the schema”).
- **Application model**: A concrete application definition conforming to the meta-model (entities, pages, workflows, navigation, etc.).
- **Runtime**: Compiled artifacts and deployments derived from validated application definitions.

The meta-model enables:
- Consistent authoring (builder UI and AI agents produce valid definitions).
- Validation and tooling (JSON Schema, IDE support, API contracts).
- Clear lifecycle: Authoring JSON → Release Snapshot → Compiled Artifact → Environment Deployment.

### Schema structure

The schema is split into multiple files by domain. The root schema is `platform-meta-model.schema.json` (or `application-meta-model.schema.json`); it references definitions in the `defs/` folder. Each file is named after the definition it contains (e.g. `ValidationRuleDefinition.json`, `EntityDefinition.json`). Shared definitions used across domains (e.g. `ValidationRuleDefinition`) live in their own file. Extension definitions are nested in the application (`extensionDefinitions`) and validate against `defs/ExtensionDefinition.json`. Validate application JSON from the `application` directory so relative `$ref` paths resolve correctly. **Clickable refs:** In VS Code/Cursor, use **Ctrl+Click** (Mac: **Cmd+Click**) on a `$ref` path to jump to the definition; for reliable navigation, install the [JSON Reference Navigator](https://marketplace.visualstudio.com/items?itemName=matsoob.jsonrefnavigator) extension.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Platform Meta-Model (this document + platform-meta-model.schema.json)   │
│  Defines: entities, properties, relations, pages, navigation, workflows, │
│           roles, permissions, persistence, themes, component registry   │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  Application Definition (Authoring JSON)                                 │
│  Editable definition created in the no-code builder; must conform to    │
│  the platform meta-model.                                                │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  Release Snapshot (artifact-based)                                       │
│  ApplicationReleaseDefinition (metadata) + ApplicationReleaseArtifact   │
│  Definition rows (one per entity, page, navigation, etc.). Full snapshot  │
│  assembled on demand. Used for deployment and tenant installation.        │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  Compiled Runtime Artifact                                               │
│  Optimized render trees, workflows, navigation, DB migrations.          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Meta-Model Components

### 1. Application Metadata

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier for the app |
| `name` | string | Human-readable name |
| `version` | string | Semantic version (e.g. `"1.0.0"`) |
| `multiTenant` | boolean | Whether the app supports multi-tenancy |
| `defaultTenantId` | string (optional) | Default tenant ID if single-tenant |
| `createdBy` | string | User ID who created the app |
| `createdAt` | string (datetime) | Timestamp |

### 2. Entity Definition

Entities are the core business data structures.

- **id** (string): Unique entity identifier (e.g. `"Customer"`, `"Order"`).
- **displayName** (string): Human-readable name.
- **tenantScoped** (boolean): Whether the entity is tenant-specific.
- **properties**: List of property definitions (name, type, required, readonly, default, length, precision, scale, expression, persist).
- **relations**: List of relation definitions (name, type, target, inverse, required, tenantScoped).
- **calculatedFields**: Optional list of calculated field definitions (name, type, expression, persist).

**Property types** include: `string`, `uuid`, `int`, `decimal`, `date`, `datetime`, `boolean`, `relation`, and others as defined in the schema.

**Relation types**: `one-to-one`, `one-to-many`, `many-to-one`, `many-to-many`.

### 3. Persistence Definition

- **databaseProvider**: e.g. `"postgres"`.
- **namingStrategy**: e.g. `"snake_case"`.
- **entities**: Per-entity persistence (tableName, indexes, property overrides).

### 4. Page Definition

- **id**, **type** (`EntityList` | `EntityEdit` | `Custom`), **entity** (optional).
- **layout**: Tree of layout nodes (`Section`, `Row`, `Tabs`, `Tab`, `Field`, `DataTable`) with `id`, `type`, `props`, `children`.
- **listConfig**: For list pages (columns, filterFields, quickSearchFields, defaultSort, pageSize).
- **fieldOverrides**: Per-field runtime overrides (readonly, visible, default expressions).
- **permissions**: Allowed roles or permission rules.

### 5. Navigation Definition

Hierarchical **NavigationItem**: `id`, `label`, `page` (page ID), `children`, optional `tenantId`.

### 6. Workflow Definition (BPMN-aligned)

- **id**, **entity**, optional **tenantId**.
- **startEvent**: e.g. `onCreate`, `onUpdate`, `onDelete`, `manual`, `timer`.
- **tasks**: e.g. `updateField`, `createEntity`, `sendNotification`, `customAction`.
- **sequenceFlows**: Order and conditional flows between tasks.

### 7. Roles and Permissions

- **Role**: `id`, `name`, optional `inherits[]`, `tenantScoped`.
- **Permission**: `resourceType` (`Entity` | `Page` | `Workflow`), `resourceId`, `actions` (`create` | `read` | `update` | `delete` | `execute`), `tenantScoped`.

### 8. Component Registry

Registered UI components: `id`, `category` (`field` | `layout` | `data`), `version`, optional `propsSchema`.

### 9. Themes

- **id**, **colors** (e.g. primary, secondary, background, text), **typography** (fontFamily, fontSize).

### 10. Application Extensions

**Authored in the application:** An application definition can contain one or more **extension definitions** in **extensionDefinitions** (array of **ExtensionDefinition**). Each extension is a reusable bundle of entities, pages, navigation, workflows, roles, permissions, code tables, data sources, and translations (same content types as the application, with required `id`, `name`, `version`). From the application you **release** these extensions and **add them to the extension catalog** so other applications can use them via **extensionReferences**.

**Consumed from the catalog:** Applications list extensions they use in **extensionReferences** (array of **ExtensionReference**: `extensionId`, `version`, optional `source`, optional **overrides**). Optional **overrides** let the application extend an extension: **entityPropertyAdditions** (add properties to extension entities, e.g. height and weight on Employee) and **pageOverrides** (fieldOverrides, listConfig per extension page). All override shapes are defined in the schema; no additionalProperties. At load/build time the platform resolves each reference, applies overrides, merges content with namespacing (extensionId.id), and produces the effective application definition.

**ExtensionDefinition** (optional fields): `dependsOn` (array of `{ extensionId, version }`) for cross-extension references; same content arrays as application (entities, pages, navigation, etc.). Extension authors use **local** ids; the platform prefixes all ids with `extensionId` when merging (e.g. entity `Hunter` from extension `employee` becomes `employee.Hunter`). Applications reference extension content by **namespaced** id (e.g. `employee.Hunter`).

**Process:** Create application definition → define one or more extensions in `extensionDefinitions` → release application (ApplicationReleaseDefinition + ApplicationReleaseArtifactDefinition rows, including in-app extension content) → release each extension (ExtensionReleaseDefinition + ExtensionReleaseArtifactDefinition, add to extension catalog) → other applications reference them via `extensionReferences`.

**Merge semantics and mitigations:** Namespacing (extensionId.id); no override of extension entities; cross-extension refs via dependsOn; merge by array; translations under `translations[extensionId]`; versioning and resolvedExtensionVersions in application release; data sources structure-only; security and discovery as for catalogs.

## Validation and Lifecycle

1. **Authoring**: The no-code builder (and AI agents) produce Application Definition JSON that must validate against the platform meta-model schema (`platform-meta-model.schema.json`).
2. **Release**: ApplicationReleaseDefinition (metadata) is created; content is stored as ApplicationReleaseArtifactDefinition rows in a separate resource/table (one row per entity, page, navigation, workflow, role, permission, codeTable, theme, dataSource, breakpoint, extension). Validation status (`Pending` | `Valid` | `Invalid`) applies to the release; full snapshot can be assembled on demand.
3. **Compilation**: Valid snapshots (assembled from artifacts when needed) are compiled into runtime artifacts (render trees, workflows, navigation, DDL).
4. **Deployment**: Tenant application releases copy app + referenced extension artifacts into TenantApplicationReleaseArtifactDefinition (overrides merged); environments (per tenant, per release) use the compiled artifact and run migrations.

## Related Documentation

- [Application Definition draft (part2)](../application-definition/part2/draft.txt) — Target shape and examples for the authoring JSON.
- [Application Definition implementation plan](../application-definition/part2/implementation-plan.md) — Refactoring steps toward this model.
- [AI Context overview](../../ai-context/00-OVERVIEW.md) — Datarizen platform overview and deployment models.

## Schema Files

**Application content (meta model):** The formal definition is in **`application-meta-model.schema.json`** (root) with defs in **`defs/`** including **`ExtensionDefinition.json`** and **`ExtensionReference.json`** for extensions (authored in `extensionDefinitions`, consumed via `extensionReferences`). Use for:

- Validating Application Definition JSON (root schema).
- Generating types (e.g. TypeScript, C# DTOs).
- IDE and API documentation.

**Common (shared definitions):** **`Common/`** holds definitions used across application and lifecycle: **`SemanticVersionDefinition.json`** (semantic version / version range), **`AuditDefinition.json`** (createdAt, createdBy, updatedAt, updatedBy, deletedAt, deletedBy), **`DomainModelDefinition.json`** (DDD optimistic concurrency version). Application and release definitions reference these via `allOf` or property refs.

**Lifecycle (release, catalog, tenant, environment, deploy, extension catalog):** A separate schema in **`lifecycle/application-lifecycle.schema.json`** defines application release, **application release artifacts** (separate resource), application catalog, extension release, **extension release artifacts** (separate resource), extension catalog, tenant application, tenant application release, **tenant application release artifacts** (separate resource), environment, and deployment. See **[lifecycle/README.md](lifecycle/README.md)** for the list of defs and process alignment. Release/tenant release definitions hold metadata only; content is in artifact definitions. Snapshot content (per artifact) conforms to the application meta model.
