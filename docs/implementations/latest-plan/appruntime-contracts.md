# AppRuntime Module - Contracts Layer Implementation Plan

## Overview

The AppRuntime Contracts layer defines public interfaces and DTOs for inter-module communication. Other modules can reference this project to interact with AppRuntime without taking a dependency on its implementation.

**Key Principle**: AppRuntime is the **execution engine** that loads structure from AppBuilder and configuration from TenantApplication.

---

## Project Structure

```
AppRuntime.Contracts/
├── Services/
│   ├── IRuntimeInstanceService.cs
│   ├── IRuntimeVersionService.cs
│   ├── IComponentRegistrationService.cs
│   └── ICompatibilityCheckService.cs
├── DTOs/
│   ├── RuntimeInstanceDto.cs
│   ├── RuntimeVersionDto.cs
│   ├── ComponentRegistrationDto.cs
│   ├── LoadedApplicationDto.cs
│   └── CompatibilityCheckResultDto.cs
├── Events/
│   ├── RuntimeInstanceStartedEvent.cs
│   ├── RuntimeInstanceStoppedEvent.cs
│   ├── RuntimeInstanceRestartedEvent.cs
│   ├── RuntimeInstanceConfigurationUpdatedEvent.cs
│   ├── RuntimeVersionActivatedEvent.cs
│   ├── RuntimeVersionDeactivatedEvent.cs
│   └── ComponentRegisteredEvent.cs
└── AppRuntime.Contracts.csproj
```

---

## Service Interfaces

### 1. IRuntimeInstanceService

**File**: `AppRuntime.Contracts/Services/IRuntimeInstanceService.cs`

```csharp
namespace AppRuntime.Contracts.Services;

public interface IRuntimeInstanceService
{
    Task<Result<RuntimeInstanceDto>> StartInstanceAsync(
        Guid tenantApplicationId,
        Guid environmentId,
        CancellationToken cancellationToken = default);

    Task<Result> StopInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    Task<Result> RestartInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    Task<Result<RuntimeInstanceDto>> GetInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    Task<Result<List<RuntimeInstanceDto>>> GetInstancesByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Result<List<RuntimeInstanceDto>>> GetInstancesByEnvironmentAsync(
        Guid environmentId,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateInstanceConfigurationAsync(
        Guid instanceId,
        string configuration,
        CancellationToken cancellationToken = default);

    Task<Result> RecordHealthCheckAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);
}
```

---

### 2. IRuntimeVersionService

**File**: `AppRuntime.Contracts/Services/IRuntimeVersionService.cs`

```csharp
namespace AppRuntime.Contracts.Services;

public interface IRuntimeVersionService
{
    Task<Result<RuntimeVersionDto>> GetVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<Result<RuntimeVersionDto>> GetActiveVersionAsync(
        CancellationToken cancellationToken = default);

    Task<Result<List<RuntimeVersionDto>>> GetAllVersionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result> ActivateVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<Result> DeactivateVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);
}
```

---

### 3. IComponentRegistrationService

**File**: `AppRuntime.Contracts/Services/IComponentRegistrationService.cs`

```csharp
namespace AppRuntime.Contracts.Services;

public interface IComponentRegistrationService
{
    Task<Result<ComponentRegistrationDto>> RegisterComponentAsync(
        string componentType,
        string loaderAssembly,
        string loaderClass,
        CancellationToken cancellationToken = default);

    Task<Result<ComponentRegistrationDto>> GetComponentRegistrationAsync(
        string componentType,
        CancellationToken cancellationToken = default);

    Task<Result<List<ComponentRegistrationDto>>> GetAllComponentRegistrationsAsync(
        CancellationToken cancellationToken = default);

    Task<Result> UnregisterComponentAsync(
        string componentType,
        CancellationToken cancellationToken = default);
}
```

---

### 4. ICompatibilityCheckService

**File**: `AppRuntime.Contracts/Services/ICompatibilityCheckService.cs`

**Purpose**: Check if runtime can execute an application release.

```csharp
namespace AppRuntime.Contracts.Services;

public interface ICompatibilityCheckService
{
    /// <summary>
    /// Check if the current runtime version can execute the specified application release
    /// </summary>
    Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a specific runtime version can execute the specified application release
    /// </summary>
    Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid applicationReleaseId,
        Guid runtimeVersionId,
        CancellationToken cancellationToken = default);
}
```

---

## DTOs

### 1. RuntimeInstanceDto

**File**: `AppRuntime.Contracts/DTOs/RuntimeInstanceDto.cs`

```csharp
namespace AppRuntime.Contracts.DTOs;

public record RuntimeInstanceDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }                    // Added - from architecture
    public Guid TenantApplicationId { get; init; }
    public Guid ApplicationReleaseId { get; init; }
    public Guid EnvironmentId { get; init; }
    public string ApplicationVersion { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;    // Running/Stopped/Error
    public DateTime StartedAt { get; init; }
    public DateTime? StoppedAt { get; init; }
    public DateTime? LastHealthCheckAt { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
```

