# Application Definition Refactoring — Implementation Plan

**Audience**: Developers (implementation guidance from an architect)  
**Reference**: [draft.txt](./draft.txt) (target shape for application definition)  
**Scope**: Refactor current application definition **in place** toward the end goal. One step per concept; each step updates one area and the full vertical slice across ApplicationDefinition (core), AppBuilder, and TenantApplication.  
**Solution**: `server/Datarizen.sln` — every step must end with a successful build.

---

## 1. Principles and Constraints

- **Refactor, do not replace.** Evolve existing entities (EntityDefinition, PropertyDefinition, etc.) and their persistence to match draft.txt. Do not delete and replace with a single JSON document unless a later step explicitly consolidates to Authoring JSON.
- **Breaking changes allowed.** No backward compatibility; existing data will be removed and DB recreated.
- **One step per concept.** Each step refactors one main concept (e.g. EntityDefinition) and includes the **whole vertical slice**: ApplicationDefinition.Domain (entity file), ApplicationDefinition.Application (mappers, validators), ApplicationDefinition.Contracts (DTOs, requests), AppBuilder (commands, queries, handlers, repositories, configurations, API, migrations), TenantApplication (infrastructure, configurations, migrations, services that use the entity).
- **Build gate.** After each step, `dotnet build server/Datarizen.sln` must succeed.
- **Guidance.** Follow [docs/ai-context](../../ai-context) (solution structure, server coding conventions, DB migration flow).

---

## 2. Current vs Target (Summary)

| Concept | Current (key files) | Target (draft.txt) |
|--------|----------------------|--------------------|
| **Entity** | `EntityDefinition.cs`: Name, DisplayName, Description, AttributesJson, PrimaryKey | id (string), displayName, tenantScoped, properties[], relations[], calculatedFields[] |
| **Property** | `PropertyDefinition.cs`: Name, DisplayName, DataType, IsRequired, DefaultValue, ValidationRulesJson, Order | name, type, required, readonly, default, length, precision, scale, expression, persist |
| **Relation** | `RelationDefinition.cs`: SourceEntityId, TargetEntityId, Name, RelationType, CascadeDelete | name, type, target (entity id), inverse, required, tenantScoped |
| **CalculatedField** | (none) | name, type, calculated, expression, persist |
| **Page** | `PageDefinition.cs`: Name, Route, ConfigurationJson | id, type (EntityList\|EntityEdit\|Custom), entity, layout, listConfig, fieldOverrides, permissions |
| **Navigation** | `NavigationDefinition.cs`: Name, ConfigurationJson | NavigationItem: id, label, page, children, tenantId |
| **Release** | `ApplicationRelease`: multiple JSON columns | ReleaseSnapshot: Metadata (copy of Authoring JSON), ValidationStatus |

---

## 3. Implementation Steps (Refactor by Concept)

Each step names the **exact files** to change and what to change. Migrations are per-step where schema changes.

---

### Step 1: Refactor EntityDefinition to draft shape (full vertical slice)

**Goal:** Align `EntityDefinition` with draft §1.2: id (string), displayName, tenantScoped. Properties and relations remain separate entities for now (refactored in later steps); this step only changes the entity definition root.

**1.1 ApplicationDefinition.Domain**

- **File:** `server\src\Product\ApplicationDefinition\ApplicationDefinition.Domain\Entities\EntityDefinition.cs`
  - Keep `Id` (Guid) and `ApplicationDefinitionId` for persistence.
  - **Rename:** `Name` → keep as the unique entity identifier; in draft this is `id` (string). Ensure it is the logical “entity id” (e.g. "Customer", "Order").
  - **Rename:** `DisplayName` → keep; draft uses `displayName`.
  - **Add:** `TenantScoped` (bool). Default true for tenant-scoped entities.
  - **Remove or deprecate:** `Description`, `AttributesJson`, `PrimaryKey` (draft entity has no these; remove for clean refactor).
  - Update `Create(...)` and `Update(...)` signatures and validation accordingly (e.g. validate `Name` as entity id format if needed).
