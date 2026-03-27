# TenantApplication Module - Domain Layer

**Status**: Ready for implementation (shared ApplicationDefinition)  
**Last Updated**: 2026-02-15  
**Module**: TenantApplication  
**Layer**: Domain  

---

## Shared ApplicationDefinition (implementation)

- **Shared product location**: `server/src/Product/ApplicationDefinition/`  
  - **ApplicationDefinition.Domain**: Shared definition entities (`EntityDefinition`, `PropertyDefinition`, `RelationDefinition`, `NavigationDefinition`, `PageDefinition`, `DataSourceDefinition`, `ApplicationRelease`), enums (`PropertyDataType`, `RelationType`, `DataSourceType`), and repository interfaces (`IEntityDefinitionRepository`, `IPropertyDefinitionRepository`, etc.).  
- **TenantApplication.Domain** will **reference** `ApplicationDefinition.Domain`.  
- **TenantApplication-specific** types remain in this module: aggregate roots and entities (`TenantApplication`, `TenantApplicationEnvironment`, `TenantApplicationMigration`), value objects, and repository interfaces for them (`ITenantApplicationRepository`, etc.).  
- Tenant-level “definition” data uses the **same shared entity types** from ApplicationDefinition.Domain; persistence is in `tenantapplication` schema (`tenant_*_definitions`, `tenant_application_releases`) with tenant isolation.  
- **AppBuilder** is not refactored in this step; it will adopt ApplicationDefinition.Domain in a later step.

---

## Overview

Manages tenant applications with support for:
1. Installing platform applications (from AppBuilder catalog)
2. Creating custom applications (tenant-owned)
3. Forking platform applications for customization
4. Environment-based deployments (Dev/Staging/Prod)
5. Schema migrations between releases

**CRITICAL**: This module contains **duplicate AppBuilder tables** in `tenantapplication` schema to support tenant-level application development when AppBuilder feature is enabled. **Editing ownership**: When a tenant has the AppBuilder feature, **TenantApplication** owns editing of that tenant’s applications (entities, pages, navigation, data sources, releases). The AppBuilder UI calls **TenantApplication** API (not AppBuilder API) when editing a tenant application; TenantApplication performs all reads/writes against `tenantapplication` schema and enforces tenant isolation and feature checks.

---

## Dual Schema Support

### AppBuilder Tables in Two Schemas

| Table | appbuilder schema | tenantapplication schema |
|-------|-------------------|--------------------------|
| Application definitions | `application_definitions` | (see TenantApplication aggregate + tenant_*_definitions) |
| Releases | `application_releases` | `tenant_application_releases` |
| Entities | `entity_definitions` | `tenant_entity_definitions` |
| Properties | `property_definitions` | `tenant_property_definitions` |
| Relations | `relation_definitions` | `tenant_relation_definitions` |
| Navigation | `navigation_definitions` | `tenant_navigation_definitions` |
| Pages | `page_definitions` | `tenant_page_definitions` |
| DataSources | `datasource_definitions` | `tenant_datasource_definitions` |

**Note**: The main tenant-side record for an installed or custom app is `tenantapplication.tenant_applications` (TenantApplication aggregate). When AppBuilder feature is enabled, tenant-level definitions use the `tenant_*_definitions` tables. AppBuilder no longer uses `application_events` or `application_snapshots`; tenant-level event/snapshot tables are optional for future use.

**Why?**
- ✅ Platform owners create apps in `appbuilder` schema
- ✅ Tenants create apps in `tenantapplication` schema (when AppBuilder feature enabled)
- ✅ Same domain logic, different storage
- ✅ Tenant isolation guaranteed

---

## Entities

### TenantApplication (Aggregate Root)

**Purpose**: Tenant's installed or custom application

**Base Class**: `AggregateRoot<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `TenantId: Guid` - Foreign key to Tenant module
- `ApplicationReleaseId: Guid?` - **NULLABLE** - Reference to installed release OR current custom release
- `ApplicationId: Guid?` - **NULLABLE** - Denormalized application definition ID (from release when installed)
- `Major: int?`, `Minor: int?`, `Patch: int?` - Denormalized version (from release when installed)
- `Name: string` - Application name
- `Slug: string` - URL-friendly identifier (unique per tenant)
- `Description: string?` - Application description
- `IsCustom: bool` - Whether tenant-created (true) or platform-installed (false)
- `SourceApplicationReleaseId: Guid?` - If forked from platform app
- `Status: TenantApplicationStatus` - Draft, Installed, Active, Inactive, Archived
- `Configuration: Dictionary<string, object>` (or JSON) - Tenant-specific configuration
- `InstalledAt: DateTime?` - When installed (platform apps)
- `ActivatedAt: DateTime?`, `DeactivatedAt: DateTime?`, `UninstalledAt: DateTime?` - Lifecycle timestamps
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Collections**:
- `Environments: ICollection<TenantApplicationEnvironment>` - Deployment environments

**Factory Methods**:
```csharp
// Install platform application
public static Result<TenantApplication> InstallFromPlatform(
    Guid tenantId,
    Guid applicationReleaseId,
    string name,
    string slug)

