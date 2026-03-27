# Identity Module - Application Layer Plan Refactoring

## Overview

This document refactors the **Phase 0: BuildingBlocks Enhancement** plan to follow enterprise best practices:

1. **BuildingBlocks contains ONLY abstractions** (interfaces, base classes)
2. **Concrete implementations moved to Capability projects** (swappable, vendor-independent)
3. **No-op/placeholder implementations** in BuildingBlocks.Infrastructure for default behavior
4. **Hosts choose which capabilities to enable** via DI registration

---

## Architecture Principles

### ✅ Correct Layering

```
BuildingBlocks.Application/
  ├── Abstractions (IBackgroundJobScheduler, IErrorTracker, IFeatureFlagService)
  └── NO implementations

BuildingBlocks.Infrastructure/
  ├── No-op implementations (NullBackgroundJobScheduler, NullErrorTracker)
  └── Default implementations (InMemoryFeatureFlagService)

Capabilities.{Feature}.{Vendor}/
  ├── Vendor-specific implementations (HangfireBackgroundJobScheduler, SentryErrorTracker)
  └── DI registration extensions (AddHangfireBackgroundJobs, AddSentryErrorTracking)

Hosts/
  ├── Choose which capabilities to enable
  └── builder.AddHangfireBackgroundJobs() OR builder.AddQuartzBackgroundJobs()
```

### ❌ Anti-Pattern (Current Plan)

```
BuildingBlocks.Infrastructure/
  ├── HangfireBackgroundJobScheduler  ❌ Vendor lock-in
  ├── SentryErrorTracker              ❌ Vendor lock-in
  └── Hangfire NuGet packages         ❌ Forces all hosts to use Hangfire
```

---

## Refactored Phase 0: BuildingBlocks Enhancement

### 0.0: Prerequisites Check (15 minutes)

**Status**: ⏳ Not Started

**Tasks**:
- [ ] Verify `BuildingBlocks.Kernel` project exists
- [ ] Verify `BuildingBlocks.Application` project exists
- [ ] Verify `BuildingBlocks.Infrastructure` project exists
- [ ] Verify `BuildingBlocks.Web` project exists
- [ ] Create `Capabilities/` folder structure:
  - `Capabilities/BackgroundJobs/Hangfire/`
  - `Capabilities/BackgroundJobs/Quartz/`
  - `Capabilities/ErrorTracking/Sentry/`
  - `Capabilities/Logging/Serilog/`
  - `Capabilities/FeatureFlags/LaunchDarkly/`

**Updated NuGet Packages** (Directory.Packages.props):

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <!-- MediatR -->
  <PackageVersion Include="MediatR" Version="13.1.0" />
  <PackageVersion Include="MediatR.Contracts" Version="2.0.1" />
  
  <!-- FluentValidation -->
  <PackageVersion Include="FluentValidation" Version="11.9.0" />
  <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
  
  <!-- Hangfire (ONLY in Capabilities.BackgroundJobs.Hangfire) -->
  <PackageVersion Include="Hangfire.Core" Version="1.8.9" />
  <PackageVersion Include="Hangfire.AspNetCore" Version="1.8.9" />
  <PackageVersion Include="Hangfire.PostgreSql" Version="1.20.8" />
  
  <!-- Quartz (ONLY in Capabilities.BackgroundJobs.Quartz) -->
  <PackageVersion Include="Quartz" Version="3.8.0" />
  <PackageVersion Include="Quartz.Extensions.Hosting" Version="3.8.0" />
  
  <!-- Sentry (ONLY in Capabilities.ErrorTracking.Sentry) -->
  <PackageVersion Include="Sentry.AspNetCore" Version="4.0.0" />
  
  <!-- Serilog (ONLY in Capabilities.Logging.Serilog) -->
  <PackageVersion Include="Serilog.AspNetCore" Version="8.0.0" />
  <PackageVersion Include="Serilog.Sinks.Console" Version="5.0.1" />
  <PackageVersion Include="Serilog.Sinks.File" Version="5.0.0" />
  <PackageVersion Include="Serilog.Enrichers.Environment" Version="2.3.0" />
  <PackageVersion Include="Serilog.Enrichers.Thread" Version="3.1.0" />
