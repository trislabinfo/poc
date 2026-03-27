# AppRuntime Module - Module Layer Implementation Plan

## Overview

The AppRuntime Module layer is responsible for:
- Registering all AppRuntime services and dependencies
- Configuring the module for different topologies
- Mapping API endpoints
- Providing module metadata

---

## Project Structure

```
AppRuntime.Module/
├── AppRuntimeModule.cs
├── DependencyInjection.cs
└── AppRuntime.Module.csproj
```

---

## Module Registration

### AppRuntimeModule

**File**: `AppRuntime.Module/AppRuntimeModule.cs`

**Purpose**: Main module class implementing `IModule` interface.

```csharp
using BuildingBlocks.Application.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppRuntime.Module;

public class AppRuntimeModule : IModule
{
    public string Name => "AppRuntime";
    
    public string Version => "1.0.0";
    
    public string Description => "Runtime instance management, version control, and component loader registry";
    
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register all AppRuntime services
        services.AddAppRuntimeModule(configuration);
    }
    
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // API endpoints are registered via controllers in AppRuntime.Api
        // No additional endpoint mapping needed here
    }
}
```

---

## Dependency Injection

**File**: `AppRuntime.Module/DependencyInjection.cs`

**Purpose**: Extension methods for registering AppRuntime services.

```csharp
using AppRuntime.Api;
using AppRuntime.Application;
using AppRuntime.Application.Services;
using AppRuntime.Contracts.Services;
using AppRuntime.Infrastructure;
using AppRuntime.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppRuntime.Module;

public static class DependencyInjection
{
    public static IServiceCollection AddAppRuntimeModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Application layer (MediatR handlers, validators, services)
        services.AddAppRuntimeApplication();
        
        // Infrastructure layer (DbContext, repositories, external services)
        services.AddAppRuntimeInfrastructure(configuration);
        
        // API layer (controllers)
        services.AddAppRuntimeApi();
        
        // Contract implementations (for inter-module communication)
        services.AddScoped<IRuntimeInstanceService, RuntimeInstanceService>();
        services.AddScoped<IRuntimeVersionService, RuntimeVersionService>();
        services.AddScoped<IComponentRegistrationService, ComponentRegistrationService>();
        
        return services;
    }
    
    /// <summary>
    /// Run AppRuntime database migrations.
    /// </summary>
    public static async Task MigrateAppRuntimeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppRuntimeDbContext>();
        await context.Database.MigrateAsync();
    }
}
```

---

## Project Configuration

**File**: `AppRuntime.Module/AppRuntime.Module.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>AppRuntime.Module</RootNamespace>
    <AssemblyName>Datarizen.AppRuntime.Module</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\AppRuntime.Api\AppRuntime.Api.csproj" />
    <ProjectReference Include="..\AppRuntime.Application\AppRuntime.Application.csproj" />
    <ProjectReference Include="..\AppRuntime.Infrastructure\AppRuntime.Infrastructure.csproj" />
    <ProjectReference Include="..\AppRuntime.Contracts\AppRuntime.Contracts.csproj" />
    <ProjectReference Include="..\..\BuildingBlocks\Application\BuildingBlocks.Application.csproj" />
  </ItemGroup>
</Project>
```

---

## Host Integration

### Monolith Host

**File**: `server/src/Hosts/MonolithHost/Program.cs`

Add AppRuntime module registration:

```csharp
// Load all modules unconditionally (monolith topology)
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<IdentityModule>(builder.Configuration);
builder.Services.AddModule<UserModule>(builder.Configuration);
builder.Services.AddModule<FeatureModule>(builder.Configuration);
builder.Services.AddModule<AppBuilderModule>(builder.Configuration);
builder.Services.AddModule<TenantApplicationModule>(builder.Configuration);
builder.Services.AddModule<AppRuntimeModule>(builder.Configuration); // ADD THIS

// ... later in the pipeline ...

app.UseModule<TenantModule>();
app.UseModule<IdentityModule>();
app.UseModule<UserModule>();
app.UseModule<FeatureModule>();
app.UseModule<AppBuilderModule>();
app.UseModule<TenantApplicationModule>();
app.UseModule<AppRuntimeModule>(); // ADD THIS
```

---

### MultiApp Hosts

**AppBuilder Host** (manages runtime):

**File**: `server/src/Hosts/MultiAppAppBuilderHost/Program.cs`

```csharp
var loadedModules = builder.Configuration.GetSection("LoadedModules").Get<string[]>() ?? Array.Empty<string>();

foreach (var module in loadedModules)
{
    switch (module)
    {
        case "FeatureManagement":
            builder.Services.AddModule<FeatureModule>(builder.Configuration);
            break;
        case "AppBuilder":
            builder.Services.AddModule<AppBuilderModule>(builder.Configuration);
            break;
        case "TenantApplication":
            builder.Services.AddModule<TenantApplicationModule>(builder.Configuration);
            break;
        case "AppRuntime": // ADD THIS
            builder.Services.AddModule<AppRuntimeModule>(builder.Configuration);
            break;
    }
}

// ... later in the pipeline ...

foreach (var module in loadedModules)
{
    switch (module)
    {
        case "FeatureManagement":
            app.UseModule<FeatureModule>();
            break;
        case "AppBuilder":
            app.UseModule<AppBuilderModule>();
            break;
        case "TenantApplication":
            app.UseModule<TenantApplicationModule>();
            break;
        case "AppRuntime": // ADD THIS
            app.UseModule<AppRuntimeModule>();
            break;
    }
}
```

