# Latest Implementation Plan - Module Documentation

## ⚠️ CRITICAL CORRECTION: Configuration Ownership

### ✅ CORRECT Understanding

**Configuration is owned by TenantApplication, NOT AppBuilder**

```
AppBuilder Module:
├── ApplicationDefinition (structure, metadata)
├── ApplicationRelease (immutable version; navigation, pages, data sources, entities as JSON - NO CONFIG)
└── *Definition entities (EntityDefinition, NavigationDefinition, PageDefinition, DataSourceDefinition, etc.)

TenantApplication Module:
├── TenantApplication.Configuration (tenant-specific settings)
└── TenantApplicationEnvironment.Configuration (environment overrides)

AppRuntime Module:
└── Loads structure from AppBuilder + config from TenantApplication
```

**Why?**
- ✅ Tenants customize applications after installation
- ✅ Same app can have different settings per tenant
- ✅ Same app can have different settings per environment
- ✅ Configuration changes don't require new releases

**Example**:
```
Base App: "CRM" (from AppBuilder)
├── Tenant A: { theme: "dark", maxUsers: 100 }
└── Tenant B: { theme: "light", maxUsers: 50 }
```

---

## Module Documentation Files

### AppBuilder Module
- `appbuilder-domain.md` - Domain entities (ApplicationDefinition, ApplicationRelease, EntityDefinition, PropertyDefinition, RelationDefinition, NavigationDefinition, PageDefinition, DataSourceDefinition)
- `appbuilder-application.md` - Commands, queries, DTOs
- `appbuilder-infrastructure.md` - DbContext, repositories
- `appbuilder-api.md` - REST endpoints
- `appbuilder-migrations.md` - Database migrations

**AppBuilder provides**: Application STRUCTURE for **platform applications only** (definitions and immutable releases with navigation, pages, data sources, entities in `appbuilder` schema)
**AppBuilder does NOT provide**: Tenant-specific CONFIGURATION; **AppBuilder does NOT edit tenant applications** (that is TenantApplication’s job when the tenant has the AppBuilder feature)

### TenantApplication Module
- `tenantapplication-domain.md` - Domain entities (TenantApplication, TenantApplicationEnvironment, tenant-level definitions when AppBuilder feature enabled)
- `tenantapplication-application.md` - Commands, queries, DTOs (install/config + **definition CRUD for tenant apps**)
- `tenantapplication-infrastructure.md` - DbContext, repositories
- `tenantapplication-api.md` - REST endpoints (install/config + **definition CRUD for tenant apps**)
- `tenantapplication-migrations.md` - Database migrations

**TenantApplication provides**: Tenant-specific CONFIGURATION (settings, customizations, overrides); **when a tenant has the AppBuilder feature**, TenantApplication also provides **editing of that tenant’s applications** (tenant_*_definitions CRUD: entities, pages, navigation, data sources, releases in `tenantapplication` schema). The same “AppBuilder” UX can target **AppBuilder API** (platform apps) or **TenantApplication API** (tenant apps) depending on context.
**TenantApplication does NOT provide**: Application structure for **platform** apps (that’s AppBuilder’s job)

### AppRuntime Module
- `appruntime-architecture.md` - **NEW**: Architecture and internals explanation
- `appruntime-domain.md` - Domain entities (RuntimeInstance, RuntimeVersion)
- `appruntime-application.md` - Commands, queries, DTOs
- `appruntime-infrastructure.md` - DbContext, repositories
- `appruntime-api.md` - REST endpoints
- `appruntime-migrations.md` - Database migrations

**AppRuntime provides**: EXECUTION engine (loads structure + config, renders app)
**AppRuntime does NOT provide**: Storage of structure or configuration

---

## Data Flow: Structure + Configuration → Execution

```
┌──────────────────┐
│   AppBuilder     │
│  (Structure)     │
│                  │
│  - Navigation    │
│  - Pages         │
│  - Data Sources  │
└────────┬─────────┘
         │
         │ ApplicationReleaseId
         ↓
┌──────────────────┐      ┌──────────────────┐
│ TenantApplication│      │    AppRuntime    │
│  (Configuration) │      │   (Execution)    │
│                  │      │                  │
│  - Tenant Config │─────→│  1. Load struct  │
│  - Env Overrides │      │  2. Load config  │
└──────────────────┘      │  3. Merge        │
                          │  4. Render       │
                          └──────────────────┘
```

