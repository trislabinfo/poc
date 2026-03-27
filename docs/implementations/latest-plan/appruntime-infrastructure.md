# AppRuntime Module - Infrastructure Layer

**Status**: ✅ Updated to align with new domain model  
**Last Updated**: 2026-02-11  
**Module**: AppRuntime  
**Layer**: Infrastructure  

---

## Overview

Persistence, engine implementations, and external integrations for AppRuntime.

**Key Changes from Domain Update**:
- ✅ Updated repository to query by Major/Minor/Patch
- ✅ Added GetActiveReleaseAsync method
- ✅ Removed Version string queries
- ✅ Updated entity configurations

---

## Repository Implementation

### IApplicationReleaseRepository

```csharp
namespace Datarizen.AppRuntime.Infrastructure.Persistence.Repositories;

public sealed class ApplicationReleaseRepository 
    : Repository<ApplicationRelease, Guid>, IApplicationReleaseRepository
{
    public ApplicationReleaseRepository(AppRuntimeDbContext context) 
        : base(context)
    {
    }

    public async Task<ApplicationRelease?> GetByVersionAsync(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ApplicationRelease>()
            .FirstOrDefaultAsync(r => 
                r.ApplicationId == applicationId &&
                r.Major == major &&
                r.Minor == minor &&
                r.Patch == patch,
                cancellationToken);
    }

    public async Task<ApplicationRelease?> GetActiveReleaseAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ApplicationRelease>()
            .Where(r => r.ApplicationId == applicationId && r.IsActive)
            .OrderByDescending(r => r.Major)
            .ThenByDescending(r => r.Minor)
            .ThenByDescending(r => r.Patch)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ApplicationRelease>> GetByApplicationIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ApplicationRelease>()
            .Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.Major)
            .ThenByDescending(r => r.Minor)
            .ThenByDescending(r => r.Patch)
            .ToListAsync(cancellationToken);
    }
}
```

---

## Engine Factories

### INavigationEngineFactory

```csharp
namespace Datarizen.AppRuntime.Infrastructure.Engines.Navigation;

public sealed class NavigationEngineFactory : INavigationEngineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _engines = new();

    public NavigationEngineFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterEngines();
    }

    private void RegisterEngines()
    {
        _engines["v1"] = typeof(NavigationEngineV1);
        _engines["v2"] = typeof(NavigationEngineV2);
        // Add more versions as needed
    }

    public Result<INavigationEngine> GetEngine(string version)
    {
        if (!_engines.TryGetValue(version, out var engineType))
        {
            return Result<INavigationEngine>.Failure(Error.NotFound(
                "AppRuntime.NavigationEngine.NotFound",
                $"Navigation engine version '{version}' not found"));
        }

        var engine = (INavigationEngine)_serviceProvider.GetRequiredService(engineType);
        return Result<INavigationEngine>.Success(engine);
    }

    public List<string> GetAvailableVersions()
    {
        return _engines.Keys.OrderBy(v => v).ToList();
    }
}
```

### IPageEngineFactory

```csharp
namespace Datarizen.AppRuntime.Infrastructure.Engines.Page;

public sealed class PageEngineFactory : IPageEngineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _engines = new();

    public PageEngineFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterEngines();
    }

    private void RegisterEngines()
    {
        _engines["v1"] = typeof(PageEngineV1);
        _engines["v2"] = typeof(PageEngineV2);
        // Add more versions as needed
    }

    public Result<IPageEngine> GetEngine(string version)
    {
        if (!_engines.TryGetValue(version, out var engineType))
        {
            return Result<IPageEngine>.Failure(Error.NotFound(
                "AppRuntime.PageEngine.NotFound",
                $"Page engine version '{version}' not found"));
        }

        var engine = (IPageEngine)_serviceProvider.GetRequiredService(engineType);
        return Result<IPageEngine>.Success(engine);
    }

    public List<string> GetAvailableVersions()
    {
        return _engines.Keys.OrderBy(v => v).ToList();
    }
}
```

### IDataSourceEngineFactory

```csharp
namespace Datarizen.AppRuntime.Infrastructure.Engines.DataSource;

public sealed class DataSourceEngineFactory : IDataSourceEngineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _engines = new();

    public DataSourceEngineFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterEngines();
    }

    private void RegisterEngines()
    {
        _engines["v1"] = typeof(DataSourceEngineV1);
        _engines["v2"] = typeof(DataSourceEngineV2);
        // Add more versions as needed
    }

    public Result<IDataSourceEngine> GetEngine(string version)
    {
        if (!_engines.TryGetValue(version, out var engineType))
        {
            return Result<IDataSourceEngine>.Failure(Error.NotFound(
                "AppRuntime.DataSourceEngine.NotFound",
                $"Data source engine version '{version}' not found"));
        }

        var engine = (IDataSourceEngine)_serviceProvider.GetRequiredService(engineType);
        return Result<IDataSourceEngine>.Success(engine);
    }

    public List<string> GetAvailableVersions()
    {
        return _engines.Keys.OrderBy(v => v).ToList();
    }
}
```

---

## Engine Implementations

### NavigationEngineV1

