# AppRuntime Module - Application Layer Implementation Plan

## Overview

The AppRuntime Application layer contains:
- Command and query handlers (CQRS pattern)
- Application services (contract implementations)
- DTOs and mapping
- Validation logic
- Domain event handlers

---

## Project Structure

```
AppRuntime.Application/
├── Commands/
│   ├── StartRuntimeInstance/
│   │   ├── StartRuntimeInstanceCommand.cs
│   │   ├── StartRuntimeInstanceCommandHandler.cs
│   │   └── StartRuntimeInstanceCommandValidator.cs
│   ├── StopRuntimeInstance/
│   ├── RestartRuntimeInstance/
│   ├── UpdateRuntimeInstanceConfiguration/
│   ├── CreateRuntimeVersion/
│   ├── ActivateRuntimeVersion/
│   ├── DeactivateRuntimeVersion/
│   ├── SetDefaultRuntimeVersion/
│   ├── RegisterComponent/
│   ├── ActivateComponent/
│   └── DeactivateComponent/
├── Queries/
│   ├── GetRuntimeInstanceById/
│   ├── GetRuntimeInstancesByEnvironment/
│   ├── GetRunningInstances/
│   ├── GetRuntimeVersionById/
│   ├── GetDefaultRuntimeVersion/
│   ├── GetCompatibleRuntimeVersions/
│   ├── GetComponentRegistrationById/
│   ├── GetComponentRegistrationByType/
│   └── GetActiveComponentRegistrations/
├── Services/
│   ├── RuntimeInstanceService.cs
│   ├── RuntimeVersionService.cs
│   └── ComponentRegistrationService.cs
├── EventHandlers/
│   ├── RuntimeInstanceStartedEventHandler.cs
│   └── RuntimeVersionActivatedEventHandler.cs
├── Mappings/
│   └── AppRuntimeMappingProfile.cs
├── AppRuntimeApplicationModule.cs
├── DependencyInjection.cs
└── AppRuntime.Application.csproj
```

---

## Commands

### 1. StartRuntimeInstanceCommand

**File**: `AppRuntime.Application/Commands/StartRuntimeInstance/StartRuntimeInstanceCommand.cs`

```csharp
using BuildingBlocks.Application;
using BuildingBlocks.Kernel.Application;

namespace AppRuntime.Application.Commands.StartRuntimeInstance;

public record StartRuntimeInstanceCommand(
    Guid TenantApplicationId,
    Guid EnvironmentId,
    Dictionary<string, object>? Configuration = null) : ICommand<Result<Guid>>;
```

**File**: `AppRuntime.Application/Commands/StartRuntimeInstance/StartRuntimeInstanceCommandHandler.cs`

```csharp
using AppRuntime.Contracts.Events;
using AppRuntime.Domain.Repositories;
using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.EventBus;
using BuildingBlocks.Kernel.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AppRuntime.Application.Commands.StartRuntimeInstance;

public class StartRuntimeInstanceCommandHandler 
    : ICommandHandler<StartRuntimeInstanceCommand, Result<Guid>>
{
    private readonly IRuntimeInstanceRepository _instanceRepository;
    private readonly IRuntimeVersionRepository _versionRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<StartRuntimeInstanceCommandHandler> _logger;
    
    public StartRuntimeInstanceCommandHandler(
        IRuntimeInstanceRepository instanceRepository,
        IRuntimeVersionRepository versionRepository,
        IEventBus eventBus,
        ILogger<StartRuntimeInstanceCommandHandler> logger)
    {
        _instanceRepository = instanceRepository;
        _versionRepository = versionRepository;
        _eventBus = eventBus;
        _logger = logger;
    }
    
    public async Task<Result<Guid>> Handle(
        StartRuntimeInstanceCommand request,
        CancellationToken cancellationToken)
    {
        // Check if there's already a running instance
        var hasActiveInstance = await _instanceRepository.HasActiveInstanceAsync(
            request.TenantApplicationId,
            request.EnvironmentId,
            cancellationToken);
        
        if (hasActiveInstance)
        {
            return Result<Guid>.Failure(
                Error.Conflict(
                    "AppRuntime.InstanceAlreadyRunning",
                    "An instance is already running for this tenant application in this environment"));
        }
        
        // Get default runtime version
        var runtimeVersion = await _versionRepository.GetDefaultAsync(cancellationToken);
        
        if (runtimeVersion == null)
        {
            return Result<Guid>.Failure(
                Error.NotFound(
                    "AppRuntime.NoDefaultVersion",
                    "No default runtime version is configured"));
        }
        
        // TODO: Load ApplicationRelease from TenantApplication module
        // For now, use placeholder values
        var applicationReleaseId = Guid.NewGuid();
        var applicationVersion = "1.0.0";
        
        // Create runtime instance
        var instance = Domain.Entities.RuntimeInstance.Create(
            request.TenantApplicationId,
            applicationReleaseId,
            request.EnvironmentId,
            applicationVersion,
            request.Configuration ?? new Dictionary<string, object>());
        
        await _instanceRepository.AddAsync(instance, cancellationToken);
        
        // Publish integration event
        await _eventBus.PublishAsync(
            new RuntimeInstanceStartedEvent
            {
                InstanceId = instance.Id,
                TenantApplicationId = instance.TenantApplicationId,
                EnvironmentId = instance.EnvironmentId,
                ApplicationVersion = instance.ApplicationVersion,
                StartedAt = instance.StartedAt,
                Configuration = instance.Configuration
            },
            cancellationToken);
        
        _logger.LogInformation(
            "Started runtime instance {InstanceId} for tenant application {TenantApplicationId}",
            instance.Id,
            request.TenantApplicationId);
        
        return Result<Guid>.Success(instance.Id);
    }
}
```

