# AppRuntime Module - Architecture & Internals

## Overview

**AppRuntime is the execution engine** that takes an immutable `ApplicationRelease` and runs it for a tenant in a specific environment. Think of it like a **virtual machine** or **container runtime** for your low-code applications.

---

## Core Concept

AppRuntime executes applications by:
1. Loading the **ApplicationRelease** definition: from **AppBuilder** for installed platform apps, or from **TenantApplication** for tenant custom/forked apps (tenant_application_releases in `tenantapplication` schema)
2. Loading the **tenant-specific configuration** (from TenantApplication)
3. Validating **compatibility** with current runtime version
4. **Rendering** the application using registered component loaders

---

## Configuration Ownership - CRITICAL

### ✅ CORRECT: TenantApplication Owns Configuration

**Configuration lives in TenantApplication module**, NOT AppBuilder:

```
tenantapplication.tenant_applications
├── Configuration (JSON) - Base tenant-specific settings
└── tenantapplication.tenant_application_environments
    └── Configuration (JSON) - Environment-specific overrides
```

**Why?**
- ✅ Tenants can **customize** applications after installation
- ✅ Tenants can **modify** app definitions and create their own releases
- ✅ Each tenant has **isolated configuration** for the same base app
- ✅ Same app can have different settings per tenant
- ✅ Same app can have different settings per environment (dev/staging/prod)

**Example**:
```
Base App: "CRM" (from AppBuilder catalog)
├── Tenant A installs CRM
│   ├── Configuration: { "theme": "dark", "maxUsers": 100 }
│   └── Production Environment: { "apiUrl": "https://api-a.com" }
├── Tenant B installs CRM
│   ├── Configuration: { "theme": "light", "maxUsers": 50 }
│   └── Production Environment: { "apiUrl": "https://api-b.com" }
```

### ❌ WRONG: AppBuilder Owns Configuration

**AppBuilder only owns the APPLICATION DEFINITION**, not tenant-specific configuration:

```
appbuilder.application_definitions
├── Name, Description, Slug
└── appbuilder.application_releases
    ├── Version, ReleaseNotes
    └── appbuilder.application_snapshots
        └── SnapshotData (JSON) - Structure only, no tenant config
```

**AppBuilder provides**:
- ✅ Application structure (navigation, pages, data sources)
- ✅ Component definitions (what components exist)
- ✅ Default configuration schema (what settings are available)
- ❌ NOT tenant-specific configuration values

---

## The Flow: From URL to Running App

### 1. User Accesses Application
```
https://datarizen.com/acme-corp/crm/production
                      ↓         ↓    ↓
                   tenant    app   environment
```

### 2. AppRuntime Resolution Flow

**Applications are always resolved by the TenantApplication module; AppBuilder never performs resolution.** AppBuilder (or TenantApplication for tenant custom apps) is used only to load application structure (snapshot) for the `ApplicationReleaseId` returned by TenantApplication.

```
1. RESOLVE TENANT APPLICATION (TenantApplication only)
   ↓ Call TenantApplication.Contracts.IApplicationResolverService
   ↓ Input: { tenantSlug: "acme-corp", appSlug: "crm", environment: "production" }
   ↓
   ← Returns: ResolvedApplicationDto {
       TenantApplicationId: guid,
       TenantId: guid,
       ApplicationId: guid,
       ApplicationReleaseId: guid,  // Then used to load structure
       Configuration: "{ merged config }", // FROM TENANTAPPLICATION
       EnvironmentType: Production,
       IsActive: true
     }
   ↓
2. LOAD APPLICATION STRUCTURE (AppBuilder or TenantApplication, depending on release ownership)
   ↓ Call AppBuilder.Contracts.IApplicationReleaseService (or TenantApplication for tenant releases)
   ↓ Input: { applicationReleaseId: guid }
   ↓
   ← Returns: ApplicationSnapshotDto {
       Navigation: [...],
       Pages: [...],
       DataSources: [...],
       // NO CONFIGURATION - only structure
     }
   ↓
3. CHECK COMPATIBILITY
   ↓ AppRuntime.Domain.ICompatibilityChecker
   ↓ Validate runtime supports all component types in snapshot
   ↓
   ← Compatible: true/false
   ↓
4. CREATE RUNTIME INSTANCE
   ↓ RuntimeInstance.Start(...)
   ↓ Save to appruntime.runtime_instances
   ↓
   ← Status: Running
   ↓
5. LOAD & RENDER COMPONENTS
   ↓ For each component in snapshot:
   ↓   - Get loader from component registry
   ↓   - loader.Load(componentDefinition, tenantConfiguration) // Config from step 1
   ↓   - renderer.Render(component)
   ↓
6. RETURN RENDERED APP
```

