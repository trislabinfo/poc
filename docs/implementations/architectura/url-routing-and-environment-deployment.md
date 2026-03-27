# URL Routing & Environment Deployment - Complete Design

**Status**: 🆕 New - Environment & Deployment Architecture
**Last Updated**: 2026-02-11
**Version**: 1.0
**Related Documents**:
- `docs/implementations/module-tenantapplication-all-layer-plan.md`
- `docs/implementations/module-appbuilder-all-layer-plan.md`
- `docs/implementations/module-appruntime-all-layer-plan.md`

---

## Overview

This document defines the **URL routing pattern** and **environment deployment workflow** for the Datarizen platform.

**URL Pattern**: `datarizen.com/{tenantSlug}/{appSlug}/{environment?}`

**Key Features**:
- ✅ **Tenant isolation** via tenant slug
- ✅ **Application routing** via application slug
- ✅ **Environment support** (Development, Staging, Production)
- ✅ **Default environment** (Production when not specified)
- ✅ **Deployment workflow** (dev → staging → production)
- ✅ **Version management** per environment
- ✅ **Compatibility checks** before deployment

---

## URL Routing Pattern

### Pattern Definition

```
https://datarizen.com/{tenantSlug}/{appSlug}/{environment?}
```

**Parameters**:
- `{tenantSlug}` - Tenant identifier (e.g., "acme-corp", "contoso")
- `{appSlug}` - Application identifier within tenant (e.g., "crm", "my-custom-app")
- `{environment}` - Optional environment (development, staging, production)

**Default Behavior**:
- If `{environment}` is omitted → defaults to **Production**

### URL Examples

| URL | Tenant | Application | Environment |
|-----|--------|-------------|-------------|
| `datarizen.com/acme-corp/crm` | acme-corp | crm | Production (default) |
| `datarizen.com/acme-corp/crm/development` | acme-corp | crm | Development |
| `datarizen.com/acme-corp/crm/staging` | acme-corp | crm | Staging |
| `datarizen.com/acme-corp/crm/production` | acme-corp | crm | Production |
| `datarizen.com/contoso/inventory/development` | contoso | inventory | Development |

---

## URL Resolution Flow

```
1. INCOMING REQUEST
   URL: datarizen.com/acme-corp/crm/development
   ↓
2. PARSE URL SEGMENTS
   ├─ tenantSlug = "acme-corp"
   ├─ appSlug = "crm"
   └─ environment = "development"
   ↓
3. RESOLVE TENANT
   ├─ Query: SELECT * FROM tenant.tenants WHERE slug = 'acme-corp'
   ├─ Result: Tenant (Id = guid-1, Slug = "acme-corp")
   └─ If not found → 404 Tenant Not Found
   ↓
4. RESOLVE TENANT APPLICATION
   ├─ Query: SELECT * FROM tenantapplication.tenant_applications
   │         WHERE tenant_id = guid-1 AND slug = 'crm' AND status = 'Active'
   ├─ Result: TenantApplication (Id = guid-2, TenantId = guid-1, ApplicationId = guid-3, Slug = "crm")
   └─ If not found → 404 Application Not Found or Not Active
   ↓
5. RESOLVE ENVIRONMENT
   ├─ Query: SELECT * FROM tenantapplication.tenant_application_environments
   │         WHERE tenant_application_id = guid-2 AND environment_type = 'Development' AND is_active = true
   ├─ Result: TenantApplicationEnvironment (Id = guid-4, ApplicationReleaseId = guid-5, EnvironmentType = Development)
   └─ If not found → 404 Environment Not Deployed
   ↓
6. GET APPLICATION RELEASE
   ├─ ApplicationReleaseId = guid-5
   ├─ Query AppBuilder module for ApplicationRelease details
   └─ Result: ApplicationRelease (Version = "1.2.0", Components = [NavigationComponent, PageComponent])
   ↓
7. CHECK RUNTIME COMPATIBILITY
   ├─ AppRuntime checks if it supports all component types in the release
   ├─ Query: Check RuntimeVersion supports NavigationComponent and PageComponent
   └─ If incompatible → 500 Runtime Incompatible
   ↓
8. LOAD APPLICATION
   ├─ AppRuntime creates or retrieves RuntimeInstance
   ├─ Load all components from ApplicationRelease
   ├─ Apply environment-specific configuration
   └─ Render application to user
   ↓
9. RETURN RESPONSE
   └─ Application loaded successfully
```

---

## Environment Types

```csharp
public enum EnvironmentType
{
    Development = 0,  // Development environment - for testing new features
    Staging = 1,      // Staging/UAT environment - for pre-production validation
    Production = 2    // Production environment - live user-facing application
}
```

**Environment Characteristics**:

| Environment | Purpose | Typical Use | Auto-Created |
|-------------|---------|-------------|--------------|
| Development | Feature development and testing | Developers test new features | Yes (on install) |
| Staging | Pre-production validation | QA/UAT testing before production | No (manual) |
| Production | Live user-facing application | End users access the application | No (manual) |

---

## Deployment Workflow