- **File:** `server\src\Product\ApplicationDefinition\ApplicationDefinition.Domain\Repositories\IEntityDefinitionRepository.cs`  
  - No signature change unless you add a method like `GetByIdentityIdAsync(applicationDefinitionId, string entityId)` for draft `id` lookup.

**1.2 ApplicationDefinition.Contracts**

- **File:** `server\src\Product\ApplicationDefinition\ApplicationDefinition.Contracts\DTOs\EntityDefinitionDto.cs`
  - Align with draft: include Id (Guid), ApplicationDefinitionId, Name (string entity id), DisplayName, TenantScoped. Remove Description, AttributesJson, PrimaryKey if present.
- **Files:** `CreateEntityRequest.cs`, `UpdateEntityRequest.cs` (under ApplicationDefinition.Contracts or AppBuilder)
  - Add TenantScoped; remove or replace description/attributesJson/primaryKey with draft-aligned fields.

**1.3 ApplicationDefinition.Application**

- **File:** `server\src\Product\ApplicationDefinition\ApplicationDefinition.Application\Mappers\EntityDefinitionMapper.cs`
  - Map new/renamed properties (TenantScoped; Name as entity id; no Description/AttributesJson/PrimaryKey).
- **Files:** `CreateEntityRequestValidator.cs`, `UpdateEntityRequestValidator.cs`
  - Validate TenantScoped and draft-aligned fields; remove validation for removed fields.

**1.4 AppBuilder (vertical slice)**

- **Domain:** AppBuilder.Domain does not define EntityDefinition; it uses ApplicationDefinition.Domain. No change unless you add an aggregate method that sets tenantScoped.
- **Application:**
  - **Commands:** `CreateEntityDefinitionCommand.cs`, `CreateEntityDefinitionCommandHandler.cs` — pass TenantScoped; use new Create/Update signatures.
  - **Commands:** `UpdateEntityDefinitionCommand.cs`, `UpdateEntityDefinitionCommandHandler.cs` — same.
  - **Commands:** `DeleteEntityDefinitionCommand.cs` / `DeleteEntityDefinitionCommandHandler.cs` — no change.
  - **Queries:** `GetEntityDefinitionQuery.cs`, `GetEntityDefinitionQueryHandler.cs` — return updated DTO.
  - **Queries:** `ListEntitiesByApplicationQuery.cs`, `ListEntitiesByApplicationQueryHandler.cs` — return list of updated DTO.
  - **Commands:** `CreatePropertyDefinitionCommandHandler.cs`, `CreateRelationDefinitionCommandHandler.cs`, `CreateApplicationReleaseCommandHandler.cs` — use updated EntityDefinition (e.g. Name as entity id).
- **Infrastructure:**
  - **File:** `AppBuilder.Infrastructure\Data\Configurations\EntityDefinitionConfiguration.cs` — add `TenantScoped` column; remove columns for Description, AttributesJson, PrimaryKey if dropped.
  - **File:** `AppBuilder.Infrastructure\Repositories\EntityDefinitionRepository.cs` — ensure any GetByName/GetByIdentityId uses `Name` as draft `id`.
- **API:**
  - **File:** `AppBuilder.Api\Controllers\EntityDefinitionsController.cs` — request/response models aligned with new DTO (TenantScoped in/out).
