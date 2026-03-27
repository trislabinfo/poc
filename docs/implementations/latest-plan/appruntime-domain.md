# AppRuntime Module - Domain Layer

## Overview

AppRuntime executes application configurations by loading versioned engines/processors. Each component type (Navigation, Page, DataSource) has its own engine version that can evolve independently.

---

## Core Concept

**AppRuntime = Versioned Execution Engines**

- Navigation uses `NavigationEngineVersion` (v1, v2, v3...)
- Pages use `PageEngineVersion` (v1, v2, v3...)
- DataSources use `DataSourceEngineVersion` (v1, v2, v3...)
- Each ApplicationRelease specifies which engine versions it requires

**Example**:
```
ERP Application v1.0.0
├── NavigationEngine: v1
├── PageEngine: v1
└── DataSourceEngine: v1

ERP Application v1.1.0
├── NavigationEngine: v1 (unchanged)
├── PageEngine: v2 (new features)
└── DataSourceEngine: v1 (unchanged)

ERP Application v1.2.0
├── NavigationEngine: v2 (new features)
├── PageEngine: v2 (unchanged)
└── DataSourceEngine: v2 (new features)
```

---

## Entities

### RuntimeInstance (Aggregate Root)

**Purpose**: Running instance of an application in a specific environment

**Base Class**: `AggregateRoot<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `TenantApplicationEnvironmentId: Guid` - Foreign key to TenantApplication module
- `ApplicationReleaseId: Guid` - Foreign key to AppBuilder module
- `NavigationEngineVersionId: Guid` - Which navigation engine to use
- `PageEngineVersionId: Guid` - Which page engine to use
- `DataSourceEngineVersionId: Guid` - Which data source engine to use
- `Status: RuntimeInstanceStatus` - Running, Stopped, Failed
- `StartedAt: DateTime?`
- `StoppedAt: DateTime?`
- `LastHealthCheckAt: DateTime?`
- `ErrorMessage: string?`
- `CreatedAt: DateTime`

**Factory Method**:
```csharp
public static Result<RuntimeInstance> Create(
    Guid tenantApplicationEnvironmentId,
    Guid applicationReleaseId,
    Guid navigationEngineVersionId,
    Guid pageEngineVersionId,
    Guid dataSourceEngineVersionId)
```

**Business Methods**:
- `Start()` - Start runtime instance
- `Stop()` - Stop runtime instance
- `Restart()` - Restart runtime instance
- `RecordHealthCheck()` - Update health check timestamp
- `RecordError(string errorMessage)` - Record runtime error
- `UpdateEngineVersions(Guid navEngineId, Guid pageEngineId, Guid dsEngineId)` - Upgrade engines

**Domain Events**:
- `RuntimeInstanceCreatedEvent`
- `RuntimeInstanceStartedEvent`
- `RuntimeInstanceStoppedEvent`
- `RuntimeInstanceRestartedEvent`
- `RuntimeInstanceFailedEvent`
- `RuntimeInstanceEnginesUpdatedEvent`

---

### NavigationEngineVersion (Entity)

**Purpose**: Version of navigation rendering engine

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `Version: string` - Engine version (e.g., "v1", "v2")
- `Description: string` - What's new in this version
- `IsActive: bool` - Whether available for new deployments
- `IsDeprecated: bool` - Whether marked for removal
- `DeprecationDate: DateTime?` - When deprecated
- `SupportedUntil: DateTime?` - End of support date
- `CreatedAt: DateTime`

**Factory Method**:
```csharp
public static Result<NavigationEngineVersion> Create(
    string version,
    string description)
```

**Business Methods**:
- `Activate()` - Make available for deployments
- `Deactivate()` - Remove from available engines
- `Deprecate(DateTime supportedUntil)` - Mark as deprecated

**Domain Events**:
- `NavigationEngineVersionCreatedEvent`
- `NavigationEngineVersionActivatedEvent`
- `NavigationEngineVersionDeactivatedEvent`
- `NavigationEngineVersionDeprecatedEvent`

---

### PageEngineVersion (Entity)

**Purpose**: Version of page rendering engine

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `Version: string` - Engine version (e.g., "v1", "v2")
- `Description: string` - What's new in this version
- `IsActive: bool` - Whether available for new deployments
- `IsDeprecated: bool` - Whether marked for removal
- `DeprecationDate: DateTime?` - When deprecated
- `SupportedUntil: DateTime?` - End of support date
- `CreatedAt: DateTime`

**Factory Method**:
```csharp
public static Result<PageEngineVersion> Create(
    string version,
    string description)
```

**Business Methods**:
- `Activate()` - Make available for deployments
- `Deactivate()` - Remove from available engines
- `Deprecate(DateTime supportedUntil)` - Mark as deprecated

**Domain Events**:
- `PageEngineVersionCreatedEvent`
- `PageEngineVersionActivatedEvent`
- `PageEngineVersionDeactivatedEvent`
- `PageEngineVersionDeprecatedEvent`

---

### DataSourceEngineVersion (Entity)

**Purpose**: Version of data source execution engine

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `Version: string` - Engine version (e.g., "v1", "v2")
- `Description: string` - What's new in this version
- `IsActive: bool` - Whether available for new deployments
- `IsDeprecated: bool` - Whether marked for removal
- `DeprecationDate: DateTime?` - When deprecated
- `SupportedUntil: DateTime?` - End of support date
- `CreatedAt: DateTime`

**Factory Method**:
```csharp
public static Result<DataSourceEngineVersion> Create(
    string version,
    string description)
