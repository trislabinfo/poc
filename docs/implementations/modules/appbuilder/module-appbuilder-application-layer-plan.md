## AppBuilder Module – Application Layer Implementation Plan

### 1. Application layer goals

- Expose **use‑case oriented commands and queries** over the domain model.
- Orchestrate **event‑sourced configuration edits** and **release creation**.
- Provide **preview endpoints** for draft applications (for AppRuntime preview).
- Keep all AppBuilder logic backend‑only (UI will be added later).

---

### 2. DTOs

Namespace: `AppBuilder.Application.DTOs`

- `ApplicationDto`
  - Mirrors `Application` metadata (Id, Name, Slug, Description, Status, CurrentReleaseId, CreatedAt, UpdatedAt).
- `ApplicationReleaseDto`
  - Id, ApplicationId, Version, ReleaseNotes, ReleasedBy, ReleasedAt, IsActive, CreatedAt, ConfigurationSchemaVersion.
- `NavigationComponentDto`
  - Id, ApplicationReleaseId, Label, Icon, Route, ParentId, DisplayOrder, IsVisible, CreatedAt.
- `PageComponentDto`
  - Id, ApplicationReleaseId, Title, Route, Layout, Content, CreatedAt.
- `DataSourceComponentDto`
  - Id, ApplicationReleaseId, DataSourceCode, DataSourceType, ConnectionConfig (redacted), SchemaDefinition, CreatedAt.
- `ApplicationSettingDto`
  - Id, ApplicationId, Key, Value, DataType, IsEncrypted, CreatedAt, UpdatedAt.
- `ApplicationSchemaDto`
  - ApplicationReleaseId, SchemaDefinition, SchemaVersion, CreatedAt.
- `ApplicationDraftPreviewDto`
  - Navigation tree (simple DTO), list of pages (with minimal preview info), list of data sources, validation messages.
- `PageDraftPreviewDto`
  - Page layout/content model plus optional sample data summary and validation messages.

Mappers: static classes to map domain entities and projected models into these DTOs.

---

### 3. Commands – Application lifecycle

Namespace: `AppBuilder.Application.Commands.Applications`

#### 3.1 `CreateApplication`

- **Command**
  - `CreateApplicationCommand(string Name, string Slug, string Description) : ICommand<Guid>`
- **Handler responsibilities**
  - Validate slug uniqueness via `IApplicationRepository.SlugExistsAsync`.
  - Call `Application.Create(...)`.
  - Save new `Application` via `IApplicationRepository` + `IAppBuilderUnitOfWork`.

#### 3.2 `UpdateApplication`

- **Command**
  - `UpdateApplicationCommand(Guid Id, string Name, string Description) : ICommand<Unit>`
- **Handler responsibilities**
  - Load `Application` by id.
  - Call `Update(...)` (only allowed in Draft).
  - Persist and commit.

#### 3.3 `ArchiveApplication`

- **Command**
  - `ArchiveApplicationCommand(Guid Id) : ICommand<Unit>`
- **Handler responsibilities**
  - Load application, call `Archive`, update, commit.

#### 3.4 `ReleaseApplication`

> This is where event‑sourcing and snapshotting are orchestrated.

- **Command**
  - `ReleaseApplicationCommand(Guid ApplicationId, string Version, string ReleaseNotes, Guid ReleasedBy) : ICommand<Guid>`
- **Handler responsibilities**
  1. Load `Application` by id (must be `Draft`).
  2. Use `IApplicationEventStream` to load all `ApplicationEvent`s for this app.
  3. Replay events into an **in‑memory configuration model** (navigation, pages, data sources, schema).
  4. Create `ApplicationSnapshot` via a domain service and persist it.
  5. Create `ApplicationRelease` with semantic version validation.
  6. Materialize and persist:
     - `NavigationComponent` instances.
     - `PageComponent` instances.
     - `DataSourceComponent` instances.
     - `ApplicationSchema`.
  7. Call `application.Release(releaseId, dateTimeProvider)`; update and save.
  8. Return new `releaseId`.

Add a FluentValidation validator to enforce non‑empty version and valid semver format.

---

### 4. Commands – Event‑sourced configuration editing

Namespace: `AppBuilder.Application.Commands.Configuration`

These commands do **not** update relational tables directly; they **append events** to `appbuilder.application_events`. Projections will use them.

#### 4.1 `AppendNavigationEvent`

- `AppendNavigationEventCommand(Guid ApplicationId, NavigationEventPayload Payload) : ICommand<long>`  
  - `Payload` contains label, icon, route, parentId, displayOrder, etc.
- Handler:
  - Get last `SequenceNumber` for application stream.
  - Create `ApplicationEvent` with `EventType` like `"NavigationItemAdded"` / `"NavigationItemUpdated"` / `"NavigationItemRemoved"`.
  - Append via `IApplicationEventStream.AppendAsync`.
  - Return new `SequenceNumber`.

#### 4.2 `AppendPageEvent`

Similarly:

