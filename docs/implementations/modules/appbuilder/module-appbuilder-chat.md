## Complete Module & Entity List (Backend API Focus)

### Module 1: Feature Module
**Purpose**: Manage platform-level features and tenant-scoped feature flags

#### Entity 1: Feature
**Purpose**: Logical grouping container for feature flags (not toggleable itself)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `Code` | string | Unique feature code (e.g., "advanced-analytics") | Used for feature flag evaluation |
| `Name` | string | Display name | Human-readable label |
| `Description` | string | Feature description | Documentation |
| `Category` | string | Feature category (e.g., "Analytics", "Customization") | Organization and filtering |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

#### Entity 2: FeatureFlag
**Purpose**: Actual toggleable unit linked to a feature and tenant

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `FeatureId` | Guid | FK to Feature | Links flag to its feature group |
| `TenantId` | Guid | FK to Tenant | Tenant-scoped enablement |
| `FlagCode` | string | Unique flag code within feature (e.g., "enable-dashboard") | Used for evaluation |
| `IsEnabled` | bool | Enabled state | Toggle state |
| `EvaluationLevel` | enum | Backend/Frontend/Runtime | Determines where flag is evaluated |
| `Configuration` | string (JSON) | Optional flag-specific config | Store flag-specific settings |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

---

### Module 2: AppBuilder Module
**Purpose**: No-code/low-code application building with event sourcing

#### Entity 1: Application
**Purpose**: Application definition (draft state, editable)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `Name` | string | Application name | Human-readable label |
| `Slug` | string | URL-friendly identifier | Used in URLs and references |
| `Description` | string | Application description | Documentation |
| `Status` | enum | Draft/Released/Archived | Lifecycle state |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

#### Entity 2: ApplicationEvent
**Purpose**: Immutable event in event store (source of truth)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationId` | Guid | FK to Application | Links event to application |
| `EventType` | string | Event type (e.g., "NavigationAdded", "PageUpdated") | Determines event handler |
| `EventData` | string (JSON) | Event payload | Contains change details |
| `EventVersion` | int | Event schema version | Handles event schema evolution |
| `OccurredAt` | DateTime | Event timestamp | Chronological ordering |
| `OccurredBy` | Guid | User ID who made change | Audit trail |
| `SequenceNumber` | long | Event sequence number | Ensures ordering and replay |

#### Entity 3: ApplicationSnapshot
**Purpose**: Projected state snapshot at release point (immutable)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationId` | Guid | FK to Application | Links snapshot to application |
| `ApplicationReleaseId` | Guid | FK to ApplicationRelease | Links to released version |
| `SnapshotData` | string (JSON) | Complete application configuration | Current state projection |
| `SnapshotVersion` | int | Snapshot schema version | Handles schema evolution |
| `CreatedAt` | DateTime | Snapshot creation timestamp | Audit trail |
| `EventSequenceNumber` | long | Last event sequence included | Enables incremental updates |

#### Entity 4: ApplicationRelease
**Purpose**: Immutable released version (semantic versioning)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationId` | Guid | FK to Application | Links release to application |
| `Version` | string | Semantic version (MAJOR.MINOR.PATCH) | Version identification |
| `MajorVersion` | int | Major version number | Breaking changes indicator |
| `MinorVersion` | int | Minor version number | Backward-compatible features |
| `PatchVersion` | int | Patch version number | Non-breaking fixes |
| `ReleaseNotes` | string | Release notes | Documentation |
| `ConfigurationSchemaVersion` | string | Minimum runtime compatibility version | Runtime compatibility check |
| `ReleasedAt` | DateTime | Release timestamp | Audit trail |
| `ReleasedBy` | Guid | User ID who released | Audit trail |
| `IsActive` | bool | Active release flag | Only one active per application |

#### Entity 5: ApplicationComponent
**Purpose**: Component definition (navigation, pages, data sources, etc.)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationReleaseId` | Guid | FK to ApplicationRelease | Links to released version |
| `ComponentType` | string | Component type (Navigation, Page, DataSource, etc.) | Determines component handler |
| `ComponentCode` | string | Unique code within release | Component identification |
| `Configuration` | string (JSON) | Component configuration | Component-specific settings |
| `DisplayOrder` | int | Display order | UI ordering |
| `IsEnabled` | bool | Enabled state | Toggle component visibility |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