// Create custom application from scratch
public static Result<TenantApplication> CreateCustom(
    Guid tenantId,
    string name,
    string slug,
    string description)

// Fork platform application for customization
public static Result<TenantApplication> ForkFromPlatform(
    Guid tenantId,
    Guid sourceApplicationReleaseId,
    string name,
    string slug)
```

**Business Methods**:
- `CreateEnvironment(string name, EnvironmentType type)` - Create deployment environment
- `CreateRelease(string version, string releaseNotes, Guid userId)` - Create custom release
- `UpdateConfiguration(Dictionary<string, object> configuration)` - Update tenant configuration
- `Upgrade(int major, int minor, int patch)` - Upgrade to a new release (sets ApplicationReleaseId, ApplicationId, Major, Minor, Patch from release)
- `Activate()` - Activate application
- `Deactivate()` - Deactivate application
- `Uninstall()` - Uninstall application (soft delete / status)
- `Archive()` - Archive application

**Domain Events**:
- `TenantApplicationInstalledEvent`
- `TenantApplicationCreatedEvent`
- `TenantApplicationForkedEvent`
- `TenantApplicationReleasedEvent`
- `TenantApplicationActivatedEvent`
- `TenantApplicationDeactivatedEvent`
- `TenantApplicationArchivedEvent`

---

### TenantApplicationEnvironment (Entity)

**Purpose**: Deployment environment for tenant application

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `TenantApplicationId: Guid` - Foreign key
- `Name: string` - Environment name (e.g. "Development", "Staging")
- `EnvironmentType: EnvironmentType` - Development, Staging, Production
- `ApplicationReleaseId: Guid?` - **NULLABLE** - Currently deployed release (null = not deployed)
- `ReleaseVersion: string?` - Version string (e.g., "1.0.0")
- `IsActive: bool` - Whether environment is active
- `DeployedAt: DateTime?` - When last deployed
- `DeployedBy: Guid?` - User who deployed
- `ConfigurationJson: string?` or `Configuration: Dictionary<string, object>` - Environment-specific configuration
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Factory Method**:
```csharp
public static Result<TenantApplicationEnvironment> Create(
    Guid tenantApplicationId,
    string name,
    EnvironmentType environmentType)
```

**Business Methods**:
- `DeployRelease(Guid releaseId, string version, Guid userId)` - Deploy release
- `DeployDraft()` - Deploy draft (Development only)
- `Activate()` - Activate environment
- `Deactivate()` - Deactivate environment
- `UpdateConfiguration(string configurationJson)` or `UpdateConfiguration(Dictionary<string, object> configuration)` - Update config

**Domain Events**:
- `EnvironmentCreatedEvent`
- `ReleaseDeployedEvent`
- `DraftDeployedEvent`
- `EnvironmentActivatedEvent`
- `EnvironmentDeactivatedEvent`
- `EnvironmentConfigurationUpdatedEvent`

---

### TenantApplicationMigration (Entity)

**Purpose**: Schema migration between releases

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `TenantApplicationEnvironmentId: Guid` - Foreign key
- `FromReleaseId: Guid?` - Source release (null = initial)
- `ToReleaseId: Guid` - Target release
- `MigrationScriptJson: string` - Migration SQL/commands as JSON
- `Status: MigrationStatus` - Pending, Running, Completed, Failed
- `ExecutedAt: DateTime?` - When executed
- `ErrorMessage: string?` - Error details if failed
- `CreatedAt: DateTime`

**Factory Method**:
```csharp
public static Result<TenantApplicationMigration> Create(
    Guid tenantApplicationEnvironmentId,
    Guid? fromReleaseId,
    Guid toReleaseId,
    string migrationScriptJson)
