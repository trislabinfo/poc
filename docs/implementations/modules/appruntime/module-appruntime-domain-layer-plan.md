## AppRuntime Module – Domain Layer Implementation Plan

### 1. Domain goals

- Model the **runtime environment** that executes tenant applications:
  - Track **runtime versions** and their supported component types.
  - Manage **runtime instances** per tenant application and environment.
  - Provide configuration, metrics, and logs at runtime.
- Serve as the backend for:
  - Compatibility checks used by TenantApplication during deployment.
  - Actual execution flows (including future preview mode).

---

### 2. Aggregates and entities

#### 2.1 `RuntimeVersion` (aggregate root)

- **Purpose**: Represents a version of the AppRuntime engine and the capabilities it supports.
- **Namespace**: `AppRuntime.Domain.Entities`
- **Backed table**: `appruntime.runtime_versions`

**Core properties**

- `Id : Guid`.
- `Version : string` – semantic version (e.g., `1.0.0`, `2.0.0`).
- `IsCurrent : bool` – only one current version at a time.
- `ReleasedAt : DateTime`.
- `ReleaseNotes : string`.
- `CreatedAt : DateTime`.

**Key behaviors**

- `Create(version, releaseNotes, IDateTimeProvider)` – validates semver, sets `IsCurrent` appropriately, raises `RuntimeVersionCreatedEvent`.
- `MarkAsCurrent()` – sets current flag (and a domain service will ensure only one current version).

---

#### 2.2 `ComponentTypeSupport`

- **Purpose**: Declares which component types and versions a `RuntimeVersion` supports (e.g., `NavigationComponent v1`, `PageComponent v2`).
- **Namespace**: `AppRuntime.Domain.Entities`
- **Backed table**: `appruntime.component_type_support`

**Core properties**

- `Id : Guid`.
- `RuntimeVersionId : Guid` – FK to `RuntimeVersion`.
- `ComponentType : string` – e.g., `"NavigationComponent"`, `"PageComponent"`, `"FormComponent"`.
- `ComponentVersion : string` – logical component version (`"1.0"`, `"2.0"`).
- `LoaderClassName : string` – logical name or type identifier for the loader/renderer (used by DI/registry).
- `IsEnabled : bool`.
- `CreatedAt : DateTime`.

**Usage**

- Compatibility checks:
  - Compare the set of component types+versions required by an `ApplicationRelease` against the set defined here.
- Runtime loader registration:
  - Application layer / infrastructure use `LoaderClassName` to resolve proper handlers.

---

#### 2.3 `RuntimeInstance` (aggregate root)

- **Purpose**: Represents a **running** (or previously run) instance of a tenant application release in a given environment.
- **Namespace**: `AppRuntime.Domain.Entities`
- **Backed table**: `appruntime.runtime_instances`

**Core properties**

- `Id : Guid`.
- `TenantId : Guid` – FK to tenant in Tenant module.
- `TenantApplicationId : Guid` – FK to TenantApplication module.
- `ApplicationReleaseId : Guid` – FK to AppBuilder’s `application_releases`.
- `RuntimeVersionId : Guid` – FK to `RuntimeVersion`.
- `Name : string` – human‑readable identifier (e.g., `"acme-corp/crm/production"`).
- `Status : InstanceStatus` – `Starting`, `Running`, `Stopping`, `Stopped`, `Failed`.
- `Configuration : string` – JSON representing merged runtime configuration (tenant + environment + runtime).
- `CompatibilityCheckPassed : bool`.
- `CompatibilityCheckDetails : string` – JSON with per‑component compatibility results.
- `StartedAt : DateTime?`.
- `StoppedAt : DateTime?`.
- `LastHealthCheckAt : DateTime?`.
- `HealthStatus : HealthStatus` – `Healthy`, `Degraded`, `Unhealthy`.
- `CreatedAt : DateTime`.
- `UpdatedAt : DateTime`.

**Key behaviors**

- `Create(tenantId, tenantApplicationId, applicationReleaseId, runtimeVersionId, name, configuration, compatibilityCheck, IDateTimeProvider)`:
  - Initializes in `Stopped` state, stores compatibility results (must have passed).
  - Raises `RuntimeInstanceCreatedEvent`.
- `Start(IDateTimeProvider)`:
  - Sets status to `Starting` then `Running`, sets `StartedAt`, raises `RuntimeInstanceStartedEvent`.
- `Stop(IDateTimeProvider)`:
  - Sets status to `Stopping` then `Stopped`, sets `StoppedAt`, raises `RuntimeInstanceStoppedEvent`.
- `MarkFailed(reason, IDateTimeProvider)`:
  - Sets status to `Failed`, updates `UpdatedAt`, raises `RuntimeInstanceFailedEvent`.
- `UpdateHealth(healthStatus, IDateTimeProvider)`:
  - Updates `HealthStatus`, `LastHealthCheckAt`, raises `RuntimeInstanceHealthUpdatedEvent`.

---

#### 2.4 `RuntimeConfiguration`

- **Purpose**: Fine‑grained runtime configuration entries for a specific `RuntimeInstance` (merged from app defaults, tenant overrides, environment overrides).
- **Namespace**: `AppRuntime.Domain.Entities`
- **Backed table**: `appruntime.runtime_configurations`

**Core properties**

- `Id : Guid`.
- `RuntimeInstanceId : Guid` – FK to `RuntimeInstance`.
- `Key : string`.
- `Value : string`.
- `DataType : ConfigDataType` – `String`, `Number`, `Boolean`, `JSON`.
- `Source : ConfigSource` – `Default`, `Override`, `Environment`.
- `CreatedAt : DateTime`.
- `UpdatedAt : DateTime`.