---

## Key Internal Components

### 1. RuntimeInstance (Entity)

Tracks the **running state** of an application:

```csharp
public class RuntimeInstance : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid TenantApplicationId { get; private set; }
    public Guid EnvironmentId { get; private set; }
    public Guid ApplicationReleaseId { get; private set; }
    public Guid RuntimeVersionId { get; private set; }
    public RuntimeInstanceStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? StoppedAt { get; private set; }
    public DateTime? LastHealthCheckAt { get; private set; }
    public string Configuration { get; private set; } // Merged JSON
    public string Metadata { get; private set; } // Runtime metadata JSON

    public Result<Unit> Start(Guid runtimeVersionId, string configuration, IDateTimeProvider dateTimeProvider)
    {
        if (Status == RuntimeInstanceStatus.Running)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.AlreadyRunning", "Instance is already running"));

        RuntimeVersionId = runtimeVersionId;
        Configuration = configuration;
        Status = RuntimeInstanceStatus.Running;
        StartedAt = dateTimeProvider.UtcNow;
        StoppedAt = null;

        AddDomainEvent(new RuntimeInstanceStartedEvent(
            Id, TenantId, TenantApplicationId, EnvironmentId, ApplicationReleaseId, StartedAt));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> Stop(string? reason, IDateTimeProvider dateTimeProvider)
    {
        if (Status != RuntimeInstanceStatus.Running)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.NotRunning", "Instance is not running"));

        Status = RuntimeInstanceStatus.Stopped;
        StoppedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new RuntimeInstanceStoppedEvent(
            Id, TenantId, TenantApplicationId, EnvironmentId, StoppedAt.Value, reason));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> RecordHealthCheck(IDateTimeProvider dateTimeProvider)
    {
        if (Status != RuntimeInstanceStatus.Running)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.NotRunning", "Cannot record health check for stopped instance"));

        LastHealthCheckAt = dateTimeProvider.UtcNow;
        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> UpdateConfiguration(string newConfiguration, IDateTimeProvider dateTimeProvider)
    {
        if (Status != RuntimeInstanceStatus.Running)
            return Result<Unit>.Failure(Error.Validation("RuntimeInstance.NotRunning", "Cannot update configuration for stopped instance"));

        var oldConfiguration = Configuration;
        Configuration = newConfiguration;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new RuntimeInstanceConfigurationUpdatedEvent(
            Id, TenantId, TenantApplicationId, oldConfiguration, newConfiguration, UpdatedAt.Value));

        return Result<Unit>.Success(Unit.Value);
    }
}

public enum RuntimeInstanceStatus
{
    Stopped = 0,
    Running = 1,
    Error = 2
}
```

**Purpose**: 
- Audit trail (when did this app start/stop?)
- Health monitoring (is it still running?)
- Debugging (which version is tenant X running?)

### 2. IApplicationLoader (Service)

Fetches application structure and configuration. For **installed platform apps**, structure comes from **AppBuilder** (application_releases); for **tenant custom/forked apps**, structure comes from **TenantApplication** (tenant_application_releases). Configuration always comes from TenantApplication.

```csharp
public interface IApplicationLoader
{
  Task<LoadedApplication> LoadApplicationAsync(
    Guid applicationReleaseId,   // May be AppBuilder release or TenantApplication release
    string tenantConfiguration); // FROM TENANTAPPLICATION
}

public class LoadedApplication
{
  public ApplicationSnapshotDto Snapshot { get; }           // FROM APPBUILDER or TENANTAPPLICATION
  public string TenantConfiguration { get; }                // FROM TENANTAPPLICATION
  public List<NavigationComponentDto> NavigationComponents { get; }
  public List<PageComponentDto> PageComponents { get; }
  public List<DataSourceComponentDto> DataSourceComponents { get; }
}
```

**Data Sources**:
- **AppBuilder**: Application structure (navigation, pages, data sources)
- **TenantApplication**: Tenant-specific configuration (settings, overrides)

### 3. Component Loading with Configuration

```csharp
// Component loaders receive BOTH structure and configuration
public interface IComponentLoader
{
  object Load(
    ComponentDefinitionDto definition,  // FROM APPBUILDER
    string tenantConfiguration);        // FROM TENANTAPPLICATION
}

// Example: Navigation Component Loader
public class NavigationComponentLoader : IComponentLoader
{
  public object Load(ComponentDefinitionDto definition, string tenantConfiguration)
  {
    // 1. Parse structure from AppBuilder
    var navStructure = JsonSerializer.Deserialize<NavigationStructure>(definition.StructureJson);
    
    // 2. Parse tenant config from TenantApplication
    var config = JsonSerializer.Deserialize<TenantConfig>(tenantConfiguration);
    
    // 3. Apply tenant-specific customizations
    navStructure.Theme = config.Theme;
    navStructure.Logo = config.LogoUrl;
    
    return new NavigationViewModel(navStructure);
  }
}
```