### Standard Deployment Flow

```
1. INSTALL APPLICATION
   ├─ Tenant installs Application
   ├─ TenantApplication created (Status = Installed)
   ├─ Development environment auto-created (no deployment yet)
   └─ Slug generated (e.g., "my-crm-app")
   ↓
2. DEPLOY TO DEVELOPMENT
   ├─ Select ApplicationRelease (e.g., v1.0.0)
   ├─ Check compatibility with current RuntimeVersion
   ├─ If compatible → Deploy to Development
   ├─ Set ApplicationReleaseId in TenantApplicationEnvironment
   ├─ Set IsActive = true
   └─ Access via: datarizen.com/{tenantSlug}/{appSlug}/development
   ↓
3. TEST IN DEVELOPMENT
   ├─ Developers/QA test the application
   ├─ Find bugs → Create new ApplicationRelease (v1.0.1)
   ├─ Deploy v1.0.1 to Development
   └─ Repeat until stable
   ↓
4. PROMOTE TO STAGING (Optional)
   ├─ Create Staging environment
   ├─ Deploy ApplicationRelease (e.g., v1.0.1) to Staging
   ├─ Check compatibility
   ├─ Set IsActive = true
   ├─ Access via: datarizen.com/{tenantSlug}/{appSlug}/staging
   └─ QA performs final validation
   ↓
5. PROMOTE TO PRODUCTION
   ├─ Create Production environment
   ├─ Deploy ApplicationRelease (e.g., v1.0.1) to Production
   ├─ Check compatibility
   ├─ Set IsActive = true
   ├─ Access via: datarizen.com/{tenantSlug}/{appSlug} (default)
   └─ End users access the application
   ↓
6. ACTIVATE APPLICATION
   ├─ Set TenantApplication.Status = Active
   └─ Application now available to all tenant users
```

---

---

## Entity Definitions

### TenantApplication (Updated)

**Module**: `TenantApplication.Domain`
**Table**: `tenantapplication.tenant_applications`

```csharp
public sealed class TenantApplication : Entity<Guid>, IAggregateRoot
{
    public Guid TenantId { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string Slug { get; private set; } = string.Empty; // NEW: URL-friendly identifier
    public TenantApplicationStatus Status { get; private set; }
    public string Configuration { get; private set; } = string.Empty; // JSON - base configuration
    public Guid InstalledBy { get; private set; }
    public DateTime InstalledAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public DateTime? UninstalledAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Factory method
    public static Result<TenantApplication> Install(
        Guid tenantId,
        Guid applicationId,
        string slug,  // NEW: Required for URL routing
        string? configuration,
        Guid installedBy,
        IDateTimeProvider dateTimeProvider)
    {
        // Validation: slug must be lowercase-kebab-case
        if (!IsValidSlug(slug))
            return Result<TenantApplication>.Failure(Error.Validation(
                "TenantApplication.InvalidSlug",
                "Slug must be lowercase-kebab-case (e.g., 'my-app')"));

        var tenantApp = new TenantApplication(
            Guid.NewGuid(),
            tenantId,
            applicationId,
            slug,
            TenantApplicationStatus.Installed,
            configuration ?? "{}",
            installedBy,
            dateTimeProvider.UtcNow);

        tenantApp.AddDomainEvent(new TenantApplicationInstalledEvent(
            tenantApp.Id,
            tenantApp.TenantId,
            tenantApp.ApplicationId,
            tenantApp.Slug,
            tenantApp.InstalledBy,
            tenantApp.InstalledAt));

        return Result<TenantApplication>.Success(tenantApp);
    }

    private static bool IsValidSlug(string slug)
    {
        // Slug must be lowercase-kebab-case: lowercase letters, numbers, hyphens
        return !string.IsNullOrWhiteSpace(slug) &&
               System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$");
    }
}
```

**Database Columns**:
- `id` (uuid, PK)
- `tenant_id` (uuid, FK to tenant.tenants)
- `application_id` (uuid, FK to appbuilder.applications)
- `slug` (varchar(100), unique per tenant) **NEW**
- `status` (int)
- `configuration` (jsonb)
- `installed_by` (uuid)
- `installed_at` (timestamp)
- `activated_at` (timestamp, nullable)
- `deactivated_at` (timestamp, nullable)
- `uninstalled_at` (timestamp, nullable)
- `created_at` (timestamp)
- `updated_at` (timestamp, nullable)

**Indexes**:
- `ix_tenant_applications_tenant_id` (tenant_id)
- `ix_tenant_applications_application_id` (application_id)
- `ix_tenant_applications_slug` (tenant_id, slug) **NEW - UNIQUE**
- `ix_tenant_applications_status` (status)

---

### TenantApplicationEnvironment (New Entity)

**Module**: `TenantApplication.Domain`
**Table**: `tenantapplication.tenant_application_environments`