---

### 2. RuntimeVersionDto

**File**: `AppRuntime.Contracts/DTOs/RuntimeVersionDto.cs`

```csharp
namespace AppRuntime.Contracts.DTOs;

public record RuntimeVersionDto
{
    public Guid Id { get; init; }
    public string Version { get; init; } = string.Empty;
    public string MinApplicationVersion { get; init; } = string.Empty;
    public string? MaxApplicationVersion { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public List<string> SupportedComponentTypes { get; init; } = new();
    public string? ReleaseNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
```

---

### 3. ComponentRegistrationDto

**File**: `AppRuntime.Contracts/DTOs/ComponentRegistrationDto.cs`

```csharp
namespace AppRuntime.Contracts.DTOs;

public record ComponentRegistrationDto
{
    public Guid Id { get; init; }
    public string ComponentType { get; init; } = string.Empty;
    public string LoaderAssembly { get; init; } = string.Empty;
    public string LoaderClass { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
```

---

### 4. LoadedApplicationDto

**File**: `AppRuntime.Contracts/DTOs/LoadedApplicationDto.cs`

**Purpose**: Represents a fully loaded application (structure + configuration).

```csharp
namespace AppRuntime.Contracts.DTOs;

public record LoadedApplicationDto
{
    public Guid ApplicationReleaseId { get; init; }
    public string ApplicationVersion { get; init; } = string.Empty;
    public ApplicationSnapshotDto Snapshot { get; init; } = null!;           // FROM APPBUILDER
    public string TenantConfiguration { get; init; } = string.Empty;         // FROM TENANTAPPLICATION (merged)
    public List<NavigationComponentDto> NavigationComponents { get; init; } = new();
    public List<PageComponentDto> PageComponents { get; init; } = new();
    public List<DataSourceComponentDto> DataSourceComponents { get; init; } = new();
}

// Note: ApplicationSnapshotDto, NavigationComponentDto, PageComponentDto, DataSourceComponentDto
// are defined in AppBuilder.Contracts
```

---

### 5. CompatibilityCheckResultDto

**File**: `AppRuntime.Contracts/DTOs/CompatibilityCheckResultDto.cs`

```csharp
namespace AppRuntime.Contracts.DTOs;

public record CompatibilityCheckResultDto
{
    public bool IsCompatible { get; init; }
    public List<string> MissingComponentTypes { get; init; } = new();
    public List<string> IncompatibleVersions { get; init; } = new();
    public string? ErrorMessage { get; init; }
}
```

---

## Integration Events

### 1. RuntimeInstanceStartedEvent

**File**: `AppRuntime.Contracts/Events/RuntimeInstanceStartedEvent.cs`

```csharp
namespace AppRuntime.Contracts.Events;

public record RuntimeInstanceStartedEvent : IIntegrationEvent
{
    public Guid InstanceId { get; init; }
    public Guid TenantId { get; init; }                    // Added
    public Guid TenantApplicationId { get; init; }
    public Guid EnvironmentId { get; init; }
    public Guid ApplicationReleaseId { get; init; }        // Added
    public string ApplicationVersion { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

---

### 2. RuntimeInstanceStoppedEvent

**File**: `AppRuntime.Contracts/Events/RuntimeInstanceStoppedEvent.cs`

```csharp
namespace AppRuntime.Contracts.Events;