</ItemGroup>
```

---

### 0.1: ASP.NET Middleware (3 hours)

**Location**: `BuildingBlocks.Web/Middleware/`

**Status**: ✅ Keep as-is (middleware is framework-agnostic)

**Tasks**:
- [ ] Create `GlobalExceptionHandlerMiddleware.cs`
- [ ] Create `RequestLoggingMiddleware.cs`
- [ ] Create `TenantResolutionMiddleware.cs`
- [ ] Create `CorrelationIdMiddleware.cs`
- [ ] Create `MiddlewareExtensions.cs`
- [ ] Add unit tests

**Note**: Middleware is NOT vendor-specific, so it stays in BuildingBlocks.Web.

---

### 0.2: MediatR Behaviors (2 hours)

**Location**: `BuildingBlocks.Application/Behaviors/`

**Status**: ✅ Keep as-is (behaviors are framework-agnostic)

**Tasks**:
- [ ] Create `ValidationBehavior.cs`
- [ ] Create `LoggingBehavior.cs` (uses `ILogger<T>`, not Serilog directly)
- [ ] Create `PerformanceBehavior.cs`
- [ ] Create `TransactionBehavior.cs`
- [ ] Add unit tests

**Note**: Behaviors use `ILogger<T>` abstraction, not Serilog directly.

---

### 0.3: Specification Pattern (1.5 hours)

**Location**: Use **Ardalis.Specification** (NuGet) — abstractions in `BuildingBlocks.Kernel` via package reference; EF application in `BuildingBlocks.Infrastructure` via `Ardalis.Specification.EntityFrameworkCore`.

**Status**: ✅ Done — using Ardalis.Specification instead of custom implementation

**Approach**:
- **Ardalis.Specification** provides `ISpecification<T>`, `Specification<T>` base class, and composable specs (And, Or, Not, etc.). No custom specification types are maintained in the solution.
- **BuildingBlocks.Kernel** references `Ardalis.Specification` so domain/application layers can define specs (inherit from `Specification<T>`).
- **BuildingBlocks.Infrastructure** references `Ardalis.Specification.EntityFrameworkCore`; `IRepository`/`Repository` use `Ardalis.Specification.ISpecification<T>` and apply specs via `DbSet.WithSpecification(spec)`.
- **Directory.Packages.props**: add `Ardalis.Specification` and `Ardalis.Specification.EntityFrameworkCore` (versions managed centrally).

**Tasks**:
- [x] Use Ardalis.Specification (ISpecification<T>, Specification<T>) instead of custom types
- [x] Use Ardalis.Specification.EntityFrameworkCore for Repository (WithSpecification)
- [ ] Add unit tests (optional; Ardalis is well-tested)

**Note**: Modules that need specifications reference `BuildingBlocks.Kernel` and create classes inheriting from `Ardalis.Specification.Specification<T>`.

---

### 0.4: Background Jobs - Abstraction (30 minutes)

**Location**: `BuildingBlocks.Application/BackgroundJobs/`

**Status**: ✅ Abstraction only (no vendor-specific code)

#### 0.4.1: IBackgroundJobScheduler Interface

**File**: `BuildingBlocks.Application/BackgroundJobs/IBackgroundJobScheduler.cs`

```csharp
using System.Linq.Expressions;

namespace BuildingBlocks.Application.BackgroundJobs;

/// <summary>
/// Abstraction for scheduling background jobs.
/// Implementations: Hangfire, Quartz.NET, Azure Functions, AWS Lambda.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// Enqueues a background job to be executed immediately.
    /// </summary>
    string Enqueue<T>(Expression<Action<T>> methodCall);

    /// <summary>
    /// Enqueues an async background job to be executed immediately.
    /// </summary>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Schedules a background job to be executed after a delay.
    /// </summary>
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules an async background job to be executed after a delay.
    /// </summary>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Creates or updates a recurring background job.
    /// </summary>
    void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression);

    /// <summary>
    /// Creates or updates a recurring async background job.
    /// </summary>
    void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression);
}
```

**Tasks**:
- [ ] Create `IBackgroundJobScheduler.cs`
- [ ] Add XML documentation
- [ ] Add unit tests (verify interface contract)

---

#### 0.4.2: No-op Implementation (Default)

**File**: `BuildingBlocks.Infrastructure/BackgroundJobs/NullBackgroundJobScheduler.cs`

```csharp
using System.Linq.Expressions;
using BuildingBlocks.Application.BackgroundJobs;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.BackgroundJobs;