```

**Business Methods**:
- `Execute()` - Execute migration
- `MarkCompleted()` - Mark as completed
- `MarkFailed(string errorMessage)` - Mark as failed

**Domain Events**:
- `MigrationCreatedEvent`
- `MigrationExecutedEvent`
- `MigrationCompletedEvent`
- `MigrationFailedEvent`

---

## Tenant-Level AppBuilder Entities

**When AppBuilder feature is enabled for tenant**, these entities are created in `tenantapplication` schema:

### TenantApplicationDefinition (concept)
- When AppBuilder feature is enabled and tenant creates a custom app, the tenant’s “definition” is the TenantApplication row (in `tenant_applications`) with `IsCustom = true`, plus related rows in `tenant_entity_definitions`, `tenant_navigation_definitions`, etc.
- Tenant-isolated

### TenantApplicationRelease
- Same as `ApplicationRelease` in AppBuilder module
- Stored in `tenantapplication.tenant_application_releases` table
- Tenant-isolated

### TenantEntityDefinition
- Same as `EntityDefinition` in AppBuilder module
- Stored in `tenantapplication.tenant_entity_definitions` table
- Tenant-isolated

### TenantPropertyDefinition
- Same as `PropertyDefinition` in AppBuilder module
- Stored in `tenantapplication.tenant_property_definitions` table
- Tenant-isolated

### TenantRelationDefinition
- Same as `RelationDefinition` in AppBuilder module
- Stored in `tenantapplication.tenant_relation_definitions` table
- Tenant-isolated

### TenantNavigationDefinition
- Same as `NavigationDefinition` in AppBuilder module
- Stored in `tenantapplication.tenant_navigation_definitions` table
- Tenant-isolated

### TenantPageDefinition
- Same as `PageDefinition` in AppBuilder module
- Stored in `tenantapplication.tenant_page_definitions` table
- Tenant-isolated

### TenantDataSourceDefinition
- Same as `DataSourceDefinition` in AppBuilder module
- Stored in `tenantapplication.tenant_datasource_definitions` table
- Tenant-isolated

### TenantApplicationEvent
- Same as `ApplicationEvent` in AppBuilder module
- Stored in `tenantapplication.tenant_application_events` table
- Tenant-isolated

### TenantApplicationSnapshot
- Same as `ApplicationSnapshot` in AppBuilder module
- Stored in `tenantapplication.tenant_application_snapshots` table
- Tenant-isolated

---

## Value Objects

### TenantApplicationStatus (Enum)
```csharp
public enum TenantApplicationStatus
{
    Draft = 0,        // Custom app being built
    Installed = 1,    // Platform app installed
    Active = 2,       // Application active
    Inactive = 3,     // Application inactive
    Archived = 4      // Application archived
}
```

### EnvironmentType (Enum)
```csharp
public enum EnvironmentType
{
    Development = 0,
    Staging = 1,
    Production = 2
}
```

### MigrationStatus (Enum)
```csharp
public enum MigrationStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}
```

---

## Complete Workflow

### 1. Install Platform Application
```
1. Tenant browses AppBuilder catalog
2. Tenant selects ApplicationRelease (e.g., "CRM v1.0.0")
3. System creates TenantApplication
   ├── TenantId = current tenant
   ├── ApplicationReleaseId = selected release
   ├── IsCustom = false
   ├── Status = Installed
   └── Slug = "crm"
4. System auto-creates Development environment
   ├── EnvironmentType = Development
   ├── ApplicationReleaseId = NULL (not deployed yet)
   └── IsActive = false
5. Tenant can now deploy to Development
```

### 2. Create Custom Application (AppBuilder Feature Required)
```
1. Check if tenant has AppBuilder feature enabled
2. If enabled:
   ├── Create TenantApplication
   │   ├── TenantId = current tenant
   │   ├── ApplicationReleaseId = NULL
   │   ├── IsCustom = true
   │   ├── Status = Draft
   │   └── Slug = user-provided
   ├── Create entities in tenantapplication schema
   │   ├── TenantEntityDefinition
   │   ├── TenantPropertyDefinition
   │   ├── TenantRelationDefinition
   │   ├── TenantNavigationDefinition
   │   ├── TenantPageDefinition
   │   └── TenantDataSourceDefinition
   └── Append TenantApplicationEvents
3. Tenant designs application
4. Tenant creates TenantApplicationRelease
5. Tenant deploys to environments
```

### 3. Fork Platform Application (AppBuilder Feature Required)
```
1. Check if tenant has AppBuilder feature enabled
2. Tenant has installed platform ApplicationRelease
3. Tenant clicks "Customize Application"
4. System creates TenantApplication
   ├── TenantId = current tenant
   ├── ApplicationReleaseId = NULL
   ├── IsCustom = true
   ├── SourceApplicationReleaseId = original release
   ├── Status = Draft
   └── Slug = user-provided
