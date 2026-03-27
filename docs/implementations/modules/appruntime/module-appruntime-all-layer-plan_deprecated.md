# AppRuntime Module - Complete Implementation Plan

**Status**: 🔄 Updated - Versioning & Component Registry
**Last Updated**: 2026-02-11
**Version**: 2.0 (Added Runtime Versioning & Component Compatibility)

---

## Overview

The **AppRuntime** module provides the **versioned runtime environment** for executing tenant applications built with the AppBuilder module. It manages runtime instances, component loading, version compatibility, configuration, metrics, and lifecycle operations for running applications.

### Purpose

- **Runtime Management**: Start, stop, and manage running application instances
- **Version Compatibility**: Ensure runtime can load application releases with supported component types
- **Component Loading**: Dynamically load and render components based on type and version
- **Component Registry**: Register component loaders/renderers for different component types
- **Configuration**: Apply runtime-specific configuration to applications
- **Metrics & Monitoring**: Track performance, usage, and health metrics
- **Feature Evaluation**: Evaluate feature flags at runtime based on tenant/user context
- **Lifecycle Management**: Handle application startup, shutdown, and restart operations
- **Multi-Tenancy**: Ensure complete tenant isolation for runtime instances

### Key Concepts

- **RuntimeVersion**: Version of the AppRuntime itself (e.g., 1.0.0, 1.1.0, 2.0.0)
- **ComponentTypeSupport**: Which component types a runtime version supports
- **RuntimeInstance**: A running instance of an ApplicationRelease with its own configuration and state
- **ComponentLoader**: Registered handler for loading/rendering specific component types
- **CompatibilityCheck**: Validation that runtime can load all components in a release
- **RuntimeConfiguration**: Runtime-specific settings that override application defaults
- **RuntimeMetrics**: Performance and usage metrics collected during runtime
- **RuntimeLog**: Audit trail of runtime operations and events
- **Instance Status**: Current state of a runtime instance (Starting, Running, Stopping, Stopped, Failed)

### Module Dependencies

- **TenantApplication Module**: Runtime instances execute tenant-installed applications
  - Uses `TenantApplication.Contracts.IApplicationResolverService` for URL-based application resolution
  - Resolves `{tenantSlug}/{appSlug}/{environment}` to ApplicationReleaseId
  - **TenantApplication calls AppRuntime.Contracts.ICompatibilityCheckService** to validate compatibility before deployment
- **AppBuilder Module**: Runtime instances execute ApplicationReleases (not Applications)
  - Uses `AppBuilder.Contracts.IApplicationReleaseService` for component type discovery
  - Uses `AppBuilder.Contracts` component services for loading components
- **Feature Module**: Feature flag evaluation at runtime
- **Identity Module**: User permissions for runtime management

### Services Provided to Other Modules

**AppRuntime owns and provides the following services via AppRuntime.Contracts**:

- **ICompatibilityCheckService**: Validates that a runtime version can load an application release
  - Used by TenantApplication module before deploying to an environment
  - Used by AppRuntime itself when creating runtime instances
  - In Monolith topology: Called via DI
  - In Microservices topology: Called via HTTP/gRPC

---

## URL-Based Application Loading (Environment Support)

### URL Pattern

Users access tenant applications using the following URL pattern:

```
datarizen.com/{tenantSlug}/{appSlug}/{environment?}
```

**Examples**:
- `datarizen.com/acme-corp/crm` → Production (default)
- `datarizen.com/acme-corp/crm/development` → Development
- `datarizen.com/acme-corp/crm/staging` → Staging
- `datarizen.com/acme-corp/crm/production` → Production

### URL Resolution and Loading Flow

```
1. USER ACCESSES URL
   ↓
   datarizen.com/{tenantSlug}/{appSlug}/{environment?}
   ↓
2. APPRUNTIME RECEIVES REQUEST
   ↓
   POST /api/appruntime/instances/load-from-url
   Body: { tenantSlug, appSlug, environment, userId }
   ↓
3. RESOLVE APPLICATION (via TenantApplication.Contracts)
   ├─ Call IApplicationResolverService.ResolveByUrlAsync()
   ├─ Resolves Tenant by slug
   ├─ Resolves TenantApplication by slug
   ├─ Resolves Environment by type (default: Production)
   └─ Returns: ApplicationReleaseId, TenantId, Configuration
   ↓
4. GET CURRENT RUNTIME VERSION
   ├─ Query: GetCurrentAsync()
   └─ Result: RuntimeVersion (e.g., 1.0.0)
   ↓
5. CHECK COMPATIBILITY
   ├─ Call ICompatibilityCheckService.CheckCompatibilityAsync()
   ├─ Get all component types in ApplicationRelease
   ├─ Check if RuntimeVersion supports all component types
   └─ Result: CompatibilityCheckResultDto
   ↓
6. CREATE RUNTIME INSTANCE
   ├─ If compatible → RuntimeInstance.Create()
   ├─ Name: "{tenantSlug}/{appSlug}/{environment}"
   ├─ Configuration: Environment-specific configuration
   ├─ Store compatibility check results
   └─ Result: RuntimeInstanceId
   ↓
7. LOAD COMPONENTS
   ├─ For each component in ApplicationRelease:
   │   ├─ Get component type
   │   ├─ Find registered ComponentLoader
   │   └─ Load/render component
   └─ All components loaded successfully
   ↓
8. RUNTIME INSTANCE RUNNING
   └─ Application available to user
```

### Key Features

- **Environment Isolation**: Each environment (dev/staging/production) can have different ApplicationRelease deployed
- **Compatibility Validation**: Before loading, AppRuntime validates it can load all components in the release
- **Fail-Fast**: If incompatible, loading fails immediately with detailed error message
- **Environment Configuration**: Each environment can have different configuration overrides
- **URL-Friendly**: Uses slugs instead of GUIDs for user-friendly URLs

---

## Versioning Strategy

### Runtime Version Compatibility

```
AppRuntime Version → Supported Component Types
├── v1.0.0 → [NavigationComponent, PageComponent]
├── v1.1.0 → [NavigationComponent, PageComponent, FormComponent]
├── v1.2.0 → [NavigationComponent, PageComponent, FormComponent, DashboardComponent]
└── v2.0.0 → [NavigationComponent v2, PageComponent v2, FormComponent, DashboardComponent, ReportComponent]
```

**Key Rules**:
- ✅ Each **AppRuntime version** declares which **component types** it supports
- ✅ Before loading an **ApplicationRelease**, runtime checks if it supports **all component types** in that release
- ✅ If runtime doesn't support a component type → **reject loading** with clear error message
- ✅ Component types can have **versions** (e.g., NavigationComponent v1 vs v2)
- ✅ Runtime can support **multiple versions** of the same component type (backward compatibility)
- ✅ ApplicationRelease stores **component type + version** for each component

### Compatibility Check Workflow

```
1. TENANT ACTIVATES APPLICATION
   ↓
2. TenantApplication.Activate()
   ↓
3. RUNTIME INSTANCE CREATED
   ├─ Load ApplicationRelease
   ├─ Get all components in release
   ├─ Check runtime supports all component types
   └─ If incompatible → FAIL with error
   ↓
4. RUNTIME LOADS COMPONENTS
   ├─ For each component:
   │   ├─ Get component type + version
   │   ├─ Find registered ComponentLoader
   │   └─ Load/render component
   └─ All components loaded successfully
   ↓
5. RUNTIME INSTANCE RUNNING
```

### Component Type Evolution Example

```
Timeline:
├── 2026-01 → AppRuntime v1.0.0 released
│             Supports: NavigationComponent v1, PageComponent v1
│
├── 2026-02 → New requirement: Forms needed
│             AppRuntime v1.1.0 released
│             Supports: NavigationComponent v1, PageComponent v1, FormComponent v1
│
├── 2026-03 → Navigation redesign (breaking change)
│             AppRuntime v2.0.0 released
│             Supports: NavigationComponent v2, PageComponent v1, FormComponent v1
│             (Still supports NavigationComponent v1 for backward compatibility)
│
└── 2026-04 → Application "CRM" released with FormComponent v1
              ├─ Can run on AppRuntime v1.1.0 ✅
              ├─ Can run on AppRuntime v2.0.0 ✅
              └─ Cannot run on AppRuntime v1.0.0 ❌ (no FormComponent support)
```

---

## Architecture

### Domain Model

```
RuntimeVersion (Aggregate Root)
├── Id: Guid
├── Version: string (semver: 1.0.0, 1.1.0, 2.0.0)
├── IsCurrent: bool (only one current version)
├── ReleasedAt: DateTime
├── ReleaseNotes: string
├── CreatedAt: DateTime
└── DomainEvents: List<IDomainEvent>

ComponentTypeSupport (Entity)
├── Id: Guid
├── RuntimeVersionId: Guid (FK to appruntime.runtime_versions)
├── ComponentType: string (e.g., "NavigationComponent", "PageComponent", "FormComponent")
├── ComponentVersion: string (e.g., "1.0", "2.0")
├── LoaderClassName: string (e.g., "NavigationComponentLoader")
├── IsEnabled: bool
├── CreatedAt: DateTime

RuntimeInstance (Aggregate Root)
├── Id: Guid
├── TenantId: Guid (FK to tenant.tenants)
├── TenantApplicationId: Guid (FK to tenantapplication.tenant_applications)
├── ApplicationReleaseId: Guid (FK to appbuilder.application_releases)
├── RuntimeVersionId: Guid (FK to appruntime.runtime_versions)
├── Name: string
├── Status: InstanceStatus (Starting, Running, Stopping, Stopped, Failed)
├── Configuration: string (JSON)
├── CompatibilityCheckPassed: bool
├── CompatibilityCheckDetails: string (JSON - which components checked)
├── StartedAt: DateTime?
├── StoppedAt: DateTime?
├── LastHealthCheckAt: DateTime?
├── HealthStatus: HealthStatus (Healthy, Degraded, Unhealthy)
├── CreatedAt: DateTime
├── UpdatedAt: DateTime
└── DomainEvents: List<IDomainEvent>

RuntimeConfiguration
├── Id: Guid
├── RuntimeInstanceId: Guid (FK to appruntime.runtime_instances)
├── Key: string
├── Value: string
├── DataType: ConfigDataType (String, Number, Boolean, JSON)
├── Source: ConfigSource (Default, Override, Environment)
├── CreatedAt: DateTime
├── UpdatedAt: DateTime

RuntimeMetrics
├── Id: Guid
├── RuntimeInstanceId: Guid (FK to appruntime.runtime_instances)
├── MetricType: MetricType (CPU, Memory, Requests, Errors, ResponseTime)
├── MetricName: string
├── Value: decimal
├── Unit: string
├── Timestamp: DateTime
├── Tags: string (JSON)

RuntimeLog
├── Id: Guid
├── RuntimeInstanceId: Guid (FK to appruntime.runtime_instances)
├── Level: LogLevel (Debug, Info, Warning, Error, Critical)
├── Message: string
├── Exception: string?
├── Context: string (JSON)
├── Timestamp: DateTime
```

### Entity Relationships

```
RuntimeVersion (1) ────< (many) ComponentTypeSupport
                │
                │
                └────< (many) RuntimeInstance
                                │
                                ├────< (many) RuntimeConfiguration
                                ├────< (many) RuntimeMetrics
                                └────< (many) RuntimeLog

TenantApplication (1) ────< (many) RuntimeInstance
ApplicationRelease (1) ────< (many) RuntimeInstance
```

### Enums

```csharp
public enum InstanceStatus
{
    Starting = 0,
    Running = 1,
    Stopping = 2,
    Stopped = 3,
    Failed = 4
}

public enum HealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2
}

public enum ConfigSource
{
    Default = 0,
    Override = 1,
    Environment = 2
}

public enum MetricType
{
    CPU = 0,
    Memory = 1,
    Requests = 2,
    Errors = 3,
    ResponseTime = 4,
    Custom = 99
}

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
```