/// <summary>
/// No-op implementation of <see cref="IBackgroundJobScheduler"/>.
/// Logs job scheduling requests but does NOT execute them.
/// Use this when background jobs are disabled or for testing.
/// </summary>
internal sealed class NullBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly ILogger<NullBackgroundJobScheduler> _logger;

    public NullBackgroundJobScheduler(ILogger<NullBackgroundJobScheduler> logger)
    {
        _logger = logger;
    }

    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        _logger.LogWarning("Background job enqueued but NOT executed (NullBackgroundJobScheduler): {MethodCall}", methodCall);
        return Guid.NewGuid().ToString();
    }

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        _logger.LogWarning("Background job enqueued but NOT executed (NullBackgroundJobScheduler): {MethodCall}", methodCall);
        return Guid.NewGuid().ToString();
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        _logger.LogWarning("Background job scheduled but NOT executed (NullBackgroundJobScheduler): {MethodCall}, Delay: {Delay}", methodCall, delay);
        return Guid.NewGuid().ToString();
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        _logger.LogWarning("Background job scheduled but NOT executed (NullBackgroundJobScheduler): {MethodCall}, Delay: {Delay}", methodCall, delay);
        return Guid.NewGuid().ToString();
    }

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression)
    {
        _logger.LogWarning("Recurring job registered but NOT executed (NullBackgroundJobScheduler): {JobId}, {MethodCall}, Cron: {Cron}", recurringJobId, methodCall, cronExpression);
    }

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        _logger.LogWarning("Recurring job registered but NOT executed (NullBackgroundJobScheduler): {JobId}, {MethodCall}, Cron: {Cron}", recurringJobId, methodCall, cronExpression);
    }
}
```

**Tasks**:
- [ ] Create `NullBackgroundJobScheduler.cs`
- [ ] Register in `BuildingBlocks.Infrastructure` DI
- [ ] Add unit tests

---

### 0.5: Feature Flags - Abstraction (30 minutes)

**Location**: `BuildingBlocks.Application/FeatureFlags/`

**Status**: ✅ Abstraction only

#### 0.5.1: IFeatureFlagService Interface

**File**: `BuildingBlocks.Application/FeatureFlags/IFeatureFlagService.cs`

```csharp
namespace BuildingBlocks.Application.FeatureFlags;

/// <summary>
/// Abstraction for feature flag evaluation.
/// Implementations: InMemory (appsettings.json), LaunchDarkly, Azure App Configuration.
/// </summary>
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
    Task<bool> IsEnabledForUserAsync(string featureName, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsEnabledForTenantAsync(string featureName, Guid tenantId, CancellationToken cancellationToken = default);
}
```

**Tasks**:
- [ ] Create `IFeatureFlagService.cs`
- [ ] Add XML documentation

---

#### 0.5.2: In-Memory Implementation (Default)

**File**: `BuildingBlocks.Infrastructure/FeatureFlags/InMemoryFeatureFlagService.cs`

```csharp
using BuildingBlocks.Application.FeatureFlags;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Infrastructure.FeatureFlags;

/// <summary>
/// In-memory feature flag service that reads from appsettings.json.
/// Use this for simple feature flags without external dependencies.
/// </summary>
internal sealed class InMemoryFeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;

    public InMemoryFeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var value = _configuration[$"FeatureFlags:{featureName}"];
        return Task.FromResult(bool.TryParse(value, out var enabled) && enabled);
    }

    public Task<bool> IsEnabledForUserAsync(string featureName, Guid userId, CancellationToken cancellationToken = default)
    {
        // Simple implementation: same as IsEnabledAsync
        return IsEnabledAsync(featureName, cancellationToken);
    }

    public Task<bool> IsEnabledForTenantAsync(string featureName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Simple implementation: same as IsEnabledAsync
        return IsEnabledAsync(featureName, cancellationToken);
    }
}
```

**Tasks**:
- [ ] Create `InMemoryFeatureFlagService.cs`
- [ ] Register in `BuildingBlocks.Infrastructure` DI
- [ ] Add unit tests

---

### 0.6: Health Checks (30 minutes)

**Location**: `BuildingBlocks.Web/HealthChecks/`

**Status**: ✅ Keep as-is (health checks are framework-agnostic)

**Tasks**:
- [ ] Create `DatabaseHealthCheck.cs`
- [ ] Create `RedisHealthCheck.cs`
- [ ] Create `RabbitMqHealthCheck.cs`
- [ ] Register in `BuildingBlocksExtensions.cs`
- [ ] Add unit tests

---

### 0.7: Observability - Abstractions (3.5 hours)

**Location**: `BuildingBlocks.Application/Observability/`

#### 0.7.1: Correlation ID Middleware (45 minutes)

**Status**: ✅ Keep as-is (middleware is framework-agnostic)

**File**: `BuildingBlocks.Web/Middleware/CorrelationIdMiddleware.cs`

**Tasks**:
- [ ] Create `CorrelationIdMiddleware.cs`
- [ ] Store correlation ID in `HttpContext.Items`
- [ ] Add to response headers
- [ ] Add unit tests

---

#### 0.7.2: IStructuredLogger Abstraction (1 hour)

**File**: `BuildingBlocks.Application/Logging/IStructuredLogger.cs`

```csharp
namespace BuildingBlocks.Application.Logging;