#### Entity 6: ApplicationSchema
**Purpose**: Database schema definition derived from configuration

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationReleaseId` | Guid | FK to ApplicationRelease | Links schema to release |
| `SchemaDefinition` | string (JSON) | Complete schema definition | Stores table/column definitions |
| `SchemaVersion` | int | Schema version number | Tracks schema evolution |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

---

### Module 3: TenantApplication Module
**Purpose**: Tenant installation, customization (fork model), and environment management

#### Entity 1: TenantApplication
**Purpose**: Tenant-installed application instance

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `TenantId` | Guid | FK to Tenant | Tenant ownership |
| `ApplicationId` | Guid | FK to Application | Original platform application |
| `ApplicationReleaseId` | Guid | FK to ApplicationRelease | Installed release version |
| `Slug` | string | URL-friendly identifier | Used in URL routing |
| `Status` | enum | Installed/Active/Inactive/Uninstalled | Lifecycle state |
| `BaseConfiguration` | string (JSON) | Tenant-specific base config | Tenant customization |
| `IsCustomized` | bool | Customization flag | Indicates if tenant has forked |
| `InstalledAt` | DateTime | Installation timestamp | Audit trail |
| `InstalledBy` | Guid | User ID who installed | Audit trail |
| `ActivatedAt` | DateTime? | Activation timestamp | Audit trail |
| `DeactivatedAt` | DateTime? | Deactivation timestamp | Audit trail |
| `UninstalledAt` | DateTime? | Uninstallation timestamp | Audit trail |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

#### Entity 2: TenantApplicationFork
**Purpose**: Tenant-owned fork of platform application (customization)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `TenantApplicationId` | Guid | FK to TenantApplication | Links fork to tenant app |
| `ForkedFromReleaseId` | Guid | FK to ApplicationRelease | Original release forked from |
| `ForkVersion` | string | Fork version (semantic) | Version tracking for fork |
| `ForkConfiguration` | string (JSON) | Forked application configuration | Tenant-specific changes |
| `IsActive` | bool | Active fork flag | Only one active fork per tenant app |
| `ForkedAt` | DateTime | Fork creation timestamp | Audit trail |
| `ForkedBy` | Guid | User ID who created fork | Audit trail |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

#### Entity 3: TenantApplicationForkEvent
**Purpose**: Event store for tenant fork (event sourcing)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `TenantApplicationForkId` | Guid | FK to TenantApplicationFork | Links event to fork |
| `EventType` | string | Event type | Determines event handler |
| `EventData` | string (JSON) | Event payload | Contains change details |
| `EventVersion` | int | Event schema version | Handles event schema evolution |
| `OccurredAt` | DateTime | Event timestamp | Chronological ordering |
| `OccurredBy` | Guid | User ID who made change | Audit trail |
| `SequenceNumber` | long | Event sequence number | Ensures ordering and replay |

#### Entity 4: TenantApplicationEnvironment
**Purpose**: Environment deployment (dev/staging/production)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `TenantApplicationId` | Guid | FK to TenantApplication | Links environment to tenant app |
| `EnvironmentType` | enum | Development/Staging/Production | Environment type |
| `ApplicationReleaseId` | Guid? | FK to ApplicationRelease | Deployed release (null if not deployed) |
| `TenantApplicationForkId` | Guid? | FK to TenantApplicationFork | Custom fork deployed (if customized) |
| `IsActive` | bool | Active environment flag | Controls access |
| `EnvironmentConfiguration` | string (JSON) | Environment-specific config overrides | Environment-specific settings |
| `DeployedAt` | DateTime? | Deployment timestamp | Audit trail |
| `DeployedBy` | Guid? | User ID who deployed | Audit trail |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

#### Entity 5: TenantApplicationSchema
**Purpose**: Tenant-specific schema (from customization)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `TenantApplicationId` | Guid | FK to TenantApplication | Links schema to tenant app |
| `TenantApplicationForkId` | Guid? | FK to TenantApplicationFork | Links to fork (if customized) |
| `SchemaDefinition` | string (JSON) | Tenant-specific schema definition | Stores modified schema |
| `SchemaVersion` | int | Schema version number | Tracks schema evolution |
| `MigrationStatus` | enum | Pending/Applied/Failed | Migration tracking |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

#### Entity 6: TenantApplicationSchemaMigration
**Purpose**: Schema migration tracking for tenant customization

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `TenantApplicationSchemaId` | Guid | FK to TenantApplicationSchema | Links migration to schema |
| `MigrationType` | enum | AddProperty/RemoveProperty/ModifyProperty | Migration type |
| `MigrationScript` | string | SQL migration script | Actual migration SQL |
| `FromSchemaVersion` | int | Source schema version | Migration source |
| `ToSchemaVersion` | int | Target schema version | Migration target |
| `Status` | enum | Pending/Applied/Failed/RolledBack | Migration status |
| `AppliedAt` | DateTime? | Application timestamp | Audit trail |
| `AppliedBy` | Guid? | User ID who applied | Audit trail |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

---

### Module 4: AppRuntime Module
**Purpose**: Runtime execution engine for tenant applications

#### Entity 1: RuntimeVersion
**Purpose**: Runtime version with component support

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `Version` | string | Semantic version | Version identification |
| `IsCurrent` | bool | Current runtime flag | Only one current version |
| `MinimumConfigurationSchemaVersion` | string | Minimum config schema version supported | Compatibility check |
| `SupportedComponentTypes` | string (JSON) | List of supported component types | Component compatibility |
| `ReleasedAt` | DateTime | Release timestamp | Audit trail |
| `ReleaseNotes` | string | Release notes | Documentation |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

#### Entity 2: RuntimeInstance
**Purpose**: Running application instance

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `TenantId` | Guid | FK to Tenant | Tenant ownership |
| `TenantApplicationId` | Guid | FK to TenantApplication | Links to tenant app |
| `ApplicationReleaseId` | Guid? | FK to ApplicationRelease | Platform release (if not customized) |
| `TenantApplicationForkId` | Guid? | FK to TenantApplicationFork | Custom fork (if customized) |
| `RuntimeVersionId` | Guid | FK to RuntimeVersion | Runtime version used |
| `EnvironmentType` | enum | Development/Staging/Production | Environment type |
| `InstanceName` | string | Instance identifier | Human-readable name |
| `Status` | enum | Starting/Running/Stopping/Stopped/Failed | Instance lifecycle state |
| `Configuration` | string (JSON) | Runtime configuration | Merged config (app + env) |
| `CompatibilityCheckPassed` | bool | Compatibility check result | Runtime validation |
| `CompatibilityCheckDetails` | string (JSON) | Compatibility check details | Detailed validation info |
| `StartedAt` | DateTime? | Start timestamp | Audit trail |
| `StoppedAt` | DateTime? | Stop timestamp | Audit trail |
| `LastHealthCheckAt` | DateTime? | Last health check timestamp | Health monitoring |
| `HealthStatus` | enum | Healthy/Degraded/Unhealthy | Health status |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

#### Entity 3: ComponentRegistry
**Purpose**: Registered component loaders/renderers

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `RuntimeVersionId` | Guid | FK to RuntimeVersion | Links to runtime version |
| `ComponentType` | string | Component type name | Component identification |
| `ComponentVersion` | string | Component version | Version compatibility |
| `LoaderClassName` | string | Component loader class name | Runtime component loading |
| `IsEnabled` | bool | Enabled flag | Toggle component support |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

#### Entity 4: RuntimeMetrics
**Purpose**: Performance and usage metrics

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `RuntimeInstanceId` | Guid | FK to RuntimeInstance | Links to instance |
| `MetricType` | enum | CPU/Memory/Requests/Errors/ResponseTime | Metric category |
| `MetricName` | string | Metric name | Metric identification |
| `Value` | decimal | Metric value | Actual metric value |
| `Unit` | string | Metric unit | Value unit |
| `Timestamp` | DateTime | Metric timestamp | Time series data |
| `Tags` | string (JSON) | Additional tags | Metric filtering |

#### Entity 5: RuntimeLog
**Purpose**: Runtime operation audit trail

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `RuntimeInstanceId` | Guid | FK to RuntimeInstance | Links to instance |
| `LogLevel` | enum | Debug/Info/Warning/Error/Critical | Log severity |
| `Message` | string | Log message | Log content |
| `Exception` | string? | Exception details | Error details |
| `Context` | string (JSON) | Additional context | Log context |
| `Timestamp` | DateTime | Log timestamp | Chronological ordering |

---

### Module 5: EventStore Module (Shared Infrastructure)
**Purpose**: Centralized event store for event sourcing

#### Entity 1: EventStore
**Purpose**: Central event store (all application events)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `StreamId` | string | Event stream identifier | Groups related events |
| `StreamType` | string | Stream type (Application/Fork) | Stream categorization |
| `EventType` | string | Event type name | Event handler routing |
| `EventData` | string (JSON) | Event payload | Event content |
| `EventVersion` | int | Event schema version | Schema evolution |
| `OccurredAt` | DateTime | Event timestamp | Chronological ordering |
| `OccurredBy` | Guid | User ID who triggered | Audit trail |
| `SequenceNumber` | long | Event sequence number | Ordering and replay |
| `Metadata` | string (JSON) | Additional metadata | Extended event info |

#### Entity 2: EventStream
**Purpose**: Event stream metadata

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `StreamId` | string | Unique stream identifier | Stream identification |
| `StreamType` | string | Stream type | Stream categorization |
| `LastSequenceNumber` | long | Last event sequence | Stream position |
| `SnapshotId` | Guid? | FK to latest snapshot | Links to latest snapshot |
| `CreatedAt` | DateTime | Stream creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

#### Entity 3: EventSnapshot
**Purpose**: Projected state snapshots

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `StreamId` | string | FK to EventStream | Links to stream |
| `SnapshotData` | string (JSON) | Projected state | Current state |
| `SnapshotVersion` | int | Snapshot schema version | Schema evolution |
| `EventSequenceNumber` | long | Last event sequence included | Snapshot position |
| `CreatedAt` | DateTime | Snapshot creation timestamp | Audit trail |

#### Entity 5: NavigationComponent
**Purpose**: Navigation menu structure definition

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationReleaseId` | Guid | FK to ApplicationRelease | Links to released version (immutable snapshot) |
| `Label` | string | Navigation item label (e.g., "Dashboard", "Reports") | Display text in navigation |
| `Icon` | string | Icon identifier (e.g., "dashboard", "chart-bar") | Visual icon in navigation |
| `Route` | string | Route path (e.g., "/dashboard", "/reports") | URL routing for navigation item |
| `ParentId` | Guid? | FK to NavigationComponent (self-reference) | Supports nested/hierarchical navigation |
| `DisplayOrder` | int | Display order | Controls navigation item sequence |
| `IsVisible` | bool | Visibility flag | Toggle navigation item visibility |
| `FeatureFlagCode` | string? | Optional feature flag code | Feature flag gating for navigation |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