---

## Phase 1: Domain Layer (10 hours)

### 1.1: RuntimeInstance Entity (5 hours)

**File**: `AppRuntime.Domain/Entities/RuntimeInstance.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppRuntime.Domain.Events;

namespace AppRuntime.Domain.Entities;

public sealed class RuntimeInstance : Entity<Guid>, IAggregateRoot
{
    private RuntimeInstance() { } // EF Core

    private RuntimeInstance(
        Guid id,
        Guid tenantId,
        Guid tenantApplicationId,
        Guid applicationReleaseId,
        Guid runtimeVersionId,
        string name,
        string configuration,
        bool compatibilityCheckPassed,
        string compatibilityCheckDetails,
        DateTime createdAt)
    {
        Id = id;
        TenantId = tenantId;
        TenantApplicationId = tenantApplicationId;
        ApplicationReleaseId = applicationReleaseId;
        RuntimeVersionId = runtimeVersionId;
        Name = name;
        Status = InstanceStatus.Stopped;
        Configuration = configuration;
        CompatibilityCheckPassed = compatibilityCheckPassed;
        CompatibilityCheckDetails = compatibilityCheckDetails;
        HealthStatus = HealthStatus.Healthy;
        CreatedAt = createdAt;
    }

    public Guid TenantId { get; private set; }
    public Guid TenantApplicationId { get; private set; }
    public Guid ApplicationReleaseId { get; private set; }
    public Guid RuntimeVersionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public InstanceStatus Status { get; private set; }
    public string Configuration { get; private set; } = string.Empty; // JSON
    public bool CompatibilityCheckPassed { get; private set; }
    public string CompatibilityCheckDetails { get; private set; } = string.Empty; // JSON
    public DateTime? StartedAt { get; private set; }
    public DateTime? StoppedAt { get; private set; }
    public DateTime? LastHealthCheckAt { get; private set; }
    public HealthStatus HealthStatus { get; private set; }

    /// <summary>
    /// Create a new RuntimeInstance with compatibility check
    /// </summary>
    public static Result<RuntimeInstance> Create(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid applicationReleaseId,
        Guid runtimeVersionId,
        string name,
        string configuration,
        bool compatibilityCheckPassed,
        string compatibilityCheckDetails,
        IDateTimeProvider dateTimeProvider)
    {
        if (tenantId == Guid.Empty)
            return Result<RuntimeInstance>.Failure(Error.Validation("RuntimeInstance.InvalidTenantId", "Tenant ID is required"));

        if (tenantApplicationId == Guid.Empty)
            return Result<RuntimeInstance>.Failure(Error.Validation("RuntimeInstance.InvalidTenantApplicationId", "TenantApplication ID is required"));

        if (applicationReleaseId == Guid.Empty)
            return Result<RuntimeInstance>.Failure(Error.Validation("RuntimeInstance.InvalidReleaseId", "ApplicationRelease ID is required"));

        if (runtimeVersionId == Guid.Empty)
            return Result<RuntimeInstance>.Failure(Error.Validation("RuntimeInstance.InvalidRuntimeVersionId", "RuntimeVersion ID is required"));

        if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 200)
            return Result<RuntimeInstance>.Failure(Error.Validation("RuntimeInstance.InvalidName", "Name must be between 3 and 200 characters"));

        if (!compatibilityCheckPassed)
            return Result<RuntimeInstance>.Failure(Error.Validation("RuntimeInstance.IncompatibleRelease",
                $"Runtime version does not support all component types in this release. Details: {compatibilityCheckDetails}"));

        var instance = new RuntimeInstance(
            Guid.NewGuid(),
            tenantId,
            tenantApplicationId,
            applicationReleaseId,
            runtimeVersionId,
            name,
            configuration ?? "{}",
            compatibilityCheckPassed,
            compatibilityCheckDetails,
            dateTimeProvider.UtcNow);

        instance.AddDomainEvent(new RuntimeInstanceCreatedEvent(
            instance.Id,
            instance.TenantId,
            instance.ApplicationReleaseId,
            instance.RuntimeVersionId));

        return Result<RuntimeInstance>.Success(instance);
    }

    public Result<Unit> Start(IDateTimeProvider dateTimeProvider)
    {
        if (Status == InstanceStatus.Running)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.AlreadyRunning", "Instance is already running"));

        if (Status == InstanceStatus.Starting)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.AlreadyStarting", "Instance is already starting"));

        if (!CompatibilityCheckPassed)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.IncompatibleRelease",
                "Cannot start instance with incompatible application release"));

        Status = InstanceStatus.Starting;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new RuntimeInstanceStartingEvent(Id, TenantId, ApplicationReleaseId, RuntimeVersionId));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> MarkAsRunning(IDateTimeProvider dateTimeProvider)
    {
        if (Status != InstanceStatus.Starting)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.NotStarting", "Instance is not in starting state"));

        Status = InstanceStatus.Running;
        StartedAt = dateTimeProvider.UtcNow;
        StoppedAt = null;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new RuntimeInstanceStartedEvent(Id, TenantId, ApplicationReleaseId, RuntimeVersionId));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> Stop(IDateTimeProvider dateTimeProvider)
    {
        if (Status == InstanceStatus.Stopped)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.AlreadyStopped", "Instance is already stopped"));

        if (Status == InstanceStatus.Stopping)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.AlreadyStopping", "Instance is already stopping"));

        Status = InstanceStatus.Stopping;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new RuntimeInstanceStoppingEvent(Id, TenantId, ApplicationReleaseId));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> MarkAsStopped(IDateTimeProvider dateTimeProvider)
    {
        if (Status != InstanceStatus.Stopping)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.NotStopping", "Instance is not in stopping state"));

        Status = InstanceStatus.Stopped;
        StoppedAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new RuntimeInstanceStoppedEvent(Id, TenantId, ApplicationReleaseId));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> MarkAsFailed(string reason, IDateTimeProvider dateTimeProvider)
    {
        Status = InstanceStatus.Failed;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new RuntimeInstanceFailedEvent(Id, TenantId, ApplicationReleaseId, reason));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> UpdateHealthStatus(HealthStatus healthStatus, IDateTimeProvider dateTimeProvider)
    {
        HealthStatus = healthStatus;
        LastHealthCheckAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> UpdateConfiguration(string configuration, IDateTimeProvider dateTimeProvider)
    {
        if (Status == InstanceStatus.Running)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.CannotUpdateWhileRunning", "Cannot update configuration while instance is running"));

        Configuration = configuration ?? "{}";
        UpdatedAt = dateTimeProvider.UtcNow;

        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 1.2: RuntimeVersion Entity (2 hours)

**File**: `AppRuntime.Domain/Entities/RuntimeVersion.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppRuntime.Domain.Events;

namespace AppRuntime.Domain.Entities;

/// <summary>
/// Represents a version of the AppRuntime with its supported component types
/// </summary>
public sealed class RuntimeVersion : Entity<Guid>, IAggregateRoot
{
    private RuntimeVersion() { } // EF Core

    private RuntimeVersion(
        Guid id,
        string version,
        bool isCurrent,
        string releaseNotes,
        DateTime releasedAt,
        DateTime createdAt)
    {
        Id = id;
        Version = version;
        IsCurrent = isCurrent;
        ReleaseNotes = releaseNotes;
        ReleasedAt = releasedAt;
        CreatedAt = createdAt;
    }

    public string Version { get; private set; } = string.Empty; // Semver: 1.0.0, 1.1.0, 2.0.0
    public bool IsCurrent { get; private set; }
    public DateTime ReleasedAt { get; private set; }
    public string ReleaseNotes { get; private set; } = string.Empty;

    /// <summary>
    /// Create a new RuntimeVersion
    /// </summary>
    public static Result<RuntimeVersion> Create(
        string version,
        bool isCurrent,
        string releaseNotes,
        DateTime releasedAt,
        IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(version))
            return Result<RuntimeVersion>.Failure(Error.Validation("RuntimeVersion.InvalidVersion", "Version is required"));

        // Basic semver validation (e.g., 1.0.0, 1.1.0, 2.0.0)
        if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"))
            return Result<RuntimeVersion>.Failure(Error.Validation("RuntimeVersion.InvalidVersionFormat", "Version must be in semver format (e.g., 1.0.0)"));

        var runtimeVersion = new RuntimeVersion(
            Guid.NewGuid(),
            version,
            isCurrent,
            releaseNotes ?? string.Empty,
            releasedAt,
            dateTimeProvider.UtcNow);

        runtimeVersion.AddDomainEvent(new RuntimeVersionCreatedEvent(runtimeVersion.Id, runtimeVersion.Version, runtimeVersion.IsCurrent));