```csharp
public sealed class TenantApplicationEnvironment : Entity<Guid>
{
    private TenantApplicationEnvironment() { } // EF Core

    private TenantApplicationEnvironment(
        Guid id,
        Guid tenantApplicationId,
        EnvironmentType environmentType,
        Guid? applicationReleaseId,
        bool isActive,
        string configuration,
        Guid? deployedBy,
        DateTime? deployedAt,
        DateTime createdAt)
    {
        Id = id;
        TenantApplicationId = tenantApplicationId;
        EnvironmentType = environmentType;
        ApplicationReleaseId = applicationReleaseId;
        IsActive = isActive;
        Configuration = configuration;
        DeployedBy = deployedBy;
        DeployedAt = deployedAt;
        CreatedAt = createdAt;
    }

    public Guid TenantApplicationId { get; private set; }
    public EnvironmentType EnvironmentType { get; private set; }
    public Guid? ApplicationReleaseId { get; private set; } // FK to appbuilder.application_releases
    public bool IsActive { get; private set; }
    public string Configuration { get; private set; } = string.Empty; // JSON - environment-specific overrides
    public Guid? DeployedBy { get; private set; } // UserId who deployed
    public DateTime? DeployedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Create a new environment (no deployment yet)
    /// </summary>
    public static Result<TenantApplicationEnvironment> Create(
        Guid tenantApplicationId,
        EnvironmentType environmentType,
        string? configuration,
        IDateTimeProvider dateTimeProvider)
    {
        if (tenantApplicationId == Guid.Empty)
            return Result<TenantApplicationEnvironment>.Failure(Error.Validation(
                "TenantApplicationEnvironment.InvalidTenantApplicationId",
                "TenantApplication ID is required"));

        var environment = new TenantApplicationEnvironment(
            Guid.NewGuid(),
            tenantApplicationId,
            environmentType,
            null, // No deployment yet
            false, // Not active until deployed
            configuration ?? "{}",
            null,
            null,
            dateTimeProvider.UtcNow);

        environment.AddDomainEvent(new EnvironmentCreatedEvent(
            environment.Id,
            environment.TenantApplicationId,
            environment.EnvironmentType));

        return Result<TenantApplicationEnvironment>.Success(environment);
    }

    /// <summary>
    /// Deploy an ApplicationRelease to this environment
    /// </summary>
    public Result<Unit> Deploy(
        Guid applicationReleaseId,
        Guid deployedBy,
        IDateTimeProvider dateTimeProvider)
    {
        if (applicationReleaseId == Guid.Empty)
            return Result<Unit>.Failure(Error.Validation(
                "TenantApplicationEnvironment.InvalidReleaseId",
                "ApplicationRelease ID is required"));

        if (deployedBy == Guid.Empty)
            return Result<Unit>.Failure(Error.Validation(
                "TenantApplicationEnvironment.InvalidDeployedBy",
                "DeployedBy user ID is required"));

        ApplicationReleaseId = applicationReleaseId;
        DeployedBy = deployedBy;
        DeployedAt = dateTimeProvider.UtcNow;
        IsActive = true; // Activate environment after deployment
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new EnvironmentDeployedEvent(
            Id,
            TenantApplicationId,
            EnvironmentType,
            applicationReleaseId,
            deployedBy,
            DeployedAt.Value));

        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Deactivate environment (make unavailable without removing deployment)
    /// </summary>
    public Result<Unit> Deactivate(IDateTimeProvider dateTimeProvider)
    {
        if (!IsActive)
            return Result<Unit>.Failure(Error.Validation(
                "TenantApplicationEnvironment.AlreadyInactive",
                "Environment is already inactive"));

        IsActive = false;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new EnvironmentDeactivatedEvent(
            Id,
            TenantApplicationId,
            EnvironmentType));

        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Activate environment (make available for access)
    /// </summary>
    public Result<Unit> Activate(IDateTimeProvider dateTimeProvider)
    {
        if (ApplicationReleaseId == null)
            return Result<Unit>.Failure(Error.Validation(
                "TenantApplicationEnvironment.NoDeployment",
                "Cannot activate environment without a deployment"));

        if (IsActive)
            return Result<Unit>.Failure(Error.Validation(
                "TenantApplicationEnvironment.AlreadyActive",
                "Environment is already active"));

        IsActive = true;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new EnvironmentActivatedEvent(
            Id,
            TenantApplicationId,
            EnvironmentType));

        return Result<Unit>.Success(Unit.Value);
    }
}
```

**Database Columns**:
- `id` (uuid, PK)
- `tenant_application_id` (uuid, FK to tenantapplication.tenant_applications)
- `environment_type` (int: 0=Development, 1=Staging, 2=Production)
- `application_release_id` (uuid, FK to appbuilder.application_releases, nullable)
- `is_active` (boolean)
- `configuration` (jsonb)
- `deployed_by` (uuid, nullable)
- `deployed_at` (timestamp, nullable)
- `created_at` (timestamp)
- `updated_at` (timestamp, nullable)

**Indexes**:
- `ix_tenant_application_environments_tenant_application_id` (tenant_application_id)
- `ix_tenant_application_environments_application_release_id` (application_release_id)
- `ix_tenant_application_environments_unique` (tenant_application_id, environment_type) **UNIQUE**