**Why separate entity**: Navigation is a distinct UI structure (menu hierarchy) separate from page content. Needed for dynamic navigation building at runtime.

#### Entity 6: PageComponent
**Purpose**: Page definition with layout and content

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationReleaseId` | Guid | FK to ApplicationRelease | Links to released version (immutable snapshot) |
| `Title` | string | Page title (e.g., "Dashboard", "User Profile") | Page display title |
| `Route` | string | Route path (e.g., "/dashboard", "/profile") | URL routing for page |
| `Layout` | string | Layout type (e.g., "default", "full-width", "sidebar") | Page layout template |
| `Content` | string (JSON) | Page content configuration | Structured page content (components, widgets, etc.) |
| `Metadata` | string (JSON) | Additional page metadata | Extended page configuration |
| `FeatureFlagCode` | string? | Optional feature flag code | Feature flag gating for page |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

**Why separate entity**: Pages are distinct from navigation. Runtime needs explicit page definitions to render content. Content stored as JSON for flexibility.

#### Entity 7: DataSourceComponent
**Purpose**: Data source definition (tables, APIs, etc.)

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationReleaseId` | Guid | FK to ApplicationRelease | Links to released version |
| `DataSourceCode` | string | Unique code within release | Data source identification |
| `DataSourceType` | enum | Database/API/File/External | Data source type |
| `ConnectionString` | string (encrypted) | Connection details | Secure connection info |
| `SchemaDefinition` | string (JSON) | Schema/structure definition | Data structure definition |
| `Configuration` | string (JSON) | Data source configuration | Additional settings |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

**Why separate entity**: Data sources are distinct from UI components. Needed for schema evolution and runtime data access.