---

## Cross-Module Communication

### AppRuntime → TenantApplication
```csharp
// Get configuration
using TenantApplication.Contracts.Services;

var resolved = await _applicationResolverService.ResolveByEnvironmentAsync(environmentId);
// Returns: ApplicationReleaseId + Configuration (merged)
```

### AppRuntime → AppBuilder
```csharp
// Get structure
using AppBuilder.Contracts.Services;

var snapshot = await _applicationReleaseService.GetSnapshotAsync(releaseId);
// Returns: Navigation, Pages, DataSources (NO configuration)
```

### AppRuntime Execution
```csharp
// Merge and execute
var loadedApp = new LoadedApplication(
    Structure: snapshot,           // FROM APPBUILDER
    Configuration: resolved.Config // FROM TENANTAPPLICATION
);

foreach (var component in loadedApp.Structure.Components)
{
    var loader = _registry.GetLoader(component.Type);
    var rendered = loader.Load(
        component.Definition,        // FROM APPBUILDER
        loadedApp.Configuration);    // FROM TENANTAPPLICATION
}
```

---

## Implementation Order

1. ✅ **AppBuilder** - Define application structure
2. ✅ **TenantApplication** - Install apps, store configuration
3. ✅ **AppRuntime** - Execute apps using structure + configuration

---

## TenantApplication + shared ApplicationDefinition – ready for implementation

**Status**: Docs updated for **review before implementation**.

Planned implementation steps (TenantApplication module first; AppBuilder refactor in a later step):

1. **Shared product**  
   Create `ApplicationDefinition.Domain` under **`server/src/Product/ApplicationDefinition/`**.  
   Shared types: `EntityDefinition`, `PropertyDefinition`, `RelationDefinition`, `NavigationDefinition`, `PageDefinition`, `DataSourceDefinition`, `ApplicationRelease`, enums (`PropertyDataType`, `RelationType`, `DataSourceType`), and repository interfaces. AppBuilder aggregate `ApplicationDefinition` stays in AppBuilder for now.

2. **TenantApplication layers**  
   - **Domain**: Reference `ApplicationDefinition.Domain`; keep TenantApplication-specific aggregates (`TenantApplication`, `TenantApplicationEnvironment`, `TenantApplicationMigration`).  
   - **Application**: Use shared definition types and repository interfaces for definition CRUD.  
   - **Infrastructure**: Map shared entity types to `tenantapplication` schema (`tenant_entity_definitions`, `tenant_property_definitions`, etc.); implement shared repo interfaces scoped by TenantApplicationId.  
   - **API**: No direct ApplicationDefinition reference unless needed; uses Application layer.  
   - **Migrations**: FluentMigrator migrations for `tenantapplication` schema and all `tenant_*` tables (including definition tables).

3. **AppBuilder**  
   No refactor in this step; AppBuilder will be updated to use `ApplicationDefinition.Domain` in a **next step**.

**Review**: Please confirm the plan above and the per-layer details in `tenantapplication-*.md` and `tenantapplication-migrations.md` before implementation starts.

---

## Key Takeaways

1. **AppBuilder** = Platform application catalog and structure definitions (edits only `appbuilder` schema)
2. **TenantApplication** = Tenant installations, configurations, and **editing of tenant applications** when the tenant has the AppBuilder feature (edits `tenantapplication` schema; same definition CRUD as AppBuilder but tenant-scoped)
3. **AppRuntime** = Execution engine that combines both (loads structure from AppBuilder or TenantApplication depending on app type)

**Remember**: Configuration flows from **TenantApplication**, not AppBuilder. **Tenant app editing** (when AppBuilder feature is on) is done via **TenantApplication** API, not AppBuilder API.

### Editing ownership (who edits what)

| Context | Who edits | API / schema |
|--------|------------|---------------|
| Platform application (catalog) | AppBuilder | AppBuilder API → `appbuilder` schema |
| Tenant application (tenant has AppBuilder feature) | TenantApplication | TenantApplication API → `tenantapplication` schema (tenant_*_definitions) |

The **AppBuilder UX** (UI) can be used for both: when the user is editing a platform app, the UI calls **AppBuilder** API; when the user is editing a tenant app (and the tenant has the AppBuilder feature), the UI calls **TenantApplication** API (same patterns, tenant-scoped endpoints).