---

## Domain Events

### TenantApplication Events (Updated)

```csharp
public sealed record TenantApplicationInstalledEvent(
    Guid TenantApplicationId,
    Guid TenantId,
    Guid ApplicationId,
    string Slug,  // NEW
    Guid InstalledBy,
    DateTime InstalledAt) : IDomainEvent;

public sealed record TenantApplicationActivatedEvent(
    Guid TenantApplicationId,
    Guid TenantId,
    Guid ApplicationId,
    DateTime ActivatedAt) : IDomainEvent;

public sealed record TenantApplicationDeactivatedEvent(
    Guid TenantApplicationId,
    Guid TenantId,
    Guid ApplicationId,
    DateTime DeactivatedAt) : IDomainEvent;

public sealed record TenantApplicationUninstalledEvent(
    Guid TenantApplicationId,
    Guid TenantId,
    Guid ApplicationId,
    DateTime UninstalledAt) : IDomainEvent;
```

### TenantApplicationEnvironment Events (New)

```csharp
public sealed record EnvironmentCreatedEvent(
    Guid EnvironmentId,
    Guid TenantApplicationId,
    EnvironmentType EnvironmentType) : IDomainEvent;

public sealed record EnvironmentDeployedEvent(
    Guid EnvironmentId,
    Guid TenantApplicationId,
    EnvironmentType EnvironmentType,
    Guid ApplicationReleaseId,
    Guid DeployedBy,
    DateTime DeployedAt) : IDomainEvent;

public sealed record EnvironmentActivatedEvent(
    Guid EnvironmentId,
    Guid TenantApplicationId,
    EnvironmentType EnvironmentType) : IDomainEvent;

public sealed record EnvironmentDeactivatedEvent(
    Guid EnvironmentId,
    Guid TenantApplicationId,
    EnvironmentType EnvironmentType) : IDomainEvent;
```


---

## Application Layer Commands

### Environment Management Commands

#### CreateEnvironmentCommand

```csharp
public sealed record CreateEnvironmentCommand(
    Guid TenantApplicationId,
    EnvironmentType EnvironmentType,
    string? Configuration) : IRequest<Result<Guid>>;

public sealed class CreateEnvironmentCommandHandler : IRequestHandler<CreateEnvironmentCommand, Result<Guid>>
{
    private readonly ITenantApplicationEnvironmentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Guid>> Handle(CreateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        // Check if environment already exists
        var existing = await _repository.GetByTenantApplicationAndTypeAsync(
            request.TenantApplicationId,
            request.EnvironmentType,
            cancellationToken);

        if (existing != null)
            return Result<Guid>.Failure(Error.Conflict(
                "Environment.AlreadyExists",
                $"Environment {request.EnvironmentType} already exists for this application"));

        // Create environment
        var environmentResult = TenantApplicationEnvironment.Create(
            request.TenantApplicationId,
            request.EnvironmentType,
            request.Configuration,
            _dateTimeProvider);

        if (environmentResult.IsFailure)
            return Result<Guid>.Failure(environmentResult.Error);

        await _repository.AddAsync(environmentResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(environmentResult.Value.Id);
    }
}
```

#### DeployToEnvironmentCommand

```csharp
public sealed record DeployToEnvironmentCommand(
    Guid EnvironmentId,
    Guid ApplicationReleaseId,
    Guid DeployedBy) : IRequest<Result<Unit>>;

public sealed class DeployToEnvironmentCommandHandler : IRequestHandler<DeployToEnvironmentCommand, Result<Unit>>
{
    private readonly ITenantApplicationEnvironmentRepository _repository;
    private readonly ICompatibilityCheckService _compatibilityCheckService; // From AppRuntime.Contracts
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Unit>> Handle(DeployToEnvironmentCommand request, CancellationToken cancellationToken)
    {
        // Get environment
        var environment = await _repository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (environment == null)
            return Result<Unit>.Failure(Error.NotFound("Environment.NotFound", "Environment not found"));

        // Check compatibility with current runtime version
        var compatibilityResult = await _compatibilityCheckService.CheckCompatibilityAsync(
            request.ApplicationReleaseId,
            cancellationToken);

        if (compatibilityResult.IsFailure)
            return Result<Unit>.Failure(compatibilityResult.Error);

        if (!compatibilityResult.Value.IsCompatible)
            return Result<Unit>.Failure(Error.Validation(
                "Environment.IncompatibleRelease",
                $"ApplicationRelease is not compatible with current runtime. Details: {string.Join(", ", compatibilityResult.Value.ComponentChecks.Where(c => !c.IsSupported).Select(c => c.ErrorMessage))}"));

        // Deploy to environment
        var deployResult = environment.Deploy(
            request.ApplicationReleaseId,
            request.DeployedBy,
            _dateTimeProvider);

        if (deployResult.IsFailure)
            return deployResult;

        await _repository.UpdateAsync(environment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
```

#### ActivateEnvironmentCommand