        return Result<RuntimeVersion>.Success(runtimeVersion);
    }

    /// <summary>
    /// Mark this version as current (only one version can be current at a time)
    /// </summary>
    public Result<Unit> MarkAsCurrent(IDateTimeProvider dateTimeProvider)
    {
        if (IsCurrent)
            return Result<Unit>.Failure(Error.Validation("RuntimeVersion.AlreadyCurrent", "This version is already marked as current"));

        IsCurrent = true;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new RuntimeVersionMarkedAsCurrentEvent(Id, Version));

        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Unmark this version as current
    /// </summary>
    public Result<Unit> UnmarkAsCurrent(IDateTimeProvider dateTimeProvider)
    {
        if (!IsCurrent)
            return Result<Unit>.Failure(Error.Validation("RuntimeVersion.NotCurrent", "This version is not marked as current"));

        IsCurrent = false;
        UpdatedAt = dateTimeProvider.UtcNow;

        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 1.3: ComponentTypeSupport Entity (2 hours)

**File**: `AppRuntime.Domain/Entities/ComponentTypeSupport.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppRuntime.Domain.Events;

namespace AppRuntime.Domain.Entities;

/// <summary>
/// Declares which component types a RuntimeVersion supports
/// </summary>
public sealed class ComponentTypeSupport : Entity<Guid>
{
    private ComponentTypeSupport() { } // EF Core

    private ComponentTypeSupport(
        Guid id,
        Guid runtimeVersionId,
        string componentType,
        string componentVersion,
        string loaderClassName,
        bool isEnabled,
        DateTime createdAt)
    {
        Id = id;
        RuntimeVersionId = runtimeVersionId;
        ComponentType = componentType;
        ComponentVersion = componentVersion;
        LoaderClassName = loaderClassName;
        IsEnabled = isEnabled;
        CreatedAt = createdAt;
    }

    public Guid RuntimeVersionId { get; private set; }
    public string ComponentType { get; private set; } = string.Empty; // e.g., "NavigationComponent", "PageComponent"
    public string ComponentVersion { get; private set; } = string.Empty; // e.g., "1.0", "2.0"
    public string LoaderClassName { get; private set; } = string.Empty; // e.g., "NavigationComponentLoader"
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Create a new ComponentTypeSupport
    /// </summary>
    public static Result<ComponentTypeSupport> Create(
        Guid runtimeVersionId,
        string componentType,
        string componentVersion,
        string loaderClassName,
        bool isEnabled,
        IDateTimeProvider dateTimeProvider)
    {
        if (runtimeVersionId == Guid.Empty)
            return Result<ComponentTypeSupport>.Failure(Error.Validation("ComponentTypeSupport.InvalidRuntimeVersionId", "RuntimeVersion ID is required"));

        if (string.IsNullOrWhiteSpace(componentType) || componentType.Length < 3 || componentType.Length > 100)
            return Result<ComponentTypeSupport>.Failure(Error.Validation("ComponentTypeSupport.InvalidComponentType", "ComponentType must be between 3 and 100 characters"));

        if (string.IsNullOrWhiteSpace(componentVersion))
            return Result<ComponentTypeSupport>.Failure(Error.Validation("ComponentTypeSupport.InvalidComponentVersion", "ComponentVersion is required"));

        if (string.IsNullOrWhiteSpace(loaderClassName) || loaderClassName.Length < 3 || loaderClassName.Length > 200)
            return Result<ComponentTypeSupport>.Failure(Error.Validation("ComponentTypeSupport.InvalidLoaderClassName", "LoaderClassName must be between 3 and 200 characters"));

        var support = new ComponentTypeSupport(
            Guid.NewGuid(),
            runtimeVersionId,
            componentType,
            componentVersion,
            loaderClassName,
            isEnabled,
            dateTimeProvider.UtcNow);

        return Result<ComponentTypeSupport>.Success(support);
    }

    /// <summary>
    /// Enable this component type support
    /// </summary>
    public Result<Unit> Enable(IDateTimeProvider dateTimeProvider)
    {
        if (IsEnabled)
            return Result<Unit>.Failure(Error.Validation("ComponentTypeSupport.AlreadyEnabled", "Component type support is already enabled"));

        IsEnabled = true;
        UpdatedAt = dateTimeProvider.UtcNow;

        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Disable this component type support
    /// </summary>
    public Result<Unit> Disable(IDateTimeProvider dateTimeProvider)
    {
        if (!IsEnabled)
            return Result<Unit>.Failure(Error.Validation("ComponentTypeSupport.AlreadyDisabled", "Component type support is already disabled"));

        IsEnabled = false;
        UpdatedAt = dateTimeProvider.UtcNow;

        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 1.4: RuntimeConfiguration Entity (1.5 hours)

**File**: `AppRuntime.Domain/Entities/RuntimeConfiguration.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;

namespace AppRuntime.Domain.Entities;

public sealed class RuntimeConfiguration : Entity<Guid>
{
    private RuntimeConfiguration() { } // EF Core

    private RuntimeConfiguration(
        Guid id,
        Guid runtimeInstanceId,
        string key,
        string value,
        ConfigDataType dataType,
        ConfigSource source,
        DateTime createdAt)
    {
        Id = id;
        RuntimeInstanceId = runtimeInstanceId;
        Key = key;
        Value = value;
        DataType = dataType;
        Source = source;
        CreatedAt = createdAt;
    }

    public Guid RuntimeInstanceId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public ConfigDataType DataType { get; private set; }
    public ConfigSource Source { get; private set; }

    public static Result<RuntimeConfiguration> Create(
        Guid runtimeInstanceId,
        string key,
        string value,
        ConfigDataType dataType,
        ConfigSource source,
        IDateTimeProvider dateTimeProvider)
    {
        if (runtimeInstanceId == Guid.Empty)
            return Result<RuntimeConfiguration>.Failure(Error.Validation("RuntimeConfiguration.InvalidInstanceId", "Runtime Instance ID is required"));

        if (string.IsNullOrWhiteSpace(key) || key.Length < 2 || key.Length > 100)
            return Result<RuntimeConfiguration>.Failure(Error.Validation("RuntimeConfiguration.InvalidKey", "Key must be between 2 and 100 characters"));

        var config = new RuntimeConfiguration(
            Guid.NewGuid(),
            runtimeInstanceId,
            key,
            value ?? string.Empty,
            dataType,
            source,
            dateTimeProvider.UtcNow);

        return Result<RuntimeConfiguration>.Success(config);
    }

    public Result<Unit> UpdateValue(string value, IDateTimeProvider dateTimeProvider)
    {
        Value = value ?? string.Empty;
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 1.3: RuntimeMetrics Entity (1.5 hours)

**File**: `AppRuntime.Domain/Entities/RuntimeMetrics.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;

namespace AppRuntime.Domain.Entities;

public sealed class RuntimeMetrics : Entity<Guid>
{
    private RuntimeMetrics() { } // EF Core

    private RuntimeMetrics(
        Guid id,
        Guid runtimeInstanceId,
        MetricType metricType,
        string metricName,
        decimal value,
        string unit,
        string tags,
        DateTime timestamp)
    {
        Id = id;
        RuntimeInstanceId = runtimeInstanceId;
        MetricType = metricType;
        MetricName = metricName;
        Value = value;
        Unit = unit;
        Tags = tags;
        Timestamp = timestamp;
    }

    public Guid RuntimeInstanceId { get; private set; }
    public MetricType MetricType { get; private set; }
    public string MetricName { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string Tags { get; private set; } = string.Empty; // JSON
    public DateTime Timestamp { get; private set; }

    public static Result<RuntimeMetrics> Create(
        Guid runtimeInstanceId,
        MetricType metricType,
        string metricName,
        decimal value,
        string unit,
        string? tags,
        IDateTimeProvider dateTimeProvider)
    {
        if (runtimeInstanceId == Guid.Empty)
            return Result<RuntimeMetrics>.Failure(Error.Validation("RuntimeMetrics.InvalidInstanceId", "Runtime Instance ID is required"));

        if (string.IsNullOrWhiteSpace(metricName))
            return Result<RuntimeMetrics>.Failure(Error.Validation("RuntimeMetrics.InvalidMetricName", "Metric name is required"));

        var metrics = new RuntimeMetrics(
            Guid.NewGuid(),
            runtimeInstanceId,
            metricType,
            metricName,
            value,
            unit ?? string.Empty,
            tags ?? "{}",
            dateTimeProvider.UtcNow);

        return Result<RuntimeMetrics>.Success(metrics);
    }
}
```

---

### 1.5: Domain Events (1.5 hours)

**File**: `AppRuntime.Domain/Events/RuntimeVersionEvents.cs`

```csharp
using BuildingBlocks.Kernel.Domain;

namespace AppRuntime.Domain.Events;

/// <summary>
/// Raised when a new RuntimeVersion is created
/// </summary>
public sealed record RuntimeVersionCreatedEvent(
    Guid RuntimeVersionId,
    string Version,
    bool IsCurrent) : IDomainEvent;

/// <summary>
/// Raised when a RuntimeVersion is marked as current
/// </summary>
public sealed record RuntimeVersionMarkedAsCurrentEvent(
    Guid RuntimeVersionId,
    string Version) : IDomainEvent;
```

**File**: `AppRuntime.Domain/Events/RuntimeInstanceEvents.cs`

```csharp
using BuildingBlocks.Kernel.Domain;

namespace AppRuntime.Domain.Events;

public sealed record RuntimeInstanceCreatedEvent(
    Guid InstanceId,
    Guid TenantId,
    Guid ApplicationReleaseId,
    Guid RuntimeVersionId) : IDomainEvent;

public sealed record RuntimeInstanceStartingEvent(
    Guid InstanceId,
    Guid TenantId,
    Guid ApplicationReleaseId,
    Guid RuntimeVersionId) : IDomainEvent;

public sealed record RuntimeInstanceStartedEvent(
    Guid InstanceId,
    Guid TenantId,
    Guid ApplicationReleaseId,
    Guid RuntimeVersionId) : IDomainEvent;

public sealed record RuntimeInstanceStoppingEvent(
    Guid InstanceId,
    Guid TenantId,
    Guid ApplicationReleaseId) : IDomainEvent;

public sealed record RuntimeInstanceStoppedEvent(
    Guid InstanceId,
    Guid TenantId,
    Guid ApplicationReleaseId) : IDomainEvent;

public sealed record RuntimeInstanceFailedEvent(
    Guid InstanceId,
    Guid TenantId,
    Guid ApplicationReleaseId,
    string Reason) : IDomainEvent;
```

---

### 1.6: Repository Interfaces (2.5 hours)

**File**: `AppRuntime.Domain/Repositories/IRuntimeVersionRepository.cs`

```csharp
using BuildingBlocks.Kernel.Persistence;
using AppRuntime.Domain.Entities;

namespace AppRuntime.Domain.Repositories;

public interface IRuntimeVersionRepository : IRepository<RuntimeVersion, Guid>
{
    /// <summary>
    /// Get the current runtime version
    /// </summary>
    Task<RuntimeVersion?> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get runtime version by version string
    /// </summary>
    Task<RuntimeVersion?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a version already exists
    /// </summary>
    Task<bool> ExistsByVersionAsync(string version, CancellationToken cancellationToken = default);
}
```

**File**: `AppRuntime.Domain/Repositories/IComponentTypeSupportRepository.cs`

```csharp
using BuildingBlocks.Kernel.Persistence;
using AppRuntime.Domain.Entities;

namespace AppRuntime.Domain.Repositories;

public interface IComponentTypeSupportRepository : IRepository<ComponentTypeSupport, Guid>
{
    /// <summary>
    /// Get all component type supports for a runtime version
    /// </summary>
    Task<IEnumerable<ComponentTypeSupport>> GetByRuntimeVersionAsync(Guid runtimeVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get enabled component type supports for a runtime version
    /// </summary>
    Task<IEnumerable<ComponentTypeSupport>> GetEnabledByRuntimeVersionAsync(Guid runtimeVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a runtime version supports a specific component type
    /// </summary>
    Task<bool> SupportsComponentTypeAsync(Guid runtimeVersionId, string componentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get component type support by runtime version and component type
    /// </summary>
    Task<ComponentTypeSupport?> GetByRuntimeVersionAndComponentTypeAsync(Guid runtimeVersionId, string componentType, CancellationToken cancellationToken = default);
}
```

**File**: `AppRuntime.Domain/Repositories/IRuntimeInstanceRepository.cs`

```csharp
using BuildingBlocks.Kernel.Persistence;
using AppRuntime.Domain.Entities;

namespace AppRuntime.Domain.Repositories;

public interface IRuntimeInstanceRepository : IRepository<RuntimeInstance, Guid>
{
    Task<IEnumerable<RuntimeInstance>> GetAllByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RuntimeInstance>> GetAllByTenantApplicationAsync(Guid tenantApplicationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RuntimeInstance>> GetAllByApplicationReleaseAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RuntimeInstance>> GetByStatusAsync(InstanceStatus status, CancellationToken cancellationToken = default);
    Task<RuntimeInstance?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);
}

public interface IRuntimeConfigurationRepository : IRepository<RuntimeConfiguration, Guid>
{
    Task<IEnumerable<RuntimeConfiguration>> GetAllByInstanceAsync(Guid runtimeInstanceId, CancellationToken cancellationToken = default);
    Task<RuntimeConfiguration?> GetByKeyAsync(Guid runtimeInstanceId, string key, CancellationToken cancellationToken = default);
}

public interface IRuntimeMetricsRepository : IRepository<RuntimeMetrics, Guid>
{
    Task<IEnumerable<RuntimeMetrics>> GetAllByInstanceAsync(Guid runtimeInstanceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RuntimeMetrics>> GetByTimeRangeAsync(Guid runtimeInstanceId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public interface IAppRuntimeUnitOfWork : IUnitOfWork
{
}
```

**Deliverable**: Domain layer complete with entities (RuntimeVersion, ComponentTypeSupport, RuntimeInstance, RuntimeConfiguration, RuntimeMetrics), events, repository interfaces.

---

## Phase 2: Application Layer (14 hours)

### 2.1: RuntimeVersion DTOs & Commands (3 hours)

**File**: `AppRuntime.Application/DTOs/RuntimeVersionDto.cs`

```csharp
namespace AppRuntime.Application.DTOs;

public sealed record RuntimeVersionDto(
    Guid Id,
    string Version,
    bool IsCurrent,
    DateTime ReleasedAt,
    string ReleaseNotes,
    DateTime CreatedAt);

public sealed record ComponentTypeSupportDto(
    Guid Id,
    Guid RuntimeVersionId,
    string ComponentType,
    string ComponentVersion,
    string LoaderClassName,
    bool IsEnabled,
    DateTime CreatedAt);

public sealed record RuntimeVersionWithSupportsDto(
    Guid Id,
    string Version,
    bool IsCurrent,
    DateTime ReleasedAt,
    string ReleaseNotes,
    IEnumerable<ComponentTypeSupportDto> SupportedComponentTypes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

**File**: `AppRuntime.Application/Commands/CreateRuntimeVersion/CreateRuntimeVersionCommand.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Application.Commands.CreateRuntimeVersion;

public sealed record CreateRuntimeVersionCommand(
    string Version,
    bool IsCurrent,
    string ReleaseNotes,
    DateTime ReleasedAt) : ICommand<Result<Guid>>;
```

**File**: `AppRuntime.Application/Commands/CreateRuntimeVersion/CreateRuntimeVersionCommandHandler.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppRuntime.Domain.Entities;
using AppRuntime.Domain.Repositories;

namespace AppRuntime.Application.Commands.CreateRuntimeVersion;

public sealed class CreateRuntimeVersionCommandHandler : ICommandHandler<CreateRuntimeVersionCommand, Result<Guid>>
{
    private readonly IRuntimeVersionRepository _repository;
    private readonly IAppRuntimeUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRuntimeVersionCommandHandler(
        IRuntimeVersionRepository repository,
        IAppRuntimeUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateRuntimeVersionCommand request, CancellationToken cancellationToken)
    {
        // Check if version already exists
        var exists = await _repository.ExistsByVersionAsync(request.Version, cancellationToken);
        if (exists)
            return Result<Guid>.Failure(Error.Conflict("RuntimeVersion.AlreadyExists", $"Runtime version {request.Version} already exists"));

        // If marking as current, unmark the current version
        if (request.IsCurrent)
        {
            var currentVersion = await _repository.GetCurrentAsync(cancellationToken);
            if (currentVersion != null)
            {
                var unmarkResult = currentVersion.UnmarkAsCurrent(_dateTimeProvider);
                if (unmarkResult.IsFailure)
                    return Result<Guid>.Failure(unmarkResult.Error);
            }
        }

        // Create new runtime version
        var result = RuntimeVersion.Create(
            request.Version,
            request.IsCurrent,
            request.ReleaseNotes,
            request.ReleasedAt,
            _dateTimeProvider);

        if (result.IsFailure)
            return Result<Guid>.Failure(result.Error);

        await _repository.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(result.Value.Id);
    }
}
```

**File**: `AppRuntime.Application/Commands/AddComponentTypeSupport/AddComponentTypeSupportCommand.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Application.Commands.AddComponentTypeSupport;

public sealed record AddComponentTypeSupportCommand(
    Guid RuntimeVersionId,
    string ComponentType,
    string ComponentVersion,
    string LoaderClassName,
    bool IsEnabled) : ICommand<Result<Guid>>;
```

**File**: `AppRuntime.Application/Commands/AddComponentTypeSupport/AddComponentTypeSupportCommandHandler.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppRuntime.Domain.Entities;
using AppRuntime.Domain.Repositories;

namespace AppRuntime.Application.Commands.AddComponentTypeSupport;

public sealed class AddComponentTypeSupportCommandHandler : ICommandHandler<AddComponentTypeSupportCommand, Result<Guid>>
{
    private readonly IComponentTypeSupportRepository _repository;
    private readonly IRuntimeVersionRepository _runtimeVersionRepository;
    private readonly IAppRuntimeUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddComponentTypeSupportCommandHandler(
        IComponentTypeSupportRepository repository,
        IRuntimeVersionRepository runtimeVersionRepository,
        IAppRuntimeUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _runtimeVersionRepository = runtimeVersionRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AddComponentTypeSupportCommand request, CancellationToken cancellationToken)
    {
        // Verify runtime version exists
        var runtimeVersion = await _runtimeVersionRepository.GetByIdAsync(request.RuntimeVersionId, cancellationToken);
        if (runtimeVersion == null)
            return Result<Guid>.Failure(Error.NotFound("RuntimeVersion.NotFound", "Runtime version not found"));

        // Check if component type support already exists
        var existing = await _repository.GetByRuntimeVersionAndComponentTypeAsync(
            request.RuntimeVersionId,
            request.ComponentType,
            cancellationToken);

        if (existing != null)
            return Result<Guid>.Failure(Error.Conflict("ComponentTypeSupport.AlreadyExists",
                $"Component type {request.ComponentType} is already registered for this runtime version"));

        // Create component type support
        var result = ComponentTypeSupport.Create(
            request.RuntimeVersionId,
            request.ComponentType,
            request.ComponentVersion,
            request.LoaderClassName,
            request.IsEnabled,
            _dateTimeProvider);

        if (result.IsFailure)
            return Result<Guid>.Failure(result.Error);

        await _repository.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(result.Value.Id);
    }
}
```

---

### 2.2: RuntimeInstance DTOs & Commands (4 hours)

**File**: `AppRuntime.Application/DTOs/RuntimeInstanceDto.cs`

```csharp
namespace AppRuntime.Application.DTOs;

public sealed record RuntimeInstanceDto(
    Guid Id,
    Guid TenantId,
    Guid TenantApplicationId,
    Guid ApplicationReleaseId,
    Guid RuntimeVersionId,
    string Name,
    InstanceStatus Status,
    string Configuration,
    bool CompatibilityCheckPassed,
    string CompatibilityCheckDetails,
    DateTime? StartedAt,
    DateTime? StoppedAt,
    DateTime? LastHealthCheckAt,
    HealthStatus HealthStatus,
    DateTime CreatedAt);

public sealed record CompatibilityCheckResultDto(
    bool IsCompatible,
    string RuntimeVersion,
    string ApplicationReleaseVersion,
    IEnumerable<ComponentCompatibilityDto> ComponentChecks);

public sealed record ComponentCompatibilityDto(
    string ComponentType,
    string ComponentVersion,
    bool IsSupported,
    string? Message);
```

**File**: `AppRuntime.Application/Commands/CreateRuntimeInstance/CreateRuntimeInstanceCommand.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Application.Commands.CreateRuntimeInstance;

public sealed record CreateRuntimeInstanceCommand(
    Guid TenantId,
    Guid TenantApplicationId,
    Guid ApplicationReleaseId,
    string Name,
    string Configuration) : ICommand<Result<Guid>>;
```

**File**: `AppRuntime.Application/Commands/CreateRuntimeInstance/CreateRuntimeInstanceCommandHandler.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppRuntime.Domain.Entities;
using AppRuntime.Domain.Repositories;
using AppRuntime.Application.Services;

namespace AppRuntime.Application.Commands.CreateRuntimeInstance;

public sealed class CreateRuntimeInstanceCommandHandler : ICommandHandler<CreateRuntimeInstanceCommand, Result<Guid>>
{
    private readonly IRuntimeInstanceRepository _repository;
    private readonly IRuntimeVersionRepository _runtimeVersionRepository;
    private readonly ICompatibilityCheckService _compatibilityCheckService;
    private readonly IAppRuntimeUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRuntimeInstanceCommandHandler(
        IRuntimeInstanceRepository repository,
        IRuntimeVersionRepository runtimeVersionRepository,
        ICompatibilityCheckService compatibilityCheckService,
        IAppRuntimeUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _runtimeVersionRepository = runtimeVersionRepository;
        _compatibilityCheckService = compatibilityCheckService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateRuntimeInstanceCommand request, CancellationToken cancellationToken)
    {
        // Get current runtime version
        var runtimeVersion = await _runtimeVersionRepository.GetCurrentAsync(cancellationToken);
        if (runtimeVersion == null)
            return Result<Guid>.Failure(Error.NotFound("RuntimeVersion.NotFound", "No current runtime version found"));

        // Check compatibility between runtime version and application release
        var compatibilityResult = await _compatibilityCheckService.CheckCompatibilityAsync(
            runtimeVersion.Id,
            request.ApplicationReleaseId,
            cancellationToken);

        if (compatibilityResult.IsFailure)
            return Result<Guid>.Failure(compatibilityResult.Error);

        var compatibilityCheck = compatibilityResult.Value;

        // Create runtime instance
        var result = RuntimeInstance.Create(
            request.TenantId,
            request.TenantApplicationId,
            request.ApplicationReleaseId,
            runtimeVersion.Id,
            request.Name,
            request.Configuration,
            compatibilityCheck.IsCompatible,
            System.Text.Json.JsonSerializer.Serialize(compatibilityCheck),
            _dateTimeProvider);

        if (result.IsFailure)
            return Result<Guid>.Failure(result.Error);

        await _repository.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(result.Value.Id);
    }
}
```

**File**: `AppRuntime.Application/Commands/StartRuntimeInstance/StartRuntimeInstanceCommand.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Application.Commands.StartRuntimeInstance;

public sealed record StartRuntimeInstanceCommand(Guid Id) : ICommand<Result<Unit>>;
```

**File**: `AppRuntime.Application/Commands/StopRuntimeInstance/StopRuntimeInstanceCommand.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Application.Commands.StopRuntimeInstance;

public sealed record StopRuntimeInstanceCommand(Guid Id) : ICommand<Result<Unit>>;
```

#### LoadApplicationFromUrlCommand (NEW - Environment-Based Loading)

**File**: `AppRuntime.Application/Commands/LoadApplicationFromUrl/LoadApplicationFromUrlCommand.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Application.Commands.LoadApplicationFromUrl;

/// <summary>
/// Load an application by URL segments (tenant slug, app slug, environment)
/// This command resolves the URL to an ApplicationRelease and creates a RuntimeInstance
/// </summary>
public sealed record LoadApplicationFromUrlCommand(
    string TenantSlug,
    string AppSlug,
    string? Environment,
    Guid UserId) : ICommand<Result<Guid>>; // Returns RuntimeInstanceId
```

**File**: `AppRuntime.Application/Commands/LoadApplicationFromUrl/LoadApplicationFromUrlCommandHandler.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppRuntime.Domain.Entities;
using AppRuntime.Domain.Repositories;
using AppRuntime.Application.Services;
using TenantApplication.Contracts; // For IApplicationResolverService

namespace AppRuntime.Application.Commands.LoadApplicationFromUrl;

public sealed class LoadApplicationFromUrlCommandHandler : ICommandHandler<LoadApplicationFromUrlCommand, Result<Guid>>
{
    private readonly IApplicationResolverService _resolverService; // From TenantApplication.Contracts
    private readonly IRuntimeInstanceRepository _runtimeInstanceRepository;
    private readonly IRuntimeVersionRepository _runtimeVersionRepository;
    private readonly ICompatibilityCheckService _compatibilityCheckService;
    private readonly IAppRuntimeUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoadApplicationFromUrlCommandHandler(
        IApplicationResolverService resolverService,
        IRuntimeInstanceRepository runtimeInstanceRepository,
        IRuntimeVersionRepository runtimeVersionRepository,
        ICompatibilityCheckService compatibilityCheckService,
        IAppRuntimeUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _resolverService = resolverService;
        _runtimeInstanceRepository = runtimeInstanceRepository;
        _runtimeVersionRepository = runtimeVersionRepository;
        _compatibilityCheckService = compatibilityCheckService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(LoadApplicationFromUrlCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve application by URL (calls TenantApplication module)
        var resolvedResult = await _resolverService.ResolveByUrlAsync(
            request.TenantSlug,
            request.AppSlug,
            request.Environment,
            cancellationToken);

        if (resolvedResult.IsFailure)
            return Result<Guid>.Failure(resolvedResult.Error);

        var resolved = resolvedResult.Value;

        // 2. Get current runtime version
        var runtimeVersion = await _runtimeVersionRepository.GetCurrentAsync(cancellationToken);
        if (runtimeVersion == null)
            return Result<Guid>.Failure(Error.NotFound("RuntimeVersion.NotFound", "No active runtime version found"));

        // 3. Check compatibility with current runtime version
        var compatibilityResult = await _compatibilityCheckService.CheckCompatibilityAsync(
            resolved.ApplicationReleaseId,
            cancellationToken);

        if (compatibilityResult.IsFailure)
            return Result<Guid>.Failure(compatibilityResult.Error);

        var compatibilityCheck = compatibilityResult.Value;

        if (!compatibilityCheck.IsCompatible)
            return Result<Guid>.Failure(Error.Validation(
                "RuntimeInstance.IncompatibleRelease",
                $"ApplicationRelease is not compatible with current runtime version {runtimeVersion.Version}. " +
                $"Unsupported components: {string.Join(", ", compatibilityCheck.ComponentChecks.Where(c => !c.IsSupported).Select(c => c.ComponentType))}"));

        // 4. Create RuntimeInstance
        var instanceName = $"{resolved.TenantSlug}/{resolved.AppSlug}/{resolved.EnvironmentType}";
        var runtimeInstanceResult = RuntimeInstance.Create(
            resolved.TenantId,
            resolved.TenantApplicationId,
            resolved.ApplicationReleaseId,
            runtimeVersion.Id,
            instanceName,
            resolved.EnvironmentConfiguration, // Use environment-specific configuration
            compatibilityCheck.IsCompatible,
            System.Text.Json.JsonSerializer.Serialize(compatibilityCheck),
            _dateTimeProvider);

        if (runtimeInstanceResult.IsFailure)
            return Result<Guid>.Failure(runtimeInstanceResult.Error);

        var runtimeInstance = runtimeInstanceResult.Value;

        // 5. Save RuntimeInstance
        await _runtimeInstanceRepository.AddAsync(runtimeInstance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(runtimeInstance.Id);
    }
}
```

**File**: `AppRuntime.Application/Commands/LoadApplicationFromUrl/LoadApplicationFromUrlCommandValidator.cs`

```csharp
using FluentValidation;

namespace AppRuntime.Application.Commands.LoadApplicationFromUrl;

public sealed class LoadApplicationFromUrlCommandValidator : AbstractValidator<LoadApplicationFromUrlCommand>
{
    public LoadApplicationFromUrlCommandValidator()
    {
        RuleFor(x => x.TenantSlug)
            .NotEmpty().WithMessage("Tenant slug is required")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Tenant slug must be lowercase-kebab-case");

        RuleFor(x => x.AppSlug)
            .NotEmpty().WithMessage("Application slug is required")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Application slug must be lowercase-kebab-case");

        RuleFor(x => x.Environment)
            .Must(env => string.IsNullOrWhiteSpace(env) ||
                         env.Equals("development", StringComparison.OrdinalIgnoreCase) ||
                         env.Equals("staging", StringComparison.OrdinalIgnoreCase) ||
                         env.Equals("production", StringComparison.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.Environment))
            .WithMessage("Environment must be 'development', 'staging', or 'production'");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}
```

---

### 2.3: Queries (3 hours)

**File**: `AppRuntime.Application/Queries/GetRuntimeVersionById/GetRuntimeVersionByIdQuery.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;
using AppRuntime.Application.DTOs;

namespace AppRuntime.Application.Queries.GetRuntimeVersionById;

public sealed record GetRuntimeVersionByIdQuery(Guid Id) : IQuery<Result<RuntimeVersionWithSupportsDto>>;
```

**File**: `AppRuntime.Application/Queries/GetCurrentRuntimeVersion/GetCurrentRuntimeVersionQuery.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;
using AppRuntime.Application.DTOs;

namespace AppRuntime.Application.Queries.GetCurrentRuntimeVersion;

public sealed record GetCurrentRuntimeVersionQuery() : IQuery<Result<RuntimeVersionWithSupportsDto>>;
```

**File**: `AppRuntime.Application/Queries/CheckReleaseCompatibility/CheckReleaseCompatibilityQuery.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;
using AppRuntime.Application.DTOs;

namespace AppRuntime.Application.Queries.CheckReleaseCompatibility;

public sealed record CheckReleaseCompatibilityQuery(
    Guid ApplicationReleaseId) : IQuery<Result<CompatibilityCheckResultDto>>;
```

**File**: `AppRuntime.Application/Queries/GetRuntimeInstancesByTenant/GetRuntimeInstancesByTenantQuery.cs`

```csharp
using BuildingBlocks.Kernel.CQRS;
using BuildingBlocks.Kernel.Results;
using AppRuntime.Application.DTOs;

namespace AppRuntime.Application.Queries.GetRuntimeInstancesByTenant;

public sealed record GetRuntimeInstancesByTenantQuery(Guid TenantId) : IQuery<Result<IEnumerable<RuntimeInstanceDto>>>;
```

---

### 2.4: Compatibility Check Service (2 hours)

**File**: `AppRuntime.Application/Services/ICompatibilityCheckService.cs`

```csharp
using BuildingBlocks.Kernel.Results;
using AppRuntime.Application.DTOs;

namespace AppRuntime.Application.Services;

/// <summary>
/// Service for checking compatibility between runtime versions and application releases
/// </summary>
public interface ICompatibilityCheckService
{
    /// <summary>
    /// Check if a runtime version can load an application release
    /// </summary>
    Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid runtimeVersionId,
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default);
}
```

**File**: `AppRuntime.Application/Services/CompatibilityCheckService.cs`

```csharp
using BuildingBlocks.Kernel.Results;
using AppRuntime.Application.DTOs;
using AppRuntime.Domain.Repositories;
using AppBuilder.Contracts.Services; // Reference to AppBuilder.Contracts

namespace AppRuntime.Application.Services;

public sealed class CompatibilityCheckService : ICompatibilityCheckService
{
    private readonly IRuntimeVersionRepository _runtimeVersionRepository;
    private readonly IComponentTypeSupportRepository _componentTypeSupportRepository;
    private readonly IApplicationReleaseService _applicationReleaseService; // From AppBuilder.Contracts

    public CompatibilityCheckService(
        IRuntimeVersionRepository runtimeVersionRepository,
        IComponentTypeSupportRepository componentTypeSupportRepository,
        IApplicationReleaseService applicationReleaseService)
    {
        _runtimeVersionRepository = runtimeVersionRepository;
        _componentTypeSupportRepository = componentTypeSupportRepository;
        _applicationReleaseService = applicationReleaseService;
    }

    public async Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid runtimeVersionId,
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default)
    {
        // Get runtime version
        var runtimeVersion = await _runtimeVersionRepository.GetByIdAsync(runtimeVersionId, cancellationToken);
        if (runtimeVersion == null)
            return Result<CompatibilityCheckResultDto>.Failure(Error.NotFound("RuntimeVersion.NotFound", "Runtime version not found"));

        // Get supported component types for this runtime version
        var supportedComponents = await _componentTypeSupportRepository.GetEnabledByRuntimeVersionAsync(runtimeVersionId, cancellationToken);
        var supportedComponentTypes = supportedComponents.Select(c => c.ComponentType).ToHashSet();

        // Get application release components (from AppBuilder.Contracts)
        var releaseComponentsResult = await _applicationReleaseService.GetReleaseComponentTypesAsync(applicationReleaseId, cancellationToken);
        if (releaseComponentsResult.IsFailure)
            return Result<CompatibilityCheckResultDto>.Failure(releaseComponentsResult.Error);

        var releaseComponents = releaseComponentsResult.Value;

        // Check each component type in the release
        var componentChecks = new List<ComponentCompatibilityDto>();
        var allCompatible = true;

        foreach (var component in releaseComponents)
        {
            var isSupported = supportedComponentTypes.Contains(component.ComponentType);

            componentChecks.Add(new ComponentCompatibilityDto(
                component.ComponentType,
                component.ComponentVersion ?? "1.0",
                isSupported,
                isSupported ? null : $"Runtime version {runtimeVersion.Version} does not support {component.ComponentType}"));

            if (!isSupported)
                allCompatible = false;
        }

        var result = new CompatibilityCheckResultDto(
            allCompatible,
            runtimeVersion.Version,
            releaseComponents.FirstOrDefault()?.ReleaseVersion ?? "unknown",
            componentChecks);

        return Result<CompatibilityCheckResultDto>.Success(result);
    }
}
```

---

### 2.5: Service Registration (2 hours)

**File**: `AppRuntime.Application/AppRuntimeApplicationServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using AppRuntime.Application.Services;

namespace AppRuntime.Application;

public static class AppRuntimeApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAppRuntimeApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AppRuntimeApplicationServiceCollectionExtensions).Assembly));

        // Register services
        services.AddScoped<ICompatibilityCheckService, CompatibilityCheckService>();

        return services;
    }
}
```

**Deliverable**: Application layer complete with commands, queries, DTOs, handlers, compatibility check service.

---

## Phase 3: Infrastructure Layer (8 hours)

### 3.1: DbContext & Configurations (5 hours)

**File**: `AppRuntime.Infrastructure/Data/AppRuntimeDbContext.cs`

```csharp
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AppRuntime.Domain.Entities;

