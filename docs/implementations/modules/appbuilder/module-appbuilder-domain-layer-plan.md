## AppBuilder Module – Domain Layer Implementation Plan

### 1. Domain goals

- **Configuration‑driven applications**: Represent applications as metadata plus an event‑sourced configuration model.
- **Versioned releases**: Model immutable released versions that can be installed by tenants and executed by AppRuntime.
- **Componentized UI & data**: Represent navigation, pages, and data sources in a way that’s easy to project for runtime.
- **Extensible & auditable**: Support new component types over time and keep a complete audit trail of changes.

---

### 2. Aggregates and entities

#### 2.1 `Application` (aggregate root)

- **Purpose**: Logical definition of an app being built in AppBuilder (draft / released / archived).
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.applications`

**Core properties**

- `Id : Guid` – identity.
- `Name : string` – human‑readable name.
- `Slug : string` – globally unique, lowercase kebab‑case identifier (used in URLs and contracts).
- `Description : string` – documentation text.
- `Status : ApplicationStatus` – `Draft`, `Released`, `Archived`.
- `CurrentReleaseId : Guid?` – FK to latest active `ApplicationRelease` (for catalog / install flows).
- `CreatedAt : DateTime`, `UpdatedAt : DateTime?` – audit fields.

**Key behaviors**

- `Create(name, slug, description, IDateTimeProvider)`  
  - Validates name and slug, initializes as `Draft`, raises `ApplicationCreatedEvent`.
- `Update(name, description, IDateTimeProvider)`  
  - Only allowed when `Status == Draft`, raises `ApplicationUpdatedEvent`.
- `Release(releaseId, IDateTimeProvider)`  
  - Only from `Draft`, moves to `Released`, sets `CurrentReleaseId`, raises `ApplicationReleasedEvent`.
- `CreateNewVersion(IDateTimeProvider)`  
  - Only from `Released`, clones metadata into a new `Draft` application, raises `ApplicationNewVersionCreatedEvent`.
- `Archive(IDateTimeProvider)`  
  - Moves to `Archived`, raises `ApplicationArchivedEvent`.

---

#### 2.2 `ApplicationEvent` (event‑sourced configuration stream)

- **Purpose**: Immutable event log that describes every change to an application’s configuration (navigation, pages, data sources, settings).
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.application_events`

**Core properties**

- `Id : Guid` – event id.
- `ApplicationId : Guid` – FK to `Application`; defines the stream.
- `EventType : string` – logical type (`"NavigationItemAdded"`, `"PageUpdated"`, `"DataSourceRemoved"`, etc.).
- `EventData : string` – JSON payload with the change details.
- `EventVersion : int` – schema version for `EventData`.
- `SequenceNumber : long` – per‑application monotonic sequence; ordering for replay/snapshots.
- `OccurredAt : DateTime` – when the change happened.
- `OccurredBy : Guid` – user who made the change.

**Notes**

- This is **not** an aggregate root in the DDD sense; it’s the storage model behind the event‑sourced behavior.
- Application services will append events; projections (draft read models, preview, release) will replay them.

---

#### 2.3 `ApplicationSnapshot`

- **Purpose**: Point‑in‑time projection of an application’s configuration, used primarily when creating releases and for fast preview.
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.application_snapshots`

**Core properties**

- `Id : Guid`.
- `ApplicationId : Guid` – FK to `Application`.
- `ApplicationReleaseId : Guid?` – FK to `ApplicationRelease` when snapshot represents a release.
- `SnapshotData : string` – JSON representing canonical configuration (navigation tree, pages, data sources, schema).
- `SnapshotVersion : int` – version of the snapshot schema.
- `EventSequenceNumber : long` – last `ApplicationEvent.SequenceNumber` included.
- `CreatedAt : DateTime`.

**Usage**

- When user hits “Release”, the domain service:
  - Replays events up to a given sequence.
  - Emits an `ApplicationSnapshot`.
  - Creates an `ApplicationRelease` tied to that snapshot.

---

#### 2.4 `ApplicationRelease` (aggregate root)

- **Purpose**: Immutable, semantic‑versioned snapshot that tenants can install and AppRuntime can execute.
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.application_releases`