/// <summary>
/// Abstraction for structured logging.
/// Implementations: Serilog, NLog, Microsoft.Extensions.Logging.
/// </summary>
public interface IStructuredLogger
{
    void LogInformation(string messageTemplate, params object[] propertyValues);
    void LogWarning(string messageTemplate, params object[] propertyValues);
    void LogError(Exception exception, string messageTemplate, params object[] propertyValues);
    void LogDebug(string messageTemplate, params object[] propertyValues);
}
```

**Tasks**:
- [ ] Create `IStructuredLogger.cs`
- [ ] Add XML documentation

---

#### 0.7.3: Security Audit Logging (1.5 hours)

**File**: `BuildingBlocks.Application/Auditing/ISecurityEventLogger.cs`

```csharp
namespace BuildingBlocks.Application.Auditing;

/// <summary>
/// Abstraction for security event logging (login attempts, permission changes, data access).
/// </summary>
public interface ISecurityEventLogger
{
    Task LogLoginAttemptAsync(Guid userId, bool success, string ipAddress, CancellationToken cancellationToken = default);
    Task LogPermissionChangeAsync(Guid userId, string permission, string action, CancellationToken cancellationToken = default);
    Task LogDataAccessAsync(Guid userId, string entityType, Guid entityId, string action, CancellationToken cancellationToken = default);
}
```

**File**: `BuildingBlocks.Infrastructure/Auditing/DatabaseSecurityEventLogger.cs`

```csharp
using BuildingBlocks.Application.Auditing;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Database-backed security event logger (default implementation).
/// Logs security events to a dedicated audit table.
/// </summary>
internal sealed class DatabaseSecurityEventLogger : ISecurityEventLogger
{
    private readonly ILogger<DatabaseSecurityEventLogger> _logger;

    public DatabaseSecurityEventLogger(ILogger<DatabaseSecurityEventLogger> logger)
    {
        _logger = logger;
    }

    public Task LogLoginAttemptAsync(Guid userId, bool success, string ipAddress, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt: UserId={UserId}, Success={Success}, IP={IpAddress}", userId, success, ipAddress);
        // TODO: Insert into SecurityAuditLog table
        return Task.CompletedTask;
    }

    public Task LogPermissionChangeAsync(Guid userId, string permission, string action, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Permission change: UserId={UserId}, Permission={Permission}, Action={Action}", userId, permission, action);
        // TODO: Insert into SecurityAuditLog table
        return Task.CompletedTask;
    }

    public Task LogDataAccessAsync(Guid userId, string entityType, Guid entityId, string action, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Data access: UserId={UserId}, EntityType={EntityType}, EntityId={EntityId}, Action={Action}", userId, entityType, entityId, action);
        // TODO: Insert into SecurityAuditLog table
        return Task.CompletedTask;
    }
}
```

**Tasks**:
- [ ] Create `ISecurityEventLogger.cs`
- [ ] Create `DatabaseSecurityEventLogger.cs` (placeholder)
- [ ] Register in `BuildingBlocks.Infrastructure` DI
- [ ] Add unit tests

---

#### 0.7.4: Error Tracking - Abstraction (30 minutes)

**File**: `BuildingBlocks.Application/ErrorTracking/IErrorTracker.cs`

```csharp
namespace BuildingBlocks.Application.ErrorTracking;

/// <summary>
/// Abstraction for error tracking and monitoring.
/// Implementations: Sentry, Application Insights, Raygun, Rollbar.
/// </summary>
public interface IErrorTracker
{
    void CaptureException(Exception exception, ErrorContext context);
    void CaptureMessage(string message, ErrorSeverity severity, ErrorContext context);
}

public sealed class ErrorContext
{
    public string? CorrelationId { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? UserId { get; init; }
    public string? RequestPath { get; init; }
    public Dictionary<string, object>? AdditionalData { get; init; }
}

public enum ErrorSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
```

**Tasks**:
- [ ] Create `IErrorTracker.cs`
- [ ] Create `ErrorContext.cs`
- [ ] Create `ErrorSeverity.cs`
- [ ] Add XML documentation

---

#### 0.7.5: No-op Error Tracker (Default)

**File**: `BuildingBlocks.Infrastructure/ErrorTracking/NullErrorTracker.cs`

```csharp
using BuildingBlocks.Application.ErrorTracking;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.ErrorTracking;

/// <summary>
/// No-op implementation of <see cref="IErrorTracker"/>.
/// Logs errors but does NOT send them to external service.
/// Use this when error tracking is disabled or for testing.
/// </summary>
internal sealed class NullErrorTracker : IErrorTracker
{
    private readonly ILogger<NullErrorTracker> _logger;

    public NullErrorTracker(ILogger<NullErrorTracker> logger)
    {
        _logger = logger;
    }

    public void CaptureException(Exception exception, ErrorContext context)
    {
        _logger.LogError(exception, "Error captured (NullErrorTracker): CorrelationId={CorrelationId}, TenantId={TenantId}, UserId={UserId}",
            context.CorrelationId, context.TenantId, context.UserId);
    }