**File**: `AppRuntime.Application/Commands/StartRuntimeInstance/StartRuntimeInstanceCommandValidator.cs`

```csharp
using FluentValidation;

namespace AppRuntime.Application.Commands.StartRuntimeInstance;

public class StartRuntimeInstanceCommandValidator : AbstractValidator<StartRuntimeInstanceCommand>
{
    public StartRuntimeInstanceCommandValidator()
    {
        RuleFor(x => x.TenantApplicationId)
            .NotEmpty()
            .WithMessage("Tenant application ID is required");
        
        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment ID is required");
    }
}
```

---

### 2. StopRuntimeInstanceCommand

**File**: `AppRuntime.Application/Commands/StopRuntimeInstance/StopRuntimeInstanceCommand.cs`

```csharp
using BuildingBlocks.Application;
using BuildingBlocks.Kernel.Application;

namespace AppRuntime.Application.Commands.StopRuntimeInstance;

public record StopRuntimeInstanceCommand(Guid InstanceId) : ICommand<Result>;
```

**File**: `AppRuntime.Application/Commands/StopRuntimeInstance/StopRuntimeInstanceCommandHandler.cs`

```csharp
using AppRuntime.Contracts.Events;
using AppRuntime.Domain.Repositories;
using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.EventBus;
using BuildingBlocks.Kernel.Application;
using Microsoft.Extensions.Logging;

namespace AppRuntime.Application.Commands.StopRuntimeInstance;

public class StopRuntimeInstanceCommandHandler : ICommandHandler<StopRuntimeInstanceCommand, Result>
{
    private readonly IRuntimeInstanceRepository _instanceRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<StopRuntimeInstanceCommandHandler> _logger;
    
    public StopRuntimeInstanceCommandHandler(
        IRuntimeInstanceRepository instanceRepository,
        IEventBus eventBus,
        ILogger<StopRuntimeInstanceCommandHandler> logger)
    {
        _instanceRepository = instanceRepository;
        _eventBus = eventBus;
        _logger = logger;
    }
    
    public async Task<Result> Handle(
        StopRuntimeInstanceCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _instanceRepository.GetByIdAsync(request.InstanceId, cancellationToken);
        
        if (instance == null)
        {
            return Result.Failure(
                Error.NotFound(
                    "AppRuntime.InstanceNotFound",
                    $"Runtime instance {request.InstanceId} not found"));
        }
        
        instance.Stop();
        
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        
        await _eventBus.PublishAsync(
            new RuntimeInstanceStoppedEvent
            {
                InstanceId = instance.Id,
                TenantApplicationId = instance.TenantApplicationId,
                EnvironmentId = instance.EnvironmentId,
                StoppedAt = instance.StoppedAt!.Value
            },
            cancellationToken);
        
        _logger.LogInformation(
            "Stopped runtime instance {InstanceId}",
            instance.Id);
        
        return Result.Success();
    }
}
```

---

### 3. CreateRuntimeVersionCommand

**File**: `AppRuntime.Application/Commands/CreateRuntimeVersion/CreateRuntimeVersionCommand.cs`