**Core properties**

- `Id : Guid`.
- `ApplicationId : Guid`.
- `Version : string` – full semantic version `MAJOR.MINOR.PATCH`.
- `MajorVersion : int`, `MinorVersion : int`, `PatchVersion : int`.
- `ReleaseNotes : string`.
- `ConfigurationSchemaVersion : string` – config schema/runtime compatibility indicator.
- `ReleasedAt : DateTime`.
- `ReleasedBy : Guid`.
- `IsActive : bool` – only one active per application.
- `CreatedAt : DateTime`.

**Key behaviors**

- `Create(applicationId, version, releaseNotes, releasedBy, IDateTimeProvider)`  
  - Validates semantic version format, sets `IsActive = true`, raises `ApplicationReleasedEvent`.
- `Activate()` / `Deactivate()`  
  - Toggle active flag, raising `ReleaseActivatedEvent` / `ReleaseDeactivatedEvent`.

Relations:

- One `Application` → many `ApplicationRelease`.
- One `ApplicationRelease` → many `NavigationComponent`, `PageComponent`, `DataSourceComponent`, and one `ApplicationSchema`.

---

#### 2.5 `NavigationComponent`

- **Purpose**: Frozen navigation menu items for a specific release.
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.navigation_components`

**Core properties**

- `Id : Guid`.
- `ApplicationReleaseId : Guid`.
- `Label : string`.
- `Icon : string`.
- `Route : string`.
- `ParentId : Guid?` – self‑reference for nested menus.
- `DisplayOrder : int`.
- `IsVisible : bool`.
- `CreatedAt : DateTime`.

**Key behaviors**

- `Create(applicationReleaseId, label, icon, route, parentId, displayOrder, IDateTimeProvider)`  
  - Validates FK, label length, route format; raises `NavigationComponentCreatedEvent`.
- `UpdateDisplayOrder(displayOrder)`.
- `Show()` / `Hide()`.

---

#### 2.6 `PageComponent`

- **Purpose**: Frozen page definitions (route + layout + content) per release.
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.page_components`

**Core properties**

- `Id : Guid`.
- `ApplicationReleaseId : Guid`.
- `Title : string`.
- `Route : string`.
- `Layout : string`.
- `Content : string` – JSON describing page widgets/components.
- `CreatedAt : DateTime`.

**Key behaviors**

- `Create(applicationReleaseId, title, route, layout, content, IDateTimeProvider)`  
  - Validates FK, title length, route format, non‑empty layout; raises `PageComponentCreatedEvent`.
- `UpdateContent(content)` / `UpdateLayout(layout)` – with validation.

---

#### 2.7 `DataSourceComponent`

- **Purpose**: Frozen data‑source definitions for a release (DB tables, APIs, etc.).
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.data_source_components`

**Core properties**

- `Id : Guid`.
- `ApplicationReleaseId : Guid`.
- `DataSourceCode : string` – unique code within release.
- `DataSourceType : string/enum` – e.g., `Database`, `RestApi`, `GraphQL`.
- `ConnectionConfig : string` – JSON with connection settings (non‑secret portions).
- `SchemaDefinition : string` – JSON describing the logical schema for this data source.
- `CreatedAt : DateTime`.

**Key behaviors**

- `Create(applicationReleaseId, code, type, connectionConfig, schemaDefinition, IDateTimeProvider)` – with validation of code uniqueness per release.

---

#### 2.8 `ApplicationSetting`

- **Purpose**: App‑level configuration (defaults) that are not tenant‑specific.
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.application_settings`

**Core properties**

- `Id : Guid`.
- `ApplicationId : Guid`.
- `Key : string`.
- `Value : string`.
- `DataType : SettingDataType` – `String`, `Number`, `Boolean`, `JSON`.
- `IsEncrypted : bool`.
- `CreatedAt : DateTime`.
- `UpdatedAt : DateTime?`.