```csharp
public sealed record ActivateEnvironmentCommand(Guid EnvironmentId) : IRequest<Result<Unit>>;

public sealed class ActivateEnvironmentCommandHandler : IRequestHandler<ActivateEnvironmentCommand, Result<Unit>>
{
    private readonly ITenantApplicationEnvironmentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Unit>> Handle(ActivateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await _repository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (environment == null)
            return Result<Unit>.Failure(Error.NotFound("Environment.NotFound", "Environment not found"));

        var result = environment.Activate(_dateTimeProvider);
        if (result.IsFailure)
            return result;

        await _repository.UpdateAsync(environment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
```

#### DeactivateEnvironmentCommand

```csharp
public sealed record DeactivateEnvironmentCommand(Guid EnvironmentId) : IRequest<Result<Unit>>;

public sealed class DeactivateEnvironmentCommandHandler : IRequestHandler<DeactivateEnvironmentCommand, Result<Unit>>
{
    private readonly ITenantApplicationEnvironmentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Unit>> Handle(DeactivateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await _repository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (environment == null)
            return Result<Unit>.Failure(Error.NotFound("Environment.NotFound", "Environment not found"));

        var result = environment.Deactivate(_dateTimeProvider);
        if (result.IsFailure)
            return result;

        await _repository.UpdateAsync(environment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

## Application Layer Queries

### GetEnvironmentsByTenantApplicationQuery

```csharp
public sealed record GetEnvironmentsByTenantApplicationQuery(
    Guid TenantApplicationId) : IRequest<Result<IEnumerable<TenantApplicationEnvironmentDto>>>;

public sealed class GetEnvironmentsByTenantApplicationQueryHandler
    : IRequestHandler<GetEnvironmentsByTenantApplicationQuery, Result<IEnumerable<TenantApplicationEnvironmentDto>>>
{
    private readonly ITenantApplicationEnvironmentRepository _repository;

    public async Task<Result<IEnumerable<TenantApplicationEnvironmentDto>>> Handle(
        GetEnvironmentsByTenantApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var environments = await _repository.GetByTenantApplicationAsync(
            request.TenantApplicationId,
            cancellationToken);

        var dtos = environments.Select(e => new TenantApplicationEnvironmentDto(
            e.Id,
            e.TenantApplicationId,
            e.EnvironmentType.ToString(),
            e.ApplicationReleaseId,
            e.IsActive,
            e.Configuration,
            e.DeployedBy,
            e.DeployedAt,
            e.CreatedAt,
            e.UpdatedAt));

        return Result<IEnumerable<TenantApplicationEnvironmentDto>>.Success(dtos);
    }
}
```

### ResolveApplicationByUrlQuery

```csharp
public sealed record ResolveApplicationByUrlQuery(
    string TenantSlug,
    string AppSlug,
    string? Environment) : IRequest<Result<ResolvedApplicationDto>>;

public sealed class ResolveApplicationByUrlQueryHandler
    : IRequestHandler<ResolveApplicationByUrlQuery, Result<ResolvedApplicationDto>>
{
    private readonly ITenantService _tenantService; // From Tenant.Contracts
    private readonly ITenantApplicationRepository _tenantApplicationRepository;
    private readonly ITenantApplicationEnvironmentRepository _environmentRepository;

    public async Task<Result<ResolvedApplicationDto>> Handle(
        ResolveApplicationByUrlQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Resolve Tenant by slug
        var tenantResult = await _tenantService.GetBySlugAsync(request.TenantSlug, cancellationToken);
        if (tenantResult.IsFailure || tenantResult.Value == null)
            return Result<ResolvedApplicationDto>.Failure(Error.NotFound(
                "Tenant.NotFound",
                $"Tenant '{request.TenantSlug}' not found"));

        var tenant = tenantResult.Value;

        // 2. Resolve TenantApplication by slug
        var tenantApp = await _tenantApplicationRepository.GetByTenantAndSlugAsync(
            tenant.Id,
            request.AppSlug,
            cancellationToken);

        if (tenantApp == null || tenantApp.Status != TenantApplicationStatus.Active)
            return Result<ResolvedApplicationDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                $"Application '{request.AppSlug}' not found or not active for tenant '{request.TenantSlug}'"));

        // 3. Resolve Environment (default to Production if not specified)
        var environmentType = string.IsNullOrWhiteSpace(request.Environment)
            ? EnvironmentType.Production
            : Enum.Parse<EnvironmentType>(request.Environment, ignoreCase: true);

        var environment = await _environmentRepository.GetByTenantApplicationAndTypeAsync(
            tenantApp.Id,
            environmentType,
            cancellationToken);

        if (environment == null || !environment.IsActive)
            return Result<ResolvedApplicationDto>.Failure(Error.NotFound(
                "Environment.NotFound",
                $"Environment '{environmentType}' not found or not active for application '{request.AppSlug}'"));

        if (environment.ApplicationReleaseId == null)
            return Result<ResolvedApplicationDto>.Failure(Error.Validation(
                "Environment.NoDeployment",
                $"Environment '{environmentType}' has no deployment"));

        // 4. Return resolved application details
        var result = new ResolvedApplicationDto(
            tenant.Id,
            tenant.Slug,
            tenantApp.Id,
            tenantApp.ApplicationId,
            tenantApp.Slug,
            environment.Id,
            environment.EnvironmentType.ToString(),
            environment.ApplicationReleaseId.Value,
            environment.Configuration);

        return Result<ResolvedApplicationDto>.Success(result);
    }
}

