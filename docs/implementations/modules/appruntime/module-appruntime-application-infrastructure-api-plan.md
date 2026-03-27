## AppRuntime Module – Application, Infrastructure & API Plan

This file groups the non‑domain layers for AppRuntime:

- Application layer (commands/queries/services).
- Infrastructure (DbContext, configurations, repositories).
- API layer (controllers).

---

## 1. Application layer

### 1.1 Goals

- Implement **compatibility checks** consumed by TenantApplication and internal flows.
- Manage **lifecycle of runtime instances** (start/stop/fail/health).
- Expose **query APIs** to inspect runtime versions, instances, metrics, and logs.

---

### 1.2 DTOs

Namespace: `AppRuntime.Application.DTOs`

- `RuntimeVersionDto`
  - Id, Version, IsCurrent, ReleasedAt, ReleaseNotes, CreatedAt.
- `ComponentTypeSupportDto`
  - Id, RuntimeVersionId, ComponentType, ComponentVersion, LoaderClassName, IsEnabled, CreatedAt.
- `RuntimeInstanceDto`
  - Id, TenantId, TenantApplicationId, ApplicationReleaseId, RuntimeVersionId,
    Name, Status, HealthStatus, StartedAt, StoppedAt, LastHealthCheckAt, CreatedAt, UpdatedAt.
- `RuntimeConfigurationDto`
  - Id, Key, Value, DataType, Source, CreatedAt, UpdatedAt.
- `RuntimeMetricDto`
  - MetricType, MetricName, Value, Unit, Timestamp, Tags.
- `RuntimeLogDto`
  - Level, Message, Exception, Context, Timestamp.
- `ComponentCompatibilityDto`
  - ComponentType, ComponentVersion, IsSupported, ErrorMessage.
- `CompatibilityCheckResultDto`
  - IsCompatible, IEnumerable\<ComponentCompatibilityDto\>.

Mappers convert domain entities into DTOs for API responses and contracts.

---

### 1.3 Commands & queries

Namespace: `AppRuntime.Application`

#### 1.3.1 Runtime version management

- `CreateRuntimeVersionCommand(string Version, string ReleaseNotes) : ICommand<Guid>`
  - Handler:
    - Validate semantic version format.
    - Use `RuntimeVersion.Create(...)`.
    - Persist via `IRuntimeVersionRepository` + `IAppRuntimeUnitOfWork`.

- `GetRuntimeVersionByIdQuery(Guid Id) : IQuery<RuntimeVersionDto>`
- `GetCurrentRuntimeVersionQuery() : IQuery<RuntimeVersionDto>`
- `GetAllRuntimeVersionsQuery() : IQuery<IEnumerable<RuntimeVersionDto>>`

#### 1.3.2 Component type support

- `AddComponentTypeSupportCommand(Guid RuntimeVersionId, string ComponentType, string ComponentVersion, string LoaderClassName) : ICommand<Guid>`
  - Handler:
    - Validate runtime version exists.
    - Ensure no duplicate (RuntimeVersionId, ComponentType, ComponentVersion).

- `GetComponentTypeSupportByRuntimeVersionQuery(Guid RuntimeVersionId) : IQuery<IEnumerable<ComponentTypeSupportDto>>`

#### 1.3.3 Compatibility checking

Implementation of `ICompatibilityCheckService` (in `AppRuntime.Application.Services`) uses:

- `IRuntimeVersionRepository` + `IComponentTypeSupportRepository`.
- A contracts‑level service from AppBuilder (`IApplicationReleaseService`) to fetch required components (type + version) for a given `ApplicationReleaseId`.

Method:

```csharp
Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
    Guid applicationReleaseId,
    CancellationToken cancellationToken);
```

Flow:

1. Resolve the **current RuntimeVersion**.
2. Get all `ComponentTypeSupport` entries for that version.
3. Ask AppBuilder contracts which components (type + version) the release uses.
4. For each component, determine support (supported or not).
5. Return `CompatibilityCheckResultDto` with `IsCompatible` and detailed checks.

#### 1.3.4 Runtime instance lifecycle

- `CreateRuntimeInstanceCommand(Guid TenantId, Guid TenantApplicationId, Guid ApplicationReleaseId, string Name, string ConfigurationJson) : ICommand<Guid>`
  - Handler:
    1. Get current `RuntimeVersion`.
    2. Run compatibility check for `ApplicationReleaseId`.
    3. If not compatible → fail with validation error.
    4. Create `RuntimeInstance` with `CompatibilityCheckPassed = true` and `CompatibilityCheckDetails` from check result.
    5. Persist and return new `RuntimeInstanceId`.