    public void CaptureMessage(string message, ErrorSeverity severity, ErrorContext context)
    {
        _logger.LogWarning("Message captured (NullErrorTracker): {Message}, Severity={Severity}, CorrelationId={CorrelationId}",
            message, severity, context.CorrelationId);
    }
}
```

**Tasks**:
- [ ] Create `NullErrorTracker.cs`
- [ ] Register in `BuildingBlocks.Infrastructure` DI
- [ ] Add unit tests

---

### 0.8: Register All BuildingBlocks in DI (1 hour)

**File**: `BuildingBlocks.Web/Extensions/BuildingBlocksExtensions.cs`

```csharp
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.FeatureFlags;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Infrastructure.FeatureFlags;
using BuildingBlocks.Infrastructure.Security;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.Extensions;

public static class BuildingBlocksExtensions
{
    /// <summary>
    /// Registers all BuildingBlocks components (MediatR, FluentValidation, behaviors, default implementations).
    /// NOTE: Background jobs, error tracking, and structured logging are registered via Capability projects.
    /// </summary>
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        });

        // FluentValidation
        services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

        // MediatR Behaviors (order matters!)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // Default BuildingBlocks implementations (can be overridden by Capability projects)
        services.AddBuildingBlocksInfrastructure();

        return services;
    }
}
```

**File**: `BuildingBlocks.Infrastructure/Extensions/InfrastructureServiceCollectionExtensions.cs`

```csharp
using BuildingBlocks.Application.Auditing;
using BuildingBlocks.Application.BackgroundJobs;
using BuildingBlocks.Application.ErrorTracking;
using BuildingBlocks.Application.FeatureFlags;
using BuildingBlocks.Infrastructure.Auditing;
using BuildingBlocks.Infrastructure.BackgroundJobs;
using BuildingBlocks.Infrastructure.ErrorTracking;
using BuildingBlocks.Infrastructure.FeatureFlags;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers default BuildingBlocks implementations (no-op/in-memory).
    /// These can be overridden by Capability projects (e.g., Hangfire, Sentry, Serilog).
    /// </summary>
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services)
    {
        // Feature Flags: In-memory (appsettings.json)
        services.AddSingleton<IFeatureFlagService, InMemoryFeatureFlagService>();

        // Security Audit Logging: Database (placeholder)
        services.AddScoped<ISecurityEventLogger, DatabaseSecurityEventLogger>();

        // Background Jobs: No-op (logs but does not execute)
        services.AddSingleton<IBackgroundJobScheduler, NullBackgroundJobScheduler>();

        // Error Tracking: No-op (logs but does not send to external service)
        services.AddSingleton<IErrorTracker, NullErrorTracker>();

        return services;
    }
}
```

**Tasks**:
- [ ] Create `BuildingBlocksExtensions.cs`
- [ ] Create `InfrastructureServiceCollectionExtensions.cs`
- [ ] Update all host `Program.cs` files to call `AddBuildingBlocks()`
- [ ] Verify all behaviors are registered
- [ ] Add unit tests

---

## Capability Projects (Vendor-Specific Implementations)

### Capability 1: Background Jobs - Hangfire (1 hour)

**Location**: `server/src/Capabilities/BackgroundJobs/Hangfire/`

**Project**: `Capabilities.BackgroundJobs.Hangfire.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Application\BuildingBlocks.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Hangfire.Core" />
    <PackageReference Include="Hangfire.AspNetCore" />
    <PackageReference Include="Hangfire.PostgreSql" />
  </ItemGroup>

</Project>
```

**File**: `HangfireBackgroundJobScheduler.cs`

```csharp
using System.Linq.Expressions;
using BuildingBlocks.Application.BackgroundJobs;
using Hangfire;

namespace Capabilities.BackgroundJobs.Hangfire;

internal sealed class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    public string Enqueue<T>(Expression<Action<T>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression) =>
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression) =>
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);
}
```

**File**: `HangfireExtensions.cs`

```csharp
using BuildingBlocks.Application.BackgroundJobs;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Capabilities.BackgroundJobs.Hangfire;

public static class HangfireExtensions
{
    public static WebApplicationBuilder AddHangfireBackgroundJobs(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return builder;
        }

        builder.Services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                });
        });

        builder.Services.AddHangfireServer();

        // Override default NullBackgroundJobScheduler with Hangfire implementation
        builder.Services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

        return builder;
    }

    public static WebApplication UseHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard("/admin/jobs", new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()],
        });

        return app;
    }
}