public sealed record ResolvedApplicationDto(
    Guid TenantId,
    string TenantSlug,
    Guid TenantApplicationId,
    Guid ApplicationId,
    string AppSlug,
    Guid EnvironmentId,
    string EnvironmentType,
    Guid ApplicationReleaseId,
    string EnvironmentConfiguration);
```


---

## API Layer Endpoints

### TenantApplicationEnvironmentController

**File**: `TenantApplication.Api/Controllers/TenantApplicationEnvironmentController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TenantApplication.Application.Commands.CreateEnvironment;
using TenantApplication.Application.Commands.DeployToEnvironment;
using TenantApplication.Application.Commands.ActivateEnvironment;
using TenantApplication.Application.Commands.DeactivateEnvironment;
using TenantApplication.Application.Queries.GetEnvironmentsByTenantApplication;

namespace TenantApplication.Api.Controllers;

[ApiController]
[Route("api/tenantapplication/applications/{tenantApplicationId:guid}/environments")]
public sealed class TenantApplicationEnvironmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantApplicationEnvironmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all environments for a tenant application
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnvironments(Guid tenantApplicationId, CancellationToken cancellationToken)
    {
        var query = new GetEnvironmentsByTenantApplicationQuery(tenantApplicationId);
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(statusCode: 500, detail: result.Error.Message);
    }

    /// <summary>
    /// Create a new environment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEnvironment(
        Guid tenantApplicationId,
        [FromBody] CreateEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEnvironmentCommand(
            tenantApplicationId,
            request.EnvironmentType,
            request.Configuration);

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created(string.Empty, new { id = result.Value })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Deploy an ApplicationRelease to an environment
    /// </summary>
    [HttpPost("{environmentId:guid}/deploy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeployToEnvironment(
        Guid tenantApplicationId,
        Guid environmentId,
        [FromBody] DeployToEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new DeployToEnvironmentCommand(
            environmentId,
            request.ApplicationReleaseId,
            request.DeployedBy);

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Activate an environment
    /// </summary>
    [HttpPost("{environmentId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateEnvironment(
        Guid tenantApplicationId,
        Guid environmentId,
        CancellationToken cancellationToken)
    {
        var command = new ActivateEnvironmentCommand(environmentId);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Deactivate an environment
    /// </summary>
    [HttpPost("{environmentId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateEnvironment(
        Guid tenantApplicationId,
        Guid environmentId,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateEnvironmentCommand(environmentId);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }
}

public sealed record CreateEnvironmentRequest(
    EnvironmentType EnvironmentType,
    string? Configuration);

public sealed record DeployToEnvironmentRequest(
    Guid ApplicationReleaseId,
    Guid DeployedBy);
```

### ApplicationResolverController (New)

**File**: `TenantApplication.Api/Controllers/ApplicationResolverController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TenantApplication.Application.Queries.ResolveApplicationByUrl;

namespace TenantApplication.Api.Controllers;

[ApiController]
[Route("api/tenantapplication/resolve")]
public sealed class ApplicationResolverController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationResolverController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Resolve application by URL segments (used by AppRuntime for loading applications)
    /// </summary>
    /// <param name="tenantSlug">Tenant slug from URL</param>
    /// <param name="appSlug">Application slug from URL</param>
    /// <param name="environment">Environment from URL (optional, defaults to Production)</param>
    [HttpGet("{tenantSlug}/{appSlug}/{environment?}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveByUrl(
        string tenantSlug,
        string appSlug,
        string? environment,
        CancellationToken cancellationToken)
    {
        var query = new ResolveApplicationByUrlQuery(tenantSlug, appSlug, environment);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error.Message });
    }
}
```

---

## Database Migrations

### Migration 1: Add Slug Column to TenantApplications

**File**: `TenantApplication.Migrations/Migrations/20260211400000_AddSlugToTenantApplications.cs`

```csharp
using FluentMigrator;

namespace TenantApplication.Migrations.Migrations;

[Migration(20260211400000, "Add slug column to tenant_applications table")]
public class AddSlugToTenantApplications : Migration
{
    public override void Up()
    {
        // Add slug column
        Alter.Table("tenant_applications")
            .InSchema("tenantapplication")
            .AddColumn("slug").AsString(100).NotNullable().WithDefaultValue("default-app");

        // Create unique index on (tenant_id, slug)
        Create.Index("ix_tenant_applications_tenant_slug")
            .OnTable("tenant_applications")
            .InSchema("tenantapplication")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("slug").Ascending()
            .Unique();
    }

    public override void Down()
    {
        Delete.Index("ix_tenant_applications_tenant_slug")
            .OnTable("tenant_applications")
            .InSchema("tenantapplication");

        Delete.Column("slug")
            .FromTable("tenant_applications")
            .InSchema("tenantapplication");
    }
}
```

### Migration 2: Create TenantApplicationEnvironments Table

**File**: `TenantApplication.Migrations/Migrations/20260211401000_CreateTenantApplicationEnvironmentsTable.cs`

```csharp
using FluentMigrator;

namespace TenantApplication.Migrations.Migrations;

[Migration(20260211401000, "Create tenant_application_environments table")]
public class CreateTenantApplicationEnvironmentsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_application_environments")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_application_environments")
            .WithColumn("tenant_application_id").AsGuid().NotNullable()
            .WithColumn("environment_type").AsInt32().NotNullable() // 0=Development, 1=Staging, 2=Production
            .WithColumn("application_release_id").AsGuid().Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("configuration").AsCustom("jsonb").Nullable()
            .WithColumn("deployed_by").AsGuid().Nullable()
            .WithColumn("deployed_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        // Foreign key to tenant_applications
        Create.ForeignKey("fk_tenant_application_environments_tenant_application")
            .FromTable("tenant_application_environments").InSchema("tenantapplication").ForeignColumn("tenant_application_id")
            .ToTable("tenant_applications").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Indexes
        Create.Index("ix_tenant_application_environments_tenant_application_id")
            .OnTable("tenant_application_environments")
            .InSchema("tenantapplication")
            .OnColumn("tenant_application_id");

        Create.Index("ix_tenant_application_environments_application_release_id")
            .OnTable("tenant_application_environments")
            .InSchema("tenantapplication")
            .OnColumn("application_release_id");

        // Unique constraint: one environment type per tenant application
        Create.Index("ix_tenant_application_environments_unique")
            .OnTable("tenant_application_environments")
            .InSchema("tenantapplication")
            .OnColumn("tenant_application_id").Ascending()
            .OnColumn("environment_type").Ascending()
            .Unique();
    }

    public override void Down()
    {
        Delete.Table("tenant_application_environments").InSchema("tenantapplication");
    }
}
```





---

## TenantApplication.Contracts (Inter-Module Communication)

### IApplicationResolverService

**File**: `TenantApplication.Contracts/Services/IApplicationResolverService.cs`

**Purpose**: Allows other modules (especially AppRuntime) to resolve URLs to ApplicationReleases without direct dependency on TenantApplication.Domain.

```csharp
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Contracts.Services;

