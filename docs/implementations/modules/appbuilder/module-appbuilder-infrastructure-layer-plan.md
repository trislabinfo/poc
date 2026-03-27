## AppBuilder Module – Infrastructure Layer Implementation Plan

### 1. Infrastructure goals

- Provide **EF Core‑based persistence** for AppBuilder domain entities.
- Implement the **event store** for `ApplicationEvent`.
- Support efficient **read models** for releases and (optionally) draft projections.
- Integrate cleanly with the shared `BuildingBlocks.Infrastructure` abstractions.

---

### 2. DbContext

**File**: `AppBuilder.Infrastructure/Data/AppBuilderDbContext.cs`  
**Base**: `BaseModuleDbContext` (from `BuildingBlocks.Infrastructure.Persistence`)  
**Schema**: `"appbuilder"`

**DbSets**

- `DbSet<Application>` – `Applications`.
- `DbSet<ApplicationRelease>` – `ApplicationReleases`.
- `DbSet<NavigationComponent>` – `NavigationComponents`.
- `DbSet<PageComponent>` – `PageComponents`.
- `DbSet<DataSourceComponent>` – `DataSourceComponents`.
- `DbSet<ApplicationSetting>` – `ApplicationSettings`.
- `DbSet<ApplicationSnapshot>` – `ApplicationSnapshots`.
- `DbSet<ApplicationEvent>` – `ApplicationEvents`.
- `DbSet<ApplicationSchema>` – `ApplicationSchemas`.

`OnModelCreating`:

- Call `base.OnModelCreating(modelBuilder)`.
- `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppBuilderDbContext).Assembly);`

---

### 3. Entity configurations

Namespace: `AppBuilder.Infrastructure.Data.Configurations`

Each configuration should:

- Map to a specific table under schema `"appbuilder"`.
- Configure primary keys, required fields, string lengths, indexes, and FKs.

#### 3.1 `ApplicationConfiguration`

- **Table**: `appbuilder.applications`
- Columns:
  - `id : Guid` (PK).
  - `name : string(200)` (required).
  - `slug : string(100)` (required, unique index `uq_applications_slug`).
  - `description : string(1000)` (nullable).
  - `status : int` (required, index `ix_applications_status`).
  - `current_release_id : Guid?`.
  - `created_at : DateTime` (default `now()`).
  - `updated_at : DateTime?`.

#### 3.2 `ApplicationReleaseConfiguration`

- **Table**: `appbuilder.application_releases`
- Columns:
  - `id : Guid` (PK).
  - `application_id : Guid` (FK → `applications.id`).
  - `version : string(50)` (required).
  - `major_version : int`.
  - `minor_version : int`.
  - `patch_version : int`.
  - `release_notes : string(2000)` (nullable).
  - `configuration_schema_version : string(50)` (nullable).
  - `released_at : DateTime` (required).
  - `released_by : Guid` (required).
  - `is_active : bool` (required, default `true`).
  - `created_at : DateTime` (required).
- Indexes:
  - Unique `(application_id, version)` for version uniqueness.
  - `(application_id, is_active)` to quickly find active release.

#### 3.3 `NavigationComponentConfiguration`

- **Table**: `appbuilder.navigation_components`
- Columns:
  - `id : Guid` (PK).
  - `application_release_id : Guid` (FK → `application_releases.id`).
  - `label : string(100)` (required).
  - `icon : string(100)` (nullable).
  - `route : string(200)` (required).
  - `parent_id : Guid?` (FK → `navigation_components.id`).
  - `display_order : int` (required).
  - `is_visible : bool` (required).
  - `created_at : DateTime` (required).
- Indexes:
  - `(application_release_id)` for loading nav per release.
  - Optional `(application_release_id, route)` for fast route lookup.

#### 3.4 `PageComponentConfiguration`

- **Table**: `appbuilder.page_components`
- Columns:
  - `id : Guid` (PK).
  - `application_release_id : Guid` (FK → `application_releases.id`).
  - `title : string(200)` (required).
  - `route : string(200)` (required).
  - `layout : string(100)` (required).
  - `content : text/jsonb` (required).
  - `created_at : DateTime` (required).
- Indexes:
  - `(application_release_id)` for loading all pages per release.
  - Unique `(application_release_id, route)` to prevent duplicate routes.

#### 3.5 `DataSourceComponentConfiguration`

- **Table**: `appbuilder.data_source_components`
- Columns:
  - `id : Guid` (PK).
  - `application_release_id : Guid` (FK → `application_releases.id`).
  - `data_source_code : string(100)` (required).
  - `data_source_type : string(50)` (required).
  - `connection_config : jsonb` (nullable; secrets handled elsewhere).
  - `schema_definition : jsonb` (nullable but recommended).
  - `created_at : DateTime` (required).
- Indexes:
  - Unique `(application_release_id, data_source_code)` to prevent duplicates.

#### 3.6 `ApplicationSettingConfiguration`

- **Table**: `appbuilder.application_settings`
- Columns:
  - `id : Guid` (PK).
  - `application_id : Guid` (FK → `applications.id`).
  - `key : string(100)` (required).
  - `value : text` (required).
  - `data_type : int` (required).
  - `is_encrypted : bool` (required).
  - `created_at : DateTime` (required).
  - `updated_at : DateTime?`.
- Indexes:
  - Unique `(application_id, key)` to avoid duplicates.

#### 3.7 `ApplicationSnapshotConfiguration`

