## AppBuilder Module – API, Migrations & Composition Plan

This plan covers:

- HTTP API surface for backend clients (AppBuilder UI later; currently backend‑only).
- Database migrations for the `appbuilder` schema.
- Module composition and hosting.

---

## 1. API layer

### 1.1 Goals

- Provide clean HTTP endpoints for:
  - Managing applications and releases.
  - Editing configuration via event‑append commands.
  - Reading draft and release configuration.
  - Draft preview flows (navigation/pages/data sources).
- Keep controllers **thin** – delegate to MediatR commands/queries.

---

### 1.2 Controllers

Namespace: `AppBuilder.Api.Controllers`

#### 1.2.1 `ApplicationController`

**Route base**: `api/appbuilder/applications`

Endpoints:

- `GET /{id:guid}`
  - Query: `GetApplicationByIdQuery`.
- `GET /`
  - Query: `GetAllApplicationsQuery`.
- `POST /`
  - Body: `CreateApplicationCommand`.
  - Responses: `201 Created` (id), `400/409` on validation/conflict.
- `PUT /{id:guid}`
  - Body: `UpdateApplicationCommand` (with `Id` taken from route).
- `POST /{id:guid}/archive`
  - Body: none.
  - Command: `ArchiveApplicationCommand`.
- `GET /{id:guid}/releases`
  - Query: `GetReleasesByApplicationQuery`.

#### 1.2.2 `ReleaseController`

**Route base**: `api/appbuilder/applications/{applicationId:guid}/releases`

Endpoints:

- `POST /`
  - Body: `ReleaseRequest` → mapped to `ReleaseApplicationCommand`.
  - Returns: new `releaseId`.
- `GET /{releaseId:guid}`
  - Returns `ApplicationReleaseDto` plus optional configuration summary.
- `GET /{releaseId:guid}/navigation`
  - Query: `GetNavigationByReleaseQuery`.
- `GET /{releaseId:guid}/pages`
  - Query: `GetPagesByReleaseQuery`.
- `GET /{releaseId:guid}/datasources`
  - Query: `GetDataSourcesByReleaseQuery`.
- `GET /{releaseId:guid}/schema`
  - Query: `GetApplicationSchemaByReleaseQuery`.

#### 1.2.3 `DraftConfigurationController`

**Route base**: `api/appbuilder/applications/{applicationId:guid}/draft`

Endpoints:

- `GET /navigation`
  - Query: `GetApplicationDraftNavigationQuery`.
- `GET /pages`
  - Query: `GetApplicationDraftPagesQuery`.
- `GET /datasources`
  - Query: `GetApplicationDraftDataSourcesQuery`.

These endpoints replay the event stream and return **current draft** state, not release data.

#### 1.2.4 `DraftEventController`

**Route base**: `api/appbuilder/applications/{applicationId:guid}/events`

Endpoints:

- `POST /navigation`
  - Body: payload describing nav change.
  - Command: `AppendNavigationEventCommand`.
- `POST /pages`
  - Body: page event payload.
  - Command: `AppendPageEventCommand`.
- `POST /datasources`
  - Body: data source event payload.
  - Command: `AppendDataSourceEventCommand`.

These are the write‑endpoints for the event stream.

#### 1.2.5 `DraftPreviewController`

**Route base**: `api/appbuilder/applications/{applicationId:guid}/preview`

Endpoints:

- `GET /application`
  - Query: `GetDraftApplicationPreviewQuery`.
  - Returns `ApplicationDraftPreviewDto`.
- `GET /pages`
  - Query parameters: `route`.
  - Query: `GetDraftPagePreviewQuery`.
  - Returns `PageDraftPreviewDto`.

These are the endpoints the future AppBuilder UI will use to show preview using backend‑built models.

---

### 1.3 API conventions

- Use standard REST verbs:
  - `GET` for queries.
  - `POST` for create/append operations.
  - `PUT` for updates.
  - `POST` for actions (`/archive`, `/preview`).
- Use `Result<T>` from Application layer to map to HTTP:
  - `ErrorType.Validation` → `400 Bad Request`.
  - `ErrorType.Conflict` → `409 Conflict`.
  - `ErrorType.NotFound` → `404 Not Found`.
  - Default → `500 Internal Server Error`.