**Update appsettings.json**:

```json
{
  "LoadedModules": [
    "FeatureManagement",
    "AppBuilder",
    "TenantApplication",
    "AppRuntime"
  ]
}
```

---

### Microservices Topology

Create a dedicated **AppRuntimeServiceHost** (similar to TenantServiceHost):

**File**: `server/src/Hosts/AppRuntimeServiceHost/Program.cs`

```csharp
using BuildingBlocks.Application.Modules;
using BuildingBlocks.Web.Extensions;
using Capabilities.Logging.Serilog;
using AppRuntime.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var applicationAssemblies = new[] { new AppRuntime.Application.AppRuntimeApplicationModule().ApplicationAssembly };
builder.Services.AddBuildingBlocks(applicationAssemblies);
builder.AddBuildingBlocksHealthChecks();
builder.AddSerilogStructuredLogging();

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

**File**: `server/src/Hosts/AppRuntimeServiceHost/AppRuntime.Service.Host.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>AppRuntime.Service.Host</RootNamespace>
  </PropertyGroup>

  <PropertyGroup Condition="'$(DATARIZEN_ASPIRE_RUN_ID)' != ''">
    <BaseOutputPath>..\..\..\..\artifacts\aspire\$(DATARIZEN_ASPIRE_RUN_ID)\$(MSBuildProjectName)\bin\</BaseOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\BuildingBlocks\Web\BuildingBlocks.Web.csproj" />
    <ProjectReference Include="..\..\product\AppRuntime\AppRuntime.Module\AppRuntime.Module.csproj" />
    <ProjectReference Include="..\..\product\AppRuntime\AppRuntime.Api\AppRuntime.Api.csproj" />
    <ProjectReference Include="..\..\Capabilities\Logging\Serilog\Capabilities.Logging.csproj" />
    <ProjectReference Include="..\..\ServiceDefaults\ServiceDefaults.csproj" />
  </ItemGroup>
</Project>
```

---

## AppHost Configuration (Aspire)

**File**: `server/src/AppHost/Program.cs`

Add AppRuntime service to Microservices topology:

```csharp
case "Microservices":
    // ... existing services ...
    
    // AppRuntime microservice host (project)
    builder.AddProject("appruntime", "../Hosts/AppRuntimeServiceHost/AppRuntime.Service.Host.csproj")
        .WithHttpEndpoint(targetPort: 62012, name: "appruntimeHttp")
        .WithHttpsEndpoint(targetPort: 62011, name: "appruntimeHttps")
        .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
        .WithReference(postgres)
        .WithReference(redis)
        .WithReference(rabbitMq);
    
    builder.AddProject("gateway", "../ApiGateway/ApiGateway.csproj")
        .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId);
    break;
```

---

## API Gateway Configuration

**File**: `server/src/ApiGateway/appsettings.json`

Add AppRuntime routes:

```json
{
  "ReverseProxy": {
    "Routes": {
      "appruntime-route": {
        "ClusterId": "appruntime-cluster",
        "Match": {
          "Path": "/api/runtime/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "appruntime-cluster": {
        "Destinations": {
          "appruntime": {
            "Address": "https://appruntime"
          }
        }
      }
    }
  }
}
```

---

## Implementation Checklist

### Module Layer
- [ ] Create `AppRuntimeModule.cs`
- [ ] Create `DependencyInjection.cs`
- [ ] Create `AppRuntime.Module.csproj`

### Host Integration
- [ ] Update `MonolithHost/Program.cs`
- [ ] Update `MultiAppAppBuilderHost/Program.cs`
- [ ] Update `MultiAppAppBuilderHost/appsettings.json`
- [ ] Create `AppRuntimeServiceHost/Program.cs` (Microservices)
- [ ] Create `AppRuntimeServiceHost/AppRuntime.Service.Host.csproj`

### Aspire & Gateway
- [ ] Update `AppHost/Program.cs` (Microservices topology)
- [ ] Update `ApiGateway/appsettings.json`

### Solution
- [ ] Add `AppRuntime.Module` to solution
- [ ] Add `AppRuntimeServiceHost` to solution (Microservices)
- [ ] Add project references to `AppHost.csproj`

---

## Estimated Time: 4 hours

- Module layer: 1 hour
- Monolith host integration: 0.5 hours
- MultiApp host integration: 1 hour
- Microservices host creation: 1 hour
- Aspire & Gateway configuration: 0.5 hours