- `StartRuntimeInstanceCommand(Guid RuntimeInstanceId) : ICommand<Unit>`
  - Handler:
    - Load instance, call `Start()`, persist.
    - Real process start (containers/hosts) is outside this module; this command records state only.

- `StopRuntimeInstanceCommand(Guid RuntimeInstanceId) : ICommand<Unit>`
- `MarkRuntimeInstanceFailedCommand(Guid RuntimeInstanceId, string Reason) : ICommand<Unit>`
- `UpdateRuntimeInstanceHealthCommand(Guid RuntimeInstanceId, HealthStatus HealthStatus) : ICommand<Unit>`

Queries:

- `GetRuntimeInstanceByIdQuery(Guid Id) : IQuery<RuntimeInstanceDto>`
- `GetRuntimeInstancesByTenantApplicationQuery(Guid TenantApplicationId) : IQuery<IEnumerable<RuntimeInstanceDto>>`
- `GetRuntimeInstancesByTenantQuery(Guid TenantId) : IQuery<IEnumerable<RuntimeInstanceDto>>`

#### 1.3.5 Configuration, metrics, logs

Commands/queries to inspect configuration and telemetry:

- `GetRuntimeConfigurationQuery(Guid RuntimeInstanceId) : IQuery<IEnumerable<RuntimeConfigurationDto>>`
- `GetRuntimeMetricsQuery(Guid RuntimeInstanceId, DateTime? From, DateTime? To) : IQuery<IEnumerable<RuntimeMetricDto>>`
- `GetRuntimeLogsQuery(Guid RuntimeInstanceId, DateTime? From, DateTime? To, LogLevel? MinLevel) : IQuery<IEnumerable<RuntimeLogDto>>`

Metrics/logs might be written by background processes or health probes; in the first iteration, you can stub write‑side and focus on read‑side.

---

## 2. Infrastructure layer

### 2.1 DbContext

**File**: `AppRuntime.Infrastructure/Data/AppRuntimeDbContext.cs`  
**Base**: `BaseModuleDbContext`  
**Schema**: `"appruntime"`

**DbSets**

- `DbSet<RuntimeVersion> RuntimeVersions`
- `DbSet<ComponentTypeSupport> ComponentTypeSupports`
- `DbSet<RuntimeInstance> RuntimeInstances`
- `DbSet<RuntimeConfiguration> RuntimeConfigurations`
- `DbSet<RuntimeMetrics> RuntimeMetrics`
- `DbSet<RuntimeLog> RuntimeLogs`

`OnModelCreating`:

- `SchemaName => "appruntime"`.
- `ApplyConfigurationsFromAssembly(...)`.

### 2.2 Entity configurations

Namespace: `AppRuntime.Infrastructure.Data.Configurations`

Configure:

- `RuntimeVersionConfiguration`
  - Table `appruntime.runtime_versions`
  - Columns for version, flags, notes, timestamps.
  - Unique index on `version`.
  - Index on `is_current`.

- `ComponentTypeSupportConfiguration`
  - Table `appruntime.component_type_support`
  - FK to `runtime_versions`.
  - Unique index `(runtime_version_id, component_type, component_version)`.

- `RuntimeInstanceConfiguration`
  - Table `appruntime.runtime_instances`
  - FKs to `tenant.tenants`, `tenantapplication.tenant_applications`, `appbuilder.application_releases`, `appruntime.runtime_versions`.
  - Indexes on `(tenant_id)`, `(tenant_application_id)`, `(application_release_id)`.

- `RuntimeConfigurationConfiguration`
  - Table `appruntime.runtime_configurations`
  - FK to `runtime_instances`.
  - Unique `(runtime_instance_id, key)` index.

- `RuntimeMetricsConfiguration`
  - Table `appruntime.runtime_metrics`
  - FK to `runtime_instances`.
  - Index `(runtime_instance_id, timestamp)`.

- `RuntimeLogConfiguration`
  - Table `appruntime.runtime_logs`
  - FK to `runtime_instances`.
  - Index `(runtime_instance_id, timestamp)`.

### 2.3 Repository implementations

Namespace: `AppRuntime.Infrastructure.Repositories`

- `RuntimeVersionRepository : Repository<RuntimeVersion, Guid>, IRuntimeVersionRepository`
- `ComponentTypeSupportRepository : Repository<ComponentTypeSupport, Guid>, IComponentTypeSupportRepository`
- `RuntimeInstanceRepository : Repository<RuntimeInstance, Guid>, IRuntimeInstanceRepository`
- `RuntimeConfigurationRepository : Repository<RuntimeConfiguration, Guid>, IRuntimeConfigurationRepository`
- `RuntimeMetricsRepository : Repository<RuntimeMetrics, Guid>, IRuntimeMetricsRepository`
- `RuntimeLogRepository : Repository<RuntimeLog, Guid>, IRuntimeLogRepository`
- `AppRuntimeUnitOfWork : IAppRuntimeUnitOfWork`