```csharp
namespace Datarizen.AppRuntime.Infrastructure.Engines.Navigation;

public sealed class NavigationEngineV1 : INavigationEngine
{
    public string Version => "v1";

    public async Task<Result<NavigationExecutionResult>> ExecuteAsync(
        string navigationJson,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse navigation JSON
            var navigation = JsonSerializer.Deserialize<NavigationV1Model>(navigationJson);
            if (navigation is null)
            {
                return Result<NavigationExecutionResult>.Failure(Error.Failure(
                    "AppRuntime.Navigation.InvalidJson",
                    "Failed to parse navigation JSON"));
            }

            // Transform to execution result
            var result = new NavigationExecutionResult
            {
                Items = navigation.Items.Select(MapToNavigationItem).ToList(),
                EngineVersion = Version,
                Metadata = new Dictionary<string, object>
                {
                    ["executedAt"] = DateTime.UtcNow,
                    ["context"] = context
                }
            };

            return Result<NavigationExecutionResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<NavigationExecutionResult>.Failure(Error.Failure(
                "AppRuntime.Navigation.ExecutionFailed",
                $"Navigation execution failed: {ex.Message}"));
        }
    }

    private static NavigationItem MapToNavigationItem(NavigationItemV1Model model)
    {
        return new NavigationItem
        {
            Label = model.Label,
            Route = model.Route,
            Icon = model.Icon,
            Children = model.Children?.Select(MapToNavigationItem).ToList() ?? new()
        };
    }
}

internal sealed record NavigationV1Model
{
    public List<NavigationItemV1Model> Items { get; init; } = new();
}

internal sealed record NavigationItemV1Model
{
    public string Label { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public List<NavigationItemV1Model>? Children { get; init; }
}
```

### PageEngineV1

```csharp
namespace Datarizen.AppRuntime.Infrastructure.Engines.Page;

public sealed class PageEngineV1 : IPageEngine
{
    public string Version => "v1";

    public async Task<Result<PageExecutionResult>> ExecuteAsync(
        string pageJson,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse page JSON
            var page = JsonSerializer.Deserialize<PageV1Model>(pageJson);
            if (page is null)
            {
                return Result<PageExecutionResult>.Failure(Error.Failure(
                    "AppRuntime.Page.InvalidJson",
                    "Failed to parse page JSON"));
            }

            // Transform to execution result
            var result = new PageExecutionResult
            {
                PageName = page.Name,
                Route = page.Route,
                Layout = page.Layout,
                EngineVersion = Version,
                Metadata = new Dictionary<string, object>
                {
                    ["executedAt"] = DateTime.UtcNow,
                    ["context"] = context
                }
            };

            return Result<PageExecutionResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PageExecutionResult>.Failure(Error.Failure(
                "AppRuntime.Page.ExecutionFailed",
                $"Page execution failed: {ex.Message}"));
        }
    }
}

internal sealed record PageV1Model
{
    public string Name { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public object Layout { get; init; } = new();
}
```

### DataSourceEngineV1

```csharp
namespace Datarizen.AppRuntime.Infrastructure.Engines.DataSource;

public sealed class DataSourceEngineV1 : IDataSourceEngine
{
    public string Version => "v1";

    public async Task<Result<DataSourceExecutionResult>> ExecuteAsync(
        string dataSourceJson,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse data source JSON
            var dataSource = JsonSerializer.Deserialize<DataSourceV1Model>(dataSourceJson);
            if (dataSource is null)
            {
                return Result<DataSourceExecutionResult>.Failure(Error.Failure(
                    "AppRuntime.DataSource.InvalidJson",
                    "Failed to parse data source JSON"));
            }

            // Execute query (simplified example)
            var data = await ExecuteQueryAsync(dataSource, parameters, cancellationToken);

            // Transform to execution result
            var result = new DataSourceExecutionResult
            {
                DataSourceName = dataSource.Name,
                Data = data,
                TotalCount = data is ICollection collection ? collection.Count : 0,
                EngineVersion = Version,
                Metadata = new Dictionary<string, object>
                {
                    ["executedAt"] = DateTime.UtcNow,
                    ["parameters"] = parameters
                }
            };

            return Result<DataSourceExecutionResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<DataSourceExecutionResult>.Failure(Error.Failure(
                "AppRuntime.DataSource.ExecutionFailed",
                $"Data source execution failed: {ex.Message}"));
        }
    }

    private static async Task<object> ExecuteQueryAsync(
        DataSourceV1Model dataSource,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // Simplified implementation
        // In real scenario, this would execute against actual data sources
        await Task.CompletedTask;
        return new List<object>();
    }
}

internal sealed record DataSourceV1Model
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public object Configuration { get; init; } = new();
}
```

---

## Service Registration

**File**: `AppRuntime.Infrastructure/DependencyInjection.cs`

```csharp
namespace Datarizen.AppRuntime.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppRuntimeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<IApplicationReleaseRepository, ApplicationReleaseRepository>();

        // Engine Factories
        services.AddSingleton<INavigationEngineFactory, NavigationEngineFactory>();
        services.AddSingleton<IPageEngineFactory, PageEngineFactory>();
        services.AddSingleton<IDataSourceEngineFactory, DataSourceEngineFactory>();

        // Engine Implementations
        services.AddTransient<NavigationEngineV1>();
        services.AddTransient<NavigationEngineV2>();
        services.AddTransient<PageEngineV1>();
        services.AddTransient<PageEngineV2>();
        services.AddTransient<DataSourceEngineV1>();
        services.AddTransient<DataSourceEngineV2>();

        return services;
    }
}
```

---

## Success Criteria

- ✅ Repository queries by Major/Minor/Patch
- ✅ GetActiveReleaseAsync implemented
- ✅ Engine factories return Result<T>
- ✅ Engine implementations handle JSON parsing
- ✅ Proper error handling in all engines
- ✅ Service registration complete

