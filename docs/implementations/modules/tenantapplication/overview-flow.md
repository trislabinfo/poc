# Tenant Application — Flow: Implemented vs Missing

This document maps the intended tenant application flow (install/custom/fork → release → environments → migrations → schema compare → deploy with migrations) to what is **implemented** today and what is **missing**.

**Audience:** Product, architecture, and implementation planning.

---

## 1. Install from platform catalog, create custom application, or fork platform application

| Aspect | Status | Notes |
|--------|--------|--------|
| **Install from platform catalog** | **Implemented** | `POST .../applications/install` with `ApplicationReleaseId` (from AppBuilder catalog), name, slug, optional configuration. Creates a `TenantApplication` record that references the platform release (`IsCustom = false`, `Status = Installed`). Slug must be unique per tenant. |
| **Create custom application** | **Implemented** | `POST .../applications/custom` with name, slug, description. Creates a `TenantApplication` with `IsCustom = true`, `Status = Draft`. No platform or source release. |
| **Fork platform application** | **Implemented** | `POST .../applications/fork` with `SourceApplicationReleaseId`, name, slug. Creates a `TenantApplication` with `IsCustom = true`, `Status = Draft` and stores the source platform release ID. Definitions are read from the tenant’s definition tables when creating a release (via `ITenantDefinitionSnapshotReader`). |

**Gap:** Install and fork do **not** validate the platform release ID with the AppBuilder service in microservice topology; invalid IDs can be stored.

---

## 2. Release the tenant application (like platform application release)

| Aspect | Status | Notes |
|--------|--------|--------|
| **Create a release for a tenant application** | **Implemented** | `POST .../applications/{tenantApplicationId}/releases` with major, minor, patch, release notes. The system loads the tenant application and calls `ITenantDefinitionSnapshotReader.GetSnapshotAsync(tenantApplicationId)` to get the current definitions (navigation, pages, data sources, entities with properties and relations) from the tenantapplication schema. These are serialized to JSON and stored in an `ApplicationRelease` record (version, release notes, nav/page/datasource/entity JSON). The release is saved in `tenant_application_releases`; the tenant application’s “current release” info is updated. |
| **List / get releases** | **Implemented** | `GET .../applications/{id}/releases` and `GET .../applications/{id}/releases/{releaseId}`. |

**Note:** The snapshot reader reads from the **tenantapplication** DbContext (entity, property, relation, navigation, page, datasource definition tables filtered by `ApplicationDefinitionId` = tenant application ID). For a forked app, those definitions must already exist in the tenant’s store (e.g. copied or synced from the platform when the app was forked or later). The release is a **definition snapshot** (JSON), not a SQL schema artifact.

---

## 3. Create an environment for the application — each environment is a new database

| Aspect | Status | Notes |
|--------|--------|--------|
| **Create an environment** | **Implemented** | `POST .../applications/{tenantApplicationId}/environments` with name and environment type (e.g. Development, Staging, Production). Creates a `TenantApplicationEnvironment` record (tenant application ID, name, type). One environment per type per app (validation in aggregate). Stored in `tenant_application_environments`. |
| **Each environment = a new database** | **Missing** | Today an environment is only a **logical** record (name, type, which release is deployed). There is **no** per-environment database: no connection string, database name, or provisioning. The whole Tenant Application module uses a single database (e.g. `ConnectionStrings:DefaultConnection`) and the `tenantapplication` schema. To support “each environment its a new database” you would need: (1) a way to store or resolve a connection string (or database name) per environment, (2) provisioning of a new database when an environment is created (or on first deploy), and (3) runtime and deploy/migration logic that operate against that environment’s database. |

**Summary:** Environment **entity** and API are implemented; **one database per environment** is not.

---

## 4. Create migrations for the released tenant application

| Aspect | Status | Notes |
|--------|--------|--------|
| **Create a migration record** | **Implemented** | `POST .../applications/{id}/environments/{envId}/migrations` with optional `FromReleaseId`, `ToReleaseId`, and optional `MigrationScriptJson`. Creates a `TenantApplicationMigration` with `Status = Pending`, stored in `tenant_application_migrations`. List and get migration by ID are also implemented. |
| **Migration script content** | **Partial** | You can pass `MigrationScriptJson` when creating a migration (free-form). There is **no** automatic generation of a migration script from “from release” vs “to release” (see point 5). |
| **Mark migration completed / failed** | **Missing (API)** | The domain entity has `MarkCompleted` and `MarkFailed`; there is **no** API or command to call them. So migrations stay “Pending” from the API’s point of view after creation. |