public record RuntimeInstanceStoppedEvent : IIntegrationEvent
{
    public Guid InstanceId { get; init; }
    public Guid TenantId { get; init; }
    public Guid TenantApplicationId { get; init; }
    public Guid EnvironmentId { get; init; }
    public DateTime StoppedAt { get; init; }
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

---

### 3. RuntimeInstanceRestartedEvent

**File**: `AppRuntime.Contracts/Events/RuntimeInstanceRestartedEvent.cs`

```csharp
namespace AppRuntime.Contracts.Events;

public record RuntimeInstanceRestartedEvent : IIntegrationEvent
{
    public Guid InstanceId { get; init; }
    public Guid TenantId { get; init; }
    public Guid TenantApplicationId { get; init; }
    public Guid EnvironmentId { get; init; }
    public DateTime RestartedAt { get; init; }
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

---

### 4. RuntimeInstanceConfigurationUpdatedEvent

**File**: `AppRuntime.Contracts/Events/RuntimeInstanceConfigurationUpdatedEvent.cs`

```csharp
namespace AppRuntime.Contracts.Events;

public record RuntimeInstanceConfigurationUpdatedEvent : IIntegrationEvent
{
    public Guid InstanceId { get; init; }
    public Guid TenantId { get; init; }
    public Guid TenantApplicationId { get; init; }
    public string OldConfiguration { get; init; } = string.Empty;
    public string NewConfiguration { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

---

### 5. RuntimeVersionActivatedEvent

**File**: `AppRuntime.Contracts/Events/RuntimeVersionActivatedEvent.cs`

```csharp
namespace AppRuntime.Contracts.Events;

public record RuntimeVersionActivatedEvent : IIntegrationEvent
{
    public Guid VersionId { get; init; }
    public string Version { get; init; } = string.Empty;
    public List<string> SupportedComponentTypes { get; init; } = new();
    public DateTime ActivatedAt { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

---

### 6. RuntimeVersionDeactivatedEvent

**File**: `AppRuntime.Contracts/Events/RuntimeVersionDeactivatedEvent.cs`

```csharp
namespace AppRuntime.Contracts.Events;

public record RuntimeVersionDeactivatedEvent : IIntegrationEvent
{
    public Guid VersionId { get; init; }
    public string Version { get; init; } = string.Empty;
    public DateTime DeactivatedAt { get; init; }
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

---

### 7. ComponentRegisteredEvent

**File**: `AppRuntime.Contracts/Events/ComponentRegisteredEvent.cs`

```csharp
namespace AppRuntime.Contracts.Events;

public record ComponentRegisteredEvent : IIntegrationEvent
{
    public Guid RegistrationId { get; init; }
    public string ComponentType { get; init; } = string.Empty;
    public string LoaderAssembly { get; init; } = string.Empty;
    public string LoaderClass { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

---

## Project Configuration

**File**: `AppRuntime.Contracts/AppRuntime.Contracts.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>AppRuntime.Contracts</RootNamespace>
    <AssemblyName>Datarizen.AppRuntime.Contracts</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />
    <ProjectReference Include="..\AppBuilder\AppBuilder.Contracts\AppBuilder.Contracts.csproj" />
    <ProjectReference Include="..\TenantApplication\TenantApplication.Contracts\TenantApplication.Contracts.csproj" />
  </ItemGroup>
</Project>
```

---

## Usage Examples

### Example 1: Starting a Runtime Instance

```csharp
// From TenantApplication module or API
using AppRuntime.Contracts.Services;

var result = await _runtimeInstanceService.StartInstanceAsync(
    tenantApplicationId: tenantApp.Id,
    environmentId: environment.Id,
    cancellationToken);

if (result.IsSuccess)
{
    var instance = result.Value;
    Console.WriteLine($"Instance {instance.Id} started at {instance.StartedAt}");
}
```

---

### Example 2: Checking Compatibility

```csharp
// Before deploying a release
using AppRuntime.Contracts.Services;

var result = await _compatibilityCheckService.CheckCompatibilityAsync(
    applicationReleaseId: release.Id,
    cancellationToken);

if (result.IsSuccess && result.Value.IsCompatible)
{
    // Proceed with deployment
}
else
{
    // Show missing components
    foreach (var missing in result.Value.MissingComponentTypes)
    {
        Console.WriteLine($"Missing component loader: {missing}");
    }
}
```

---

### Example 3: Handling RuntimeInstanceStartedEvent

```csharp
// From monitoring module
using AppRuntime.Contracts.Events;

public class RuntimeInstanceStartedEventHandler 
    : IIntegrationEventHandler<RuntimeInstanceStartedEvent>
{
    public async Task Handle(RuntimeInstanceStartedEvent @event, CancellationToken cancellationToken)
    {
        // Log to monitoring system
        await _logger.LogAsync(
            $"Runtime instance {@event.InstanceId} started for tenant {@event.TenantId}",
            cancellationToken);

        // Update metrics
        await _metrics.IncrementAsync("runtime.instances.started", cancellationToken);
    }
}
```

---

## Implementation Checklist

### Service Interfaces
- [ ] Create `IRuntimeInstanceService.cs`
- [ ] Create `IRuntimeVersionService.cs`
- [ ] Create `IComponentRegistrationService.cs`
- [ ] Create `ICompatibilityCheckService.cs`

### DTOs
- [ ] Create `RuntimeInstanceDto.cs` (with TenantId)
- [ ] Create `RuntimeVersionDto.cs`
- [ ] Create `ComponentRegistrationDto.cs`
- [ ] Create `LoadedApplicationDto.cs`
- [ ] Create `CompatibilityCheckResultDto.cs`

### Integration Events
- [ ] Create `RuntimeInstanceStartedEvent.cs`
- [ ] Create `RuntimeInstanceStoppedEvent.cs`
- [ ] Create `RuntimeInstanceRestartedEvent.cs`
- [ ] Create `RuntimeInstanceConfigurationUpdatedEvent.cs`
- [ ] Create `RuntimeVersionActivatedEvent.cs`
- [ ] Create `RuntimeVersionDeactivatedEvent.cs`
- [ ] Create `ComponentRegisteredEvent.cs`

### Project Configuration
- [ ] Create `AppRuntime.Contracts.csproj`
- [ ] Add references to BuildingBlocks.Kernel
- [ ] Add references to AppBuilder.Contracts
- [ ] Add references to TenantApplication.Contracts
- [ ] Add to solution

---

## Estimated Time: 4 hours

- Service interfaces: 1.5 hours
- DTOs: 1 hour
- Integration events: 1 hour
- Project setup and documentation: 0.5 hours