```csharp
using BuildingBlocks.Application;
using BuildingBlocks.Kernel.Application;

namespace AppRuntime.Application.Commands.CreateRuntimeVersion;

public record CreateRuntimeVersionCommand(
    string Version,
    string MinApplicationVersion,
    string? MaxApplicationVersion,
    List<string> SupportedComponentTypes,
    bool IsDefault,
    string? ReleaseNotes) : ICommand<Result<Guid>>;
```

**File**: `AppRuntime.Application/Commands/CreateRuntimeVersion/CreateRuntimeVersionCommandHandler.cs`

```csharp
using AppRuntime.Domain.Repositories;
using BuildingBlocks.Application;
using BuildingBlocks.Kernel.Application;
using Microsoft.Extensions.Logging;

namespace AppRuntime.Application.Commands.CreateRuntimeVersion;

public class CreateRuntimeVersionCommandHandler 
    : ICommandHandler<CreateRuntimeVersionCommand, Result<Guid>>
{
    private readonly IRuntimeVersionRepository _versionRepository;
    private readonly ILogger<CreateRuntimeVersionCommandHandler> _logger;
    
    public CreateRuntimeVersionCommandHandler(
        IRuntimeVersionRepository versionRepository,
        ILogger<CreateRuntimeVersionCommandHandler> logger)
    {
        _versionRepository = versionRepository;
        _logger = logger;
    }
    
    public async Task<Result<Guid>> Handle(
        CreateRuntimeVersionCommand request,
        CancellationToken cancellationToken)
    {
        // Check if version already exists
        var exists = await _versionRepository.ExistsByVersionAsync(
            request.Version,
            cancellationToken);
        
        if (exists)
        {
            return Result<Guid>.Failure(
                Error.Conflict(
                    "AppRuntime.VersionAlreadyExists",
                    $"Runtime version {request.Version} already exists"));
        }
        
        // If this is set as default, unset other defaults
        if (request.IsDefault)
        {
            var currentDefault = await _versionRepository.GetDefaultAsync(cancellationToken);
            if (currentDefault != null)
            {
                currentDefault.UnsetAsDefault();
                await _versionRepository.UpdateAsync(currentDefault, cancellationToken);
            }
        }
        
        var version = Domain.Entities.RuntimeVersion.Create(
            request.Version,
            request.MinApplicationVersion,
            request.MaxApplicationVersion,
            request.SupportedComponentTypes,
            request.IsDefault,
            request.ReleaseNotes);
        
        await _versionRepository.AddAsync(version, cancellationToken);
        
        _logger.LogInformation(
            "Created runtime version {Version}",
            version.Version);
        
        return Result<Guid>.Success(version.Id);
    }
}
```

---

## Queries

### 1. GetRuntimeInstanceByIdQuery

**File**: `AppRuntime.Application/Queries/GetRuntimeInstanceById/GetRuntimeInstanceByIdQuery.cs`

```csharp
using AppRuntime.Contracts.DTOs;
using BuildingBlocks.Application;
using BuildingBlocks.Kernel.Application;

namespace AppRuntime.Application.Queries.GetRuntimeInstanceById;

public record GetRuntimeInstanceByIdQuery(Guid InstanceId) : IQuery<Result<RuntimeInstanceDto>>;
```

**File**: `AppRuntime.Application/Queries/GetRuntimeInstanceById/GetRuntimeInstanceByIdQueryHandler.cs`

```csharp
using AppRuntime.Contracts.DTOs;
using AppRuntime.Domain.Repositories;
using AutoMapper;
using BuildingBlocks.Application;
using BuildingBlocks.Kernel.Application;

namespace AppRuntime.Application.Queries.GetRuntimeInstanceById;

public class GetRuntimeInstanceByIdQueryHandler 
    : IQueryHandler<GetRuntimeInstanceByIdQuery, Result<RuntimeInstanceDto>>
{
    private readonly IRuntimeInstanceRepository _instanceRepository;
    private readonly IMapper _mapper;
    
    public GetRuntimeInstanceByIdQueryHandler(
        IRuntimeInstanceRepository instanceRepository,
        IMapper mapper)
    {
        _instanceRepository = instanceRepository;
        _mapper = mapper;
    }
    
    public async Task<Result<RuntimeInstanceDto>> Handle(
        GetRuntimeInstanceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var instance = await _instanceRepository.GetByIdAsync(request.InstanceId, cancellationToken);
        
        if (instance == null)
        {
            return Result<RuntimeInstanceDto>.Failure(
                Error.NotFound(
                    "AppRuntime.InstanceNotFound",
                    $"Runtime instance {request.InstanceId} not found"));
        }
        
        var dto = _mapper.Map<RuntimeInstanceDto>(instance);
        
        return Result<RuntimeInstanceDto>.Success(dto);
    }
}
```