**Summary:** Creating and reading migration **records** is implemented; **generation** of migration script from release diff and **lifecycle** (complete/fail) are missing or not exposed.

---

## 5. Compare the current DB schema with the new release DB schema

| Aspect | Status | Notes |
|--------|--------|--------|
| **Schema comparison** | **Missing** | There is no service or API that compares “current” schema with “new release” schema. Releases store **definition JSON** (entities, properties, relations, etc.), not SQL DDL or a canonical schema model. So either: (a) **definition-level diff**: compare entity/property/relation definitions between two releases and produce a change set (e.g. new tables, new columns, dropped columns), or (b) **database-level diff**: compare the actual database schema of the target environment with the schema that the new release would require, and produce migration DDL. Neither (a) nor (b) is implemented. |
| **Producing migration script from diff** | **Missing** | Even if a diff existed, there is no step that turns that diff into executable migration script (e.g. SQL or idempotent scripts) and attaches it to a `TenantApplicationMigration` (e.g. into `MigrationScriptJson` or a dedicated script store). |

**Summary:** Full “compare current DB schema with new release schema and produce migration script” is **missing**. It would require a clear notion of “schema” (definition-derived or actual DB) and a comparison + script-generation pipeline.

---

## 6. Deploy the new release on the target environment by applying the migrations

| Aspect | Status | Notes |
|--------|--------|--------|
| **Deploy (point environment at a release)** | **Implemented** | `POST .../applications/{id}/environments/{envId}/deploy` with `ReleaseId` and `Version`. Validates that the environment and release belong to the same tenant application, then updates the environment’s `ApplicationReleaseId`, `ReleaseVersion`, `DeployedAt`, `DeployedBy`, and sets it active. So “deploy” = **assign which release the environment runs**; it does not touch any database or run any script. |
| **Applying migrations** | **Missing** | No component runs migration scripts against an environment’s database. There is no “execute migration” step that: (1) takes a pending `TenantApplicationMigration`, (2) resolves the target database for that environment (which would require point 3 — each environment = a database), (3) runs the migration script (e.g. from `MigrationScriptJson` or generated in point 5), (4) marks the migration completed or failed. So “deploy” does **not** apply migrations; it only updates the environment’s release pointer. |

**Summary:** **Deploy** in the sense of “this environment now uses this release” is implemented. **Deploy by applying migrations** (run scripts on the environment’s DB and then point the environment at the new release) is **not** implemented and depends on 3 (per-env database) and 5 (schema compare / script generation).

---

## Summary Table

| # | Capability | Implemented | Missing |
|---|------------|--------------|---------|
| 1 | Install from catalog / create custom / fork | ✅ API and domain for all three | Optional: validate platform release ID with AppBuilder |
| 2 | Release tenant application (snapshot definitions) | ✅ Create release from current definitions; list/get releases | — |
| 3 | Create environment | ✅ Create/list/get/update/delete environment | ❌ Each environment = a new database (no per-env DB or connection) |
| 4 | Create migrations for released app | ✅ Create/list/get migration record; optional script JSON | ❌ API to mark migration completed/failed; no auto script generation |
| 5 | Compare current DB schema with new release schema | — | ❌ No schema comparison; no script generation from diff |
| 6 | Deploy new release on target env by applying migrations | ✅ Deploy = set environment’s release (pointer only) | ❌ No execution of migration scripts; no per-env DB to apply to |

---

## Suggested implementation order (for missing parts)

1. **Per-environment database (point 3)**  
   Define how an environment gets a database (e.g. connection string or database name per environment, provisioning when creating environment or on first deploy). Without this, “apply migrations” has no target DB.

2. **Schema from release (and optional compare)**  
   Define “schema” for a release (e.g. derive from entity/property definitions to a table/column model, or store/emit DDL). Optionally implement **definition-level** compare between two releases (add/remove entities, properties) and produce a change set.

3. **Migration script generation (point 5)**  
   From the change set (or from “current DB” vs “new release schema”), generate migration script (e.g. SQL) and store it on the migration record (or linked artifact).

4. **Execute migration and lifecycle (points 4 and 6)**  
   Implement “run migration” (execute script against the environment’s database, then mark migration completed/failed) and expose it (API or background job). Optionally tie “deploy” to “run pending migrations then update environment’s release.”

5. **Platform release validation (point 1)**  
   When installing or forking, call AppBuilder to validate the platform release ID (and that it is installable) before saving.