**Usage**

- Allows introspection of the final configuration used at runtime.
- Useful for debugging and audits (“why did this instance behave this way?”).

---

#### 2.5 `RuntimeMetrics`

- **Purpose**: Stores periodic metrics for a runtime instance, if kept in the module’s DB (can also be externalized to an observability system).
- **Namespace**: `AppRuntime.Domain.Entities`
- **Backed table**: `appruntime.runtime_metrics`

**Core properties**

- `Id : Guid`.
- `RuntimeInstanceId : Guid`.
- `MetricType : MetricType` – `CPU`, `Memory`, `Requests`, `Errors`, `ResponseTime`, `Custom`.
- `MetricName : string`.
- `Value : decimal`.
- `Unit : string`.
- `Timestamp : DateTime`.
- `Tags : string` – JSON for labels (e.g., route, tenant, environment).

---

#### 2.6 `RuntimeLog`

- **Purpose**: Audit trail of significant runtime events for a `RuntimeInstance`.
- **Namespace**: `AppRuntime.Domain.Entities`
- **Backed table**: `appruntime.runtime_logs`

**Core properties**

- `Id : Guid`.
- `RuntimeInstanceId : Guid`.
- `Level : LogLevel` – `Debug`, `Info`, `Warning`, `Error`, `Critical`.
- `Message : string`.
- `Exception : string?`.
- `Context : string` – JSON with structured context (tenant, route, component, etc.).
- `Timestamp : DateTime`.

---

### 3. Enums

Namespace: `AppRuntime.Domain`

- `InstanceStatus` – `Starting`, `Running`, `Stopping`, `Stopped`, `Failed`.
- `HealthStatus` – `Healthy`, `Degraded`, `Unhealthy`.
- `ConfigSource` – `Default`, `Override`, `Environment`.
- `ConfigDataType` – `String`, `Number`, `Boolean`, `JSON`.
- `MetricType` – `CPU`, `Memory`, `Requests`, `Errors`, `ResponseTime`, `Custom`.
- `LogLevel` – `Debug`, `Info`, `Warning`, `Error`, `Critical`.

---

### 4. Domain services & contracts

#### 4.1 Compatibility checking (domain service)

Although implemented primarily at the Application layer, the domain entities support compatibility by:

- Storing **component support** in `ComponentTypeSupport`.
- Storing **compatibility results** in `RuntimeInstance` (`CompatibilityCheckPassed`, `CompatibilityCheckDetails`).

A domain service (used by `ICompatibilityCheckService` implementation) will:

- Accept:
  - `RuntimeVersion` and its `ComponentTypeSupport` entries.
  - A list of components (type + version) required by an `ApplicationRelease`.
- Compute:
  - `IsCompatible : bool`.
  - Per‑component results (supported/unsupported + messages).

#### 4.2 Contracts (AppRuntime.Contracts)

- `ICompatibilityCheckService`
  - `Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(Guid applicationReleaseId, CancellationToken)`.
- `CompatibilityCheckResultDto`
  - `IsCompatible : bool`.
  - `ComponentChecks : IReadOnlyList<ComponentCompatibilityDto>`.

These contracts are used by:

- **TenantApplication** – before deploying a release to an environment.
- **AppRuntime** – before creating a new `RuntimeInstance`.

---

### 5. Repository interfaces

Namespace: `AppRuntime.Domain.Repositories`

- `IRuntimeVersionRepository : IRepository<RuntimeVersion, Guid>`
  - `Task<RuntimeVersion?> GetByVersionAsync(string version, CancellationToken);`
  - `Task<RuntimeVersion?> GetCurrentAsync(CancellationToken);`
- `IComponentTypeSupportRepository : IRepository<ComponentTypeSupport, Guid>`
  - `Task<IEnumerable<ComponentTypeSupport>> GetByRuntimeVersionAsync(Guid runtimeVersionId, CancellationToken);`
- `IRuntimeInstanceRepository : IRepository<RuntimeInstance, Guid>`
  - `Task<IEnumerable<RuntimeInstance>> GetByTenantApplicationAsync(Guid tenantApplicationId, CancellationToken);`
  - `Task<IEnumerable<RuntimeInstance>> GetByTenantAsync(Guid tenantId, CancellationToken);`
- `IRuntimeConfigurationRepository : IRepository<RuntimeConfiguration, Guid>`
- `IRuntimeMetricsRepository : IRepository<RuntimeMetrics, Guid>`
- `IRuntimeLogRepository : IRepository<RuntimeLog, Guid>`
- `IAppRuntimeUnitOfWork : IUnitOfWork`

---

### 6. Domain layer tasks

1. Implement entities:
   - `RuntimeVersion`, `ComponentTypeSupport`, `RuntimeInstance`,
     `RuntimeConfiguration`, `RuntimeMetrics`, `RuntimeLog`.
2. Define enums and value types (`InstanceStatus`, `HealthStatus`, etc.).
3. Add domain events:
   - `RuntimeVersionCreatedEvent`, `RuntimeInstanceCreatedEvent`,
     `RuntimeInstanceStartedEvent`, `RuntimeInstanceStoppedEvent`,
     `RuntimeInstanceFailedEvent`, `RuntimeInstanceHealthUpdatedEvent`.
4. Implement repository interfaces and `IAppRuntimeUnitOfWork`.
5. Write unit tests for:
   - `RuntimeInstance` lifecycle (create/start/stop/fail/health).
   - `RuntimeVersion` rules (semver, single current version).
   - Compatibility result storage in `RuntimeInstance`.

