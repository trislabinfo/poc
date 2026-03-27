# AllResources-20260225071340.txt – analysis and fixes

## 1. Monolith startup: Unable to resolve ICompatibilityCheckService (FIXED)

**Error:**  
`Unable to resolve service for type 'AppRuntime.Contracts.Services.ICompatibilityCheckService' while attempting to activate 'AppRuntime.BFF.Services.InProcessRuntimeApi'`  
at `Monolith.Host Program.cs` when building the service provider.

**Cause:** The Monolith calls `AddRuntimeBff()`, which registers `IRuntimeApi` → `InProcessRuntimeApi`. `InProcessRuntimeApi` depends on `ICompatibilityCheckService`, `IApplicationResolverService`, `IRequestDispatcher`, `IDatasourceExecutionService`. Those are registered by **AppRuntimeModule**, but the Monolith never called `AddModule<AppRuntimeModule>()`; it only called `UseModule<AppRuntimeModule>()` (middleware). So DI had no registration for `ICompatibilityCheckService`.

**Fix:** In `MonolithHost/Program.cs`, register the AppRuntime module before `AddRuntimeBff()`:

- `builder.Services.AddModule<AppRuntime.Module.AppRuntimeModule>(builder.Configuration);`

---

## 2. relation "tenant.tenants" does not exist

**Cause:** Tenant (and other) module migrations have not been applied to the database.

**Fix:** Run the MigrationRunner with the same connection string as the app (see `docs/implementations/log.txt`). The app now returns 503 with a clear message when this happens.

---

## 3. column "UpdatedAt" of relation "relation_definitions" / "tenant_relation_definitions" does not exist

**Cause:** Base `Entity<T>` has `UpdatedAt`; the `relation_definitions` and `tenant_relation_definitions` tables have no `updated_at` column.

**Status:** Both **RelationDefinitionConfiguration** (AppBuilder) and **TenantRelationDefinitionConfiguration** (TenantApplication) already call `builder.Ignore(e => e.UpdatedAt)`. If this error still appears, ensure the correct configuration assembly is applied and that no other code path maps `UpdatedAt` for these entities.

---

## 4. column "customer_id" referenced in foreign key constraint does not exist

**Cause:** DDL added a FK on `"Order"` referencing `"Customer"` with column `customer_id`, but the `"Order"` table had no `customer_id` column.

**Status:** `EfCoreSchemaDeriver` has `EnsureForeignKeyColumn` which adds the FK column to the source table in the schema before generating FKs. If the error persists, the DDL that applies the schema may need to emit `ALTER TABLE ... ADD COLUMN` for any such columns before adding the FK constraint (e.g. in the component that runs DDL for tenant/app environments).

---

## 5. database "dr-development" already exists (PostgreSQL ERROR)

**Cause:** Aspire or app code runs `CREATE DATABASE "dr-development"` on each startup; the database is persistent, so the second and subsequent runs fail.

**Fix:** Either make database creation idempotent (e.g. use `CREATE DATABASE ...` only if not exists, where supported), or treat the “already exists” error as success. This may be in Aspire’s Postgres resource or in custom DB setup code.

---

## 6. Other log entries (no code change)

- **GSSAPI security context:** PostgreSQL log when client doesn’t use GSSAPI; safe to ignore for local dev.
- **password authentication failed for user "datarizen":** Wrong connection string or credentials.
- **Could not create Endpoint object(s): service-producer annotation invalid:** Aspire/Kubernetes; port or annotation configuration.
- **RabbitMQ deprecated_features.permit.management_metrics_collection:** RabbitMQ warning; can be suppressed via config if desired.