internal sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true; // TODO: Add real authorization
}
```

**Tasks**:
- [ ] Create `Capabilities.BackgroundJobs.Hangfire` project
- [ ] Create `HangfireBackgroundJobScheduler.cs`
- [ ] Create `HangfireExtensions.cs`
- [ ] Add to solution
- [ ] Add unit tests

---

### Capability 2: Background Jobs - Quartz.NET (1 hour)

**Location**: `server/src/Capabilities/BackgroundJobs/Quartz/`

**Project**: `Capabilities.BackgroundJobs.Quartz.csproj`

**File**: `QuartzBackgroundJobScheduler.cs`

```csharp
using System.Linq.Expressions;
using BuildingBlocks.Application.BackgroundJobs;
using Microsoft.Extensions.Logging;

namespace Capabilities.BackgroundJobs.Quartz;

/// <summary>
/// Quartz.NET implementation of <see cref="IBackgroundJobScheduler"/>.
/// TODO: Implement Quartz.NET job scheduling.
/// </summary>
internal sealed class QuartzBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly ILogger<QuartzBackgroundJobScheduler> _logger;

    public QuartzBackgroundJobScheduler(ILogger<QuartzBackgroundJobScheduler> logger)
    {
        _logger = logger;
    }

    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        _logger.LogWarning("Quartz.NET implementation not yet complete: {MethodCall}", methodCall);
        return Guid.NewGuid().ToString();
    }

    // ... other methods (placeholder implementations)
}
```

**Tasks**:
- [ ] Create `Capabilities.BackgroundJobs.Quartz` project
- [ ] Create `QuartzBackgroundJobScheduler.cs` (placeholder)
- [ ] Create `QuartzExtensions.cs` (placeholder)
- [ ] Add to solution
- [ ] Document future implementation plan

---

### Capability 3: Error Tracking - Sentry (1 hour)

**Location**: `server/src/Capabilities/ErrorTracking/Sentry/`

**Project**: `Capabilities.ErrorTracking.Sentry.csproj`

**File**: `SentryErrorTracker.cs`

```csharp
using BuildingBlocks.Application.ErrorTracking;
using Sentry;

namespace Capabilities.ErrorTracking.Sentry;

internal sealed class SentryErrorTracker : IErrorTracker
{
    public void CaptureException(Exception exception, ErrorContext context)
    {
        SentrySdk.CaptureException(exception, scope =>
        {
            if (context.CorrelationId != null)
                scope.SetTag("correlation_id", context.CorrelationId);

            if (context.TenantId.HasValue)
                scope.SetTag("tenant_id", context.TenantId.Value.ToString());

            if (context.UserId.HasValue)
                scope.User = new SentryUser { Id = context.UserId.Value.ToString() };

            if (context.RequestPath != null)
                scope.SetTag("request_path", context.RequestPath);

            if (context.AdditionalData != null)
            {
                foreach (var (key, value) in context.AdditionalData)
                {
                    scope.SetExtra(key, value);
                }
            }
        });
    }

    public void CaptureMessage(string message, ErrorSeverity severity, ErrorContext context)
    {
        var sentryLevel = severity switch
        {
            ErrorSeverity.Debug => SentryLevel.Debug,
            ErrorSeverity.Info => SentryLevel.Info,
            ErrorSeverity.Warning => SentryLevel.Warning,
            ErrorSeverity.Error => SentryLevel.Error,
            ErrorSeverity.Fatal => SentryLevel.Fatal,
            _ => SentryLevel.Info
        };

        SentrySdk.CaptureMessage(message, sentryLevel);
    }
}
```

**File**: `SentryExtensions.cs`

```csharp
using BuildingBlocks.Application.ErrorTracking;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Capabilities.ErrorTracking.Sentry;

public static class SentryExtensions
{
    public static WebApplicationBuilder AddSentryErrorTracking(this WebApplicationBuilder builder)
    {
        var dsn = builder.Configuration["Sentry:Dsn"];
        if (string.IsNullOrWhiteSpace(dsn))
        {
            return builder;
        }

        builder.WebHost.UseSentry(options =>
        {
            options.Dsn = dsn;
            options.TracesSampleRate = 1.0;
            options.Environment = builder.Environment.EnvironmentName;
        });

        // Override default NullErrorTracker with Sentry implementation
        builder.Services.AddSingleton<IErrorTracker, SentryErrorTracker>();

        return builder;
    }
}
```

**Tasks**:
- [ ] Create `Capabilities.ErrorTracking.Sentry` project
- [ ] Create `SentryErrorTracker.cs`
- [ ] Create `SentryExtensions.cs`
- [ ] Add to solution
- [ ] Add unit tests

---

### Capability 4: Logging - Serilog (1 hour)

**Location**: `server/src/Capabilities/Logging/Serilog/`

**Project**: `Capabilities.Logging.Serilog.csproj`

**File**: `SerilogStructuredLogger.cs`

```csharp
using BuildingBlocks.Application.Logging;
using Serilog;

namespace Capabilities.Logging.Serilog;

internal sealed class SerilogStructuredLogger : IStructuredLogger
{
    private readonly ILogger _logger;