- Add Swagger annotations/XML docs for all public endpoints.

---

## 2. Migrations layer

Namespace: `Datarizen.AppBuilder.Migrations`

Project: `AppBuilder.Migrations`

Use **FluentMigrator** to manage schema. Declare dependency on `Feature` (as in existing plan) if needed.

### 2.1 Schema creation

- **Migration**: `20260211200000_CreateAppBuilderSchema`
  - Create schema `"appbuilder"`.

### 2.2 Core tables

- `20260211201000_CreateApplicationsTable`
  - As in the existing all‑layer plan (see current file), with:
    - PK `id`.
    - `name`, `slug`, `description`, `status`, `current_release_id`, timestamps.
    - Unique constraint on `slug`.

- `CreateApplicationReleasesTable`
  - Columns:
    - `id`, `application_id`, `version`, `major_version`, `minor_version`, `patch_version`,
      `release_notes`, `configuration_schema_version`, `released_at`, `released_by`,
      `is_active`, `created_at`.
    - FKs and indexes described in the Infrastructure plan.

- `CreateNavigationComponentsTable`
- `CreatePageComponentsTable`
- `CreateDataSourceComponentsTable`
- `CreateApplicationSettingsTable`

Each with columns and indexes matching the Infrastructure plan.

### 2.3 Event store & snapshots

- `CreateApplicationSnapshotsTable`
  - Fields for `application_id`, `application_release_id`, `snapshot_data`, `snapshot_version`,
    `event_sequence_number`, `created_at`.

- `CreateApplicationEventsTable`
  - Fields for `application_id`, `event_type`, `event_data`, `event_version`,
    `sequence_number`, `occurred_at`, `occurred_by`.
  - Composite index on `(application_id, sequence_number)` (unique).

- `CreateApplicationSchemasTable`
  - Fields for `application_release_id`, `schema_definition`, `schema_version`, `created_at`.

### 2.4 Data seeding (optional)

- Seed nothing or minimal demo apps; the plan can include:
  - A future migration to seed a “sample app” for testing AppRuntime.

---

## 3. Module composition & hosting

Project: `AppBuilder.Module`

### 3.1 `AppBuilderModule` implementation

**File**: `AppBuilder.Module/AppBuilderModule.cs`

Implements `IModule`:

- `ModuleName => "AppBuilder"`.
- `SchemaName => "appbuilder"`.
- `GetMigrationDependencies() => ["Feature"]` (only if AppBuilder migrations must run after Feature).

`RegisterServices`:

- `services.AddAppBuilderApplication();`
- `services.AddAppBuilderInfrastructure(configuration, SchemaName);`
- `services.AddControllers().AddApplicationPart(typeof(ApplicationController).Assembly);`

`ConfigureMiddleware`:

- No special middleware initially; just return `app`.

### 3.2 Service host for microservices topology

Project: `Hosts/AppBuilderServiceHost`

- `Program.cs`:
  - Use `builder.AddServiceDefaults();`
  - Register building blocks, health checks.
  - `builder.Services.AddModule<AppBuilderModule>(builder.Configuration);`
  - Enable Swagger.
  - Map controllers and health checks.

### 3.3 Topology support

- **Monolith**:
  - AppHost includes `AppBuilderModule` among loaded modules.
- **MultiApp**:
  - AppBuilder hosted in dedicated `MultiAppAppBuilderHost`, API gateway routes `/api/appbuilder/*`.
- **Microservices**:
  - Standalone `AppBuilderServiceHost` service.

---

## 4. API & migrations tasks

1. Add `AppBuilder.Api` project (if not already present) and controllers:
   - `ApplicationController`, `ReleaseController`, `DraftConfigurationController`,
     `DraftEventController`, `DraftPreviewController`.
2. Wire controllers to Application layer commands/queries using MediatR.
3. Implement error‑to‑HTTP mapping helpers (or reuse shared ones).
4. Create FluentMigrator migrations for:
   - Schema.
   - All AppBuilder tables.
   - Initial indexes and constraints.
5. Implement `AppBuilderModule` and `AppBuilderServiceHost` program as described.

