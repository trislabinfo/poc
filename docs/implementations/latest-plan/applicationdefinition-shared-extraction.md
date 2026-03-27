# ApplicationDefinition – Shared Code Extraction (AppBuilder & TenantApplication)

**Status**: Implemented (Contracts + Application; CRUD endpoints in modules not yet added)  
**Last Updated**: 2026-02-15  

---

## Goal

Identify shared code between **AppBuilder** (API + Application) and **TenantApplication** (API + Application) that should live in **ApplicationDefinition** (Contracts, Application, or optionally Api/Infrastructure) so both modules stay aligned and the same UI can call either API.

---

## Current State

| Layer | AppBuilder | TenantApplication | ApplicationDefinition |
|-------|------------|-------------------|------------------------|
| **Domain** | Own aggregate (ApplicationDefinition) + **duplicate** definition entities (EntityDefinition, PropertyDefinition, PageDefinition, etc.) in AppBuilder.Domain | Uses **ApplicationDefinition.Domain** shared entities (TenantApplication aggregate + shared definition types) | **ApplicationDefinition.Domain** only: shared entities + repo interfaces |
| **Application** | ApplicationDefinitionDto, CreateApplicationDefinition command/validator/handler, GetApplicationDefinitionById, mapper | TenantApplicationDto, InstallApplication, CreateCustomApplication, GetTenantApplications. **No definition CRUD yet** | None |
| **API** | ApplicationDefinitionsController, Create/UpdateApplicationDefinitionRequest. **No entity/property/page CRUD yet** | TenantApplicationController (install, list, get, etc.). **No definition CRUD yet** (planned in tenantapplication-api.md) | None |
| **Contracts** | AppBuilder.Contracts (referenced; may be empty) | None | None |
| **Infrastructure** | AppBuilderDbContext, appbuilder schema, own repo implementations | TenantApplicationDbContext, tenantapplication schema, configs for shared definition entities | None |

**Finding**: AppBuilder has **not** yet adopted ApplicationDefinition.Domain for definition entities; it still has its own copies in AppBuilder.Domain. TenantApplication already uses ApplicationDefinition.Domain. When both modules implement the full definition CRUD described in appbuilder-api.md and tenantapplication-api.md, **request/response shapes and validation rules will be the same** so the same UX can call either API.

---

## Planned Overlap (from API docs)

When definition CRUD is implemented in both modules:

| Resource | AppBuilder route | TenantApplication route | Same request/response shape? |
|----------|------------------|-------------------------|------------------------------|
| Entities | `api/appbuilder/entities`, `.../applications/{id}/entities` | `api/tenantapplication/tenants/{tid}/applications/{taid}/entities` | Yes (name, displayName, description, attributes, primaryKey) |
| Properties | under entity | under entity | Yes (name, displayName, dataType, isRequired, order) |
| Relations | `api/appbuilder/relations` | `.../relations` | Yes (targetEntityId, name, relationType, cascadeDelete) |
| Navigation | `api/appbuilder/navigations` | `.../navigation` | Yes (name, configurationJson) |
| Pages | `api/appbuilder/pages` | `.../pages` | Yes (name, route, configurationJson) |
| Data sources | `api/appbuilder/datasources` | `.../datasources` | Yes |
| Releases | `api/appbuilder/.../releases` | `.../releases` | Yes (version, releaseNotes, etc.) |

Tenantapplication-api.md states: *"Request/response models should align with AppBuilder's DTOs so the same UI components can call either AppBuilder or TenantApplication API."*

---

## Recommended Extraction

### 1. **ApplicationDefinition.Contracts** (high confidence)

**Purpose**: Shared DTOs and request/response types for definition resources so both APIs expose the same shapes.

**Move here**:

- **Response DTOs** (for GET by id, list):
  - `EntityDefinitionDto` (Id, ApplicationDefinitionId, Name, DisplayName, Description, AttributesJson, PrimaryKey, CreatedAt, UpdatedAt)
  - `PropertyDefinitionDto` (Id, EntityDefinitionId, Name, DisplayName, DataType, IsRequired, Order, …)
  - `RelationDefinitionDto`, `NavigationDefinitionDto`, `PageDefinitionDto`, `DataSourceDefinitionDto`, `ApplicationReleaseDto` (or equivalent)
- **Request DTOs** (for POST/PUT):
  - `CreateEntityRequest`, `UpdateEntityRequest`
  - `CreatePropertyRequest`, `UpdatePropertyRequest`
  - Same for Relation, Navigation, Page, DataSource, Release where applicable
- **Enums** used in API: can re-export or reference `ApplicationDefinition.Domain` enums (PropertyDataType, RelationType, DataSourceType) so API and UI have one source of truth.

**Referenced by**: AppBuilder.Api, TenantApplication.Api, and optionally AppBuilder.Application, TenantApplication.Application (if commands take these request types).

**Not in Contracts**: Module-specific types (e.g. ApplicationDefinitionDto that includes AppBuilder-specific Status/CurrentVersion, or TenantApplicationDto). Only the **definition resource** DTOs that are identical between both APIs.

---

### 2. **ApplicationDefinition.Application** (high confidence)

**Purpose**: Shared mappers and validators for definition resources so both modules use the same rules and mapping logic.