    public SerilogStructuredLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogInformation(string messageTemplate, params object[] propertyValues) =>
        _logger.Information(messageTemplate, propertyValues);

    public void LogWarning(string messageTemplate, params object[] propertyValues) =>
        _logger.Warning(messageTemplate, propertyValues);

    public void LogError(Exception exception, string messageTemplate, params object[] propertyValues) =>
        _logger.Error(exception, messageTemplate, propertyValues);

    public void LogDebug(string messageTemplate, params object[] propertyValues) =>
        _logger.Debug(messageTemplate, propertyValues);
}
```

**File**: `SerilogExtensions.cs`

```csharp
using BuildingBlocks.Application.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Capabilities.Logging.Serilog;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        });

        // Register IStructuredLogger implementation
        builder.Services.AddSingleton<IStructuredLogger>(sp =>
            new SerilogStructuredLogger(Log.Logger));

        return builder;
    }
}
```

**Tasks**:
- [ ] Create `Capabilities.Logging.Serilog` project
- [ ] Create `SerilogStructuredLogger.cs`
- [ ] Create `SerilogExtensions.cs`
- [ ] Add to solution
- [ ] Add unit tests

---

### Capability 5: Feature Flags - LaunchDarkly (Placeholder)

**Location**: `server/src/Capabilities/FeatureFlags/LaunchDarkly/`

**Project**: `Capabilities.FeatureFlags.LaunchDarkly.csproj`

**File**: `LaunchDarklyFeatureFlagService.cs`

```csharp
using BuildingBlocks.Application.FeatureFlags;
using Microsoft.Extensions.Logging;

namespace Capabilities.FeatureFlags.LaunchDarkly;

/// <summary>
/// LaunchDarkly implementation of <see cref="IFeatureFlagService"/>.
/// TODO: Implement LaunchDarkly SDK integration.
/// </summary>
internal sealed class LaunchDarklyFeatureFlagService : IFeatureFlagService
{
    private readonly ILogger<LaunchDarklyFeatureFlagService> _logger;

    public LaunchDarklyFeatureFlagService(ILogger<LaunchDarklyFeatureFlagService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("LaunchDarkly implementation not yet complete: {FeatureName}", featureName);
        return Task.FromResult(false);
    }

    // ... other methods (placeholder implementations)
}
```

**Tasks**:
- [ ] Create `Capabilities.FeatureFlags.LaunchDarkly` project (placeholder)
- [ ] Create `LaunchDarklyFeatureFlagService.cs` (placeholder)
- [ ] Create `LaunchDarklyExtensions.cs` (placeholder)
- [ ] Add to solution
- [ ] Document future implementation plan

---

## Updated Host Configuration

### MonolithHost/Program.cs

```csharp
using BuildingBlocks.Web.Extensions;
using Capabilities.BackgroundJobs.Hangfire;
using Capabilities.ErrorTracking.Sentry;
using Capabilities.Logging.Serilog;
using Feature.Module;
using Identity.Module;
using Tenant.Module;
using User.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ✅ BuildingBlocks: MediatR, behaviors, validators, default implementations (no-op/in-memory)
builder.Services.AddBuildingBlocks();

// ✅ BuildingBlocks: Health checks
builder.AddBuildingBlocksHealthChecks();

// ✅ Capability: Serilog structured logging (overrides default ILogger)
builder.AddSerilogStructuredLogging();

// ✅ Capability: Hangfire background jobs (overrides NullBackgroundJobScheduler)
builder.AddHangfireBackgroundJobs();

// ✅ Capability: Sentry error tracking (overrides NullErrorTracker)
builder.AddSentryErrorTracking();