/// <summary>
/// Service for resolving tenant applications by URL segments
/// Used by AppRuntime to load applications based on URL pattern
/// </summary>
public interface IApplicationResolverService
{
    /// <summary>
    /// Resolve application by URL segments
    /// </summary>
    /// <param name="tenantSlug">Tenant slug from URL (e.g., "acme-corp")</param>
    /// <param name="appSlug">Application slug from URL (e.g., "crm")</param>
    /// <param name="environment">Environment from URL (optional, defaults to "production")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved application details including ApplicationReleaseId</returns>
    Task<Result<ResolvedApplicationDto>> ResolveByUrlAsync(
        string tenantSlug,
        string appSlug,
        string? environment,
        CancellationToken cancellationToken = default);
}
```

### ResolvedApplicationDto

**File**: `TenantApplication.Contracts/DTOs/ResolvedApplicationDto.cs`

```csharp
namespace TenantApplication.Contracts.DTOs;

/// <summary>
/// Result of URL resolution containing all information needed to load an application
/// </summary>
public sealed record ResolvedApplicationDto(
    Guid TenantId,
    string TenantSlug,
    Guid TenantApplicationId,
    Guid ApplicationId,
    string AppSlug,
    Guid EnvironmentId,
    string EnvironmentType,
    Guid ApplicationReleaseId,
    string EnvironmentConfiguration);
```

### Implementation

**File**: `TenantApplication.Application/Services/ApplicationResolverService.cs`

```csharp
using BuildingBlocks.Kernel.Results;
using MediatR;
using TenantApplication.Contracts.Services;
using TenantApplication.Contracts.DTOs;
using TenantApplication.Application.Queries.ResolveApplicationByUrl;

namespace TenantApplication.Application.Services;

/// <summary>
/// Implementation of IApplicationResolverService
/// Registered in TenantApplication.Module for DI
/// </summary>
public sealed class ApplicationResolverService : IApplicationResolverService
{
    private readonly IMediator _mediator;

    public ApplicationResolverService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<ResolvedApplicationDto>> ResolveByUrlAsync(
        string tenantSlug,
        string appSlug,
        string? environment,
        CancellationToken cancellationToken = default)
    {
        var query = new ResolveApplicationByUrlQuery(tenantSlug, appSlug, environment);
        return await _mediator.Send(query, cancellationToken);
    }
}
```

### Module Registration

**File**: `TenantApplication.Module/TenantApplicationModule.cs`

```csharp
public static IServiceCollection AddTenantApplicationModule(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... other registrations

    // Register public contracts for inter-module communication
    services.AddScoped<IApplicationResolverService, ApplicationResolverService>();

    return services;
}
```

### Usage in AppRuntime

**File**: `AppRuntime.Application/Commands/LoadApplicationFromUrl/LoadApplicationFromUrlCommandHandler.cs`

```csharp
using TenantApplication.Contracts.Services;
using TenantApplication.Contracts.DTOs;