### 2.4 Infrastructure registration

**File**: `AppRuntime.Infrastructure/AppRuntimeInfrastructureServiceCollectionExtensions.cs`

Method:

```csharp
public static IServiceCollection AddAppRuntimeInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration,
    string schemaName)
```

- Configure `AppRuntimeDbContext` with connection string and schema.
- Register all repositories and `AppRuntimeUnitOfWork`.

---

## 3. API layer

### 3.1 Goals

- Expose a backend API for:
  - Compatibility checks (for TenantApplication).
  - Runtime version inspection.
  - Runtime instance lifecycle control (logical; not process orchestration).
  - Inspection of configuration, metrics, and logs.
- A **preview API** for draft execution will sit on top later but reuses same domain.

### 3.2 Controllers

Namespace: `AppRuntime.Api.Controllers`

#### 3.2.1 `RuntimeVersionController`

**Route base**: `api/appruntime/versions`

Endpoints:

- `GET /current` – returns current `RuntimeVersionDto`.
- `GET /` – returns all runtime versions.
- `GET /{id:guid}` – returns specific version.
- `POST /` – create new runtime version (internal/admin use).

#### 3.2.2 `CompatibilityController`

**Route base**: `api/appruntime/compatibility`

Endpoints:

- `GET /release/{applicationReleaseId:guid}`
  - Calls `ICompatibilityCheckService.CheckCompatibilityAsync`.
  - Returns `CompatibilityCheckResultDto`.

Used by TenantApplication when deploying to an environment.

#### 3.2.3 `RuntimeInstanceController`

**Route base**: `api/appruntime/instances`

Endpoints:

- `POST /`
  - Body: `CreateRuntimeInstanceCommand`.
- `POST /{id:guid}/start`
  - Body: none; command `StartRuntimeInstanceCommand`.
- `POST /{id:guid}/stop`
  - `StopRuntimeInstanceCommand`.
- `POST /{id:guid}/fail`
  - Body: reason; `MarkRuntimeInstanceFailedCommand`.
- `POST /{id:guid}/health`
  - Body: new health status; `UpdateRuntimeInstanceHealthCommand`.
- `GET /{id:guid}`
  - `GetRuntimeInstanceByIdQuery`.
- `GET /tenant/{tenantId:guid}`
  - `GetRuntimeInstancesByTenantQuery`.
- `GET /tenant-application/{tenantApplicationId:guid}`
  - `GetRuntimeInstancesByTenantApplicationQuery`.

#### 3.2.4 `RuntimeDiagnosticsController`

**Route base**: `api/appruntime/instances/{instanceId:guid}`

Endpoints:

- `GET /configuration` – `GetRuntimeConfigurationQuery`.
- `GET /metrics` – `GetRuntimeMetricsQuery` with optional time window.
- `GET /logs` – `GetRuntimeLogsQuery` with optional filters.

---

## 4. Migrations & module composition (summary)

Although this file focuses on non‑domain layers, you’ll also need:

- FluentMigrator migrations (similar structure to AppBuilder) under `AppRuntime.Migrations`:
  - Create `"appruntime"` schema.
  - Create tables: `runtime_versions`, `component_type_support`, `runtime_instances`,
    `runtime_configurations`, `runtime_metrics`, `runtime_logs`.
- Module project `AppRuntime.Module` with:
  - `AppRuntimeModule : IModule`:
    - `ModuleName => "AppRuntime"`, `SchemaName => "appruntime"`.
    - `GetMigrationDependencies()` depends on `TenantApplication` and `AppBuilder` (for FKs).
    - Registers Application + Infrastructure + controllers.
  - A host (e.g., `AppRuntimeServiceHost`) similar to AppBuilder’s service host.

---

## 5. Layer tasks

1. Implement DTOs and mappers for runtime versions, instances, compatibility, metrics, logs.
2. Implement MediatR commands/queries for:
   - Runtime version CRUD.
   - Component support definitions.
   - Compatibility check service.
   - Runtime instance lifecycle and diagnostics queries.
3. Implement `AppRuntimeDbContext`, configurations, repositories, and unit of work.
4. Implement DI registration in Infrastructure and Module projects.
5. Add API controllers and wire them to commands/queries with proper Result → HTTP mapping.
6. Add migrations for the `appruntime` schema and all runtime tables.