- **Table**: `appbuilder.application_snapshots`
- Columns:
  - `id : Guid` (PK).
  - `application_id : Guid` (FK → `applications.id`).
  - `application_release_id : Guid?` (FK → `application_releases.id`).
  - `snapshot_data : jsonb` (required).
  - `snapshot_version : int` (required).
  - `event_sequence_number : bigint` (required).
  - `created_at : DateTime` (required).
- Indexes:
  - `(application_id)` for listing snapshots.
  - `(application_release_id)` for release lookup.

#### 3.8 `ApplicationEventConfiguration`

- **Table**: `appbuilder.application_events`
- Columns:
  - `id : Guid` (PK).
  - `application_id : Guid` (FK → `applications.id`).
  - `event_type : string(200)` (required).
  - `event_data : jsonb` (required).
  - `event_version : int` (required).
  - `sequence_number : bigint` (required).
  - `occurred_at : DateTime` (required).
  - `occurred_by : Guid` (required).
- Indexes:
  - Composite `(application_id, sequence_number)` **clustered/indexed** for fast stream reads.
  - `(application_id, event_type)` for per‑type queries.

#### 3.9 `ApplicationSchemaConfiguration`

- **Table**: `appbuilder.application_schemas`
- Columns:
  - `id : Guid` (PK).
  - `application_release_id : Guid` (FK → `application_releases.id`).
  - `schema_definition : jsonb` (required).
  - `schema_version : int` (required).
  - `created_at : DateTime` (required).

---

### 4. Repository implementations

Namespace: `AppBuilder.Infrastructure.Repositories`

Implement interfaces defined in the Domain layer using `Repository<TEntity, TKey>` base classes where possible.

#### 4.1 `ApplicationRepository`

- Implements `IApplicationRepository`.
- Methods:
  - `GetBySlugAsync(slug)`
  - `SlugExistsAsync(slug)`
  - `GetAllAsync()`
  - `GetByStatusAsync(status)`

#### 4.2 `ApplicationReleaseRepository`

- Implements `IApplicationReleaseRepository`.
- Methods:
  - `GetAllByApplicationAsync(applicationId)`
  - `GetByVersionAsync(applicationId, version)`
  - `GetActiveReleaseAsync(applicationId)`

#### 4.3 `NavigationComponentRepository`

- Implements `INavigationComponentRepository`.
- Methods:
  - `GetAllByReleaseAsync(applicationReleaseId)`
  - `GetByRouteAsync(applicationReleaseId, route)`

#### 4.4 `PageComponentRepository`

- Implements `IPageComponentRepository`.

#### 4.5 `DataSourceComponentRepository`

- Implements `IDataSourceComponentRepository`.
- Methods:
  - `GetAllByReleaseAsync(applicationReleaseId)`
  - `GetByCodeAsync(applicationReleaseId, code)`

#### 4.6 `ApplicationSettingRepository`

- Implements `IApplicationSettingRepository`.

#### 4.7 Event stream implementation

- Implement `IApplicationEventStream` using `AppBuilderDbContext`:
  - `AppendAsync(ApplicationEvent evt, CancellationToken)`:
    - Set `SequenceNumber` = last+1 (use a query or DB‑level sequence).
    - Insert row.
  - `GetStreamAsync(Guid applicationId, CancellationToken)`:
    - Query ordered by `(application_id, sequence_number)`.
  - `GetLastSequenceAsync(Guid applicationId, CancellationToken)`:
    - Query max `sequence_number` for app.

Ensure all event operations participate in the module’s `IAppBuilderUnitOfWork` transaction.

#### 4.8 `AppBuilderUnitOfWork`

- Simple wrapper around `AppBuilderDbContext.SaveChangesAsync`.

---

### 5. Projections and read models (optional but recommended)

Although the **source of truth** is the `ApplicationEvent` stream + `Application` aggregate, some additional read models simplify queries:

- **Draft navigation/pages read models** for fast AppBuilder UI:
  - Could be denormalized tables or in‑memory caches populated by background consumers.
  - Implementation details can be added later; not required for the first pass.

For now, the main implementation plan relies on **on‑demand event replay** in the Application layer; projections are an optimization step.

---

### 6. Infrastructure registration

**File**: `AppBuilder.Infrastructure/AppBuilderInfrastructureServiceCollectionExtensions.cs`

Method:

```csharp
public static IServiceCollection AddAppBuilderInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration,
    string schemaName)
```

Responsibilities:

- Register `AppBuilderDbContext` with connection string (likely the shared application DB).
- Configure migrations history table for EF Core (schema‑aware).
- Register repositories:
  - `IApplicationRepository`, `IApplicationReleaseRepository`,
    `INavigationComponentRepository`, `IPageComponentRepository`,
    `IDataSourceComponentRepository`, `IApplicationSettingRepository`.
- Register event stream implementation `IApplicationEventStream`.
- Register `IAppBuilderUnitOfWork`.

---

### 7. Infrastructure tasks

1. Implement `AppBuilderDbContext` with `SchemaName = "appbuilder"`.
2. Add entity type configurations for all AppBuilder entities.
3. Implement all repository classes and the event stream.
4. Implement `AppBuilderUnitOfWork`.
5. Add DI registration extension (`AddAppBuilderInfrastructure`).
6. Write integration tests:
   - Basic CRUD for `Application` and `ApplicationRelease`.
   - Event stream append/read behavior.
   - Navigation/page/data source queries by release and route/code.