public sealed class LoadApplicationFromUrlCommandHandler : ICommandHandler<LoadApplicationFromUrlCommand, Result<Guid>>
{
    private readonly IApplicationResolverService _resolverService; // Injected from TenantApplication.Contracts

    public async Task<Result<Guid>> Handle(LoadApplicationFromUrlCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve application by URL
        var resolvedResult = await _resolverService.ResolveByUrlAsync(
            request.TenantSlug,
            request.AppSlug,
            request.Environment,
            cancellationToken);

        if (resolvedResult.IsFailure)
            return Result<Guid>.Failure(resolvedResult.Error);

        var resolved = resolvedResult.Value;

        // 2. Use resolved.ApplicationReleaseId to create RuntimeInstance
        // ... rest of the handler
    }
}
```

---

## Deployment Topology Support

### Monolith Deployment

In monolith deployment, all modules are in the same process:

```csharp
// AppHost/Program.cs
builder.Services
    .AddTenantModule(configuration)
    .AddTenantApplicationModule(configuration)
    .AddAppBuilderModule(configuration)
    .AddAppRuntimeModule(configuration);
```

- `IApplicationResolverService` is registered in DI container
- AppRuntime directly calls TenantApplication via DI
- All modules share the same database with schema isolation

### Microservices Deployment

In microservices deployment, modules are separate services:

```
┌─────────────────────┐
│ TenantApplication   │
│ Microservice        │
│                     │
│ - Exposes HTTP API  │
│ - /api/resolve/{t}/{a}/{e}
└─────────────────────┘
          ↑
          │ HTTP Call
          │
┌─────────────────────┐
│ AppRuntime          │
│ Microservice        │
│                     │
│ - Calls TenantApp   │
│ - Creates Instances │
└─────────────────────┘
```

**Implementation**:
- `IApplicationResolverService` implementation makes HTTP call to TenantApplication microservice
- Uses service discovery to find TenantApplication endpoint
- Calls `GET /api/tenantapplication/resolve/{tenantSlug}/{appSlug}/{environment}`
- Deserializes response to `ResolvedApplicationDto`

**File**: `TenantApplication.Contracts/Services/HttpApplicationResolverService.cs` (for microservices)

```csharp
using System.Net.Http.Json;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Contracts.DTOs;

namespace TenantApplication.Contracts.Services;

/// <summary>
/// HTTP-based implementation of IApplicationResolverService for microservices deployment
/// </summary>
public sealed class HttpApplicationResolverService : IApplicationResolverService
{
    private readonly HttpClient _httpClient;

    public HttpApplicationResolverService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("TenantApplicationService");
    }

    public async Task<Result<ResolvedApplicationDto>> ResolveByUrlAsync(
        string tenantSlug,
        string appSlug,
        string? environment,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/tenantapplication/resolve/{tenantSlug}/{appSlug}";
        if (!string.IsNullOrWhiteSpace(environment))
            url += $"/{environment}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<ResolvedApplicationDto>.Failure(Error.External(
                "ApplicationResolver.ResolutionFailed",
                $"Failed to resolve application: {errorContent}"));
        }

        var resolved = await response.Content.ReadFromJsonAsync<ResolvedApplicationDto>(cancellationToken);
        return Result<ResolvedApplicationDto>.Success(resolved!);
    }
}
```

---

## Summary

This document provides a complete design for URL routing and environment deployment:

✅ **URL Pattern**: `datarizen.com/{tenantSlug}/{appSlug}/{environment?}`
✅ **Entity Definitions**: TenantApplication (with Slug), TenantApplicationEnvironment
✅ **Domain Events**: EnvironmentCreated, EnvironmentDeployed, EnvironmentActivated, EnvironmentDeactivated
✅ **Commands**: CreateEnvironment, DeployToEnvironment, ActivateEnvironment, DeactivateEnvironment
✅ **Queries**: GetEnvironmentsByTenantApplication, ResolveApplicationByUrl
✅ **API Endpoints**: TenantApplicationEnvironmentController, ApplicationResolverController
✅ **Database Migrations**: AddSlugToTenantApplications, CreateTenantApplicationEnvironmentsTable
✅ **Contracts**: IApplicationResolverService for inter-module communication
✅ **AppRuntime Integration**: LoadApplicationFromUrlCommand for environment-based loading
✅ **Deployment Support**: Both monolith and microservices topologies

**Next Steps**:
1. Implement TenantApplication module updates (add Slug, TenantApplicationEnvironment entity)
2. Implement AppRuntime module updates (add LoadApplicationFromUrlCommand)
3. Test URL resolution flow end-to-end
4. Test deployment workflow (dev → staging → production)
5. Test compatibility checks during deployment