---

## Implementation Plan

This section outlines the implementation plan for the missing capabilities described above.

### 1. Per-Environment Database Creation

**Requirements:**
- When a new environment is created, a new database must be created on the same or other DB server (for now, one DB server for all databases)
- Database naming convention: `{tenant-slug}-{tenantapplicationSlug}-{environmentName}`
- Store database name and connection string in `TenantApplicationEnvironment` entity

**Implementation Steps:**
1. Add `DatabaseName` and `ConnectionString` properties to `TenantApplicationEnvironment` domain entity
2. Create `IDatabaseProvisioner` interface in Infrastructure layer
3. Implement `DatabaseProvisioner` that:
   - Generates database name from tenant slug, application slug, and environment name
   - Creates database using EF Core migrations or raw SQL
   - Returns connection string for the new database
4. Update `CreateEnvironmentCommandHandler` to:
   - Resolve tenant slug from `TenantId` (via Tenant module query/repository)
   - Call `IDatabaseProvisioner.CreateDatabaseAsync()` with tenant slug, app slug, environment name
   - Store database name and connection string in the environment entity
5. Add migration to add `database_name` and `connection_string` columns to `tenant_application_environments` table

**Files to Create/Modify:**
- `TenantApplication.Domain/Entities/TenantApplicationEnvironment.cs` - Add properties
- `TenantApplication.Infrastructure/DatabaseProvisioning/IDatabaseProvisioner.cs` - Interface
- `TenantApplication.Infrastructure/DatabaseProvisioning/DatabaseProvisioner.cs` - Implementation
- `TenantApplication.Application/Commands/CreateEnvironment/CreateEnvironmentCommandHandler.cs` - Update handler
- Migration file for schema changes

---

### 2. Schema Derivation from Entity/Property/Relation Definitions

**Requirements:**
- When a release is created, derive a table/column model from entity/property/relation definitions
- Store the derived schema in the release (add `SchemaJson` field to `ApplicationRelease`)
- Same logic must be provided for both AppBuilder module and TenantApplication module

**Implementation Steps:**
1. Create schema model classes (e.g., `TableSchema`, `ColumnSchema`, `ForeignKeySchema`) in `ApplicationDefinition.Domain` or a shared location
2. Create `ISchemaDeriver` interface that converts entity/property/relation definitions to schema model
3. Implement `SchemaDeriver` that:
   - Reads entity definitions with properties and relations
   - Maps entities to tables, properties to columns
   - Handles data types, nullable, primary keys, foreign keys
   - Returns schema model as JSON-serializable object
4. Add `SchemaJson` property to `ApplicationRelease` domain entity
5. Update `CreateApplicationReleaseCommandHandler` (AppBuilder) to:
   - Call `ISchemaDeriver.DeriveSchemaAsync()` with entity/property/relation definitions
   - Serialize schema model to JSON
   - Pass `SchemaJson` to `ApplicationRelease.Create()`
6. Update `CreateReleaseCommandHandler` (TenantApplication) similarly
7. Add migration to add `schema_json` column to both `application_releases` and `tenant_application_releases` tables

**Files to Create/Modify:**
- `ApplicationDefinition.Domain/Schema/` - Schema model classes
- `ApplicationDefinition.Application/SchemaDerivation/ISchemaDeriver.cs` - Interface
- `ApplicationDefinition.Application/SchemaDerivation/SchemaDeriver.cs` - Implementation
- `ApplicationDefinition.Domain/Entities/ApplicationRelease.cs` - Add `SchemaJson` property
- `AppBuilder.Application/Commands/CreateApplicationRelease/CreateApplicationReleaseCommandHandler.cs` - Update handler
- `TenantApplication.Application/Commands/CreateRelease/CreateReleaseCommandHandler.cs` - Update handler (if exists)
- Migration files for schema changes

---

### 3. Migration Script Generation and Storage

**Requirements:**
- Migration scripts must be created between the target application release and the target application environment
- Migration scripts must be stored in the database
- User can review the migration scripts
- Make it possible to update them

**Implementation Steps:**
1. Create `ISchemaComparer` interface that compares two schema models and produces a change set
2. Implement `SchemaComparer` that:
   - Takes "from release schema" and "to release schema"
   - Detects: new tables, dropped tables, new columns, dropped columns, modified columns, new foreign keys, dropped foreign keys
   - Returns `SchemaChangeSet` with list of changes