**Move here**:

- **Mappers** (domain entity → DTO):
  - `EntityDefinitionMapper.ToDto(EntityDefinition)` → `EntityDefinitionDto`
  - Same for PropertyDefinition, PageDefinition, NavigationDefinition, RelationDefinition, DataSourceDefinition, ApplicationRelease (using types from ApplicationDefinition.Domain and ApplicationDefinition.Contracts).
- **Validators** (FluentValidation):
  - Rules for “create/update entity” (Name required, max 100; DisplayName required, max 200; Description max length; etc.).
  - Rules for “create/update property” (Name, DisplayName, DataType, Order, etc.).
  - Same for other definition resources. These can be **validator components** or **base validators** that AppBuilder and TenantApplication command validators use (e.g. RuleSet or Include). Commands themselves stay in each module (CreateEntityCommand in AppBuilder has ApplicationId; in TenantApplication has TenantId + TenantApplicationId).

**Referenced by**: AppBuilder.Application, TenantApplication.Application.

**Do not move**: Command/query handlers (they use IAppBuilderUnitOfWork vs ITenantApplicationUnitOfWork and different repository scope). Only mappers and reusable validation rules.

---

### 3. **ApplicationDefinition.Api** (low priority / optional)

**Purpose**: Not a runnable API (routes differ). At most, shared API-surface helpers.

**Optional contents**:

- A **base controller** or **response helper** that both ApplicationDefinitionsController (AppBuilder) and a future TenantApplication definition controller use (e.g. shared `ToActionResult(Result<T>)` or standard response envelope). BuildingBlocks already provide `BaseCrudController`; any extra shared logic here would be thin.
- If we don’t add anything, both APIs can keep using their own controllers and only share types from **ApplicationDefinition.Contracts** and **ApplicationDefinition.Application**. Recommendation: **skip ApplicationDefinition.Api** unless we later find repeated controller code.

---

### 4. **ApplicationDefinition.Infrastructure** (not recommended for now)

**Purpose**: Shared persistence for definition data.

**Why not extract**:

- AppBuilder uses **appbuilder** schema and **application_definition_id**.
- TenantApplication uses **tenantapplication** schema and **tenant_application_id** (same logical “parent” but different column names and schemas).
- DbContexts, EF configurations, and repository implementations are schema-specific. Sharing would require abstracting parent ID and table names, which adds complexity for little gain.

**Revisit only if**: We later have concrete shared infrastructure (e.g. a shared SQL fragment, migration helper, or read model) that both modules need.

---

## Implementation Order

1. **Introduce ApplicationDefinition.Contracts**
   - Add project under `server/src/Product/ApplicationDefinition/ApplicationDefinition.Contracts`.
   - Define shared request/response DTOs for definition resources (Entity, Property, Relation, Navigation, Page, DataSource, Release) as they are implemented. Start with Entity + Property if that’s the first CRUD to build.
   - AppBuilder.Api and TenantApplication.Api (and their Application layers if needed) reference Contracts.

2. **Introduce ApplicationDefinition.Application**
   - Add project under `server/src/Product/ApplicationDefinition/ApplicationDefinition.Application`.
   - Reference ApplicationDefinition.Domain and ApplicationDefinition.Contracts.
   - Add mappers (entity → DTO) and shared FluentValidation validators for definition commands. AppBuilder.Application and TenantApplication.Application reference this project and use the mappers/validators.

3. **Refactor AppBuilder to use ApplicationDefinition.Domain**
   - Remove duplicate definition entities from AppBuilder.Domain; use ApplicationDefinition.Domain instead. Keep only AppBuilder-specific aggregate (e.g. ApplicationDefinition with Status, CurrentVersion) in AppBuilder.Domain if it differs from what TenantApplication needs.
   - Align AppBuilder.Infrastructure with ApplicationDefinition.Domain entities (already planned in docs).

4. **Implement definition CRUD in both modules**
   - AppBuilder: add entity/property/page/… CRUD using ApplicationDefinition.Contracts DTOs and ApplicationDefinition.Application mappers/validators.
   - TenantApplication: add tenant-scoped definition CRUD using the same DTOs and validators; only context (tenantId, tenantApplicationId) and authorization differ.

5. **ApplicationDefinition.Api / Infrastructure**
   - Add ApplicationDefinition.Api only if we find clear duplicated controller or response logic.
   - Do not add ApplicationDefinition.Infrastructure unless we identify concrete shared persistence needs.

---

## Summary Table

| Project | Contents | Referenced by |
|---------|----------|----------------|
| **ApplicationDefinition.Contracts** | Request/response DTOs for definition resources (Entity, Property, Relation, Navigation, Page, DataSource, Release) | AppBuilder.Api, TenantApplication.Api, optionally both Application projects |
| **ApplicationDefinition.Application** | Mappers (entity → DTO), shared FluentValidation rules for definition create/update | AppBuilder.Application, TenantApplication.Application |
| **ApplicationDefinition.Api** | Optional: shared base controller or response helpers | Optional: AppBuilder.Api, TenantApplication.Api |
| **ApplicationDefinition.Infrastructure** | Not recommended | — |

This keeps a single definition of “what an entity/property/page looks like” in API and validation, while leaving commands, queries, and persistence in each module.
