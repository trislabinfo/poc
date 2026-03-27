# Datarizen – Initial Implementation Guide

This document captures the first implementation steps and how to use the project scaffolding tools.

## Table of Contents

1. [Module Registration Pattern](#module-registration-pattern)
2. [Module Loading and Startup](#module-loading-and-startup)
3. [Why IModule is in BuildingBlocks.Web](#why-imodule-is-in-buildingblocksweb)
4. [Implementation Plan](#implementation-plan)

---

## Module Registration Pattern

### Overview

Each module follows a **standardized registration pattern** using the `IModule` interface. This ensures:

- ✅ Consistent module initialization across all modules
- ✅ Proper service registration order (Domain → Application → Infrastructure → API)
- ✅ Easy integration with all host types (Monolith, MultiApp, Microservices)
- ✅ Clear separation of concerns

### IModule Interface

Located in `BuildingBlocks.Web`, the `IModule` interface defines the contract for module registration:

```csharp
namespace BuildingBlocks.Web.Modules;

/// <summary>
/// Defines the contract for module registration and configuration.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the unique name of the module (e.g., "Tenant", "Identity").
    /// </summary>
    string ModuleName { get; }
    
    /// <summary>
    /// Gets the database schema name for this module (e.g., "tenant").
    /// Schema names are used for database organization and multi-tenancy.
    /// </summary>
    string SchemaName { get; }
    
    /// <summary>
    /// Gets the list of module names that must be registered before this module.
    /// Used for migration dependency resolution and service registration order.
    /// </summary>
    string[] GetMigrationDependencies();
    
    /// <summary>
    /// Registers all module services with the dependency injection container.
    /// Called during application startup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// Configures the module's middleware pipeline.
    /// Called after all modules are registered but before the app starts.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app);
}
```

### Module Implementation Pattern

Each module implements `IModule` in its `{ModuleName}Module.cs` file:

```csharp
// server/src/Product/Tenant/Tenant.Module/TenantModule.cs
using BuildingBlocks.Web.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Tenant.Module;

public class TenantModule : IModule
{
    public string ModuleName => "Tenant";
    public string SchemaName => "tenant";
    
    public string[] GetMigrationDependencies() => Array.Empty<string>();
    
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register Domain services (if any)
        // services.AddScoped<ITenantDomainService, TenantDomainService>();
        
        // 2. Register Application services (MediatR handlers, validators)
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TenantApplicationAssembly).Assembly));
        
        // 3. Register Infrastructure services (repositories, DbContext)
        // services.AddDbContext<TenantDbContext>(options => ...);
        // services.AddScoped<ITenantRepository, TenantRepository>();
        
        // 4. Register API controllers
        services.AddControllers()
            .AddApplicationPart(typeof(Tenant.Api.Controllers.TenantController).Assembly);
        
        return services;
    }
    
    public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
    {
        // Add module-specific middleware if needed
        // app.UseMiddleware<TenantContextMiddleware>();
        
        return app;
    }
}
```

### Extension Methods

The `AddModule<T>` and `UseModule<T>` extension methods are defined in `BuildingBlocks.Web`:

```csharp
// server/src/BuildingBlocks/Web/Extensions/ModuleExtensions.cs
namespace BuildingBlocks.Web.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddModule<TModule>(
        this IServiceCollection services,
        IConfiguration configuration) 
        where TModule : IModule, new()
    {
        var module = new TModule();
        return module.RegisterServices(services, configuration);
    }
    
    public static IApplicationBuilder UseModule<TModule>(
        this IApplicationBuilder app) 
        where TModule : IModule, new()
    {
        var module = new TModule();
        return module.ConfigureMiddleware(app);
    }
}
```

---

## Module Loading and Startup

### How Module Loading Works

The module loading process follows a **two-phase initialization pattern**:

#### Phase 1: Service Registration (Startup)

During application startup, each module registers its services with the DI container:

```csharp
// MonolithHost/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Modules are registered in dependency order
builder.Services.AddModule<TenantModule>(builder.Configuration);      // No dependencies
builder.Services.AddModule<IdentityModule>(builder.Configuration);    // Depends on Tenant
builder.Services.AddModule<UserModule>(builder.Configuration);        // Depends on Tenant, Identity
builder.Services.AddModule<FeatureModule>(builder.Configuration);     // Depends on Tenant
```

**What happens during `AddModule<T>()`:**

1. Creates a new instance of the module (e.g., `new TenantModule()`)
2. Calls `module.RegisterServices(services, configuration)`
3. Module registers its services in order:
   - Domain services (domain logic, specifications)
   - Application services (MediatR handlers, validators)
   - Infrastructure services (DbContext, repositories, external APIs)
   - API controllers (via `AddApplicationPart()`)

#### Phase 2: Middleware Configuration (After Build)

After the application is built, each module configures its middleware:

```csharp
var app = builder.Build();

// Modules configure middleware in the same order
app.UseModule<TenantModule>();
app.UseModule<IdentityModule>();
app.UseModule<UserModule>();
app.UseModule<FeatureModule>();
```

**What happens during `UseModule<T>()`:**

1. Creates a new instance of the module
2. Calls `module.ConfigureMiddleware(app)`
3. Module adds middleware to the pipeline (e.g., tenant context resolution, authentication)

### Dependency Resolution Between Modules

Modules declare their dependencies via `GetMigrationDependencies()`:

```csharp
// TenantModule.cs
public string[] GetMigrationDependencies() => Array.Empty<string>();

// IdentityModule.cs
public string[] GetMigrationDependencies() => new[] { "Tenant" };

// UserModule.cs
public string[] GetMigrationDependencies() => new[] { "Tenant", "Identity" };

// FeatureModule.cs
public string[] GetMigrationDependencies() => new[] { "Tenant" };
```

**Dependency Resolution Rules:**

1. **Registration Order**: Hosts must register modules in dependency order (dependencies first)
2. **Migration Order**: MigrationRunner uses `GetMigrationDependencies()` to determine execution order
3. **Service Dependencies**: Modules can inject services from their dependencies via DI

**Example Dependency Graph:**

```
Tenant (no dependencies)
  ├── Identity (depends on Tenant)
  │     └── User (depends on Tenant, Identity)
  └── Feature (depends on Tenant)
```

**Correct Registration Order:**

```csharp
// ✅ Correct: Dependencies registered first
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<IdentityModule>(builder.Configuration);
builder.Services.AddModule<UserModule>(builder.Configuration);
builder.Services.AddModule<FeatureModule>(builder.Configuration);

// ❌ Wrong: UserModule registered before IdentityModule
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<UserModule>(builder.Configuration);  // ERROR: Identity not registered yet
builder.Services.AddModule<IdentityModule>(builder.Configuration);
```

### Host-Specific Loading Patterns

#### MonolithHost (All Modules)

Loads all modules unconditionally:

```csharp
// MonolithHost/Program.cs
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<IdentityModule>(builder.Configuration);
builder.Services.AddModule<UserModule>(builder.Configuration);
builder.Services.AddModule<FeatureModule>(builder.Configuration);
```

#### MultiApp Hosts (Conditional Loading)

Loads modules based on configuration:

```csharp
// MultiAppControlPanelHost/Program.cs
var loadedModules = builder.Configuration
    .GetSection("LoadedModules")
    .Get<string[]>() ?? Array.Empty<string>();

foreach (var module in loadedModules)
{
    switch (module)
    {
        case "TenantManagement":
            builder.Services.AddModule<TenantModule>(builder.Configuration);
            break;
        case "Identity":
            builder.Services.AddModule<IdentityModule>(builder.Configuration);
            break;
        // ... other modules
    }
}
```

**Configuration Example:**

```json
// appsettings.json
{
  "LoadedModules": ["TenantManagement", "Identity"]
}
```

**Important**: When using conditional loading, ensure dependencies are included:

```json
// ✅ Correct: Identity depends on Tenant, both are loaded
{
  "LoadedModules": ["TenantManagement", "Identity"]
}

// ❌ Wrong: Identity depends on Tenant, but Tenant is not loaded
{
  "LoadedModules": ["Identity"]
}
```

---

## Why IModule is in BuildingBlocks.Web

The `IModule` interface is defined in `BuildingBlocks.Web` for several architectural reasons:

### 1. Separation of Concerns

`BuildingBlocks.Web` contains **web/API-specific infrastructure** that modules need for HTTP-based hosting:

- `IModule` is fundamentally about **web application composition** - registering services (`IServiceCollection`) and configuring middleware (`IApplicationBuilder`)
- It depends on ASP.NET Core abstractions (`Microsoft.AspNetCore.App`)
- It's about **how modules integrate with web hosts**, not domain logic

### 2. Dependency Direction

```
BuildingBlocks.Kernel  ← Domain/Application primitives (Entity, ValueObject, ICommand, IQuery)
BuildingBlocks.Web     ← Web/hosting primitives (IModule, ModuleExtensions)
```

- `BuildingBlocks.Kernel` is **infrastructure-agnostic** - it has no knowledge of web, databases, or hosting
- `BuildingBlocks.Web` is **web-specific** - it knows about controllers, middleware, HTTP
- Modules reference **both**: Kernel for domain patterns, Web for hosting integration

### 3. Avoids Circular Dependencies

If `IModule` were in `BuildingBlocks.Kernel`:

- ❌ Kernel would need to reference ASP.NET Core (`IServiceCollection`, `IApplicationBuilder`)
- ❌ Kernel would become web-specific, breaking its infrastructure-agnostic design
- ❌ Non-web scenarios (CLI tools, background workers) would carry unnecessary web dependencies

### 4. Clear Responsibility

```
BuildingBlocks.Kernel         → "What is a domain entity? What is a command?"
BuildingBlocks.Web            → "How do modules register with a web host?"
BuildingBlocks.Infrastructure → "How do we implement caching, messaging, persistence?"
```

Each building block has a **single, clear responsibility**. `IModule` is about web application composition, so it belongs in the web layer.

### 5. Follows ASP.NET Core Patterns

ASP.NET Core itself separates concerns this way:

- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI primitives
- `Microsoft.AspNetCore.Http.Abstractions` - HTTP primitives  
- `Microsoft.AspNetCore.Mvc` - MVC/API primitives

Similarly, we separate:
- **Kernel** - Domain/application primitives
- **Web** - Web hosting primitives (including `IModule`)

### 6. Enables Polymorphic Module Registration

By centralizing `IModule` in `BuildingBlocks.Web`, all modules implement the **same contract**, and hosts can use **generic extension methods** to register any module uniformly:

```csharp
// ✅ Possible because IModule is in a shared location
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<IdentityModule>(builder.Configuration);

// ❌ Not possible if each module defines its own interface
builder.Services.AddTenantModule();  // Different signature
builder.Services.AddIdentityModule(config);  // Different signature
```

---

## Implementation Plan

### Phase 1: Create IModule Infrastructure (30 minutes)

**Objective**: Create the `IModule` interface and extension methods in `BuildingBlocks.Web`.

#### Tasks:

1. **Create BuildingBlocks.Web Project (if not exists)**
   - [x] Create `server/src/BuildingBlocks/Web/` directory
   - [x] Create `BuildingBlocks.Web.csproj`:
     ```xml
     <Project Sdk="Microsoft.NET.Sdk">
       <PropertyGroup>
         <TargetFramework>net10.0</TargetFramework>
         <RootNamespace>BuildingBlocks.Web</RootNamespace>
         <AssemblyName>Datarizen.BuildingBlocks.Web</AssemblyName>
         <ImplicitUsings>enable</ImplicitUsings>
         <Nullable>enable</Nullable>
       </PropertyGroup>
       <ItemGroup>
         <FrameworkReference Include="Microsoft.AspNetCore.App" />
       </ItemGroup>
     </Project>
     ```
   - [x] Add to solution: `dotnet sln server/Datarizen.sln add server/src/BuildingBlocks/Web/BuildingBlocks.Web.csproj`

2. **Create IModule Interface**
   - [x] Create `server/src/BuildingBlocks/Web/Modules/` directory
   - [x] Create `IModule.cs` with interface definition (see above)
   - [x] Add XML documentation comments

3. **Create ModuleExtensions**
   - [x] Create `server/src/BuildingBlocks/Web/Extensions/` directory
   - [x] Create `ModuleExtensions.cs` with `AddModule<T>()` and `UseModule<T>()` methods
   - [x] Add XML documentation comments

4. **Build and Verify**
   - [x] Run: `dotnet build server/src/BuildingBlocks/Web`
   - [x] Verify no compilation errors

5. **Create Documentation**
   - [x] Create `server/src/BuildingBlocks/Web/Modules/README.md`
   - [x] Document `IModule` interface purpose and usage
   - [x] Add examples of module implementation
   - [x] Document extension method usage
   - [x] Explain why `IModule` is in `BuildingBlocks.Web`

**Deliverable**: Working `IModule` infrastructure in `BuildingBlocks.Web`.

---

### Phase 2: Implement IModule in Existing Modules (45 minutes)

**Objective**: Refactor all 4 existing modules to implement `IModule` interface.

#### Tasks for Each Module (Tenant, Identity, User, Feature):

1. **Update Module Project Reference**
   - [x] Add reference to `BuildingBlocks.Web` in `{ModuleName}.Module.csproj`:
     ```xml
     <ItemGroup>
       <ProjectReference Include="..\..\..\BuildingBlocks\Web\BuildingBlocks.Web.csproj" />
       <ProjectReference Include="..\..\{ModuleName}.Api\{ModuleName}.Api.csproj" />
     </ItemGroup>
     ```

2. **Implement IModule in {ModuleName}Module.cs**
   - [x] Create or update `server/src/Product/{ModuleName}/{ModuleName}.Module/{ModuleName}Module.cs`
   - [x] Add `using BuildingBlocks.Web.Modules;`
   - [x] Implement `IModule` interface:
     ```csharp
     public class {ModuleName}Module : IModule
     {
         public string ModuleName => "{ModuleName}";
         public string SchemaName => "{modulename}";  // lowercase, no "_schema" suffix
         
         public string[] GetMigrationDependencies()
         {
             // Tenant: Array.Empty<string>()
             // Identity: new[] { "Tenant" }
             // User: new[] { "Tenant", "Identity" }
             // Feature: new[] { "Tenant" }
             return Array.Empty<string>();
         }
         
         public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
         {
             // TODO: Register Domain services
             
             // TODO: Register Application services (MediatR)
             
             // TODO: Register Infrastructure services (DbContext, Repositories)
             
             // Register API controllers
             services.AddControllers()
                 .AddApplicationPart(typeof({ModuleName}.Api.Controllers.{ModuleName}Controller).Assembly);
             
             return services;
         }
         
         public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
         {
             // TODO: Add module-specific middleware if needed
             return app;
         }
     }
     ```

3. **Remove Old Extension Methods**
   - [x] Delete `ModuleServiceCollectionExtensions.cs` (if exists)
   - [x] Delete any `Add{ModuleName}Module()` methods
   - [x] Delete any `Use{ModuleName}Module()` methods

4. **Verify Module Builds**
   - [x] Run: `dotnet build server/src/Product/{ModuleName}/{ModuleName}.Module`
   - [x] Fix any compilation errors

**Modules to Update:**
- [x] Tenant module (`SchemaName => "tenant"`)
- [x] Identity module (`SchemaName => "identity"`, dependencies: `["Tenant"]`)
- [x] User module (`SchemaName => "user"`, dependencies: `["Tenant", "Identity"]`)
- [x] Feature module (`SchemaName => "feature"`, dependencies: `["Tenant"]`)

**Deliverable**: All 4 modules implement `IModule` interface.

---

### Phase 3: Update Host Projects (30 minutes)

**Objective**: Update all host projects to use the new `IModule` pattern.

#### Tasks:

1. **Update MonolithHost**
   - [x] Add reference to `BuildingBlocks.Web` in `MonolithHost.csproj`
   - [x] Open `server/src/Hosts/MonolithHost/Program.cs`
   - [x] Add `using BuildingBlocks.Web.Extensions;`
   - [x] Replace old module registration:
     ```csharp
     // OLD:
     builder.Services.AddTenantModule();
     builder.Services.AddIdentityModule();
     builder.Services.AddUserModule();
     builder.Services.AddFeatureModule();
     
     // NEW:
     builder.Services.AddModule<TenantModule>(builder.Configuration);
     builder.Services.AddModule<IdentityModule>(builder.Configuration);
     builder.Services.AddModule<UserModule>(builder.Configuration);
     builder.Services.AddModule<FeatureModule>(builder.Configuration);
     ```
   - [x] Add module middleware configuration (after `var app = builder.Build();`):
     ```csharp
     app.UseModule<TenantModule>();
     app.UseModule<IdentityModule>();
     app.UseModule<UserModule>();
     app.UseModule<FeatureModule>();
     ```
   - [x] Build: `dotnet build server/src/Hosts/MonolithHost`

2. **Update MultiAppControlPanelHost**
   - [x] Add reference to `BuildingBlocks.Web`
   - [x] Open `server/src/Hosts/MultiAppControlPanelHost/Program.cs`
   - [x] Add `using BuildingBlocks.Web.Extensions;`
   - [x] Replace module registration in switch statement:
     ```csharp
     case "TenantManagement":
         builder.Services.AddModule<TenantModule>(builder.Configuration);
         break;
     case "Identity":
         builder.Services.AddModule<IdentityModule>(builder.Configuration);
         break;
     ```
   - [x] Add module middleware configuration after `var app = builder.Build();`:
     ```csharp
     foreach (var module in loadedModules)
     {
         switch (module)
         {
             case "TenantManagement":
                 app.UseModule<TenantModule>();
                 break;
             case "Identity":
                 app.UseModule<IdentityModule>();
                 break;
         }
     }
     ```
   - [x] Build: `dotnet build server/src/Hosts/MultiAppControlPanelHost`

3. **Update MultiAppRuntimeHost**
   - [x] Add reference to `BuildingBlocks.Web`
   - [x] Open `server/src/Hosts/MultiAppRuntimeHost/Program.cs`
   - [x] Apply same changes as MultiAppControlPanelHost
   - [x] Update switch statement for UserManagement module
   - [x] Build: `dotnet build server/src/Hosts/MultiAppRuntimeHost`

4. **Update MultiAppAppBuilderHost**
   - [x] Add reference to `BuildingBlocks.Web`
   - [x] Open `server/src/Hosts/MultiAppAppBuilderHost/Program.cs`
   - [x] Apply same changes as MultiAppControlPanelHost
   - [x] Update switch statement for FeatureManagement module
   - [x] Build: `dotnet build server/src/Hosts/MultiAppAppBuilderHost`

5. **Verify All Hosts Build**
   - [x] Run: `dotnet build server/Datarizen.sln`
   - [x] Fix any compilation errors

**Deliverable**: All 4 hosts use the new `IModule` pattern.

---

### Phase 4: Create Stub API Controllers (60 minutes)

**Objective**: Create working API controllers that return fake data or HTTP 501 (Not Implemented).

#### Tasks for Each Module:

1. **Tenant Module - TenantController**
   - [x] Open `server/src/Product/Tenant/Tenant.Api/Controllers/TenantController.cs`
   - [x] Implement endpoints:
     ```csharp
     [ApiController]
     [Route("api/[controller]")]
     public class TenantController : ControllerBase
     {
         [HttpGet]
         [ProducesResponseType(StatusCodes.Status200OK)]
         public IActionResult GetAll()
         {
             return Ok(new[]
             {
                 new { Id = Guid.NewGuid(), Name = "Acme Corp", Slug = "acme", IsActive = true },
                 new { Id = Guid.NewGuid(), Name = "TechStart Inc", Slug = "techstart", IsActive = true }
             });
         }
         
         [HttpGet("{id:guid}")]
         [ProducesResponseType(StatusCodes.Status200OK)]
         public IActionResult GetById(Guid id)
         {
             return Ok(new { Id = id, Name = "Acme Corp", Slug = "acme", IsActive = true });
         }
         
         [HttpPost]
         [ProducesResponseType(StatusCodes.Status201Created)]
         public IActionResult Create([FromBody] object request)
         {
             var newId = Guid.NewGuid();
             return CreatedAtAction(nameof(GetById), new { id = newId }, 
                 new { Id = newId, Name = "New Tenant", Slug = "new-tenant", IsActive = true });
         }
         
         [HttpPut("{id:guid}")]
         [ProducesResponseType(StatusCodes.Status501NotImplemented)]
         public IActionResult Update(Guid id, [FromBody] object request)
         {
             return StatusCode(501, new { Message = "Update not implemented yet" });
         }
         
         [HttpDelete("{id:guid}")]
         [ProducesResponseType(StatusCodes.Status501NotImplemented)]
         public IActionResult Delete(Guid id)
         {
             return StatusCode(501, new { Message = "Delete not implemented yet" });
         }
     }
     ```
   - [x] Add XML documentation comments

2. **Identity Module - IdentityController**
   - [x] Open `server/src/Product/Identity/Identity.Api/Controllers/IdentityController.cs`
   - [x] Implement endpoints:
     ```csharp
     [ApiController]
     [Route("api/[controller]")]
     public class IdentityController : ControllerBase
     {
         [HttpPost("login")]
         [ProducesResponseType(StatusCodes.Status200OK)]
         public IActionResult Login([FromBody] object credentials)
         {
             return Ok(new
             {
                 Token = "fake-jwt-token-12345",
                 ExpiresIn = 3600,
                 User = new { Id = Guid.NewGuid(), Email = "user@example.com" }
             });
         }
         
         [HttpPost("register")]
         [ProducesResponseType(StatusCodes.Status501NotImplemented)]
         public IActionResult Register([FromBody] object request)
         {
             return StatusCode(501, new { Message = "Registration not implemented yet" });
         }
         
         [HttpPost("refresh")]
         [ProducesResponseType(StatusCodes.Status501NotImplemented)]
         public IActionResult RefreshToken([FromBody] object request)
         {
             return StatusCode(501, new { Message = "Token refresh not implemented yet" });
         }
     }
     ```
   - [x] Add XML documentation comments

3. **User Module - UserController**
   - [x] Open `server/src/Product/User/User.Api/Controllers/UserController.cs`
   - [x] Implement endpoints:
     ```csharp
     [ApiController]
     [Route("api/[controller]")]
     public class UserController : ControllerBase
     {
         [HttpGet]
         [ProducesResponseType(StatusCodes.Status200OK)]
         public IActionResult GetAll()
         {
             return Ok(new[]
             {
                 new { Id = Guid.NewGuid(), Email = "john@example.com", FirstName = "John", LastName = "Doe" },
                 new { Id = Guid.NewGuid(), Email = "jane@example.com", FirstName = "Jane", LastName = "Smith" }
             });
         }
         
         [HttpGet("{id:guid}")]
         [ProducesResponseType(StatusCodes.Status200OK)]
         public IActionResult GetById(Guid id)
         {
             return Ok(new { Id = id, Email = "john@example.com", FirstName = "John", LastName = "Doe" });
         }
         
         [HttpPost]
         [ProducesResponseType(StatusCodes.Status501NotImplemented)]
         public IActionResult Create([FromBody] object request)
         {
             return StatusCode(501, new { Message = "User creation not implemented yet" });
         }
     }
     ```
   - [x] Add XML documentation comments

4. **Feature Module - FeatureController**
   - [x] Open `server/src/Product/Feature/Feature.Api/Controllers/FeatureController.cs`
   - [x] Implement endpoints:
     ```csharp
     [ApiController]
     [Route("api/[controller]")]
     public class FeatureController : ControllerBase
     {
         [HttpGet]
         [ProducesResponseType(StatusCodes.Status200OK)]
         public IActionResult GetAll()
         {
             return Ok(new[]
             {
                 new { Id = Guid.NewGuid(), Name = "DarkMode", IsEnabled = true, Description = "Dark mode UI" },
                 new { Id = Guid.NewGuid(), Name = "BetaFeatures", IsEnabled = false, Description = "Beta features access" }
             });
         }
         
         [HttpGet("{id:guid}")]
         [ProducesResponseType(StatusCodes.Status200OK)]
         public IActionResult GetById(Guid id)
         {
             return Ok(new { Id = id, Name = "DarkMode", IsEnabled = true, Description = "Dark mode UI" });
         }
         
         [HttpPost("{id:guid}/toggle")]
         [ProducesResponseType(StatusCodes.Status501NotImplemented)]
         public IActionResult ToggleFeature(Guid id)
         {
             return StatusCode(501, new { Message = "Feature toggle not implemented yet" });
         }
     }
     ```
   - [x] Add XML documentation comments

5. **Build All API Projects**
   - [x] Run: `dotnet build server/src/Product/Tenant/Tenant.Api`
   - [x] Run: `dotnet build server/src/Product/Identity/Identity.Api`
   - [x] Run: `dotnet build server/src/Product/User/User.Api`
   - [x] Run: `dotnet build server/src/Product/Feature/Feature.Api`

**Deliverable**: All 4 modules have working stub controllers.

---

### Phase 5: Update Module Generation Script (30 minutes)

**Objective**: Update `generate-module.sh` to generate modules with `IModule` implementation.

#### Tasks:

1. **Update Script to Generate IModule Implementation**
   - [x] Open `scripts/development/generate-module.sh`
   - [x] Update `{ModuleName}.Module` project generation to include:
     - [x] Reference to `BuildingBlocks.Web`
     - [x] `{ModuleName}Module.cs` file implementing `IModule`
   - [x] Update template for `{ModuleName}Module.cs`:
     ```bash
     cat > "${MODULE_DIR}/${MODULE_NAME}.Module/${MODULE_NAME}Module.cs" <<EOF
     using BuildingBlocks.Web.Modules;
     using Microsoft.AspNetCore.Builder;
     using Microsoft.Extensions.Configuration;
     using Microsoft.Extensions.DependencyInjection;

     namespace ${MODULE_NAME}.Module;

     public class ${MODULE_NAME}Module : IModule
     {
         public string ModuleName => "${MODULE_NAME}";
         public string SchemaName => "${MODULE_NAME_LOWER}";
         
         public string[] GetMigrationDependencies()
         {
             // TODO: Add module dependencies (e.g., new[] { "Tenant" })
             return Array.Empty<string>();
         }
         
         public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
         {
             // TODO: Register Domain services
             
             // TODO: Register Application services (MediatR)
             
             // TODO: Register Infrastructure services (DbContext, Repositories)
             
             // Register API controllers
             services.AddControllers()
                 .AddApplicationPart(typeof(${MODULE_NAME}.Api.Controllers.${MODULE_NAME}Controller).Assembly);
             
             return services;
         }
         
         public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
         {
             // TODO: Add module-specific middleware if needed
             return app;
         }
     }
     EOF
     ```

2. **Update API Controller Template**
   - [x] Update `{ModuleName}Controller.cs` template to include:
     - [x] `[ApiController]` attribute
     - [x] Route prefix `api/{module}` (lowercase)
     - [x] Stub GET endpoint returning fake data
     - [x] Stub POST endpoint returning 501
     - [x] XML documentation comments

3. **Update Module.csproj Template**
   - [x] Add `BuildingBlocks.Web` project reference:
     ```xml
     <ItemGroup>
       <ProjectReference Include="..\..\..\BuildingBlocks\Web\BuildingBlocks.Web.csproj" />
       <ProjectReference Include="..\..\${MODULE_NAME}.Api\${MODULE_NAME}.Api.csproj" />
     </ItemGroup>
     ```

4. **Update Script Documentation**
   - [x] Update script header comments to mention `IModule` generation
   - [x] Add example of generated `{ModuleName}Module.cs`
   - [x] Document that modules are generated with stub controllers

5. **Test Script**
   - [ ] Generate a test module: `./scripts/development/generate-module.sh TestModule`
   - [ ] Verify `TestModule.Module/TestModuleModule.cs` implements `IModule`
   - [ ] Verify `TestModule.Api/Controllers/TestModuleController.cs` has stub endpoints
   - [ ] Build test module: `dotnet build server/src/Product/TestModule/TestModule.Module`
   - [ ] Delete test module after verification

**Deliverable**: Updated `generate-module.sh` script that generates modules with `IModule` implementation.

---

### Phase 6: Manual Testing (To be performed by team)

**Objective**: Verify the application runs correctly in both Monolith and MultiApp topologies.

#### Monolith Topology Testing:

**Prerequisites:**
- [ ] Ensure all phases 1-5 are complete
- [ ] Ensure Docker is running (for PostgreSQL, Redis, RabbitMQ)

**Test Steps:**

1. **Start Monolith via Aspire**
   - [ ] Run: `dotnet run --project server/src/AppHost`
   - [ ] Verify Aspire dashboard opens in browser
   - [ ] Verify "monolith" service shows green status
   - [ ] Verify PostgreSQL, Redis, RabbitMQ show green status

2. **Access Swagger UI**
   - [ ] Click on "monolith" service in Aspire dashboard
   - [ ] Click on the HTTPS endpoint (e.g., `https://localhost:56796`)
   - [ ] Navigate to `/swagger` endpoint
   - [ ] Verify Swagger UI loads successfully

3. **Test Tenant Endpoints**
   - [ ] Execute `GET /api/tenant` → Expect 200 OK with fake tenant list
   - [ ] Execute `GET /api/tenant/{id}` → Expect 200 OK with fake tenant
   - [ ] Execute `POST /api/tenant` → Expect 201 Created
   - [ ] Execute `PUT /api/tenant/{id}` → Expect 501 Not Implemented
   - [ ] Execute `DELETE /api/tenant/{id}` → Expect 501 Not Implemented

4. **Test Identity Endpoints**
   - [ ] Execute `POST /api/identity/login` → Expect 200 OK with fake JWT
   - [ ] Execute `POST /api/identity/register` → Expect 501 Not Implemented
   - [ ] Execute `POST /api/identity/refresh` → Expect 501 Not Implemented

5. **Test User Endpoints**
   - [ ] Execute `GET /api/user` → Expect 200 OK with fake user list
   - [ ] Execute `GET /api/user/{id}` → Expect 200 OK with fake user
   - [ ] Execute `POST /api/user` → Expect 501 Not Implemented

6. **Test Feature Endpoints**
   - [ ] Execute `GET /api/feature` → Expect 200 OK with fake feature flags
   - [ ] Execute `GET /api/feature/{id}` → Expect 200 OK with fake feature
   - [ ] Execute `POST /api/feature/{id}/toggle` → Expect 501 Not Implemented

7. **Verify Logs**
   - [ ] Check Aspire dashboard logs for errors
   - [ ] Verify no exceptions during startup
   - [ ] Verify all modules registered successfully

#### MultiApp Topology Testing:

**Prerequisites:**
- [ ] Monolith testing complete and successful

**Test Steps:**

1. **Switch to MultiApp Topology**
   - [ ] Stop Aspire (Ctrl+C)
   - [ ] Update `server/src/AppHost/appsettings.json`:
     ```json
     {
       "Deployment": {
         "Topology": "DistributedApp"
       }
     }
     ```
   - [ ] Run: `dotnet run --project server/src/AppHost`

2. **Verify All Hosts Start**
   - [ ] Verify "controlpanel" service shows green status
   - [ ] Verify "runtime" service shows green status
   - [ ] Verify "appbuilder" service shows green status
   - [ ] Verify "gateway" service shows green status (if configured)

3. **Test ControlPanel Host (Tenant + Identity)**
   - [ ] Click on "controlpanel" service
   - [ ] Navigate to `/swagger`
   - [ ] Verify only Tenant and Identity endpoints appear
   - [ ] Test `GET /api/tenant` → Expect 200 OK
   - [ ] Test `POST /api/identity/login` → Expect 200 OK

4. **Test Runtime Host (User)**
   - [ ] Click on "runtime" service
   - [ ] Navigate to `/swagger`
   - [ ] Verify only User endpoints appear
   - [ ] Test `GET /api/user` → Expect 200 OK

5. **Test AppBuilder Host (Feature)**
   - [ ] Click on "appbuilder" service
   - [ ] Navigate to `/swagger`
   - [ ] Verify only Feature endpoints appear
   - [ ] Test `GET /api/feature` → Expect 200 OK

6. **Verify Module Isolation**
   - [ ] Verify ControlPanel does NOT have `/api/user` or `/api/feature` endpoints
   - [ ] Verify Runtime does NOT have `/api/tenant` or `/api/identity` endpoints
   - [ ] Verify AppBuilder does NOT have `/api/tenant` or `/api/user` endpoints

7. **Document Results**
   - [ ] Create `docs/testing/manual-test-results.md`
   - [ ] Document test results for Monolith topology
   - [ ] Document test results for MultiApp topology
   - [ ] Add screenshots (optional)
   - [ ] Note any issues or unexpected behavior

**Deliverable**: Verified working application in both Monolith and MultiApp topologies.

---

### Phase 7: Update Documentation (15 minutes)

**Objective**: Document the new module registration pattern and API endpoints.

#### Tasks:

1. **Update Module Documentation**
   - [ ] Update `docs/ai-context/05-MODULES.md`
   - [ ] Add section on `IModule` interface
   - [ ] Add examples of module implementation
   - [ ] Update module registration examples in hosts
   - [ ] Document module dependency resolution
   - [ ] Explain why `IModule` is in `BuildingBlocks.Web`

2. **Update API Documentation**
   - [ ] Create or update `docs/architecture/api-layer.md`
   - [ ] Document stub controller pattern
   - [ ] Add examples of fake data responses
   - [ ] Document HTTP 501 usage for unimplemented endpoints
   - [ ] Add Swagger UI screenshots

3. **Create Quick Start Guide**
   - [ ] Create `docs/getting-started/quick-start.md`
   - [ ] Document how to run the application (Monolith)
   - [ ] Document how to run MultiApp topology
   - [ ] Document how to access Swagger UI
   - [ ] Document how to test API endpoints
   - [ ] Add troubleshooting section

4. **Update README**
   - [ ] Update `README.md` in repository root
   - [ ] Add "Running the Application" section
   - [ ] Add link to Quick Start Guide
   - [ ] Add link to API documentation
   - [ ] Add link to module documentation

**Deliverable**: Complete documentation covering module registration, startup process, and API endpoints.

---

## Success Criteria

At the end of this implementation plan, you should have:

- ✅ `IModule` interface defined in `BuildingBlocks.Web`
- ✅ All 4 modules implement `IModule` interface with correct schema names (no "_schema" suffix)
- ✅ All 4 hosts use `AddModule<T>()` and `UseModule<T>()` pattern
- ✅ All 4 modules have working stub controllers
- ✅ Module generation script updated to generate `IModule` implementations
- ✅ Application runs successfully via Aspire in Monolith topology
- ✅ Application runs successfully via Aspire in MultiApp topology
- ✅ Swagger UI shows correct endpoints for each host
- ✅ GET endpoints return fake data (200 OK)
- ✅ Unimplemented endpoints return 501 Not Implemented
- ✅ Documentation updated with new patterns and startup process
- ✅ Manual testing completed and documented

## Next Steps After Completion

1. **Implement Domain Models**
   - Create real entities in `{ModuleName}.Domain`
   - Add value objects and domain events
   - Implement domain logic and invariants

2. **Implement Application Layer**
   - Create MediatR command/query handlers
   - Add DTOs and mappers
   - Implement FluentValidation validators
   - Add application services

3. **Implement Infrastructure Layer**
   - Create DbContext and entity configurations
   - Add database migrations using FluentMigrator
   - Implement repositories
   - Add external service integrations

4. **Replace Stub Controllers**
   - Update controllers to use MediatR
   - Remove fake data responses
   - Add proper error handling
   - Implement request/response DTOs

5. **Add Authentication/Authorization**
   - Implement JWT authentication in Identity module
   - Add role-based authorization
   - Secure API endpoints
   - Implement tenant context resolution

6. **Write Tests**
   - Unit tests for domain logic
   - Unit tests for application handlers
   - Integration tests for API endpoints
   - Integration tests for database operations
   - End-to-end tests for critical flows

7. **Implement Migration Runner**
   - Update MigrationRunner to use `IModule.GetMigrationDependencies()`
   - Test migration execution order
   - Add migration rollback support