#### Entity 8: ApplicationComponent (Generic)
**Purpose**: Other component types (forms, widgets, etc.) - extensible

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationReleaseId` | Guid | FK to ApplicationRelease | Links to released version |
| `ComponentType` | string | Component type (Form, Widget, Chart, etc.) | Component type identification |
| `ComponentCode` | string | Unique code within release | Component identification |
| `Configuration` | string (JSON) | Component configuration | Component-specific settings |
| `DisplayOrder` | int | Display order | UI ordering |
| `IsEnabled` | bool | Enabled state | Toggle component |
| `FeatureFlagCode` | string? | Optional feature flag code | Feature flag gating |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |

**Why separate entity**: Supports extensible component types beyond navigation/pages. Allows future component types without schema changes.

#### Entity 9: ApplicationSchema
*(Same as before)*

#### Entity 10: ApplicationSetting
**Purpose**: Application-level key-value configuration

| Property | Type | Description | Why Needed |
|----------|------|-------------|------------|
| `Id` | Guid | Primary key | Unique identifier |
| `ApplicationId` | Guid | FK to Application | Links to application (not release) |
| `Key` | string | Setting key | Setting identification |
| `Value` | string | Setting value | Setting value |
| `DataType` | enum | String/Number/Boolean/JSON | Value type for validation |
| `IsEncrypted` | bool | Encryption flag | Security for sensitive settings |
| `CreatedAt` | DateTime | Creation timestamp | Audit trail |
| `UpdatedAt` | DateTime? | Last update timestamp | Audit trail |

**Why separate entity**: Application settings are distinct from component configuration. Needed for app-level configuration that persists across releases.

---

### Updated Summary

**AppBuilder Module Entities**: 10 entities
1. Application
2. ApplicationEvent
3. ApplicationSnapshot
4. ApplicationRelease
5. NavigationComponent (explicit)
6. PageComponent (explicit)
7. DataSourceComponent (explicit)
8. ApplicationComponent (generic/extensible)
9. ApplicationSchema
10. ApplicationSetting

**Total Entities Across All Modules**: 26 entities (was 22, now +4)

**Rationale for separate NavigationComponent and PageComponent**:
- Different concerns: Navigation is menu structure; Pages are content definitions
- Different runtime handling: Navigation builds menus; Pages render content
- Different relationships: Navigation can be hierarchical (ParentId); Pages are flat
- Different properties: Navigation has Icon/ParentId; Pages have Layout/Content
- Query patterns: Often query navigation separately from pages
- Feature flag integration: Can gate navigation items and pages independently

This aligns with the requirements mentioning "navigation definitions" and "page configurations" as distinct concepts.

**Cursor**

### AppBuilder module – entities, properties, relations

I’ll use logical table names like `appbuilder.*` to make the flows concrete.

---

#### 1. `Application`

**Purpose**: Root entity representing an app being built in AppBuilder (draft / released / archived).

**Table**: `appbuilder.applications`

**Key properties**

- **`Id : Guid`**  
  Unique identifier for the application. Used as the root key for events, releases, etc.

- **`Name : string`**  
  Human‑readable name (“CRM”, “Inventory”). Used in UI lists, logs, and references.

- **`Slug : string`**  
  Lowercase, URL‑friendly identifier (`"my-crm-app"`). Used as part of URLs and as a stable identifier across environments.

- **`Description : string`**  
  Longer description of the app. Helps admins understand what the app does.

- **`Status : ApplicationStatus` (`Draft`, `Released`, `Archived`)**  
  Lifecycle state:  
  - `Draft`: editable; can accumulate events.  
  - `Released`: immutable definition; only fork/new versions allowed.  
  - `Archived`: no longer offered to new tenants.

- **`CurrentReleaseId : Guid?`**  
  FK to `appbuilder.application_releases`. Points to the latest “active” release (for catalog, “install latest” operations).

- **`CreatedAt : DateTime` / `UpdatedAt : DateTime?`**  
  Audit fields for when the app was created and last changed at the metadata level.

**Relations**

- One `Application` → many `ApplicationEvent` (event stream for configuration).
- One `Application` → many `ApplicationRelease`.
- One `Application` → many `ApplicationSetting` (app‑level config).

**External dependencies**

- Referenced (read‑only) by **TenantApplication** module when listing available apps.  
- No direct runtime dependency on Feature/Runtime; they use releases, not raw applications.

---

#### 2. `ApplicationEvent` (event store for AppBuilder)

**Purpose**: Immutable event stream for application configuration (event sourcing).

**Table**: `appbuilder.application_events`

**Key properties**

- **`Id : Guid`**  
  Unique id per event (for querying and debugging).

- **`ApplicationId : Guid`**  
  FK to `appbuilder.applications`. Ties event to one application’s stream.

- **`EventType : string`**  
  Logical type (`"NavigationItemAdded"`, `"PageUpdated"`, `"DataSourceRemoved"`). Used by event handlers to interpret `EventData`.

- **`EventData : string (JSON)`**  
  Payload describing the change (e.g., specific nav item properties). The actual configuration change lives here.

- **`EventVersion : int`**  
  Version of the event schema for this `EventType`. Allows evolving event shapes over time.

- **`SequenceNumber : long`**  
  Monotonically increasing number per `ApplicationId`. Defines ordering for replay and snapshotting.

- **`OccurredAt : DateTime`**  
  When the change happened.

- **`OccurredBy : Guid`**  
  User id who made the change. Provides full auditability.

**Relations**

- Many events per `Application`.
- `SequenceNumber` + `ApplicationId` define the logical stream order.

**External dependencies**

- None at DB level. The **AppBuilder Application layer** plus an **EventStore infrastructure** component will consume and project these.

---

#### 3. `ApplicationSnapshot` (projection of configuration at a point in time)

**Purpose**: Point‑in‑time projection of full app configuration (used at release time and for fast loading large apps).

**Table**: `appbuilder.application_snapshots`

**Key properties**

- **`Id : Guid`**  
  Unique snapshot id.

- **`ApplicationId : Guid`**  
  FK to `appbuilder.applications`.

- **`ApplicationReleaseId : Guid?`**  
  FK to `appbuilder.application_releases` for release snapshots. Can be null for internal/non‑release snapshots.

- **`SnapshotData : string (JSON)`**  
  Canonical “current configuration” at that point (navigation tree, pages, data sources, etc., in a normalized JSON schema).

- **`SnapshotVersion : int`**  
  Version of the snapshot schema itself (so snapshot JSON can evolve).

- **`EventSequenceNumber : long`**  
  Last event included in this snapshot. Used to know from where to resume replay.

- **`CreatedAt : DateTime`**  
  When the snapshot was taken.

**Relations**

- One `Application` → many snapshots.
- Typically one snapshot tied to each `ApplicationRelease` (release snapshot).

**External dependencies**

- Consumed by **TenantApplication** and **AppRuntime** indirectly (via release) to know the frozen configuration.

---

#### 4. `ApplicationRelease`

**Purpose**: Immutable released version of an app, linked to a snapshot of the event stream.

**Table**: `appbuilder.application_releases`

**Key properties**

- **`Id : Guid`**  
  Release identifier.

- **`ApplicationId : Guid`**  
  FK to `appbuilder.applications`.

- **`Version : string`**  
  Semantic version string (`"1.0.0"`). Used in UI, APIs, and compatibility logic.

- **`MajorVersion : int` / `MinorVersion : int` / `PatchVersion : int`**  
  Parsed components of `Version`. Make it easy to query or enforce rules (breaking vs non‑breaking).

- **`ReleaseNotes : string`**  
  Human‑readable description of what changed.

- **`ConfigurationSchemaVersion : string`**  
  Version of the configuration/schema capabilities the app uses. AppRuntime checks this against its own supported schema versions.

- **`ReleasedAt : DateTime` / `ReleasedBy : Guid`**  
  When and by whom the release was created.

- **`IsActive : bool`**  
  Whether this release is considered “active” for the app (you may keep old releases for history but clearly mark one as current).

**Relations**

- One `Application` → many `ApplicationRelease`.
- One `ApplicationRelease` → many `NavigationComponent`, `PageComponent`, `DataSourceComponent` (immutable copies at release time).
- One `ApplicationRelease` often has one `ApplicationSnapshot` as its backing configuration.

**External dependencies**

- **TenantApplication**: installs a specific `ApplicationReleaseId`.  
- **AppRuntime**: executes a `TenantApplication` that references `ApplicationReleaseId`.

---

#### 5. `NavigationComponent`

**Purpose**: Frozen navigation tree for a specific release.

**Table**: `appbuilder.navigation_components`

**Key properties**

- **`Id : Guid`**  
  Navigation item id.

- **`ApplicationReleaseId : Guid`**  
  FK to `appbuilder.application_releases`. Binds nav structure to a specific release (immutable).

- **`Label : string`**  
  Menu text (“Dashboard”, “Reports”).

- **`Icon : string`**  
  Icon identifier (e.g., `"dashboard"`, `"chart-bar"`).

- **`Route : string`**  
  Route path (e.g., `"/dashboard"`, `"/reports"`). Ties nav to pages.

- **`ParentId : Guid?`**  
  Optional FK to another `navigation_components.Id`. Enables nested menus.

- **`DisplayOrder : int`**  
  Order within the same parent.

- **`IsVisible : bool`**  
  Whether this nav item should show in menus (can be hidden but still routable).

- **`CreatedAt : DateTime`**  
  When this nav entry was materialized for the release.

**Relations**

- One `ApplicationRelease` → many `NavigationComponent`.
- Self‑referential parent/child for hierarchy.
- Typically, `Route` values should correspond to `PageComponent.Route`.

**External dependencies**

- **AppRuntime** uses this to build runtime navigation menus.  
- Can be combined with **Feature flags** (Feature module) at runtime to hide/show items.

---

#### 6. `PageComponent`

**Purpose**: Frozen page definitions for a specific release.

**Table**: `appbuilder.page_components`

**Key properties**

- **`Id : Guid`**  
  Page id.

- **`ApplicationReleaseId : Guid`**  
  FK to `appbuilder.application_releases`.

- **`Title : string`**  
  Page title (“Dashboard”, “User Profile”).

- **`Route : string`**  
  Unique route within the app (`"/dashboard"`, `"/profile"`). Used by routing and deep linking.

- **`Layout : string`**  
  Layout template key (“default”, “full-width”, “sidebar”), interpreted by runtime UI.

- **`Content : string (JSON)`**  
  Structured configuration describing page content (components/widgets, sections, bindings). This is what the runtime renders.

- **`CreatedAt : DateTime`**  
  When the page definition was materialized for this release.

**Relations**

- One `ApplicationRelease` → many `PageComponent`.
- `Route` often referenced by `NavigationComponent.Route`.

**External dependencies**

- **AppRuntime**: loads page config and renders UI accordingly.

---

#### 7. `DataSourceComponent` (optional but recommended)

**Purpose**: Frozen data‑source configuration for a release (database tables, APIs, etc.).

**Table**: `appbuilder.data_source_components`

**Key properties**

- **`Id : Guid`**  
  Data source id.

- **`ApplicationReleaseId : Guid`**  
  FK to `appbuilder.application_releases`.

- **`DataSourceCode : string`**  
  Code/name used by pages/bindings to reference this data source.

- **`DataSourceType : string/enum`**  
  Type of data source (Database, REST API, GraphQL, etc.).

- **`ConnectionConfig : string (JSON)`**  
  Connection/config details (host, endpoint, auth, etc.). May be combined with secrets configuration elsewhere.

- **`SchemaDefinition : string (JSON)`**  
  Logical schema of the data (tables, fields, types). Used for schema evolution / validations.

- **`CreatedAt : DateTime`**  
  When the data‑source definition was materialized for this release.

**Relations**

- One `ApplicationRelease` → many `DataSourceComponent`.
- Pages may reference `DataSourceCode` in `Content` JSON.

**External dependencies**

- **AppRuntime**: uses this to know how to fetch data for pages/forms.  
- **TenantApplication** (and downstream tenant‑specific schema modules) may use this as a base schema.

---

#### 8. `ApplicationSetting`

**Purpose**: Application‑level key/value configuration (not per‑tenant).

**Table**: `appbuilder.application_settings`

**Key properties**

- **`Id : Guid`**  
  Setting id.

- **`ApplicationId : Guid`**  
  FK to `appbuilder.applications`. Settings are attached to the app, not individual releases.

- **`Key : string`**  
  Setting name (`"defaultLocale"`, `"maxItemsPerPage"`).

- **`Value : string`**  
  Serialized value.

- **`DataType : SettingDataType` (`String`, `Number`, `Boolean`, `JSON`)**  
  Helps validate and correctly parse `Value`.

- **`IsEncrypted : bool`**  
  Whether `Value` is encrypted (for secrets like API keys).

- **`CreatedAt : DateTime` / `UpdatedAt : DateTime?`**  
  Audit fields.

**Relations**

- One `Application` → many `ApplicationSetting`.

**External dependencies**

- **TenantApplication** and **AppRuntime** can read these as defaults and override per‑tenant/per‑environment.

---

#### 9. `ApplicationSchema` (platform‑level configuration schema)

**Purpose**: Captures the abstract schema of the app (fields, entities) as defined by configuration for a release.

**Table**: `appbuilder.application_schemas`

**Key properties**

- **`Id : Guid`**  
  Schema id.

- **`ApplicationReleaseId : Guid`**  
  FK to `appbuilder.application_releases`.

- **`SchemaDefinition : string (JSON)`**  
  Logical schema description (entities, fields, types). Used as the base for tenant‑level schema evolution.

- **`SchemaVersion : int`**  
  Version number for this schema definition (for internal evolution).

- **`CreatedAt : DateTime`**  
  When schema was created for the release.

**Relations**

- One `ApplicationRelease` → one (or zero/one) `ApplicationSchema`.

**External dependencies**

- **TenantApplication** module: uses this as baseline when tenants customize schema.  
- **AppRuntime**: may validate that configured components match schema.

---

### External dependencies of AppBuilder module (summary)

- **Feature module**:  
  - A global feature flag controls whether AppBuilder is available to a user/tenant, but AppBuilder’s own persistence does not depend on Feature tables.

- **Tenant & TenantApplication modules**:  
  - Read `Application` and `ApplicationRelease` to show catalogs and allow installation.

- **AppRuntime module**:  
  - Consumes `ApplicationRelease`, `NavigationComponent`, `PageComponent`, `DataSourceComponent`, `ApplicationSchema` when running tenant apps.

---

### Flow: how an application is built (draft lifecycle)

Below, “insert/update in table X” describes backend API behavior; exact read models can be projections of the event stream, but I’ll describe the core writes.

#### 1. User opens AppBuilder and creates a new app

1. Frontend calls backend API: `POST /api/appbuilder/applications` with `Name`, `Slug`, `Description`.
2. Backend:
   - Validates slug uniqueness.
   - Creates an `Application` aggregate in memory with `Status = Draft`.
   - **Inserts row into `appbuilder.applications`** with metadata and `Status = Draft`.
   - Optionally writes an initial `ApplicationEvent` like `"ApplicationCreated"` for the event stream:
     - **Insert into `appbuilder.application_events`** with `EventType = "ApplicationCreated"`, `EventData` containing name/slug/description, `SequenceNumber = 1`.

Result: you now have an empty draft application.

#### 2. User adds / edits navigation

1. Frontend calls backend API (for example):  
   `POST /api/appbuilder/applications/{applicationId}/navigation` with nav item config.
2. Backend:
   - Loads latest event sequence for that `ApplicationId`.
   - Creates an event like `"NavigationItemAdded"`, with full nav item details in `EventData`.
   - **Insert into `appbuilder.application_events`** with incremented `SequenceNumber`.
3. A projection process (within AppBuilder) may:
   - Rebuild or incrementally maintain a “draft navigation read model” for fast UI queries (this can be an internal table or in‑memory cache; the source of truth is still events).

Edits (rename, move, hide) similarly produce `"NavigationItemUpdated"`, `"NavigationItemRemoved"` events, and the read model is recomputed from events.

#### 3. User adds / edits pages

1. Frontend calls  
   `POST /api/appbuilder/applications/{applicationId}/pages` with page config (route, layout, content JSON).
2. Backend:
   - Writes `"PageCreated"` or `"PageUpdated"` events to **`appbuilder.application_events`** with full content in `EventData` and next `SequenceNumber`.
3. Read models for “draft pages” are updated by replaying or incrementally applying these events.

#### 4. User adds data sources and settings

- Data sources: `"DataSourceAdded"`, `"DataSourceUpdated"` events into `appbuilder.application_events`.
- Application‑level settings (non‑event‑sourced or also event‑sourced, depending on how strict you want) typically:
  - **Insert/Update `appbuilder.application_settings`** rows.
  - Optionally record corresponding events (e.g., `"SettingChanged"`).

#### 5. User continues editing

- Every change in AppBuilder (pages, navigation, datasources, settings if event‑sourced) **appends a new `ApplicationEvent`**.  
- Draft state = **projection of the event stream** for that `ApplicationId`.

---

### Flow: release process (from draft to immutable release)

Assume we have a draft application fully configured via events.

#### 1. User triggers a release

1. Frontend calls:  
   `POST /api/appbuilder/applications/{applicationId}/releases` with target `Version` and `ReleaseNotes`.
2. Backend:

   **a. Load event stream and build snapshot**

   - Fetch all `ApplicationEvent` rows for `ApplicationId` ordered by `SequenceNumber`.
   - Replay them in memory to compute the **current configuration** (navigation tree, pages, data sources, schema).
   - Serialize the full config to JSON.

   - **Insert snapshot into `appbuilder.application_snapshots`**:
     - `ApplicationId`, `SnapshotData`, `SnapshotVersion`, `EventSequenceNumber = last sequence`.

   **b. Create immutable ApplicationRelease**

   - Validate semantic versioning rules (major/minor/patch).
   - Create `ApplicationRelease` record:
     - **Insert into `appbuilder.application_releases`** with:
       - `ApplicationId`
       - `Version` / `MajorVersion` / `MinorVersion` / `PatchVersion`
       - `ConfigurationSchemaVersion` (based on features used in config)
       - `ReleaseNotes`, `ReleasedBy`, `ReleasedAt`
       - `IsActive = true` (and mark previous active releases as inactive, if needed).

   **c. Materialize components for runtime**

   - From the in‑memory configuration (snapshot), produce per‑release component records:

   - For each nav item:
     - **Insert into `appbuilder.navigation_components`** with `ApplicationReleaseId = new release id` and nav properties.

   - For each page:
     - **Insert into `appbuilder.page_components`** with `ApplicationReleaseId` and page properties/content JSON.

   - For each data source:
     - **Insert into `appbuilder.data_source_components`** with `ApplicationReleaseId` and DS config.

   - Compute abstract schema from configuration (if used at platform level):
     - **Insert into `appbuilder.application_schemas`** with derived `SchemaDefinition` and `SchemaVersion`.

   **d. Update Application status and current release**

   - **Update `appbuilder.applications`**:
     - Set `Status = Released` if this is the first release (or keep `Released` if already released).
     - Set `CurrentReleaseId = new release id`.

#### 2. Post‑release behavior

- The combination of:
  - `ApplicationRelease`
  - `NavigationComponent` / `PageComponent` / `DataSourceComponent`
  - `ApplicationSchema`
  - Associated `ApplicationSnapshot`

  now forms an **immutable** view of that version of the app.

- If a bug is found:
  - You don’t edit the release. Instead:
    - Either:
      - Create a **new draft version** by replaying events up to the release and then appending new events (“fork from release”), then repeat the release process with incremented version (e.g., `1.0.1`).
    - Or **fork** conceptually at the Tenant level (for tenant customization), which is handled by the **TenantApplication** module but still consumes this release as a baseline.

---

### Short narrative: build + release in “table write” terms

- **Create app**  
  → `INSERT appbuilder.applications` (+ optional `INSERT appbuilder.application_events` “ApplicationCreated”).

- **Edit nav/pages/datasources**  
  → series of `INSERT appbuilder.application_events` for each change.  
  (Projections/read‑models updated internally.)

- **Release**  
  - Load events → build config →  
  → `INSERT appbuilder.application_snapshots`  
  → `INSERT appbuilder.application_releases`  
  → `INSERT appbuilder.navigation_components` (per nav item)  
  → `INSERT appbuilder.page_components` (per page)  
  → `INSERT appbuilder.data_source_components` (per data source)  
  → `INSERT appbuilder.application_schemas` (optional, if schema used)  
  → `UPDATE appbuilder.applications` (`Status`, `CurrentReleaseId`).

This gives you both:

- **Event‑sourced history** (everything that ever happened), and  
- **Relational, runtime‑friendly projections** for fast querying and execution by TenantApplication and AppRuntime.

---

**User**

appbuilder.application_events will become very big it would be better to have an event table per component like appbuilder.navigation_events, appbuilder.page_events, etc? yes or no explain why

---

**Cursor**

**No – keeping a single `appbuilder.application_events` (with good stream + indexing design) is usually better than splitting into `navigation_events`, `page_events`, etc.**

### Why a single event table is better

- **Event sourcing is stream‑centric, not table‑per-aggregate‑type**  
  The natural unit is “all events for `ApplicationId` in order”, regardless of whether a change affected navigation, pages, or data sources. A single table with `(ApplicationId, SequenceNumber)` makes it trivial to rebuild the full app state by replaying one ordered stream.

- **Cross‑component workflows need all events anyway**  
  Many projections (and the release process) must see *all* events in order:
  - Example: a page references a data source that was added earlier; removing that data source later should invalidate page bindings.  
  - With separate tables, you must merge multiple ordered streams (`navigation_events`, `page_events`, `data_source_events`) by timestamp/seq, which adds complexity and race conditions.

- **Simpler API surface and infrastructure**  
  - One event table → one generic append API, one generic reader, one generic projection pipeline.  
  - Multiple event tables → more code paths, more migration/maintenance overhead, more chances for inconsistency.

- **Scale is usually handled via partitioning and indexing, not table proliferation**  
  If `application_events` grows huge:
  - Partition by `ApplicationId`, or by time, or both.
  - Use composite indexes (`ApplicationId, SequenceNumber`, `ApplicationId, EventType`).
  - Archive old events to cold storage if needed.  
  These techniques preserve the clean stream model while still scaling.

- **Querying by component is still easy**  
  You can:
  - Filter by `EventType` (`WHERE EventType LIKE 'Navigation%'`)  
  - Or include a `ComponentType` / `Category` column.  
  That gives you efficient “navigation‑only” queries without separate tables.

### When multiple tables *might* be justified (edge cases)

Use multiple tables **only if** you have a very strong, measured need, such as:

- Different **write loads and retention policies** per component type (e.g., billions of log‑like events for one component that you want to prune independently).
- Very different **storage backends** per event category.

Even then, you usually solve it with **separate projections** (per‑component read models) rather than separate event stores.

---

**Conclusion**:  
**Keep a single `appbuilder.application_events`** (with good partitioning/indexing + clear `EventType` / `ComponentType`), and build per‑component read models on top. Multiple event tables per component add complexity and hurt the core event‑sourcing model more than they help.

---

**User**

question: how an user building an application can test it in draft mode? what will be the best approach to test navigation, pages, datasources?

---

**Cursor**

### Short answer

**Best approach**: use the *same AppRuntime* you’ll use in production, but add a **“preview” mode** where runtime can execute an **unreleased draft configuration** directly from AppBuilder (events/snapshot) without going through TenantApplication or real tenants.

That way:

- Navigation, pages, and data sources are tested by the *real runtime engine*.
- No fake releases, no noise in TenantApplication, no duplicate renderer logic.

Below is how that works in practice.

---

### 1. Core idea: Runtime “preview” mode for drafts

Add a **Preview API** in AppRuntime, e.g.:

- `POST /api/appruntime/preview/application/{applicationId}`  
- `POST /api/appruntime/preview/application/{applicationId}/page` (with `route`)

Backend flow:

1. **AppBuilder** projects current draft:
   - Reads latest `ApplicationEvent`s for `ApplicationId`.
   - Replays them → builds in‑memory **draft configuration** (nav tree, pages, data sources, schema).
   - Optionally caches a “draft snapshot” (not tied to a release).

2. AppBuilder calls **AppRuntime Preview API** with:
   - `ApplicationId`
   - Draft configuration payload (or an ID the runtime can fetch).
   - Optional: which page/route to preview.
   - A special **preview tenant context** (e.g., `TenantId = null` or a synthetic `preview-tenant`).

3. **AppRuntime**:
   - Validates draft config against its supported schema/version.
   - Builds **navigation model**, **page model**, and **data bindings** as usual.
   - Executes any **data source calls in “preview mode”** (see below).
   - Returns a JSON “view model” to the client (later frontend will render it).

No `ApplicationRelease`, no `TenantApplication`, no deployment — just direct draft execution.

---

### 2. Testing navigation (draft)

User actions:

1. In AppBuilder UI, user clicks “Preview navigation”.
2. Frontend calls: `POST /api/appruntime/preview/application/{applicationId}` (no specific page).

Runtime behavior:

- Builds the **navigation model** from draft:
  - Uses all draft navigation events → computed nav tree.
  - Validates that every `Route` has a corresponding page.
- Returns navigation structure:
  - Labels, icons, routes, hierarchy.

What this gives you:

- Validate menu structure, nesting, ordering.
- Detect broken routes (nav pointing to missing pages) *before* release.

---

### 3. Testing pages (draft)

User actions:

1. In AppBuilder UI, on a page editor, user clicks “Preview page”.
2. Frontend calls:  
   `POST /api/appruntime/preview/application/{applicationId}/page` with:
   - `route` (e.g., `/dashboard`)
   - Optional preview parameters (filters, test user, etc.).

Runtime behavior:

- From draft config:
  - Find `PageComponent` for that `route`.
  - Resolve layout + `Content` JSON into an internal render tree.
- Runs usual validation:
  - All referenced components exist and are supported by current runtime.
  - All bindings reference existing data sources/fields.
- Optionally executes read‑only data fetch (see next section).
- Returns:
  - Page layout model.
  - Component tree.
  - Validation warnings/errors.
  - Optional sample data.

Result: developer sees exactly how the runtime *would* render the page, even though the app is still draft.

---

### 4. Testing data sources (draft)

There are two complementary ways:

#### A. Direct “Test data source” endpoints (AppBuilder)

For each `DataSourceComponent`:

- `POST /api/appbuilder/applications/{applicationId}/datasources/{dataSourceCode}/test-connection`
- `POST /api/appbuilder/applications/{applicationId}/datasources/{dataSourceCode}/test-query` (with optional filter, sample query)

Backend:

- Reads draft data source config from events.
- Opens a **read‑only** connection to the configured target.
- Runs:
  - A simple “ping” / `SELECT 1` or metadata query.
  - For test‑query: a limited sample query with `TOP N` / `LIMIT`.

This lets users validate:

- Credentials / connection strings.
- That the schema definition matches reality.
- That queries return what they expect.

#### B. Data source use in page preview (AppRuntime)

When previewing a page:

- Runtime sees bound data sources in the page config.
- For each:
  - Calls the same “test” path or a shared **read‑only execution mode**.
  - Returns **sample rows** (truncated) into the preview model.

This gives end‑to‑end validation:

- Page bindings → data source → actual data.

---

### 5. Why this is better than other options

- **Better than creating fake releases**:  
  No version noise, no accidental tenant installs, no need to clean up fake versions.

- **Better than embedding a mini runtime in AppBuilder**:  
  - No duplicated rendering/business logic.
  - One place (AppRuntime) holds all compatibility rules and component behavior.

- **Consistent with production**:  
  - The same runtime code path is used for:
    - tenant/production apps (via `TenantApplication` + `ApplicationRelease`), and  
    - draft previews (via direct draft config).
  - Fewer “it worked in preview but not in production” surprises.

---

### 6. Quick conceptual flow summary

**Draft preview flow (navigation/pages/data sources)**

1. User edits app in AppBuilder → events appended.
2. User clicks “Preview”.
3. AppBuilder:
   - Replays events → draft config.
   - Calls AppRuntime Preview API with draft config.
4. AppRuntime:
   - Validates config (schema + runtime compatibility).
   - Builds nav/page/data source models.
   - Optionally runs read‑only data queries.
5. Returns a structured preview model → frontend renders it.

This model lets users thoroughly test navigation, pages, and data sources **while the application is still in Draft**, without polluting TenantApplication or releases.

---

**User**

the appruntime needs any db table?

---

**Cursor**

### Short answer

**Yes, AppRuntime should have its own tables**, but you can keep the *minimal* set fairly small if you want the runtime to be mostly stateless.

### What absolutely makes sense to persist

- **`RuntimeVersion` (+ component support)**  
  - **Why**: You need a durable record of which runtime versions exist and **what component types/schema versions they support**, so:
    - TenantApplication can call `ICompatibilityCheckService` and get consistent answers.
    - Ops can see “what is currently deployed” and roll back if needed.
  - Typical tables:
    - `appruntime.runtime_versions` – id, version, is_current, released_at, release_notes.
    - `appruntime.component_type_support` – which component types/versions each runtime version supports.

- **`RuntimeInstance` (optional but recommended)**  
  - **Why**: Track which tenant apps are currently running on which runtime version, for:
    - Debugging (“why is tenant X seeing this behavior?”).
    - Operational views (how many instances, which environments).
    - Historical analysis and troubleshooting.
  - Table:
    - `appruntime.runtime_instances` – tenant, tenant_app, application_release, runtime_version, status, started_at, stopped_at, health info, last compatibility result, etc.

### What is nice‑to‑have (depends on how “operational” you want Runtime to be)

- **Metrics & logs** (can also go to external observability stack):
  - `appruntime.runtime_metrics`
  - `appruntime.runtime_logs`  
  These are useful if you want module‑local observability instead of (or in addition to) Prometheus / OpenTelemetry.

### What AppRuntime does *not* need to store

- **Application configuration** itself (navigation/pages/datasources).  
  That lives in **AppBuilder** (and TenantApplication for tenant forks). Runtime just **reads** the released configuration (or draft preview) and executes it.

---

So: **AppRuntime doesn’t need to duplicate app config tables**, but it **does benefit from its own schema** for runtime versions, compatibility metadata, and (optionally) instances/metrics/logs.