- `AppendPageEventCommand(Guid ApplicationId, PageEventPayload Payload) : ICommand<long>`
  - For creating/updating/removing pages, content, layout.

#### 4.3 `AppendDataSourceEvent`

- `AppendDataSourceEventCommand(Guid ApplicationId, DataSourceEventPayload Payload) : ICommand<long>`

#### 4.4 (Optional) `AppendSettingEvent`

- If you decide to event‑source settings as well (optional in DB; mandatory in events).

> Draft read models (for AppBuilder UI) are built by replaying these events; release uses the same stream.

---

### 5. Queries

Namespace: `AppBuilder.Application.Queries`

#### 5.1 Application queries

- `GetApplicationByIdQuery(Guid Id) : IQuery<ApplicationDto>`
- `GetAllApplicationsQuery() : IQuery<IEnumerable<ApplicationDto>>`
- `GetReleasesByApplicationQuery(Guid ApplicationId) : IQuery<IEnumerable<ApplicationReleaseDto>>`

Handlers:

- Use repositories (`IApplicationRepository`, `IApplicationReleaseRepository`) and map to DTOs.

#### 5.2 Draft configuration queries

These are for the AppBuilder UI to show the **current draft** without hitting releases.

- `GetApplicationDraftNavigationQuery(Guid ApplicationId)`  
  - Handler:
    - Load all events via `IApplicationEventStream`.
    - Replay only navigation‑related events.
    - Build a navigation tree DTO for the UI.

- `GetApplicationDraftPagesQuery(Guid ApplicationId)`  
  - Similar, but focuses on page events.

- `GetApplicationDraftDataSourcesQuery(Guid ApplicationId)`

These queries operate purely on events (no dependency on `ApplicationRelease`).

#### 5.3 Release‑based component queries

- `GetNavigationByReleaseQuery(Guid ApplicationReleaseId) : IQuery<IEnumerable<NavigationComponentDto>>`
- `GetPagesByReleaseQuery(Guid ApplicationReleaseId) : IQuery<IEnumerable<PageComponentDto>>`
- `GetDataSourcesByReleaseQuery(Guid ApplicationReleaseId) : IQuery<IEnumerable<DataSourceComponentDto>>`
- `GetApplicationSchemaByReleaseQuery(Guid ApplicationReleaseId) : IQuery<ApplicationSchemaDto>`

These are used by TenantApplication or ops tools when inspecting released versions.

---

### 6. Preview commands/queries (for draft testing)

Namespace: `AppBuilder.Application.Preview`

#### 6.1 `GetDraftApplicationPreview`

- `GetDraftApplicationPreviewQuery(Guid ApplicationId) : IQuery<ApplicationDraftPreviewDto>`
- Handler:
  - Load all events for `ApplicationId`.
  - Replay into a unified in‑memory config.
  - Run basic validations (e.g., pages without nav entries, nav entries pointing to missing pages).
  - Populate `ApplicationDraftPreviewDto` (nav tree + page summary + data source summary + validation warnings).

#### 6.2 `GetDraftPagePreview`

- `GetDraftPagePreviewQuery(Guid ApplicationId, string Route) : IQuery<PageDraftPreviewDto>`
- Handler:
  - Replay events, extract specific page config and its bound data sources.
  - Optionally call a **contracts‑level “preview data” service** to return sample data (read‑only).
  - Populate DTO with:
    - Page layout & content.
    - Data binding metadata.
    - Validation messages.

These queries are designed to be consumed by AppRuntime’s preview API later, but remain in the AppBuilder Application layer to keep AppRuntime focused on execution.

---

### 7. Integration contracts

Namespace: `AppBuilder.Contracts`

- `ApplicationConfigurationDto`
  - Summarized, release‑based configuration for other modules.
- `IApplicationReleaseService`
  - Methods such as:
    - `Task<ApplicationConfigurationDto?> GetReleaseConfigurationAsync(Guid applicationReleaseId, CancellationToken)`
    - `Task<bool> ReleaseExistsAsync(Guid applicationReleaseId, CancellationToken)`

These contracts are used by:

- **TenantApplication** – when installing or validating applications.
- **AppRuntime** – when resolving the full configuration for execution.

---

### 8. Application layer tasks

1. Define DTOs and mappers for:
   - `Application`, `ApplicationRelease`, components, settings, schema, and previews.
2. Implement commands:
   - `CreateApplication`, `UpdateApplication`, `ArchiveApplication`, `ReleaseApplication`.
   - Event‑append commands for navigation, pages, data sources (and optionally settings).
3. Implement queries:
   - Metadata (`GetApplicationById`, `GetAllApplications`, `GetReleasesByApplication`).
   - Draft views via event replay.
   - Release‑based queries via repositories.
   - Draft preview queries.
4. Add FluentValidation validators for commands (especially release + slug/name).
5. Wire MediatR pipeline behaviors as needed (e.g., transaction behavior with `IAppBuilderUnitOfWork`).
6. Add unit tests for handlers, focusing on:
   - Correct event appends.
   - Correct snapshot/release creation.
   - Correct reconstruction of draft configuration in preview queries.