3. Create `ISqlMigrationScriptGenerator` interface that converts change set to SQL migration script
4. Implement `SqlMigrationScriptGenerator` that:
   - Takes `SchemaChangeSet`
   - Generates idempotent SQL DDL statements (CREATE TABLE, ALTER TABLE, DROP TABLE, etc.)
   - Returns SQL script as string
5. Update `CreateMigrationCommandHandler` to:
   - Load "from release" and "to release" schemas from `SchemaJson`
   - Call `ISchemaComparer.CompareAsync()` to get change set
   - Call `ISqlMigrationScriptGenerator.GenerateAsync()` to get SQL script
   - Store SQL script in `TenantApplicationMigration.MigrationScriptJson`
   - If user provided custom script, allow override (store user's script)
6. Add `UpdateMigrationScriptCommand` and handler to allow users to update migration scripts
7. Add API endpoint `PUT .../applications/{id}/environments/{envId}/migrations/{migrationId}` to update script

**Files to Create/Modify:**
- `ApplicationDefinition.Application/SchemaComparison/ISchemaComparer.cs` - Interface
- `ApplicationDefinition.Application/SchemaComparison/SchemaComparer.cs` - Implementation
- `ApplicationDefinition.Application/SchemaComparison/SchemaChangeSet.cs` - Change set model
- `ApplicationDefinition.Application/MigrationScriptGeneration/ISqlMigrationScriptGenerator.cs` - Interface
- `ApplicationDefinition.Application/MigrationScriptGeneration/SqlMigrationScriptGenerator.cs` - Implementation
- `TenantApplication.Application/Commands/CreateMigration/CreateMigrationCommandHandler.cs` - Update handler
- `TenantApplication.Application/Commands/UpdateMigrationScript/UpdateMigrationScriptCommand.cs` - New command
- `TenantApplication.Application/Commands/UpdateMigrationScript/UpdateMigrationScriptCommandHandler.cs` - New handler
- `TenantApplication.Api/Controllers/TenantApplicationController.cs` - Add PUT endpoint

---

### 4. Migration Execution (Long-Running Task Interface)

**Requirements:**
- "Run migration" must be implemented as a long-running task
- For now, make it only behind an interface to not waste time with background job implementation
- See `server\src\Capabilities\BackgroundJobs` for background job patterns

**Implementation Steps:**
1. Create `IMigrationExecutor` interface in `TenantApplication.Application`:
   - `Task<Result> ExecuteMigrationAsync(Guid migrationId, CancellationToken cancellationToken)`
   - Method should be async and return Result for success/failure
2. Implement `MigrationExecutor` that:
   - Loads `TenantApplicationMigration` by ID
   - Validates migration is in `Pending` status
   - Resolves environment's connection string
   - Executes SQL script from `MigrationScriptJson` against environment's database
   - Marks migration as `Completed` on success or `Failed` on error
   - Handles transaction and error logging
3. Create `RunMigrationCommand` and handler that:
   - Validates migration exists and is pending
   - Calls `IMigrationExecutor.ExecuteMigrationAsync()`
   - Returns result
4. Add API endpoint `POST .../applications/{id}/environments/{envId}/migrations/{migrationId}/run`
5. **Future:** The handler can be adapted to enqueue via `IBackgroundJobScheduler` when background job infrastructure is ready

**Files to Create/Modify:**
- `TenantApplication.Application/MigrationExecution/IMigrationExecutor.cs` - Interface
- `TenantApplication.Application/MigrationExecution/MigrationExecutor.cs` - Implementation
- `TenantApplication.Application/Commands/RunMigration/RunMigrationCommand.cs` - New command
- `TenantApplication.Application/Commands/RunMigration/RunMigrationCommandHandler.cs` - New handler
- `TenantApplication.Api/Controllers/TenantApplicationController.cs` - Add POST endpoint

---

### Implementation Order

1. **Schema Derivation (Point 2)** - Foundation for migration generation
2. **Per-Environment Database (Point 1)** - Required for migration execution
3. **Migration Script Generation (Point 3)** - Depends on schema derivation
4. **Migration Execution (Point 4)** - Depends on database provisioning and script generation

---

### Notes

- Database provisioning assumes single DB server for now; can be extended later for multi-server support
- Schema derivation logic is shared between AppBuilder and TenantApplication modules
- Migration scripts are stored as JSON strings in the database; consider moving to a dedicated table if scripts become large
- Migration execution is synchronous for now; interface allows easy transition to background jobs later