- **Migrations:**
  - **New migration:** Add `tenant_scoped` to `appbuilder.entity_definitions`; drop `description`, `attributes_json`, `primary_key` if removed. Migration file under `AppBuilder.Migrations\Migrations\Schema\`.

**1.5 TenantApplication (vertical slice)**

- **Infrastructure:**
  - **File:** `TenantApplication.Infrastructure\Data\Configurations\TenantEntityDefinitionConfiguration.cs` — same as AppBuilder: add TenantScoped, remove deprecated columns.
  - **File:** `TenantApplication.Infrastructure\Data\TenantApplicationDbContext.cs` — no change if only configuration changes.
  - **File:** `TenantApplication.Infrastructure\TenantDefinitionSnapshotReader.cs` — read TenantScoped; use Name as entity id where needed.
  - **File:** `TenantApplication.Infrastructure\Services\CopyDefinitionsFromPlatformReleaseService.cs` — when creating EntityDefinition, use new Create(..., tenantScoped).
- **Migrations:**
  - **New migration:** Same schema changes for tenant schema (e.g. `tenant_application.entity_definitions`): add `tenant_scoped`, drop removed columns.

**1.6 ApplicationDefinition.HtmlGeneration (if used)**

- **Files:** `EntityHtmlGenerator.cs`, any code that reads EntityDefinition — use DisplayName, Name (entity id), TenantScoped; remove references to Description, AttributesJson, PrimaryKey.

**1.7 Build**

- Run `dotnet build server/Datarizen.sln`. Fix any remaining references to removed properties.

---

### Step 2: Refactor PropertyDefinition to draft shape (full vertical slice)

**Goal:** Align `PropertyDefinition` with draft §1.2.1: name, type, required, readonly, default, length, precision, scale, expression, persist.

**2.1 ApplicationDefinition.Domain**

- **File:** `server\src\Product\ApplicationDefinition\ApplicationDefinition.Domain\Entities\PropertyDefinition.cs`
  - Keep: EntityDefinitionId, Name, IsRequired, DefaultValue (as string or object; draft: default).
  - **Rename / align:** DisplayName — draft has no displayName for property; keep for UI or remove.
  - **Replace:** `PropertyDataType` (enum) with **Type** as string (draft: "string", "decimal", "uuid", "datetime", etc.).
  - **Add:** Readonly (bool), Length (int?), Precision (int?), Scale (int?), Expression (string?), Persist (bool). Remove ValidationRulesJson and Order if not in draft; or keep Order for UI ordering.
- **File:** `ApplicationDefinition.Domain\Enums\PropertyDataType.cs` — keep for backward mapping or remove and use string type everywhere.

**2.2 ApplicationDefinition.Contracts**

- **File:** `PropertyDefinitionDto.cs` — add Readonly, Length, Precision, Scale, Expression, Persist; type as string if changed.
- **Files:** CreatePropertyRequest, UpdatePropertyRequest — same fields.

**2.3 ApplicationDefinition.Application**

- **File:** `PropertyDefinitionMapper.cs` — map new/removed fields.
- **Validators:** CreatePropertyRequestValidator, UpdatePropertyRequestValidator — validate new fields (e.g. length, precision, scale).

**2.4 AppBuilder (vertical slice)**

- **Application:** CreatePropertyDefinition, UpdatePropertyDefinition command/handler — pass new fields. ListPropertiesByEntity unchanged except DTO shape.
- **Infrastructure:** `PropertyDefinitionConfiguration.cs` — new columns (readonly, length, precision, scale, expression, persist); remove or keep validation_rules_json, order per decision.
- **API:** PropertyDefinitionsController — request/response with new fields.
- **Migrations:** Add columns to `appbuilder.property_definitions`; drop or keep old columns.

**2.5 TenantApplication (vertical slice)**

- **Infrastructure:** TenantPropertyDefinitionConfiguration — same column changes. CopyDefinitionsFromPlatformReleaseService — create properties with new signature.
- **Migrations:** Same schema changes for tenant property definitions table.

**2.6 Build**

- `dotnet build server/Datarizen.sln`.

---

### Step 3: Refactor RelationDefinition to draft shape (full vertical slice)

**Goal:** Align with draft §1.2.2: name, type, target (entity id string), inverse, required, tenantScoped.

**3.1 ApplicationDefinition.Domain**

- **File:** `RelationDefinition.cs`
  - Keep: SourceEntityId, Name, RelationType (or replace with string: "one-to-one", "one-to-many", "many-to-one", "many-to-many").
  - **Replace:** TargetEntityId (Guid) → **Target** (string) — draft uses target entity **id** (logical name, e.g. "Customer").
  - **Add:** Inverse (string), Required (bool), TenantScoped (bool). Remove CascadeDelete if not in draft or keep for implementation detail.
- **Repository:** IRelationDefinitionRepository — any method that returned TargetEntityId now returns Target (string); persistence may still store a FK or just the string.

**3.2 ApplicationDefinition.Contracts**

- **RelationDefinitionDto:** Target as string (entity id), Inverse, Required, TenantScoped.
- **CreateRelationRequest, UpdateRelationRequest:** same.

**3.3 ApplicationDefinition.Application**

- **RelationDefinitionMapper.cs** — map Target, Inverse, Required, TenantScoped.

**3.4 AppBuilder (vertical slice)**

- **Application:** CreateRelationDefinitionCommandHandler — resolve Target entity by name (entity id) if needed; store Target as string. UpdateRelationDefinition similarly.
- **Infrastructure:** RelationDefinitionConfiguration — column for target (string), inverse, required, tenant_scoped. Update FK if you keep a FK to target entity or drop and use string only.
- **Migrations:** Add columns; change target from Guid to string (entity id) if applicable.

**3.5 TenantApplication (vertical slice)**

- **Infrastructure:** TenantRelationDefinitionConfiguration, CopyDefinitionsFromPlatformReleaseService — same shape (Target as string, Inverse, Required, TenantScoped).
- **Migrations:** Same for tenant relation definitions.

**3.6 Build**

- `dotnet build server/Datarizen.sln`.

---

### Step 4: Add CalculatedField and wire to EntityDefinition (full vertical slice)

**Goal:** Draft §1.2.3 — calculated fields (name, type, calculated, expression, persist). Add as a new entity or value object and associate with EntityDefinition.

**4.1 ApplicationDefinition.Domain**

- **New file:** `ApplicationDefinition.Domain\Entities\CalculatedFieldDefinition.cs` (or value object). Properties: EntityDefinitionId (if separate table), Name, Type (string), Calculated (bool), Expression (string), Persist (bool). Create/Update factory methods.
- **EntityDefinition.cs:** Optionally add navigation `IReadOnlyList<CalculatedFieldDefinition> CalculatedFields` (if owned/separate table). Otherwise repositories load calculated fields by EntityDefinitionId.
- **New repository:** `ICalculatedFieldDefinitionRepository` (GetByEntityDefinitionId, Add, Update, Delete).

**4.2 ApplicationDefinition.Contracts**

- **New:** CalculatedFieldDefinitionDto, CreateCalculatedFieldRequest, UpdateCalculatedFieldRequest.

**4.3 ApplicationDefinition.Application**

- **New:** CalculatedFieldDefinitionMapper, validators. EntityDefinitionMapper or snapshot builder may include CalculatedFields in entity payload.

**4.4 AppBuilder (vertical slice)**

- **Application:** Commands Create/Update/Delete CalculatedFieldDefinition; Query ListCalculatedFieldsByEntity. CreateApplicationReleaseCommandHandler — include calculated fields in entity snapshot.
- **Infrastructure:** CalculatedFieldDefinitionConfiguration, CalculatedFieldDefinitionRepository. DbContext: DbSet CalculatedFieldDefinitions.
- **API:** CalculatedFieldDefinitionsController or extend EntityDefinitionsController (nested resource).
- **Migrations:** Create table `appbuilder.calculated_field_definitions` (id, entity_definition_id, name, type, calculated, expression, persist, created_at, updated_at).

**4.5 TenantApplication (vertical slice)**

- **Infrastructure:** Tenant CalculatedFieldDefinition configuration and table; CopyDefinitionsFromPlatformReleaseService copies calculated fields.
- **Migrations:** Create tenant calculated_field_definitions table.

**4.6 Build**

- `dotnet build server/Datarizen.sln`.

---

### Step 5: Refactor PageDefinition to draft shape (full vertical slice)

**Goal:** Draft §1.4 — Page: id, type (EntityList | EntityEdit | Custom), entity (optional), layout (LayoutNode[]), listConfig, fieldOverrides, permissions.

**5.1 ApplicationDefinition.Domain**

- **File:** `PageDefinition.cs`
  - Keep Id, ApplicationDefinitionId. **Rename:** Name → Id (string) for draft page id.
  - **Add:** Type (enum or string: EntityList, EntityEdit, Custom), Entity (string? — entity id), LayoutJson (string or structured LayoutNode[]), ListConfigJson, FieldOverridesJson, PermissionsJson (or value objects). **Remove or replace:** Route, ConfigurationJson with layout/listConfig/fieldOverrides/permissions.
- **Value objects (optional):** LayoutNode, ListConfig, FieldOverride if you want typed domain objects instead of JSON.

**5.2 ApplicationDefinition.Contracts**

- **PageDefinitionDto:** Id (Guid), ApplicationDefinitionId, PageId (string), Type, Entity, LayoutJson (or DTO), ListConfigJson, FieldOverridesJson, PermissionsJson.
- **Requests:** CreatePageRequest, UpdatePageRequest — same shape.

**5.3 ApplicationDefinition.Application**

- **PageDefinitionMapper.cs** — map new fields. Validators for type, entity id format, layout structure if validated.

**5.4 AppBuilder (vertical slice)**

- **Application:** CreatePageDefinition, UpdatePageDefinition — pass Type, Entity, Layout, ListConfig, FieldOverrides, Permissions. GetReleaseSnapshot / CreateApplicationRelease — include page type, entity, layout, listConfig, fieldOverrides, permissions in PageJson.
- **Infrastructure:** PageDefinitionConfiguration — columns for type, entity, layout_json, list_config_json, field_overrides_json, permissions_json; drop or replace route, configuration_json.
- **API:** PageDefinitionsController — request/response with new shape.
- **Migrations:** Alter appbuilder.page_definitions; same for TenantApplication.

**5.5 TenantApplication (vertical slice)**

- **Infrastructure:** Tenant page definition configuration and copy service — same shape.
- **Migrations:** Alter tenant page_definitions.

**5.6 ApplicationDefinition.HtmlGeneration**

- **PageHtmlGenerator.cs**, **InitialViewComposer.cs** — consume Type, Entity, Layout (LayoutNode), ListConfig, FieldOverrides, Permissions instead of ConfigurationJson.

**5.7 Build**

- `dotnet build server/Datarizen.sln`.

---

### Step 6: Refactor NavigationDefinition to draft shape (full vertical slice)

**Goal:** Draft §1.5 — NavigationItem: id, label, page (Page ID), children (NavigationItem[]), tenantId (optional).

**6.1 ApplicationDefinition.Domain**

- **File:** `NavigationDefinition.cs`
  - Current: Name, ConfigurationJson. **Refactor:** Either (A) keep one row per “root” nav and store tree as JSON, or (B) model as tree: add Id (string), Label, PageId (string), ParentNavigationDefinitionId? (Guid), TenantId (string?), Children as navigation. Draft is a tree; (A) is minimal change (single JSON column for full tree). For refactor with minimal table change: keep one or more rows per app; add Id (string), Label, PageId (string), TenantId (string?), ParentId (nullable); store children in JSON or self-reference ParentId. Or keep ConfigurationJson and rename to NavigationTreeJson with schema matching NavigationItem[].
  - **Concrete:** Add NavigationItemId (string), Label, PageId (string), TenantId (string?). Keep ConfigurationJson as the serialized children (array of NavigationItem). So one row = one root item; ConfigurationJson = children array. Or one row per item with ParentId. Choose one and document.
- **Recommendation for simplicity:** Keep single row per app with full tree: add columns only if needed (e.g. root_id); store full tree as JSON matching NavigationItem[] (id, label, page, children, tenantId).

**6.2 ApplicationDefinition.Contracts**

- **NavigationDefinitionDto / NavigationItemDto:** id, label, page, children, tenantId. Request/response for save load full tree.

**6.3 ApplicationDefinition.Application**

- **NavigationDefinitionMapper** — map to/from tree DTO.

**6.4 AppBuilder (vertical slice)**

- **Application:** Create/Update NavigationDefinition — accept tree (NavigationItem[]). CreateApplicationRelease — serialize navigation tree in ReleaseSnapshot.
- **Infrastructure:** NavigationDefinitionConfiguration — ensure JSON column matches NavigationItem schema.
- **API:** NavigationDefinitionsController — get/put full tree.
- **Migrations:** Add columns if any (e.g. root_id); or only ensure JSON schema.

**6.5 TenantApplication (vertical slice)**

- **Infrastructure:** Copy navigation tree; TenantApplication navigation config.
- **Migrations:** Same shape for tenant navigation.

**6.6 ApplicationDefinition.HtmlGeneration**

- **NavigationHtmlGenerator.cs** — consume NavigationItem[] (id, label, page, children, tenantId).

**6.7 Build**

- `dotnet build server/Datarizen.sln`.

---

### Step 7: DataSourceDefinition — align or retire (full vertical slice)

**Goal:** Draft does not list “DataSource” as a top-level concept like entities/pages/navigation. Either (A) retain DataSourceDefinition for builder-specific data sources and align its config with draft-style JSON, or (B) retire it and fold into page/entity config. Choose one.

**7.1 If retaining**

- **DataSourceDefinition.cs:** Add any draft-aligned fields (e.g. type, configuration schema). Contracts, mappers, AppBuilder and TenantApplication vertical slice: update DTOs and API to match.
- **Migrations:** Add/alter columns as needed.

**7.2 If retiring**

- Remove DataSourceDefinition entity, repository, commands/queries, API, and migrations (drop table) from ApplicationDefinition, AppBuilder, and TenantApplication. Remove from CreateApplicationRelease snapshot payload and from TenantApplication copy/snapshot logic.

**7.3 Build**

- `dotnet build server/Datarizen.sln`.

---

### Step 8: ApplicationRelease → ReleaseSnapshot + Persistence definition (full vertical slice)

**Goal:** Draft §2 — ReleaseSnapshot: ReleaseId, Version, Metadata (copy of Authoring JSON), TenantOverrides (optional), ValidationStatus. Draft §1.3 — Persistence: databaseProvider, namingStrategy, entities[] (entity, tableName, indexes, properties).

**8.1 ApplicationDefinition.Domain**

- **File:** `ApplicationRelease.cs` — **Refactor** to align with ReleaseSnapshot: keep Id, ApplicationDefinitionId, Version (semantic), Major, Minor, Patch, ReleasedAt, ReleasedBy, IsActive. **Add:** ValidationStatus (enum: Pending, Valid, Invalid). **Replace** multiple JSON columns (NavigationJson, PageJson, DataSourceJson, EntityJson, SchemaJson, DdlScriptsJson) with a single **MetadataJson** (full Authoring JSON copy) and optionally **TenantOverridesJson**. Keep DdlScriptsJson and approval fields if DDL workflow is retained.
- **New (optional):** PersistenceDefinition value object or entity: DatabaseProvider, NamingStrategy, Entities[] (EntityId, TableName, Indexes, PropertyOverrides). Store at application or release level (draft shows persistence at app level).

**8.2 ApplicationDefinition.Contracts**

- **ApplicationReleaseDto / ReleaseSnapshotDto:** Version, Metadata (JSON or DTO), TenantOverrides, ValidationStatus. ApplicationSnapshotDto: derive from Metadata (navigation, pages, entities, etc.) for runtime.

**8.3 ApplicationDefinition.Application**

- **ApplicationReleaseMapper** — map MetadataJson, ValidationStatus. ToSnapshotDto builds ApplicationSnapshotDto from MetadataJson.

**8.4 AppBuilder (vertical slice)**

- **Application:** CreateApplicationReleaseCommandHandler — build full Authoring JSON from current entities (EntityDefinition, PropertyDefinition, RelationDefinition, CalculatedFieldDefinition, PageDefinition, NavigationDefinition, Persistence, etc.) and set Release.MetadataJson = that JSON; set ValidationStatus (e.g. Pending). GetReleaseSnapshot returns snapshot from MetadataJson.
- **Infrastructure:** ApplicationReleaseConfiguration — columns metadata_json, tenant_overrides_json, validation_status; drop navigation_json, page_json, data_source_json, entity_json, schema_json or keep for a transition period then drop in same migration.
- **Migrations:** Add metadata_json, tenant_overrides_json, validation_status; optionally drop old JSON columns (or do in next step).

**8.5 TenantApplication (vertical slice)**

- **Application:** GetReleaseSnapshotQueryHandler — read ReleaseSnapshot (or platform release) and return ApplicationSnapshotDto built from MetadataJson.
- **Infrastructure:** If TenantApplication stores a copy of releases, same schema (metadata_json, validation_status). CopyDefinitionsFromPlatformReleaseService — may copy MetadataJson instead of copying each definition table (if you move to snapshot-only tenant install).

**8.6 Persistence definition (app level)**

- **New entity or JSON on ApplicationDefinition:** PersistenceDefinition (databaseProvider, namingStrategy, entities[]). AppBuilder: store at app level; include in Authoring JSON when building ReleaseSnapshot. Migrations: add table or column for persistence definition.

**8.7 Build**

- `dotnet build server/Datarizen.sln`.

---

### Step 9: Workflows, Roles & Permissions, Component Registry, Themes (full vertical slice)

**Goal:** Draft §§1.6, 1.7, 1.8, 1.9 — add Workflow, Role, Permission, ComponentRegistry, Theme. These can be new entities or JSON blobs at application level; include in Authoring JSON (Metadata) when building ReleaseSnapshot.

**9.1 ApplicationDefinition.Domain**

- **New entities or value objects:** WorkflowDefinition (id, entity, tenantId, startEvent, tasks, sequenceFlows), RoleDefinition (id, name, inherits, tenantScoped), PermissionDefinition (resourceType, resourceId, actions, tenantScoped), ComponentRegistryEntry (id, category, version, propsSchema), ThemeDefinition (id, colors, typography). Decide: separate tables vs single application-level JSON column per concept.
- **Recommendation:** Add tables (e.g. workflow_definitions, role_definitions, permission_definitions, component_registry, themes) under appbuilder (and tenant if needed) so builder can edit them; when building ReleaseSnapshot, serialize into Metadata.

**9.2 ApplicationDefinition.Contracts**

- **New DTOs and requests** for Workflow, Role, Permission, ComponentRegistry, Theme per draft.

**9.3 ApplicationDefinition.Application**

- **New mappers and validators** for each. ApplicationReleaseMapper / snapshot builder — include workflows, roles, permissions, component_registry, themes in MetadataJson.

**9.4 AppBuilder (vertical slice)**

- **Application:** Commands/Queries for Create/Update/Delete/List for each of Workflow, Role, Permission, ComponentRegistry, Theme. CreateApplicationReleaseCommandHandler — load all and write into MetadataJson.
- **Infrastructure:** Configurations and repositories for each; DbSets.
- **API:** Controllers for workflows, roles, permissions, component registry, themes.
- **Migrations:** Create tables for workflow_definitions, role_definitions, permission_definitions, component_registry, themes in appbuilder schema.

**9.5 TenantApplication (vertical slice)**

- **Infrastructure:** Copy or reference workflows, roles, permissions, component registry, themes when installing a release; or rely on MetadataJson only for runtime.
- **Migrations:** Tenant tables if storing copies; otherwise no new tables.

**9.6 Build**

- `dotnet build server/Datarizen.sln`.

---

## 4. Verification Checklist (per step)

- [ ] Domain entity/file changed as described; Create/Update signatures and validation updated.
- [ ] Contracts (DTOs, requests) aligned with draft and domain.
- [ ] Application mappers and validators updated.
- [ ] AppBuilder commands, queries, handlers, repositories, configurations, API, and migrations updated.
- [ ] TenantApplication infrastructure, configurations, services, and migrations updated.
- [ ] ApplicationDefinition.HtmlGeneration (if used) updated for the concept.
- [ ] `dotnet build server/Datarizen.sln` succeeds.

---

## 5. References

- **Target spec:** [draft.txt](./draft.txt)
- **Guidance:** [docs/ai-context](../../ai-context) — [02-SOLUTION-STRUCTURE](../../ai-context/02-SOLUTION-STRUCTURE.md), [05-MODULES-DOMAIN-LAYER](../../ai-context/05-MODULES-DOMAIN-LAYER.md), [05-MODULES-APPLICATION-LAYER](../../ai-context/05-MODULES-APPLICATION-LAYER.md), [07-DB-MIGRATION-FLOW](../../ai-context/07-DB-MIGRATION-FLOW.md), [08-SERVER-CODING-CONVENTIONS](../../ai-context/08-SERVER-CODING-CONVENTIONS.md).