```

**Business Methods**:
- `Activate()` - Make available for deployments
- `Deactivate()` - Remove from available engines
- `Deprecate(DateTime supportedUntil)` - Mark as deprecated

**Domain Events**:
- `DataSourceEngineVersionCreatedEvent`
- `DataSourceEngineVersionActivatedEvent`
- `DataSourceEngineVersionDeactivatedEvent`
- `DataSourceEngineVersionDeprecatedEvent`

---

### ComponentEngineMapping (Entity)

**Purpose**: Maps application component definitions to required engine versions

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `ApplicationReleaseId: Guid` - Foreign key
- `ComponentType: ComponentType` - Navigation, Page, DataSource
- `ComponentDefinitionId: Guid` - ID of NavigationDefinition/PageDefinition/DataSourceDefinition
- `RequiredEngineVersionId: Guid` - Required engine version
- `CreatedAt: DateTime`

**Factory Method**:
```csharp
public static Result<ComponentEngineMapping> Create(
    Guid applicationReleaseId,
    ComponentType componentType,
    Guid componentDefinitionId,
    Guid requiredEngineVersionId)
```

**Domain Events**:
- `ComponentEngineMappingCreatedEvent`

---

## Value Objects

### RuntimeInstanceStatus (Enum)
```csharp
public enum RuntimeInstanceStatus
{
    Created = 0,
    Starting = 1,
    Running = 2,
    Stopping = 3,
    Stopped = 4,
    Failed = 5
}
```

### ComponentType (Enum)
```csharp
public enum ComponentType
{
    Navigation = 0,
    Page = 1,
    DataSource = 2
}
```

---

## Repository Interfaces

### IRuntimeInstanceRepository
```csharp
public interface IRuntimeInstanceRepository
{
    Task<RuntimeInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RuntimeInstance?> GetByEnvironmentAsync(Guid tenantApplicationEnvironmentId, CancellationToken cancellationToken = default);
    Task<List<RuntimeInstance>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<RuntimeInstance>> GetRunningInstancesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RuntimeInstance instance, CancellationToken cancellationToken = default);
    void Update(RuntimeInstance instance);
    void Remove(RuntimeInstance instance);
}
```

### INavigationEngineVersionRepository
```csharp
public interface INavigationEngineVersionRepository
{
    Task<NavigationEngineVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NavigationEngineVersion?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);
    Task<List<NavigationEngineVersion>> GetActiveVersionsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(NavigationEngineVersion version, CancellationToken cancellationToken = default);
    void Update(NavigationEngineVersion version);
}
```

### IPageEngineVersionRepository
```csharp
public interface IPageEngineVersionRepository
{
    Task<PageEngineVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PageEngineVersion?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);
    Task<List<PageEngineVersion>> GetActiveVersionsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PageEngineVersion version, CancellationToken cancellationToken = default);
    void Update(PageEngineVersion version);
}
```

### IDataSourceEngineVersionRepository
```csharp
public interface IDataSourceEngineVersionRepository
{
    Task<DataSourceEngineVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataSourceEngineVersion?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);
    Task<List<DataSourceEngineVersion>> GetActiveVersionsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(DataSourceEngineVersion version, CancellationToken cancellationToken = default);
    void Update(DataSourceEngineVersion version);
}
```

### IComponentEngineMappingRepository
```csharp
public interface IComponentEngineMappingRepository
{
    Task<List<ComponentEngineMapping>> GetByApplicationReleaseAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default);
    Task<ComponentEngineMapping?> GetByComponentAsync(Guid componentDefinitionId, CancellationToken cancellationToken = default);
    Task AddAsync(ComponentEngineMapping mapping, CancellationToken cancellationToken = default);
}
```

---

## Domain Services

### ICompatibilityCheckService
```csharp
public interface ICompatibilityCheckService
{
    Task<Result> ValidateEngineCompatibilityAsync(
        Guid applicationReleaseId,
        Guid navigationEngineVersionId,
        Guid pageEngineVersionId,
        Guid dataSourceEngineVersionId,
        CancellationToken cancellationToken = default);
}
```

---

## How It Works

### 1. Platform Creates Engine Versions
```
Platform Admin creates:
├── NavigationEngineVersion v1 (basic sidebar)
├── NavigationEngineVersion v2 (advanced sidebar + breadcrumbs)
├── PageEngineVersion v1 (basic grid layout)
├── PageEngineVersion v2 (advanced flex layout)
├── DataSourceEngineVersion v1 (REST API only)
└── DataSourceEngineVersion v2 (REST API + GraphQL)
```

### 2. Application Release Specifies Requirements
```
When creating ApplicationRelease:
├── System analyzes NavigationDefinitions
├── System analyzes PageDefinitions
├── System analyzes DataSourceDefinitions
├── System creates ComponentEngineMapping for each component
└── System validates all required engines exist
```

### 3. Deployment Validates Compatibility
```
When deploying to environment:
├── TenantApplication module requests deployment
├── AppRuntime validates engine compatibility
├── If compatible → Create RuntimeInstance
└── If incompatible → Block deployment
```

### 4. Runtime Loads Correct Engines
```
When user accesses application:
├── Load RuntimeInstance
├── Load NavigationEngineVersion
├── Load PageEngineVersion
├── Load DataSourceEngineVersion
├── Execute using correct engine implementations
└── Render application
```



