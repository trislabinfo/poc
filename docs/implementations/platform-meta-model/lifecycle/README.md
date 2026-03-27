# Application Lifecycle Schema

This folder contains the **application lifecycle** schema: release, catalog, tenant application, environment, and deployment. It is separate from the **application meta model** (which defines application definition *content*). Lifecycle artifacts validate against this schema; embedded snapshots (application definition content) validate against the application meta model.

## Root schema

- **`application-lifecycle.schema.json`** — Root. Optional top-level properties: `applicationRelease`, `applicationCatalog`, `tenantApplication`, `tenantApplicationRelease`, `environment`, `deploy`, `extensionRelease`, `extensionCatalog`. Use for validating lifecycle payloads.

## Definitions (`defs/`)

| File | Definition(s) | Purpose |
|------|---------------|---------|
| **ApplicationReleaseDefinition.json** | ResolvedExtensionVersion, ApplicationReleaseDefinition | Application release (metadata only): id, applicationId, version, validationStatus, resolvedExtensionVersions; includes AuditDefinition (createdBy, createdAt, updatedAt, updatedBy, deletedAt, deletedBy), optional domainModel (DDD version). Content stored as ApplicationReleaseArtifactDefinition. |
| **ApplicationReleaseArtifactDefinition.json** | ApplicationReleaseArtifactType, ApplicationReleaseArtifactDefinition | One row per piece of an app release: applicationReleaseId, artifactType (application, entity, page, navigation, workflow, role, permission, codeTable, theme, dataSource, breakpoint, extension), definitionId, extensionId?, snapshot. Stored in separate resource/table. |
| **ApplicationCatalogDefinition.json** | ApplicationCatalogDefinition | Application catalog item: id, applicationId, applicationReleaseId, name, description, tags, visibility. |
| **ExtensionReleaseDefinition.json** | ResolvedExtensionDependency, ExtensionReleaseDefinition | Extension release (metadata only): id, extensionId, version, validationStatus, resolvedDependencyVersions; includes AuditDefinition, optional domainModel. Content stored as ExtensionReleaseArtifactDefinition. |
| **ExtensionReleaseArtifactDefinition.json** | ExtensionReleaseArtifactType, ExtensionReleaseArtifactDefinition | One row per piece of an extension release: extensionReleaseId, artifactType (extension, entity, page, navigation, workflow, role, permission, codeTable, dataSource), definitionId, snapshot. Stored in separate resource/table. |
| **ExtensionCatalogDefinition.json** | ExtensionCatalogEntryDefinition | Extension catalog entry: id, extensionId, extensionReleaseId, name, description, tags, visibility. Entries offered for applications to reference (extensionReferences). |
| **TenantApplicationDefinition.json** | TenantApplicationDefinition | Tenant application: id, tenantId, applicationId, applicationReleaseId, catalogEntryId, source, status, overrides. |
| **TenantApplicationOverridesDefinition.json** | TenantApplicationOverridesDefinition | Tenant overrides: pages, navigation, permissions, translations. |
| **TenantApplicationReleaseDefinition.json** | TenantApplicationReleaseDefinition | Tenant app release (metadata only): id, tenantApplicationId, applicationReleaseId?, version, status; includes AuditDefinition, optional domainModel. Content stored as TenantApplicationReleaseArtifactDefinition (app + copied extension artifacts, overrides merged). |
| **TenantApplicationReleaseArtifactDefinition.json** | TenantApplicationReleaseArtifactType, TenantApplicationReleaseArtifactDefinition | One row per piece of effective tenant app release: tenantApplicationReleaseId, artifactType, definitionId, extensionId?, snapshot. Built by copying app + extension artifacts and merging tenant overrides. Stored in separate resource/table. |
| **EnvironmentDefinition.json** | EnvironmentDefinition | Environment: id, tenantApplicationId, name, type, config. |
| **DeployDefinition.json** | DeployDefinition | Deploy: id, tenantApplicationReleaseId, environmentId, status, deployedAt, artifactRef. |

## Process alignment

- **Application definition** (platform) → content in application meta model; may include **extensionDefinitions** (extensions authored in the app).
- **Application release** (platform) → ApplicationReleaseDefinition (metadata) + ApplicationReleaseArtifactDefinition rows (one per entity, page, navigation, workflow, role, permission, codeTable, theme, dataSource, breakpoint, extension; in-app extensions as many rows with extensionId set). Release not visible until artifacts complete; full snapshot assembled on demand.
- **Release extensions from application** → for each extension, create ExtensionReleaseDefinition + ExtensionReleaseArtifactDefinition rows; add to extension catalog.
- **Extension catalog** → extension catalog entries; applications consume via extensionReferences.
- **Tenant application** (install from catalog / from scratch / modify) → TenantApplicationDefinition (+ ApplicationCatalogDefinition; TenantApplicationOverridesDefinition).
- **Tenant application release** → TenantApplicationReleaseDefinition (metadata) + TenantApplicationReleaseArtifactDefinition rows. Build by copying app release artifacts and referenced extension release artifacts into tenant artifact store, then merging tenant overrides into snapshots (all content co-located for caching).
- **Create tenant application environment** → EnvironmentDefinition.
- **Tenant application deploy** → DeployDefinition.

Validate lifecycle JSON from the **`lifecycle`** directory so relative `$ref` paths resolve correctly.