5. System copies all definitions from source release
   ├── Copy entities → TenantEntityDefinition
   ├── Copy properties → TenantPropertyDefinition
   ├── Copy relations → TenantRelationDefinition
   ├── Copy navigation → TenantNavigationDefinition
   ├── Copy pages → TenantPageDefinition
   └── Copy data sources → TenantDataSourceDefinition
6. Tenant modifies definitions
7. Tenant creates TenantApplicationRelease
8. Tenant deploys to environments
```

### 4. Deploy to Development
```
1. Tenant selects ApplicationRelease (platform or custom)
2. System validates compatibility (calls AppRuntime)
3. System updates TenantApplicationEnvironment
   ├── ApplicationReleaseId = selected release
   ├── ReleaseVersion = "1.0.0"
   ├── DeployedAt = now
   ├── DeployedBy = current user
   └── IsActive = true
4. System raises ReleaseDeployedEvent
5. AppRuntime creates RuntimeInstance
6. Application accessible at: /{tenantSlug}/{appSlug}/development
```

### 5. Deploy to Staging
```
1. Validate Development exists and is active
2. Create/update Staging environment
3. Deploy release
4. Raise ReleaseDeployedEvent
5. AppRuntime creates RuntimeInstance
6. Application accessible at: /{tenantSlug}/{appSlug}/staging
```

### 6. Deploy to Production
```
1. Validate Staging exists and is active
2. Validate Staging has same or newer release
3. Create/update Production environment
4. Generate migration script (if schema changed)
5. Execute migration
6. Deploy release
7. Raise ReleaseDeployedEvent
8. AppRuntime creates RuntimeInstance
9. Application accessible at: /{tenantSlug}/{appSlug}
```

---

## Repository Interfaces

### ITenantApplicationRepository
```csharp
public interface ITenantApplicationRepository
{
    Task<TenantApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantApplication?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default);
    Task<List<TenantApplication>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantApplication?> GetByTenantAndApplicationAsync(Guid tenantId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<List<TenantApplication>> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsForTenantAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default);
    Task AddAsync(TenantApplication app, CancellationToken cancellationToken = default);
    void Update(TenantApplication app);
    void Remove(TenantApplication app);
}
```

### ITenantApplicationEnvironmentRepository
```csharp
public interface ITenantApplicationEnvironmentRepository
{
    Task<TenantApplicationEnvironment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantApplicationEnvironment?> GetByTenantAppAndEnvironmentAsync(Guid tenantApplicationId, EnvironmentType environmentType, CancellationToken cancellationToken = default);
    Task<List<TenantApplicationEnvironment>> GetByTenantApplicationAsync(Guid tenantApplicationId, CancellationToken cancellationToken = default);
    Task AddAsync(TenantApplicationEnvironment env, CancellationToken cancellationToken = default);
    void Update(TenantApplicationEnvironment env);
}
```

### ITenantApplicationMigrationRepository
```csharp
public interface ITenantApplicationMigrationRepository
{
    Task<TenantApplicationMigration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TenantApplicationMigration>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<TenantApplicationMigration?> GetPendingMigrationAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task AddAsync(TenantApplicationMigration migration, CancellationToken cancellationToken = default);
    void Update(TenantApplicationMigration migration);
}
```

---

## Key Design Decisions

✅ **ApplicationReleaseId is NULLABLE** - Tenants can create apps from scratch  
✅ **Dual schema support** - AppBuilder tables in both schemas  
✅ **Table name prefix** - `tenant_` prefix in tenantapplication schema  
✅ **Feature-gated** - AppBuilder feature required for custom apps  
✅ **Environment isolation** - Each environment has independent deployment  
✅ **Migration tracking** - All schema changes tracked and versioned  
✅ **Fork support** - Tenants can customize platform apps  
✅ **Draft deployment** - Development can deploy drafts (no release needed)  
✅ **Release deployment** - Staging/Production require releases  
✅ **Configuration per environment** - Each environment has own config  

---

## Testing Before Release

### Platform Testing (AppBuilder Module)
```
1. Create ApplicationDefinition (Draft)
2. Design entities, pages, navigation
3. Create preview ApplicationRelease (v0.0.0-preview)
4. Deploy to test environment
5. Test functionality
6. Delete preview release
7. Create official ApplicationRelease (v1.0.0)
```

### Tenant Testing (TenantApplication Module)
```
1. Create TenantApplication (IsCustom = true, Status = Draft)
2. Design entities, pages, navigation
3. Deploy draft to Development (no release needed)
4. Test functionality
5. When ready, create TenantApplicationRelease
6. Deploy release to Staging → Production
```