---

### 2. GetRunningInstancesQuery

**File**: `AppRuntime.Application/Queries/GetRunningInstances/GetRunningInstancesQuery.cs`

```csharp
using AppRuntime.Contracts.DTOs;
using BuildingBlocks.Application;
using BuildingBlocks.Kernel.Application;

namespace AppRuntime.Application.Queries.GetRunningInstances;

public record GetRunningInstancesQuery : IQuery<Result<List<RuntimeInstanceDto>>>;
```

**File**: `AppRuntime.Application/Queries/GetRunningInstances/GetRunningInstancesQueryHandler.cs`

```csharp
using AppRuntime.Contracts.DTOs;
using AppRuntime.Domain.Repositories;
using AutoMapper;
using BuildingBlocks.Application;
using BuildingBlocks.Kernel.Application;

namespace AppRuntime.Application.Queries.GetRunningInstances;

public class GetRunningInstancesQueryHandler 
    : IQueryHandler<GetRunningInstancesQuery, Result<List<RuntimeInstanceDto>>>
{
    private readonly IRuntimeInstanceRepository _instanceRepository;
    private readonly IMapper _mapper;
    
    public GetRunningInstancesQueryHandler(
        IRuntimeInstanceRepository instanceRepository,
        IMapper mapper)
    {
        _instanceRepository = instanceRepository;
        _mapper = mapper;
    }
    
    public async Task<Result<List<RuntimeInstanceDto>>> Handle(
        GetRunningInstancesQuery request,
        CancellationToken cancellationToken)
    {
        var instances = await _instanceRepository.GetRunningInstancesAsync(cancellationToken);
        
        var dtos = _mapper.Map<List<RuntimeInstanceDto>>(instances);
        
        return Result<List<RuntimeInstanceDto>>.Success(dtos);
    }
}
```

---

## Application Services

### RuntimeInstanceService

**File**: `AppRuntime.Application/Services/RuntimeInstanceService.cs`

```csharp
using AppRuntime.Application.Commands.StartRuntimeInstance;
using AppRuntime.Application.Commands.StopRuntimeInstance;
using AppRuntime.Application.Commands.RestartRuntimeInstance;
using AppRuntime.Application.Commands.UpdateRuntimeInstanceConfiguration;
using AppRuntime.Application.Commands.RecordHealthCheck;
using AppRuntime.Application.Queries.GetRuntimeInstanceById;
using AppRuntime.Application.Queries.GetRuntimeInstancesByEnvironment;
using AppRuntime.Application.Queries.GetRunningInstances;
using AppRuntime.Contracts.DTOs;
using AppRuntime.Contracts.Services;
using BuildingBlocks.Application;
using MediatR;

namespace AppRuntime.Application.Services;

public class RuntimeInstanceService : IRuntimeInstanceService
{
    private readonly IMediator _mediator;
    
    public RuntimeInstanceService(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<Result<RuntimeInstanceDto>> StartInstanceAsync(
        Guid tenantApplicationId,
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        var command = new StartRuntimeInstanceCommand(tenantApplicationId, environmentId);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
        {
            return Result<RuntimeInstanceDto>.Failure(result.Error);
        }
        
        return await GetByIdAsync(result.Value, cancellationToken);
    }
    
    public async Task<Result<RuntimeInstanceDto>> StopInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var command = new StopRuntimeInstanceCommand(instanceId);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
        {
            return Result<RuntimeInstanceDto>.Failure(result.Error);
        }
        
        return await GetByIdAsync(instanceId, cancellationToken);
    }
    
    public async Task<Result<RuntimeInstanceDto>> RestartInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var command = new RestartRuntimeInstanceCommand(instanceId);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
        {
            return Result<RuntimeInstanceDto>.Failure(result.Error);
        }
        
        return await GetByIdAsync(result.Value, cancellationToken);
    }
    
    public async Task<Result<RuntimeInstanceDto>> GetByIdAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRuntimeInstanceByIdQuery(instanceId);
        return await _mediator.Send(query, cancellationToken);
    }
    
    public async Task<Result<List<RuntimeInstanceDto>>> GetByEnvironmentIdAsync(
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRuntimeInstancesByEnvironmentQuery(environmentId);
        return await _mediator.Send(query, cancellationToken);
    }
    
    public async Task<Result<List<RuntimeInstanceDto>>> GetRunningInstancesAsync(
        CancellationToken cancellationToken = default)
    {
        var query = new GetRunningInstancesQuery();
        return await _mediator.Send(query, cancellationToken);
    }
    
    public async Task<Result<RuntimeInstanceDto>> UpdateConfigurationAsync(
        Guid instanceId,
        Dictionary<string, object> configuration,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateRuntimeInstanceConfigurationCommand(instanceId, configuration);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
        {
            return Result<RuntimeInstanceDto>.Failure(result.Error);
        }
        
        return await GetByIdAsync(instanceId, cancellationToken);
    }
    
    public async Task<Result<RuntimeInstanceDto>> RecordHealthCheckAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var command = new RecordHealthCheckCommand(instanceId);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
        {
            return Result<RuntimeInstanceDto>.Failure(result.Error);
        }
        
        return await GetByIdAsync(instanceId, cancellationToken);
    }
}
```