// Modules
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<IdentityModule>(builder.Configuration);
builder.Services.AddModule<UserModule>(builder.Configuration);
builder.Services.AddModule<FeatureModule>(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseGlobalExceptionHandler();
app.UseCorrelationId();
app.UseRequestLogging();
app.UseTenantResolution();

// ✅ Capability: Hangfire dashboard (when Hangfire configured)
app.UseHangfireDashboard();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
```

---

## Summary: Vendor Independence Matrix

| Abstraction | Default Implementation | Capability Implementations |
|-------------|------------------------|----------------------------|
| `IBackgroundJobScheduler` | `NullBackgroundJobScheduler` (logs only) | `HangfireBackgroundJobScheduler`, `QuartzBackgroundJobScheduler` |
| `IErrorTracker` | `NullErrorTracker` (logs only) | `SentryErrorTracker`, `ApplicationInsightsErrorTracker` |
| `IStructuredLogger` | `ILogger<T>` (Microsoft.Extensions.Logging) | `SerilogStructuredLogger`, `NLogStructuredLogger` |
| `IFeatureFlagService` | `InMemoryFeatureFlagService` (appsettings.json) | `LaunchDarklyFeatureFlagService`, `AzureAppConfigurationFeatureFlagService` |
| `ISecurityEventLogger` | `DatabaseSecurityEventLogger` (placeholder) | Future: `ElasticsearchSecurityEventLogger` |

---

## Estimated Timeline (Refactored)

### Phase 0: BuildingBlocks Enhancement
- 0.0: Prerequisites: 15 minutes
- 0.1: ASP.NET Middleware: 3 hours ✅ Keep as-is
- 0.2: MediatR Behaviors: 2 hours ✅ Keep as-is
- 0.3: Specification Pattern: 1.5 hours ✅ Keep as-is
- 0.4: Background Jobs Abstraction + No-op: 30 minutes
- 0.5: Feature Flags Abstraction + In-Memory: 30 minutes
- 0.6: Health Checks: 30 minutes ✅ Keep as-is
- 0.7: Observability Abstractions + No-op: 3.5 hours
- 0.8: DI Registration: 1 hour

**Total Phase 0: ~11.5 hours (~1.5 days)**

### Capability Projects (Optional - Implement as Needed)
- Capability 1: Hangfire: 1 hour
- Capability 2: Quartz (placeholder): 30 minutes
- Capability 3: Sentry: 1 hour
- Capability 4: Serilog: 1 hour
- Capability 5: LaunchDarkly (placeholder): 30 minutes

**Total Capabilities: ~4.5 hours (~0.5 days)**

### Phase 1: Identity Application Layer
- Unchanged: ~15.5 hours (~2 days)

**Grand Total (Critical Path): ~27 hours (~3.5 days)**
**Grand Total (With Capabilities): ~31.5 hours (~4 days)**

---

## Key Benefits of Refactoring

1. ✅ **True Vendor Independence**: Replace Hangfire with Quartz by changing ONE project reference
2. ✅ **Clean Separation**: BuildingBlocks contains ONLY abstractions
3. ✅ **Testability**: Easy to mock abstractions, no vendor lock-in
4. ✅ **Flexibility**: Hosts choose which capabilities to enable
5. ✅ **No-op Defaults**: Application works without external dependencies
6. ✅ **Future-Proof**: Add new capabilities without modifying BuildingBlocks

---

## Migration Path (From Current Plan to Refactored Plan)

### Step 1: Create Capability Projects (1 hour)
- [ ] Create `Capabilities/BackgroundJobs/Hangfire/` project
- [ ] Create `Capabilities/ErrorTracking/Sentry/` project
- [ ] Create `Capabilities/Logging/Serilog/` project
- [ ] Add to solution

### Step 2: Move Implementations (30 minutes)
- [ ] Move `HangfireBackgroundJobScheduler` → `Capabilities.BackgroundJobs.Hangfire`
- [ ] Move `SentryErrorTracker` → `Capabilities.ErrorTracking.Sentry`
- [ ] Move `SerilogStructuredLogger` → `Capabilities.Logging.Serilog`

### Step 3: Create No-op Implementations (30 minutes)
- [ ] Create `NullBackgroundJobScheduler` in `BuildingBlocks.Infrastructure`
- [ ] Create `NullErrorTracker` in `BuildingBlocks.Infrastructure`

### Step 4: Update DI Registration (30 minutes)
- [ ] Update `BuildingBlocksExtensions.cs` to register no-op implementations
- [ ] Update host `Program.cs` to call capability extensions

### Step 5: Remove Vendor Packages from BuildingBlocks (15 minutes)
- [ ] Remove Hangfire packages from `BuildingBlocks.Infrastructure`
- [ ] Remove Sentry packages from `BuildingBlocks.Infrastructure`
- [ ] Remove Serilog packages from `BuildingBlocks.Web`

### Step 6: Verify Build (15 minutes)
- [ ] Build all projects
- [ ] Run all tests
- [ ] Verify hosts start successfully

**Total Migration Time: ~3 hours**

---

## Next Steps

1. **Review this refactoring plan** with the team
2. **Approve architecture changes** (abstractions in BuildingBlocks, implementations in Capabilities)
3. **Execute migration** (3 hours)
4. **Implement Phase 0** (11.5 hours)
5. **Implement Capability projects** (4.5 hours, as needed)
6. **Proceed to Phase 1** (Identity Application Layer, 15.5 hours)

---

## Questions for Review

1. ✅ Do we agree that BuildingBlocks should contain ONLY abstractions?
2. ✅ Do we agree that vendor-specific implementations belong in Capability projects?
3. ✅ Do we agree that no-op/in-memory implementations are acceptable defaults?
4. ✅ Do we agree that hosts should explicitly choose which capabilities to enable?
5. ✅ Do we agree that this architecture provides true vendor independence?

**If all answers are YES, proceed with refactoring. If NO, discuss concerns.**