---

## Data Flow Diagram

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │ GET /acme-corp/crm/production
       ↓
┌─────────────────────────────────────────────────────────────────┐
│                         AppRuntime                              │
│                                                                 │
│  1. Resolve Application                                         │
│     ↓ Call TenantApplication.Contracts.IApplicationResolverService │
│     ← Returns: {                                                │
│         applicationReleaseId: "abc-123",                        │
│         configuration: "{ theme: 'dark', ... }" ← FROM TENANT   │
│       }                                                         │
│                                                                 │
│  2. Load Application Structure                                  │
│     ↓ Call AppBuilder.Contracts.IApplicationReleaseService      │
│     ← Returns: {                                                │
│         snapshot: { navigation: [...], pages: [...] }           │
│         // NO CONFIGURATION - only structure                    │
│       }                                                         │
│                                                                 │
│  3. Check Compatibility                                         │
│     ↓ Validate runtime supports all components                 │
│     ← Compatible: true                                          │
│                                                                 │
│  4. Create RuntimeInstance                                      │
│     ↓ Save to appruntime.runtime_instances                      │
│     ← Status: Running                                           │
│                                                                 │
│  5. Load Components with Configuration                          │
│     ↓ For each component in snapshot:                           │
│       - loader.Load(componentDef, tenantConfig) ← MERGE HERE    │
│       - renderer.Render(component)                              │
│                                                                 │
│  6. Return Rendered App                                         │
└─────┼───────────────────────────────────────────────────────────┘
      │ HTML/JSON response
      ↓
┌─────────────┐
│   Browser   │
│ (App Loaded)│
└─────────────┘
```

---

## Configuration Merge Strategy

### Configuration Layers (Priority: High → Low)

```
1. Environment-Specific Config (TenantApplication.Environment.Configuration)
   ↓ Overrides
2. Tenant Application Config (TenantApplication.Configuration)
   ↓ Overrides
3. Default Config Schema (AppBuilder.ApplicationDefinition - schema only)
```

**Example**:

```json
// AppBuilder: Default schema (not values)
{
  "configSchema": {
    "theme": { "type": "string", "default": "light" },
    "maxUsers": { "type": "number", "default": 10 }
  }
}

// TenantApplication: Tenant-specific values
{
  "theme": "dark",
  "maxUsers": 100
}

// TenantApplication.Environment: Environment override
{
  "maxUsers": 50  // Override for staging environment
}

// FINAL MERGED CONFIG (used by AppRuntime)
{
  "theme": "dark",      // From tenant config
  "maxUsers": 50        // From environment override
}
```

---

## Key Design Decisions

### 1. ✅ TenantApplication Owns Configuration
- **Reason**: Tenants customize apps after installation
- **Benefit**: Same app, different settings per tenant
- **Example**: Tenant A uses dark theme, Tenant B uses light theme

### 2. ✅ AppBuilder Owns Structure
- **Reason**: Application definition is immutable per release
- **Benefit**: Consistent structure across all tenants
- **Example**: All tenants see same navigation structure

### 3. ✅ AppRuntime Merges at Load Time
- **Reason**: Configuration can change without redeploying app
- **Benefit**: Tenants can update settings without creating new release
- **Example**: Change API URL without redeploying application

### 4. ✅ Environment-Specific Overrides
- **Reason**: Dev/Staging/Prod need different settings
- **Benefit**: Same app, different config per environment
- **Example**: Dev uses localhost API, Prod uses production API

---

## Summary

**AppRuntime is the bridge between:**
- **AppBuilder** (defines application STRUCTURE)
- **TenantApplication** (defines tenant-specific CONFIGURATION)
- **End Users** (actually using the app)

**Configuration Flow**:
```
AppBuilder (structure) + TenantApplication (config) → AppRuntime (execution)
```

**It does 3 main things:**
1. **Load** application structure from AppBuilder
2. **Load** tenant configuration from TenantApplication
3. **Merge** and execute the configured application

**It does NOT:**
- ❌ Store application structure (that's AppBuilder's job)
- ❌ Store tenant configuration (that's TenantApplication's job)
- ❌ Handle tenant installation (that's TenantApplication's job)

It's purely the **execution engine** that brings structure + configuration together at runtime.