---

## AutoMapper Profile

**File**: `AppRuntime.Application/Mappings/AppRuntimeMappingProfile.cs`

```csharp
using AppRuntime.Contracts.DTOs;
using AppRuntime.Domain.Entities;
using AutoMapper;

namespace AppRuntime.Application.Mappings;

public class AppRuntimeMappingProfile : Profile
{
    public AppRuntimeMappingProfile()
    {
        CreateMap<RuntimeInstance, RuntimeInstanceDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
        
        CreateMap<RuntimeVersion, RuntimeVersionDto>();
        
        CreateMap<ComponentRegistration, ComponentRegistrationDto>();
    }
}
```

---

## Application Module

**File**: `AppRuntime.Application/AppRuntimeApplicationModule.cs`

```csharp
using System.Reflection;
using BuildingBlocks.Application.Modules;

namespace AppRuntime.Application;

public class AppRuntimeApplicationModule : IApplicationModule
{
    public Assembly ApplicationAssembly => typeof(AppRuntimeApplicationModule).Assembly;
}
```

---

## Dependency Injection

**File**: `AppRuntime.Application/DependencyInjection.cs`

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AppRuntime.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAppRuntimeApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        
        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);
        
        // AutoMapper
        services.AddAutoMapper(assembly);
        
        return services;
    }
}
```

---

## Project Configuration

**File**: `AppRuntime.Application/AppRuntime.Application.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>AppRuntime.Application</RootNamespace>
    <AssemblyName>Datarizen.AppRuntime.Application</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\AppRuntime.Domain\AppRuntime.Domain.csproj" />
    <ProjectReference Include="..\AppRuntime.Contracts\AppRuntime.Contracts.csproj" />
    <ProjectReference Include="..\..\BuildingBlocks\Application\BuildingBlocks.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="AutoMapper" />
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="MediatR" />
  </ItemGroup>
</Project>
```

---

## Implementation Checklist

### Commands (10 total)
- [ ] StartRuntimeInstance
- [ ] StopRuntimeInstance
- [ ] RestartRuntimeInstance
- [ ] UpdateRuntimeInstanceConfiguration
- [ ] RecordHealthCheck
- [ ] CreateRuntimeVersion
- [ ] ActivateRuntimeVersion
- [ ] DeactivateRuntimeVersion
- [ ] SetDefaultRuntimeVersion
- [ ] RegisterComponent

### Queries (9 total)
- [ ] GetRuntimeInstanceById
- [ ] GetRuntimeInstancesByEnvironment
- [ ] GetRunningInstances
- [ ] GetRuntimeVersionById
- [ ] GetDefaultRuntimeVersion
- [ ] GetCompatibleRuntimeVersions
- [ ] GetComponentRegistrationById
- [ ] GetComponentRegistrationByType
- [ ] GetActiveComponentRegistrations

### Services
- [ ] RuntimeInstanceService
- [ ] RuntimeVersionService
- [ ] ComponentRegistrationService

### Configuration
- [ ] AppRuntimeMappingProfile
- [ ] AppRuntimeApplicationModule
- [ ] DependencyInjection
- [ ] AppRuntime.Application.csproj

---

## Estimated Time: 8 hours

- Commands: 4 hours
- Queries: 2 hours
- Services: 1 hour
- Mappings and configuration: 1 hour


