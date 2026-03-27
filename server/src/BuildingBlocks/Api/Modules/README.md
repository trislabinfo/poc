# BuildingBlocks.Web.Modules

This folder contains the **module composition contract** used by all Datarizen product modules.

## `IModule`

`IModule` defines a consistent startup model across all topologies (Monolith, Multi-App, Microservices):

- **Identify the module**: `ModuleName`, `SchemaName`
- **Migration ordering**: `GetMigrationDependencies()`
- **Two-phase startup**:
  - `RegisterServices(IServiceCollection, IConfiguration)` for DI registrations (Domain → Application → Infrastructure → API)
  - `ConfigureMiddleware(IApplicationBuilder)` for optional module-specific middleware

Hosts register and configure modules via `BuildingBlocks.Web.Extensions.ModuleExtensions`:

```csharp
builder.Services.AddModule<TenantModule>(builder.Configuration);
app.UseModule<TenantModule>();
```