**Key behaviors**

- `Create(applicationId, key, value, dataType, isEncrypted, IDateTimeProvider)` – with key validation.
- `UpdateValue(value, IDateTimeProvider)`.

---

#### 2.9 `ApplicationSchema`

- **Purpose**: Abstract schema of the application (entities/fields) derived from configuration at release time.
- **Namespace**: `AppBuilder.Domain.Entities`
- **Backed table**: `appbuilder.application_schemas`

**Core properties**

- `Id : Guid`.
- `ApplicationReleaseId : Guid`.
- `SchemaDefinition : string` – JSON describing logical entities/fields.
- `SchemaVersion : int`.
- `CreatedAt : DateTime`.

**Usage**

- Serves as the **baseline schema** for tenant‑level customization in the TenantApplication module.
- Used by AppRuntime for configuration/schema compatibility checks.

---

### 3. Domain events

Define record‑type events under `AppBuilder.Domain.Events`, including but not limited to:

- `ApplicationCreatedEvent`, `ApplicationUpdatedEvent`, `ApplicationReleasedEvent`, `ApplicationNewVersionCreatedEvent`, `ApplicationArchivedEvent`.
- `ReleaseActivatedEvent`, `ReleaseDeactivatedEvent`.
- `NavigationComponentCreatedEvent`.
- `PageComponentCreatedEvent`.

These events power:

- Internal invariants (e.g., keeping read models in sync).
- Audit logging.
- Potential future integration events via the Contracts project.

---

### 4. Repository interfaces

Define repository abstractions under `AppBuilder.Domain.Repositories`:

- `IApplicationRepository : IRepository<Application, Guid>`  
  - Extra methods: `GetBySlugAsync`, `SlugExistsAsync`, `GetByStatusAsync`.

- `IApplicationReleaseRepository : IRepository<ApplicationRelease, Guid>`  
  - Extra: `GetAllByApplicationAsync`, `GetByVersionAsync`, `GetActiveReleaseAsync`.

- `INavigationComponentRepository : IRepository<NavigationComponent, Guid>`  
  - Extra: `GetAllByReleaseAsync`, `GetByRouteAsync`.

- `IPageComponentRepository : IRepository<PageComponent, Guid>`  
  - Extra: `GetAllByReleaseAsync`, `GetByRouteAsync`.

- `IDataSourceComponentRepository : IRepository<DataSourceComponent, Guid>`  
  - Extra: `GetAllByReleaseAsync`, `GetByCodeAsync`.

- `IApplicationSettingRepository : IRepository<ApplicationSetting, Guid>`  
  - Extra: `GetAllByApplicationAsync`, `GetByKeyAsync`.

- `IApplicationEventStream` (or similar abstraction) for append/read operations on `ApplicationEvent`:
  - `Task AppendAsync(ApplicationEvent evt, CancellationToken)`
  - `Task<IReadOnlyList<ApplicationEvent>> GetStreamAsync(Guid applicationId, CancellationToken)`
  - `Task<long> GetLastSequenceAsync(Guid applicationId, CancellationToken)`

- `IAppBuilderUnitOfWork : IUnitOfWork`.

---

### 5. Domain layer tasks

1. **Define enums**: `ApplicationStatus`, `SettingDataType` and any required `DataSourceType`.
2. **Implement entities**: `Application`, `ApplicationRelease`, `NavigationComponent`, `PageComponent`, `DataSourceComponent`, `ApplicationSetting`, `ApplicationSnapshot`, `ApplicationEvent`, `ApplicationSchema`.
3. **Add domain events** as record types.
4. **Implement repository interfaces** and the event stream abstraction.
5. **Write unit tests**:
   - Application lifecycle (create, update, release, new version, archive).
   - Release invariants (only one active per application).
   - Navigation/page/data source creation and validation rules.
   - Event stream ordering and snapshot correctness.