namespace AppRuntime.Infrastructure.Data;

public class AppRuntimeDbContext : BaseModuleDbContext
{
    public AppRuntimeDbContext(DbContextOptions<AppRuntimeDbContext> options)
        : base(options)
    {
    }

    protected override string SchemaName => "appruntime";

    public DbSet<RuntimeVersion> RuntimeVersions => Set<RuntimeVersion>();
    public DbSet<ComponentTypeSupport> ComponentTypeSupports => Set<ComponentTypeSupport>();
    public DbSet<RuntimeInstance> RuntimeInstances => Set<RuntimeInstance>();
    public DbSet<RuntimeConfiguration> RuntimeConfigurations => Set<RuntimeConfiguration>();
    public DbSet<RuntimeMetrics> RuntimeMetrics => Set<RuntimeMetrics>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppRuntimeDbContext).Assembly);
    }
}
```

**File**: `AppRuntime.Infrastructure/Data/Configurations/RuntimeVersionConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppRuntime.Domain.Entities;

namespace AppRuntime.Infrastructure.Data.Configurations;

public class RuntimeVersionConfiguration : IEntityTypeConfiguration<RuntimeVersion>
{
    public void Configure(EntityTypeBuilder<RuntimeVersion> builder)
    {
        builder.ToTable("runtime_versions");

        builder.HasKey(rv => rv.Id);

        builder.Property(rv => rv.Id).HasColumnName("id").IsRequired();
        builder.Property(rv => rv.Version).HasColumnName("version").HasMaxLength(50).IsRequired();
        builder.Property(rv => rv.IsCurrent).HasColumnName("is_current").IsRequired();
        builder.Property(rv => rv.ReleasedAt).HasColumnName("released_at").IsRequired();
        builder.Property(rv => rv.ReleaseNotes).HasColumnName("release_notes").HasMaxLength(5000);
        builder.Property(rv => rv.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(rv => rv.UpdatedAt).HasColumnName("updated_at");

        // Unique constraint on version
        builder.HasIndex(rv => rv.Version).IsUnique().HasDatabaseName("ix_runtime_versions_version");

        // Index on IsCurrent for quick lookup
        builder.HasIndex(rv => rv.IsCurrent).HasDatabaseName("ix_runtime_versions_is_current");

        // Ignore domain events
        builder.Ignore(rv => rv.DomainEvents);
    }
}
```

**File**: `AppRuntime.Infrastructure/Data/Configurations/ComponentTypeSupportConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppRuntime.Domain.Entities;

namespace AppRuntime.Infrastructure.Data.Configurations;

public class ComponentTypeSupportConfiguration : IEntityTypeConfiguration<ComponentTypeSupport>
{
    public void Configure(EntityTypeBuilder<ComponentTypeSupport> builder)
    {
        builder.ToTable("component_type_support");

        builder.HasKey(cts => cts.Id);

        builder.Property(cts => cts.Id).HasColumnName("id").IsRequired();
        builder.Property(cts => cts.RuntimeVersionId).HasColumnName("runtime_version_id").IsRequired();
        builder.Property(cts => cts.ComponentType).HasColumnName("component_type").HasMaxLength(100).IsRequired();
        builder.Property(cts => cts.ComponentVersion).HasColumnName("component_version").HasMaxLength(50).IsRequired();
        builder.Property(cts => cts.LoaderClassName).HasColumnName("loader_class_name").HasMaxLength(200).IsRequired();
        builder.Property(cts => cts.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(cts => cts.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(cts => cts.UpdatedAt).HasColumnName("updated_at");

        // Foreign key to RuntimeVersion
        builder.HasOne<RuntimeVersion>()
            .WithMany()
            .HasForeignKey(cts => cts.RuntimeVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(cts => cts.RuntimeVersionId).HasDatabaseName("ix_component_type_support_runtime_version_id");
        builder.HasIndex(cts => cts.ComponentType).HasDatabaseName("ix_component_type_support_component_type");

        // Unique constraint: one component type per runtime version
        builder.HasIndex(cts => new { cts.RuntimeVersionId, cts.ComponentType })
            .IsUnique()
            .HasDatabaseName("ix_component_type_support_runtime_version_component_type");

        // Ignore domain events
        builder.Ignore(cts => cts.DomainEvents);
    }
}
```

**File**: `AppRuntime.Infrastructure/Data/Configurations/RuntimeInstanceConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppRuntime.Domain.Entities;

namespace AppRuntime.Infrastructure.Data.Configurations;

public class RuntimeInstanceConfiguration : IEntityTypeConfiguration<RuntimeInstance>
{
    public void Configure(EntityTypeBuilder<RuntimeInstance> builder)
    {
        builder.ToTable("runtime_instances");

        builder.HasKey(ri => ri.Id);

        builder.Property(ri => ri.Id).HasColumnName("id").IsRequired();
        builder.Property(ri => ri.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(ri => ri.TenantApplicationId).HasColumnName("tenant_application_id").IsRequired();
        builder.Property(ri => ri.ApplicationReleaseId).HasColumnName("application_release_id").IsRequired();
        builder.Property(ri => ri.RuntimeVersionId).HasColumnName("runtime_version_id").IsRequired();
        builder.Property(ri => ri.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(ri => ri.Status).HasColumnName("status").IsRequired();
        builder.Property(ri => ri.Configuration).HasColumnName("configuration").HasColumnType("jsonb");
        builder.Property(ri => ri.CompatibilityCheckPassed).HasColumnName("compatibility_check_passed").IsRequired();
        builder.Property(ri => ri.CompatibilityCheckDetails).HasColumnName("compatibility_check_details").HasColumnType("jsonb");
        builder.Property(ri => ri.StartedAt).HasColumnName("started_at");
        builder.Property(ri => ri.StoppedAt).HasColumnName("stopped_at");
        builder.Property(ri => ri.LastHealthCheckAt).HasColumnName("last_health_check_at");
        builder.Property(ri => ri.HealthStatus).HasColumnName("health_status").IsRequired();
        builder.Property(ri => ri.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(ri => ri.UpdatedAt).HasColumnName("updated_at");

        // Foreign key to RuntimeVersion
        builder.HasOne<RuntimeVersion>()
            .WithMany()
            .HasForeignKey(ri => ri.RuntimeVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ri => ri.TenantId).HasDatabaseName("ix_runtime_instances_tenant_id");
        builder.HasIndex(ri => ri.TenantApplicationId).HasDatabaseName("ix_runtime_instances_tenant_application_id");
        builder.HasIndex(ri => ri.ApplicationReleaseId).HasDatabaseName("ix_runtime_instances_application_release_id");
        builder.HasIndex(ri => ri.RuntimeVersionId).HasDatabaseName("ix_runtime_instances_runtime_version_id");
        builder.HasIndex(ri => ri.Status).HasDatabaseName("ix_runtime_instances_status");

        // Ignore domain events
        builder.Ignore(ri => ri.DomainEvents);
    }
}
```

---

### 3.2: Repositories (3 hours)

**File**: `AppRuntime.Infrastructure/Repositories/RuntimeVersionRepository.cs`

```csharp
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AppRuntime.Domain.Entities;
using AppRuntime.Domain.Repositories;
using AppRuntime.Infrastructure.Data;

namespace AppRuntime.Infrastructure.Repositories;

public sealed class RuntimeVersionRepository : Repository<RuntimeVersion, Guid>, IRuntimeVersionRepository
{
    public RuntimeVersionRepository(AppRuntimeDbContext context) : base(context)
    {
    }

    public async Task<RuntimeVersion?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<RuntimeVersion>()
            .Where(rv => rv.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RuntimeVersion?> GetByVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        return await Context.Set<RuntimeVersion>()
            .Where(rv => rv.Version == version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsByVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        return await Context.Set<RuntimeVersion>()
            .AnyAsync(rv => rv.Version == version, cancellationToken);
    }
}
```

**File**: `AppRuntime.Infrastructure/Repositories/ComponentTypeSupportRepository.cs`

```csharp
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AppRuntime.Domain.Entities;
using AppRuntime.Domain.Repositories;
using AppRuntime.Infrastructure.Data;

namespace AppRuntime.Infrastructure.Repositories;

public sealed class ComponentTypeSupportRepository : Repository<ComponentTypeSupport, Guid>, IComponentTypeSupportRepository
{
    public ComponentTypeSupportRepository(AppRuntimeDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ComponentTypeSupport>> GetByRuntimeVersionAsync(
        Guid runtimeVersionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ComponentTypeSupport>()
            .Where(cts => cts.RuntimeVersionId == runtimeVersionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ComponentTypeSupport>> GetEnabledByRuntimeVersionAsync(
        Guid runtimeVersionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ComponentTypeSupport>()
            .Where(cts => cts.RuntimeVersionId == runtimeVersionId && cts.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SupportsComponentTypeAsync(
        Guid runtimeVersionId,
        string componentType,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ComponentTypeSupport>()
            .AnyAsync(cts => cts.RuntimeVersionId == runtimeVersionId
                && cts.ComponentType == componentType
                && cts.IsEnabled,
                cancellationToken);
    }

    public async Task<ComponentTypeSupport?> GetByRuntimeVersionAndComponentTypeAsync(
        Guid runtimeVersionId,
        string componentType,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ComponentTypeSupport>()
            .Where(cts => cts.RuntimeVersionId == runtimeVersionId && cts.ComponentType == componentType)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

**Deliverable**: Infrastructure layer complete with DbContext, EF configurations for all entities, repositories for RuntimeVersion and ComponentTypeSupport.

---

## Phase 4: API Layer (7 hours)

### 4.1: RuntimeVersion Controller (2 hours)

**File**: `AppRuntime.Api/Controllers/RuntimeVersionController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using MediatR;
using AppRuntime.Application.Commands.CreateRuntimeVersion;
using AppRuntime.Application.Commands.AddComponentTypeSupport;
using AppRuntime.Application.Queries.GetCurrentRuntimeVersion;
using AppRuntime.Application.Queries.GetRuntimeVersionById;

namespace AppRuntime.Api.Controllers;

[ApiController]
[Route("api/appruntime/versions")]
public sealed class RuntimeVersionController : ControllerBase
{
    private readonly IMediator _mediator;

    public RuntimeVersionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current runtime version with supported component types
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var query = new GetCurrentRuntimeVersionQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error.Message });
    }

    /// <summary>
    /// Get runtime version by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetRuntimeVersionByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error.Message });
    }

    /// <summary>
    /// Create a new runtime version
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRuntimeVersionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Add component type support to a runtime version
    /// </summary>
    [HttpPost("{runtimeVersionId:guid}/component-types")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddComponentTypeSupport(
        Guid runtimeVersionId,
        [FromBody] AddComponentTypeSupportRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddComponentTypeSupportCommand(
            runtimeVersionId,
            request.ComponentType,
            request.ComponentVersion,
            request.LoaderClassName,
            request.IsEnabled);

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created(string.Empty, new { id = result.Value })
            : BadRequest(new { error = result.Error.Message });
    }
}

public sealed record AddComponentTypeSupportRequest(
    string ComponentType,
    string ComponentVersion,
    string LoaderClassName,
    bool IsEnabled);
```

---

### 4.2: RuntimeInstance Controller (3 hours)

**File**: `AppRuntime.Api/Controllers/RuntimeInstanceController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using MediatR;
using AppRuntime.Application.Commands.CreateRuntimeInstance;
using AppRuntime.Application.Commands.StartRuntimeInstance;
using AppRuntime.Application.Commands.StopRuntimeInstance;
using AppRuntime.Application.Commands.LoadApplicationFromUrl;
using AppRuntime.Application.Queries.GetRuntimeInstancesByTenant;
using AppRuntime.Application.Queries.CheckReleaseCompatibility;

namespace AppRuntime.Api.Controllers;

[ApiController]
[Route("api/appruntime/instances")]
public sealed class RuntimeInstanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public RuntimeInstanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all runtime instances for a tenant
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var query = new GetRuntimeInstancesByTenantQuery(tenantId);
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(statusCode: 500, detail: result.Error.Message);
    }

    /// <summary>
    /// Check if an application release is compatible with current runtime
    /// </summary>
    [HttpGet("compatibility/release/{applicationReleaseId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCompatibility(Guid applicationReleaseId, CancellationToken cancellationToken)
    {
        var query = new CheckReleaseCompatibilityQuery(applicationReleaseId);
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Create a new runtime instance
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRuntimeInstanceCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created(string.Empty, new { id = result.Value })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Load application from URL (NEW - Environment-Based Loading)
    /// This endpoint resolves the URL to an ApplicationRelease and creates a RuntimeInstance
    /// URL Pattern: datarizen.com/{tenantSlug}/{appSlug}/{environment?}
    /// </summary>
    [HttpPost("load-from-url")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LoadFromUrl(
        [FromBody] LoadApplicationFromUrlRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoadApplicationFromUrlCommand(
            request.TenantSlug,
            request.AppSlug,
            request.Environment,
            request.UserId);

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created(string.Empty, new { runtimeInstanceId = result.Value })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Start a runtime instance
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var command = new StartRuntimeInstanceCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Stop a runtime instance
    /// </summary>
    [HttpPost("{id:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Stop(Guid id, CancellationToken cancellationToken)
    {
        var command = new StopRuntimeInstanceCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }
}

/// <summary>
/// Request model for loading application from URL
/// </summary>
public sealed record LoadApplicationFromUrlRequest(
    string TenantSlug,
    string AppSlug,
    string? Environment,
    Guid UserId);
```

**Deliverable**: API layer complete with RuntimeVersionController and RuntimeInstanceController with compatibility check endpoint.

---

## Phase 5: Migrations Layer (5 hours)

### 5.1: Schema and RuntimeVersion Tables (2 hours)

**File**: `AppRuntime.Migrations/Migrations/20260211300000_CreateAppRuntimeSchema.cs`

```csharp
using FluentMigrator;

namespace AppRuntime.Migrations.Migrations;

[Migration(20260211300000, "Create appruntime schema")]
public class CreateAppRuntimeSchema : Migration
{
    public override void Up()
    {
        Create.Schema("appruntime");
    }

    public override void Down()
    {
        Delete.Schema("appruntime");
    }
}
```

**File**: `AppRuntime.Migrations/Migrations/20260211301000_CreateRuntimeVersionsTable.cs`

```csharp
using FluentMigrator;

namespace AppRuntime.Migrations.Migrations;

[Migration(20260211301000, "Create runtime_versions table")]
public class CreateRuntimeVersionsTable : Migration
{
    public override void Up()
    {
        Create.Table("runtime_versions")
            .InSchema("appruntime")
            .WithColumn("id").AsGuid().PrimaryKey("pk_runtime_versions")
            .WithColumn("version").AsString(50).NotNullable()
            .WithColumn("is_current").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("released_at").AsDateTime().NotNullable()
            .WithColumn("release_notes").AsString(5000).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        // Unique index on version
        Create.Index("ix_runtime_versions_version")
            .OnTable("runtime_versions")
            .InSchema("appruntime")
            .OnColumn("version")
            .Unique();

        // Index on is_current for quick lookup
        Create.Index("ix_runtime_versions_is_current")
            .OnTable("runtime_versions")
            .InSchema("appruntime")
            .OnColumn("is_current");
    }

    public override void Down()
    {
        Delete.Table("runtime_versions").InSchema("appruntime");
    }
}
```

**File**: `AppRuntime.Migrations/Migrations/20260211302000_CreateComponentTypeSupportTable.cs`

```csharp
using FluentMigrator;

namespace AppRuntime.Migrations.Migrations;

[Migration(20260211302000, "Create component_type_support table")]
public class CreateComponentTypeSupportTable : Migration
{
    public override void Up()
    {
        Create.Table("component_type_support")
            .InSchema("appruntime")
            .WithColumn("id").AsGuid().PrimaryKey("pk_component_type_support")
            .WithColumn("runtime_version_id").AsGuid().NotNullable()
            .WithColumn("component_type").AsString(100).NotNullable()
            .WithColumn("component_version").AsString(50).NotNullable()
            .WithColumn("loader_class_name").AsString(200).NotNullable()
            .WithColumn("is_enabled").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        // Foreign key to runtime_versions
        Create.ForeignKey("fk_component_type_support_runtime_version")
            .FromTable("component_type_support").InSchema("appruntime").ForeignColumn("runtime_version_id")
            .ToTable("runtime_versions").InSchema("appruntime").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Index on runtime_version_id
        Create.Index("ix_component_type_support_runtime_version_id")
            .OnTable("component_type_support")
            .InSchema("appruntime")
            .OnColumn("runtime_version_id");

        // Index on component_type
        Create.Index("ix_component_type_support_component_type")
            .OnTable("component_type_support")
            .InSchema("appruntime")
            .OnColumn("component_type");

        // Unique constraint: one component type per runtime version
        Create.Index("ix_component_type_support_runtime_version_component_type")
            .OnTable("component_type_support")
            .InSchema("appruntime")
            .OnColumn("runtime_version_id").Ascending()
            .OnColumn("component_type").Ascending()
            .Unique();
    }

    public override void Down()
    {
        Delete.Table("component_type_support").InSchema("appruntime");
    }
}
```

---

### 5.2: RuntimeInstance Table (2 hours)

**File**: `AppRuntime.Migrations/Migrations/20260211303000_CreateRuntimeInstancesTable.cs`

```csharp
using FluentMigrator;

namespace AppRuntime.Migrations.Migrations;

[Migration(20260211303000, "Create runtime_instances table")]
public class CreateRuntimeInstancesTable : Migration
{
    public override void Up()
    {
        Create.Table("runtime_instances")
            .InSchema("appruntime")
            .WithColumn("id").AsGuid().PrimaryKey("pk_runtime_instances")
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("tenant_application_id").AsGuid().NotNullable()
            .WithColumn("application_release_id").AsGuid().NotNullable()
            .WithColumn("runtime_version_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(3) // Stopped
            .WithColumn("configuration").AsCustom("jsonb").Nullable()
            .WithColumn("compatibility_check_passed").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("compatibility_check_details").AsCustom("jsonb").Nullable()
            .WithColumn("started_at").AsDateTime().Nullable()
            .WithColumn("stopped_at").AsDateTime().Nullable()
            .WithColumn("last_health_check_at").AsDateTime().Nullable()
            .WithColumn("health_status").AsInt32().NotNullable().WithDefaultValue(0) // Healthy
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        // Foreign key to runtime_versions
        Create.ForeignKey("fk_runtime_instances_runtime_version")
            .FromTable("runtime_instances").InSchema("appruntime").ForeignColumn("runtime_version_id")
            .ToTable("runtime_versions").InSchema("appruntime").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Restrict);

        // Indexes
        Create.Index("ix_runtime_instances_tenant_id")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("tenant_id");

        Create.Index("ix_runtime_instances_tenant_application_id")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("tenant_application_id");

        Create.Index("ix_runtime_instances_application_release_id")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("application_release_id");

        Create.Index("ix_runtime_instances_runtime_version_id")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("runtime_version_id");

        Create.Index("ix_runtime_instances_status")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("status");
    }

    public override void Down()
    {
        Delete.Table("runtime_instances").InSchema("appruntime");
    }
}
```

---

### 5.3: Seed Data (1 hour)

**File**: `AppRuntime.Migrations/Migrations/20260211304000_SeedInitialRuntimeVersion.cs`

```csharp
using FluentMigrator;

namespace AppRuntime.Migrations.Migrations;

[Migration(20260211304000, "Seed initial runtime version v1.0.0")]
public class SeedInitialRuntimeVersion : Migration
{
    public override void Up()
    {
        var runtimeVersionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Insert initial runtime version v1.0.0
        Insert.IntoTable("runtime_versions")
            .InSchema("appruntime")
            .Row(new
            {
                id = runtimeVersionId,
                version = "1.0.0",
                is_current = true,
                released_at = now,
                release_notes = "Initial AppRuntime version with support for NavigationComponent and PageComponent",
                created_at = now
            });

        // Add NavigationComponent support
        Insert.IntoTable("component_type_support")
            .InSchema("appruntime")
            .Row(new
            {
                id = Guid.NewGuid(),
                runtime_version_id = runtimeVersionId,
                component_type = "NavigationComponent",
                component_version = "1.0",
                loader_class_name = "NavigationComponentLoader",
                is_enabled = true,
                created_at = now
            });

        // Add PageComponent support
        Insert.IntoTable("component_type_support")
            .InSchema("appruntime")
            .Row(new
            {
                id = Guid.NewGuid(),
                runtime_version_id = runtimeVersionId,
                component_type = "PageComponent",
                component_version = "1.0",
                loader_class_name = "PageComponentLoader",
                is_enabled = true,
                created_at = now
            });
    }

    public override void Down()
    {
        Delete.FromTable("component_type_support").InSchema("appruntime").AllRows();
        Delete.FromTable("runtime_versions").InSchema("appruntime").AllRows();
    }
}
```

**Deliverable**: Migrations layer complete with schema, tables, and seed data for initial runtime version v1.0.0 with NavigationComponent and PageComponent support.

---

## Phase 6: Component Loader Pattern (4 hours)

### 6.1: Component Loader Interface (1 hour)

**File**: `AppRuntime.Application/ComponentLoaders/IComponentLoader.cs`

```csharp
namespace AppRuntime.Application.ComponentLoaders;

/// <summary>
/// Interface for loading and rendering specific component types
/// </summary>
public interface IComponentLoader
{
    /// <summary>
    /// The component type this loader handles (e.g., "NavigationComponent", "PageComponent")
    /// </summary>
    string ComponentType { get; }

    /// <summary>
    /// The component version this loader handles (e.g., "1.0", "2.0")
    /// </summary>
    string ComponentVersion { get; }

    /// <summary>
    /// Load a component by ID
    /// </summary>
    Task<object?> LoadComponentAsync(Guid componentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Render a component to JSON representation
    /// </summary>
    Task<string> RenderComponentAsync(object component, CancellationToken cancellationToken = default);
}
```

**File**: `AppRuntime.Application/ComponentLoaders/IComponentLoaderRegistry.cs`

```csharp
namespace AppRuntime.Application.ComponentLoaders;

/// <summary>
/// Registry for discovering and retrieving component loaders
/// </summary>
public interface IComponentLoaderRegistry
{
    /// <summary>
    /// Register a component loader
    /// </summary>
    void RegisterLoader(IComponentLoader loader);

    /// <summary>
    /// Get a component loader by component type
    /// </summary>
    IComponentLoader? GetLoader(string componentType);

    /// <summary>
    /// Get all registered component loaders
    /// </summary>
    IEnumerable<IComponentLoader> GetAllLoaders();

    /// <summary>
    /// Check if a component type is supported
    /// </summary>
    bool IsSupported(string componentType);
}
```

---

### 6.2: Component Loader Registry Implementation (1 hour)

**File**: `AppRuntime.Application/ComponentLoaders/ComponentLoaderRegistry.cs`

```csharp
using System.Collections.Concurrent;

namespace AppRuntime.Application.ComponentLoaders;

public sealed class ComponentLoaderRegistry : IComponentLoaderRegistry
{
    private readonly ConcurrentDictionary<string, IComponentLoader> _loaders = new();

    public void RegisterLoader(IComponentLoader loader)
    {
        _loaders.TryAdd(loader.ComponentType, loader);
    }

    public IComponentLoader? GetLoader(string componentType)
    {
        _loaders.TryGetValue(componentType, out var loader);
        return loader;
    }

    public IEnumerable<IComponentLoader> GetAllLoaders()
    {
        return _loaders.Values;
    }

    public bool IsSupported(string componentType)
    {
        return _loaders.ContainsKey(componentType);
    }
}
```

---

### 6.3: Component Loader Implementations (2 hours)

**File**: `AppRuntime.Application/ComponentLoaders/NavigationComponentLoader.cs`

```csharp
using AppBuilder.Contracts.Services; // Reference to AppBuilder.Contracts
using System.Text.Json;

namespace AppRuntime.Application.ComponentLoaders;

public sealed class NavigationComponentLoader : IComponentLoader
{
    private readonly INavigationComponentService _navigationComponentService;

    public NavigationComponentLoader(INavigationComponentService navigationComponentService)
    {
        _navigationComponentService = navigationComponentService;
    }

    public string ComponentType => "NavigationComponent";
    public string ComponentVersion => "1.0";

    public async Task<object?> LoadComponentAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        // Load navigation component from AppBuilder module via Contracts
        return await _navigationComponentService.GetByIdAsync(componentId, cancellationToken);
    }

    public async Task<string> RenderComponentAsync(object component, CancellationToken cancellationToken = default)
    {
        // Render navigation component to JSON
        return JsonSerializer.Serialize(component);
    }
}
```

**File**: `AppRuntime.Application/ComponentLoaders/PageComponentLoader.cs`

```csharp
using AppBuilder.Contracts.Services; // Reference to AppBuilder.Contracts
using System.Text.Json;

namespace AppRuntime.Application.ComponentLoaders;

public sealed class PageComponentLoader : IComponentLoader
{
    private readonly IPageComponentService _pageComponentService;

    public PageComponentLoader(IPageComponentService pageComponentService)
    {
        _pageComponentService = pageComponentService;
    }

    public string ComponentType => "PageComponent";
    public string ComponentVersion => "1.0";

    public async Task<object?> LoadComponentAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        // Load page component from AppBuilder module via Contracts
        return await _pageComponentService.GetByIdAsync(componentId, cancellationToken);
    }

    public async Task<string> RenderComponentAsync(object component, CancellationToken cancellationToken = default)
    {
        // Render page component to JSON
        return JsonSerializer.Serialize(component);
    }
}
```

**Update**: `AppRuntime.Application/AppRuntimeApplicationServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using AppRuntime.Application.Services;
using AppRuntime.Application.ComponentLoaders;

namespace AppRuntime.Application;

public static class AppRuntimeApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAppRuntimeApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AppRuntimeApplicationServiceCollectionExtensions).Assembly));

        // Register services
        services.AddScoped<ICompatibilityCheckService, CompatibilityCheckService>();

        // Register component loader registry
        services.AddSingleton<IComponentLoaderRegistry, ComponentLoaderRegistry>();

        // Register component loaders
        services.AddScoped<IComponentLoader, NavigationComponentLoader>();
        services.AddScoped<IComponentLoader, PageComponentLoader>();

        // Initialize component loader registry
        services.AddHostedService<ComponentLoaderRegistryInitializer>();

        return services;
    }
}
```

**File**: `AppRuntime.Application/ComponentLoaders/ComponentLoaderRegistryInitializer.cs`

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AppRuntime.Application.ComponentLoaders;

/// <summary>
/// Background service to initialize component loader registry on startup
/// </summary>
public sealed class ComponentLoaderRegistryInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ComponentLoaderRegistryInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IComponentLoaderRegistry>();
        var loaders = scope.ServiceProvider.GetServices<IComponentLoader>();

        foreach (var loader in loaders)
        {
            registry.RegisterLoader(loader);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

**Deliverable**: Component loader pattern complete with registry, interface, and implementations for NavigationComponent and PageComponent.

---

## Phase 7: Module Composition (2 hours)

**File**: `AppRuntime.Module/AppRuntimeModule.cs`

```csharp
using BuildingBlocks.Web.Modules;
using AppRuntime.Api.Controllers;
using AppRuntime.Application;
using AppRuntime.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppRuntime.Module;

public sealed class AppRuntimeModule : IModule
{
    public string ModuleName => "AppRuntime";
    public string SchemaName => "appruntime";

    public string[] GetMigrationDependencies() => ["Tenant", "TenantApplication", "AppBuilder"];

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAppRuntimeApplication();
        services.AddAppRuntimeInfrastructure(configuration, SchemaName);
        services.AddControllers()
            .AddApplicationPart(typeof(RuntimeInstanceController).Assembly)
            .AddApplicationPart(typeof(RuntimeVersionController).Assembly);

        return services;
    }

    public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
    {
        return app;
    }
}
```

**File**: `Hosts/AppRuntimeServiceHost/AppRuntime.Service.Host.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>AppRuntime.Service.Host</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\..\\BuildingBlocks\\Web\\BuildingBlocks.Web.csproj" />
    <ProjectReference Include="..\\..\\Product\\AppRuntime\\AppRuntime.Module\\AppRuntime.Module.csproj" />
    <ProjectReference Include="..\\..\\ServiceDefaults\\ServiceDefaults.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" />
  </ItemGroup>
</Project>
```

**File**: `Hosts/AppRuntimeServiceHost/Program.cs`

```csharp
using BuildingBlocks.Web.Extensions;
using AppRuntime.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddBuildingBlocks();
builder.AddBuildingBlocksHealthChecks();

builder.Services.AddModule<AppRuntimeModule>(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCorrelationId();
app.UseGlobalExceptionHandler();
app.UseRequestLogging();
app.UseTenantResolution();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseModule<AppRuntimeModule>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

await app.RunAsync();
```

**Deliverable**: Module composition complete with AppRuntimeModule and AppRuntimeServiceHost.

---

## Phase 8: Integration (2 hours)

### 8.1: Integration Points

- **Tenant Module**: Runtime instances scoped to tenants
- **TenantApplication Module**: Runtime instances linked to tenant applications
- **AppBuilder Module**: Runtime instances execute ApplicationReleases with component compatibility checks
- **Feature Module**: Feature flag evaluation at runtime via `IFeatureEvaluationService`
- **Identity Module**: User permissions for runtime management

### 8.2: Contracts

**File**: `AppRuntime.Contracts/Services/IRuntimeManagementService.cs`

```csharp
namespace AppRuntime.Contracts.Services;

public interface IRuntimeManagementService
{
    /// <summary>
    /// Check if an application release is compatible with current runtime version
    /// </summary>
    Task<CompatibilityCheckResultDto> CheckCompatibilityAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new runtime instance for a tenant application
    /// </summary>
    Task<Guid> CreateInstanceAsync(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid applicationReleaseId,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a runtime instance
    /// </summary>
    Task StartInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop a runtime instance
    /// </summary>
    Task StopInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get runtime instance status
    /// </summary>
    Task<RuntimeInstanceDto> GetInstanceStatusAsync(Guid instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current runtime version
    /// </summary>
    Task<RuntimeVersionDto> GetCurrentRuntimeVersionAsync(CancellationToken cancellationToken = default);
}
```

**File**: `AppRuntime.Contracts/DTOs/CompatibilityCheckResultDto.cs`

```csharp
namespace AppRuntime.Contracts.DTOs;

public sealed record CompatibilityCheckResultDto(
    bool IsCompatible,
    string RuntimeVersion,
    string ReleaseVersion,
    IReadOnlyList<ComponentCompatibilityDto> ComponentChecks);

public sealed record ComponentCompatibilityDto(
    string ComponentType,
    string ComponentVersion,
    bool IsSupported,
    string? ErrorMessage);
```

**Deliverable**: Integration complete with all dependent modules and public contracts for inter-module communication.

---

## Success Criteria Checklist

### Domain Layer
- [x] RuntimeVersion entity with semantic versioning
- [x] ComponentTypeSupport entity for component registry
- [x] RuntimeInstance entity with lifecycle management and compatibility tracking
- [x] Domain events for all state changes (including RuntimeVersionId)
- [x] Repository interfaces for all entities

### Application Layer
- [x] RuntimeVersion commands (CreateRuntimeVersion, AddComponentTypeSupport)
- [x] RuntimeInstance commands (CreateRuntimeInstance, StartRuntimeInstance, StopRuntimeInstance)
- [x] CompatibilityCheckService for validating runtime can load releases
- [x] Queries for runtime versions and instances
- [x] DTOs and mappers for all entities
- [x] FluentValidation validators for all commands

### Infrastructure Layer
- [x] AppRuntimeDbContext with RuntimeVersions, ComponentTypeSupport, RuntimeInstances DbSets
- [x] EF Core configurations for all entities
- [x] Repository implementations with Ardalis.Specification support
- [x] Unit of Work implementation

### API Layer
- [x] RuntimeVersionController with version management endpoints
- [x] RuntimeInstanceController with lifecycle and compatibility check endpoints
- [x] Proper HTTP status codes and error handling

### Migrations Layer
- [x] Schema creation migration
- [x] RuntimeVersions table migration
- [x] ComponentTypeSupport table migration with foreign key
- [x] RuntimeInstances table migration with new columns
- [x] Seed data for initial runtime version v1.0.0

### Module Layer
- [x] AppRuntimeModule implements IModule
- [x] Component loader registry initialization
- [x] Service registration for all layers
- [x] AppRuntimeServiceHost for microservices topology

### Component Loader Pattern
- [x] IComponentLoader interface
- [x] IComponentLoaderRegistry interface
- [x] ComponentLoaderRegistry implementation
- [x] NavigationComponentLoader implementation
- [x] PageComponentLoader implementation
- [x] ComponentLoaderRegistryInitializer hosted service

### Integration
- [x] Integration with Tenant, TenantApplication, and AppBuilder modules
- [x] Public contracts for inter-module communication
- [x] Loose coupling via AppBuilder.Contracts

---

## Deployment Topology Support

### Monolith
- ✅ All modules in single process
- ✅ Single database with `appruntime` schema
- ✅ Component loaders registered in DI container

### MultiApp
- ✅ AppRuntime module in `MultiAppRuntimeHost`
- ✅ Shared database with `appruntime` schema
- ✅ API Gateway routes `/api/appruntime/*` to Runtime host
- ✅ Component loaders can call AppBuilder via HTTP

### Microservices
- ✅ Dedicated `AppRuntimeServiceHost`
- ✅ Can use separate database (or shared with schema isolation)
- ✅ HTTP/gRPC communication via service discovery
- ✅ Component loaders call AppBuilder microservice via Contracts

---

## Phase 8: Contracts Layer (2 hours)

### 8.1: Public Interfaces and DTOs for Inter-Module Communication

**Purpose**: Expose services that other modules can consume (TenantApplication, AppBuilder, etc.)

**File**: `AppRuntime.Contracts/Services/ICompatibilityCheckService.cs`

```csharp
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Contracts.Services;

/// <summary>
/// Service for checking compatibility between runtime versions and application releases.
/// OWNED BY: AppRuntime module
/// USED BY: TenantApplication (before deployment), AppRuntime (before instance creation)
/// </summary>
public interface ICompatibilityCheckService
{
    /// <summary>
    /// Check if the current runtime version can load an application release
    /// </summary>
    /// <param name="applicationReleaseId">The application release to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compatibility check result with details</returns>
    Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a specific runtime version can load an application release
    /// </summary>
    /// <param name="runtimeVersionId">The runtime version to check against</param>
    /// <param name="applicationReleaseId">The application release to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compatibility check result with details</returns>
    Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid runtimeVersionId,
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default);
}
```

**File**: `AppRuntime.Contracts/DTOs/CompatibilityCheckResultDto.cs`

```csharp
namespace AppRuntime.Contracts.DTOs;

/// <summary>
/// Result of a compatibility check between runtime and application release
/// </summary>
public sealed record CompatibilityCheckResultDto(
    bool IsCompatible,
    string RuntimeVersion,
    IEnumerable<string> SupportedComponentTypes,
    IEnumerable<string> UnsupportedComponentTypes,
    string? ErrorMessage);
```

**File**: `AppRuntime.Contracts/DTOs/RuntimeVersionDto.cs`

```csharp
namespace AppRuntime.Contracts.DTOs;

/// <summary>
/// Public DTO for runtime version information
/// </summary>
public sealed record RuntimeVersionDto(
    Guid Id,
    string Version,
    bool IsCurrent,
    IEnumerable<string> SupportedComponentTypes);
```

**Implementation Note**: The `AppRuntime.Application.Services.CompatibilityCheckService` implements this interface and is registered in DI. In microservices topology, this is exposed via HTTP API at `/api/appruntime/compatibility/check`.

**Deliverable**: Contracts layer complete with ICompatibilityCheckService and DTOs for inter-module communication.

---

## Estimated Timeline

| Phase | Description | Time |
|-------|-------------|------|
| Phase 1 | Domain Layer (with RuntimeVersion, ComponentTypeSupport) | 12 hours |
| Phase 2 | Application Layer (with CompatibilityCheckService) | 14 hours |
| Phase 3 | Infrastructure Layer (with new entity configurations) | 10 hours |
| Phase 4 | API Layer (with RuntimeVersionController) | 7 hours |
| Phase 5 | Migrations Layer (with seed data) | 5 hours |
| Phase 6 | Component Loader Pattern | 4 hours |
| Phase 7 | Module Composition | 2 hours |
| Phase 8 | Integration & Contracts | 2 hours |
| Phase 9 | Testing & Validation | 6 hours |
| **Total** | **Complete Vertical Slice with Versioning** | **62 hours (~8 days)** |

---

## Next Steps

After completing the AppRuntime module:

1. **Update AppHost** - Add AppRuntimeServiceHost to microservices topology
2. **Integration Testing** - Test compatibility checks and component loading across all topologies
3. **Version Evolution Testing** - Test adding new component types (FormComponent, DashboardComponent)
4. **Performance Testing** - Test runtime metrics collection and health checks
5. **Documentation** - Update API documentation and user guides with versioning strategy

---

## Notes

### Instance Lifecycle
- **Starting** → **Running** → **Stopping** → **Stopped** (or **Failed**)
- Compatibility check happens before instance creation (fail-fast)
- If compatibility check fails, instance creation is rejected with detailed error

### Runtime Versioning
- Runtime versions use semantic versioning (1.0.0, 1.1.0, 2.0.0)
- Only one runtime version can be marked as `IsCurrent` at a time
- Each runtime version declares supported component types via ComponentTypeSupport table
- Component types can have versions (e.g., NavigationComponent v1.0, v2.0)

### Component Loading
- Component loaders are registered in DI container and discovered via IComponentLoaderRegistry
- Each component loader implements IComponentLoader interface
- Component loaders use AppBuilder.Contracts to load components (loose coupling)
- New component types require new runtime version and new component loader implementation

### Compatibility Checks
- Before creating RuntimeInstance, CompatibilityCheckService validates all components in ApplicationRelease
- If any component type is not supported by current runtime version, creation fails
- Compatibility check details stored as JSON in RuntimeInstance for debugging
- Compatibility check endpoint allows pre-validation before tenant activates application

### Migration Dependencies
- AppRuntime module depends on: Tenant, TenantApplication, AppBuilder modules
- Migrations must run in order: Tenant → TenantApplication → AppBuilder → AppRuntime

### Future Enhancements
- **Component Version Compatibility Matrix**: Support multiple versions of same component type
- **Runtime Auto-Upgrade**: Automatically upgrade runtime when new version is deployed
- **Component Hot-Reload**: Reload component loaders without restarting runtime
- **Component Metrics**: Track component load times and render performance


