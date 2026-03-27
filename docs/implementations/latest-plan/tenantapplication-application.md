# TenantApplication Module - Application Layer

**Status**: Ready for implementation (shared ApplicationDefinition)  
**Last Updated**: 2026-02-15  
**Module**: TenantApplication  
**Layer**: Application  

---

## Shared ApplicationDefinition usage

- **ApplicationDefinition.Domain** is referenced by TenantApplication.Application.  
- Definition CRUD (entities, pages, navigation, data sources, releases) uses **shared types** and **shared repository interfaces** from ApplicationDefinition.Domain (e.g. `IEntityDefinitionRepository.GetByApplicationDefinitionIdAsync` with TenantApplicationId as the id when operating on a tenant app).  
- Commands/queries and DTOs use the shared entities and enums where applicable; validators and handlers call the shared repositories implemented in TenantApplication.Infrastructure (tenant-scoped).  
- **Review**: Confirm this usage before implementation.

---

## Overview

CQRS commands and queries for managing tenant application installations and configurations. **When a tenant has the AppBuilder feature**, TenantApplication also exposes **definition CRUD** (create/update tenant application structure: entities, pages, navigation, data sources, releases) so that the same “AppBuilder” UX can edit tenant applications by calling TenantApplication API instead of AppBuilder API. All such commands and queries are tenant-scoped and operate on `tenantapplication` schema (`tenant_*_definitions`, `tenant_application_releases`).

**Key Changes from Domain Update**:
- ✅ Updated to use ApplicationRelease with Major/Minor/Patch
- ✅ Removed Version string references
- ✅ Added configuration management per environment
- ✅ Updated DTOs to match new domain model

---

## Commands

### 1. InstallApplicationCommand

**Purpose**: Install an application for a tenant

```csharp
namespace Datarizen.TenantApplication.Application.Commands.InstallApplication;

public sealed record InstallApplicationCommand(
    Guid TenantId,
    Guid ApplicationId,
    int Major,
    int Minor,
    int Patch,
    string Name,
    string Slug,
    Dictionary<string, object>? Configuration
) : ICommand<Result<TenantApplicationDto>>;

public sealed class InstallApplicationCommandHandler 
    : ICommandHandler<InstallApplicationCommand, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationReleaseValidator _releaseValidator;

    public async Task<Result<TenantApplicationDto>> Handle(
        InstallApplicationCommand command,
        CancellationToken cancellationToken)
    {
        // Validate release exists
        var releaseExists = await _releaseValidator.ValidateReleaseExistsAsync(
            command.ApplicationId,
            command.Major,
            command.Minor,
            command.Patch,
            cancellationToken);

        if (!releaseExists)
            return Result<TenantApplicationDto>.Failure(Error.NotFound(
                "TenantApplication.Release.NotFound",
                $"Release {command.Major}.{command.Minor}.{command.Patch} not found"));

        // Check if already installed
        var existing = await _repository.GetByTenantAndApplicationAsync(
            command.TenantId,
            command.ApplicationId,
            cancellationToken);

        if (existing is not null)
            return Result<TenantApplicationDto>.Failure(Error.Conflict(
                "TenantApplication.AlreadyInstalled",
                "Application is already installed for this tenant"));

        // Create installation (handler resolves release and creates TenantApplication with Name, Slug; domain may use InstallFromPlatform(tenantId, applicationReleaseId, name, slug) and set ApplicationId, Major, Minor, Patch from release)
        var tenantApp = TenantApplication.Install(
            command.TenantId,
            command.ApplicationId,
            command.Major,
            command.Minor,
            command.Patch,
            command.Name,
            command.Slug,
            command.Configuration ?? new Dictionary<string, object>());

        await _repository.AddAsync(tenantApp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = TenantApplicationDto.FromDomain(tenantApp);
        return Result<TenantApplicationDto>.Success(dto);
    }
}
```

### 2. UpdateConfigurationCommand

**Purpose**: Update tenant-specific configuration

```csharp
namespace Datarizen.TenantApplication.Application.Commands.UpdateConfiguration;

public sealed record UpdateConfigurationCommand(
    Guid TenantApplicationId,
    Dictionary<string, object> Configuration
) : ICommand<Result<TenantApplicationDto>>;

public sealed class UpdateConfigurationCommandHandler 
    : ICommandHandler<UpdateConfigurationCommand, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<TenantApplicationDto>> Handle(
        UpdateConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            command.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result<TenantApplicationDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        tenantApp.UpdateConfiguration(command.Configuration);

        await _repository.UpdateAsync(tenantApp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = TenantApplicationDto.FromDomain(tenantApp);
        return Result<TenantApplicationDto>.Success(dto);
    }
}
```

### 3. UpgradeApplicationCommand

**Purpose**: Upgrade application to a new release version

```csharp
namespace Datarizen.TenantApplication.Application.Commands.UpgradeApplication;

public sealed record UpgradeApplicationCommand(
    Guid TenantApplicationId,
    int Major,
    int Minor,
    int Patch
) : ICommand<Result<TenantApplicationDto>>;

public sealed class UpgradeApplicationCommandHandler 
    : ICommandHandler<UpgradeApplicationCommand, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationReleaseValidator _releaseValidator;

    public async Task<Result<TenantApplicationDto>> Handle(
        UpgradeApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            command.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result<TenantApplicationDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        // Validate new release exists
        var releaseExists = await _releaseValidator.ValidateReleaseExistsAsync(
            tenantApp.ApplicationId,
            command.Major,
            command.Minor,
            command.Patch,
            cancellationToken);

        if (!releaseExists)
            return Result<TenantApplicationDto>.Failure(Error.NotFound(
                "TenantApplication.Release.NotFound",
                $"Release {command.Major}.{command.Minor}.{command.Patch} not found"));

        tenantApp.Upgrade(command.Major, command.Minor, command.Patch);

        await _repository.UpdateAsync(tenantApp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = TenantApplicationDto.FromDomain(tenantApp);
        return Result<TenantApplicationDto>.Success(dto);
    }
}
```

### 4. ActivateApplicationCommand

**Purpose**: Activate an installed application

```csharp
namespace Datarizen.TenantApplication.Application.Commands.ActivateApplication;

public sealed record ActivateApplicationCommand(
    Guid TenantApplicationId
) : ICommand<Result<TenantApplicationDto>>;

public sealed class ActivateApplicationCommandHandler 
    : ICommandHandler<ActivateApplicationCommand, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<TenantApplicationDto>> Handle(
        ActivateApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            command.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result<TenantApplicationDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        tenantApp.Activate();

        await _repository.UpdateAsync(tenantApp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = TenantApplicationDto.FromDomain(tenantApp);
        return Result<TenantApplicationDto>.Success(dto);
    }
}
```

### 5. DeactivateApplicationCommand

**Purpose**: Deactivate an active application

```csharp
namespace Datarizen.TenantApplication.Application.Commands.DeactivateApplication;

public sealed record DeactivateApplicationCommand(
    Guid TenantApplicationId
) : ICommand<Result<TenantApplicationDto>>;

public sealed class DeactivateApplicationCommandHandler 
    : ICommandHandler<DeactivateApplicationCommand, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<TenantApplicationDto>> Handle(
        DeactivateApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            command.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result<TenantApplicationDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        tenantApp.Deactivate();

        await _repository.UpdateAsync(tenantApp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = TenantApplicationDto.FromDomain(tenantApp);
        return Result<TenantApplicationDto>.Success(dto);
    }
}
```

### 6. UninstallApplicationCommand

**Purpose**: Uninstall an application from a tenant

```csharp
namespace Datarizen.TenantApplication.Application.Commands.UninstallApplication;

public sealed record UninstallApplicationCommand(
    Guid TenantApplicationId
) : ICommand<Result>;

public sealed class UninstallApplicationCommandHandler 
    : ICommandHandler<UninstallApplicationCommand, Result>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result> Handle(
        UninstallApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            command.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        tenantApp.Uninstall();

        await _repository.UpdateAsync(tenantApp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 7. CreateEnvironmentCommand

**Purpose**: Create a new environment for a tenant application

```csharp
namespace Datarizen.TenantApplication.Application.Commands.CreateEnvironment;

public sealed record CreateEnvironmentCommand(
    Guid TenantApplicationId,
    string Name,
    string EnvironmentType,
    Dictionary<string, object>? Configuration
) : ICommand<Result<TenantApplicationEnvironmentDto>>;

public sealed class CreateEnvironmentCommandHandler 
    : ICommandHandler<CreateEnvironmentCommand, Result<TenantApplicationEnvironmentDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<TenantApplicationEnvironmentDto>> Handle(
        CreateEnvironmentCommand command,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            command.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result<TenantApplicationEnvironmentDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        var environment = tenantApp.CreateEnvironment(
            command.Name,
            command.EnvironmentType,  // Parse or validate: Development, Staging, Production
            command.Configuration ?? new Dictionary<string, object>());

        await _repository.UpdateAsync(tenantApp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = TenantApplicationEnvironmentDto.FromDomain(environment);
        return Result<TenantApplicationEnvironmentDto>.Success(dto);
    }
}
```

### 8. UpdateEnvironmentConfigurationCommand

**Purpose**: Update environment-specific configuration

```csharp
namespace Datarizen.TenantApplication.Application.Commands.UpdateEnvironmentConfiguration;

public sealed record UpdateEnvironmentConfigurationCommand(
    Guid TenantApplicationId,
    Guid EnvironmentId,
    Dictionary<string, object> Configuration
) : ICommand<Result<TenantApplicationEnvironmentDto>>;

public sealed class UpdateEnvironmentConfigurationCommandHandler 
    : ICommandHandler<UpdateEnvironmentConfigurationCommand, Result<TenantApplicationEnvironmentDto>>
{
    private readonly ITenantApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<TenantApplicationEnvironmentDto>> Handle(
        UpdateEnvironmentConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            command.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result<TenantApplicationEnvironmentDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        var environment = tenantApp.Environments
            .FirstOrDefault(e => e.Id == command.EnvironmentId);

        if (environment is null)
            return Result<TenantApplicationEnvironmentDto>.Failure(Error.NotFound(
                "TenantApplication.Environment.NotFound",
                "Environment not found"));

        environment.UpdateConfiguration(command.Configuration);

        await _repository.UpdateAsync(tenantApp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = TenantApplicationEnvironmentDto.FromDomain(environment);
        return Result<TenantApplicationEnvironmentDto>.Success(dto);
    }
}
```

---

## Queries

### 1. GetTenantApplicationQuery

**Purpose**: Get tenant application by ID

```csharp
namespace Datarizen.TenantApplication.Application.Queries.GetTenantApplication;

public sealed record GetTenantApplicationQuery(
    Guid TenantApplicationId
) : IQuery<Result<TenantApplicationDto>>;

public sealed class GetTenantApplicationQueryHandler 
    : IQueryHandler<GetTenantApplicationQuery, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;

    public async Task<Result<TenantApplicationDto>> Handle(
        GetTenantApplicationQuery query,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            query.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result<TenantApplicationDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        var dto = TenantApplicationDto.FromDomain(tenantApp);
        return Result<TenantApplicationDto>.Success(dto);
    }
}
```

### 2. GetTenantApplicationsQuery

**Purpose**: Get all applications for a tenant

```csharp
namespace Datarizen.TenantApplication.Application.Queries.GetTenantApplications;

public sealed record GetTenantApplicationsQuery(
    Guid TenantId,
    bool ActiveOnly = false
) : IQuery<Result<List<TenantApplicationDto>>>;

public sealed class GetTenantApplicationsQueryHandler 
    : IQueryHandler<GetTenantApplicationsQuery, Result<List<TenantApplicationDto>>>
{
    private readonly ITenantApplicationRepository _repository;

    public async Task<Result<List<TenantApplicationDto>>> Handle(
        GetTenantApplicationsQuery query,
        CancellationToken cancellationToken)
    {
        var tenantApps = await _repository.GetByTenantAsync(
            query.TenantId,
            cancellationToken);

        if (query.ActiveOnly)
        {
            tenantApps = tenantApps
                .Where(ta => ta.Status == TenantApplicationStatus.Active)
                .ToList();
        }

        var dtos = tenantApps
            .Select(TenantApplicationDto.FromDomain)
            .ToList();

        return Result<List<TenantApplicationDto>>.Success(dtos);
    }
}
```

### 3. GetApplicationTenantsQuery

**Purpose**: Get all tenants that have installed an application

```csharp
namespace Datarizen.TenantApplication.Application.Queries.GetApplicationTenants;

public sealed record GetApplicationTenantsQuery(
    Guid ApplicationId,
    bool ActiveOnly = false
) : IQuery<Result<List<TenantApplicationDto>>>;

public sealed class GetApplicationTenantsQueryHandler 
    : IQueryHandler<GetApplicationTenantsQuery, Result<List<TenantApplicationDto>>>
{
    private readonly ITenantApplicationRepository _repository;

    public async Task<Result<List<TenantApplicationDto>>> Handle(
        GetApplicationTenantsQuery query,
        CancellationToken cancellationToken)
    {
        var tenantApps = await _repository.GetByApplicationIdAsync(
            query.ApplicationId,
            cancellationToken);

        if (query.ActiveOnly)
        {
            tenantApps = tenantApps
                .Where(ta => ta.Status == TenantApplicationStatus.Active)
                .ToList();
        }

        var dtos = tenantApps
            .Select(TenantApplicationDto.FromDomain)
            .ToList();

        return Result<List<TenantApplicationDto>>.Success(dtos);
    }
}
```

### 4. GetEnvironmentQuery

**Purpose**: Get environment by ID

```csharp
namespace Datarizen.TenantApplication.Application.Queries.GetEnvironment;

public sealed record GetEnvironmentQuery(
    Guid TenantApplicationId,
    Guid EnvironmentId
) : IQuery<Result<TenantApplicationEnvironmentDto>>;

public sealed class GetEnvironmentQueryHandler 
    : IQueryHandler<GetEnvironmentQuery, Result<TenantApplicationEnvironmentDto>>
{
    private readonly ITenantApplicationRepository _repository;

    public async Task<Result<TenantApplicationEnvironmentDto>> Handle(
        GetEnvironmentQuery query,
        CancellationToken cancellationToken)
    {
        var tenantApp = await _repository.GetByIdAsync(
            query.TenantApplicationId,
            cancellationToken);

        if (tenantApp is null)
            return Result<TenantApplicationEnvironmentDto>.Failure(Error.NotFound(
                "TenantApplication.NotFound",
                "Tenant application not found"));

        var environment = tenantApp.Environments
            .FirstOrDefault(e => e.Id == query.EnvironmentId);

        if (environment is null)
            return Result<TenantApplicationEnvironmentDto>.Failure(Error.NotFound(
                "TenantApplication.Environment.NotFound",
                "Environment not found"));

        var dto = TenantApplicationEnvironmentDto.FromDomain(environment);
        return Result<TenantApplicationEnvironmentDto>.Success(dto);
    }
}
```

---

## Definition CRUD for tenant applications (AppBuilder feature)

When a tenant has the AppBuilder feature enabled, the **AppBuilder UX** edits that tenant’s application by calling **TenantApplication** API (not AppBuilder API). TenantApplication must expose the following commands and queries, which mirror AppBuilder’s definition operations but are **tenant-scoped** and operate on `tenantapplication` schema (`tenant_*_definitions`, `tenant_application_releases`). All handlers must enforce: (1) tenant context, (2) “tenant has AppBuilder feature” check, (3) tenant application belongs to that tenant.

**Commands (to implement)**:
- **CreateCustomApplicationCommand** – Create a new custom tenant application (TenantApplication with `IsCustom = true`, Status = Draft); then the UI can add entities, pages, etc. via the commands below.
- **ForkPlatformApplicationCommand** – Fork an installed platform release into a custom tenant application (copy definitions into `tenant_*_definitions`).
- **CreateTenantEntityDefinitionCommand** / **UpdateTenantEntityDefinitionCommand** – CRUD for `tenant_entity_definitions` (scoped by TenantApplicationId).
- **CreateTenantPropertyDefinitionCommand** / **UpdateTenantPropertyDefinitionCommand** – CRUD for `tenant_property_definitions`.
- **CreateTenantRelationDefinitionCommand** / **UpdateTenantRelationDefinitionCommand** – CRUD for `tenant_relation_definitions`.
- **CreateTenantNavigationDefinitionCommand** / **UpdateTenantNavigationDefinitionCommand** – CRUD for `tenant_navigation_definitions`.
- **CreateTenantPageDefinitionCommand** / **UpdateTenantPageDefinitionCommand** – CRUD for `tenant_page_definitions`.
- **CreateTenantDataSourceDefinitionCommand** / **UpdateTenantDataSourceDefinitionCommand** – CRUD for `tenant_datasource_definitions`.
- **CreateTenantApplicationReleaseCommand** – Create an immutable release from current definitions (writes to `tenant_application_releases`; same shape as AppBuilder’s ApplicationRelease).

**Queries (to implement)**:
- **GetTenantApplicationDefinitionQuery** – Get tenant application by id (with or without nested definitions) for the current tenant.
- **GetTenantEntityDefinitionsQuery**, **GetTenantNavigationDefinitionsQuery**, **GetTenantPageDefinitionsQuery**, **GetTenantDataSourceDefinitionsQuery** – List definitions for a tenant application (by TenantApplicationId).

DTOs and repository interfaces for tenant-level definitions should mirror the AppBuilder module (e.g. TenantEntityDefinitionDto, ITenantEntityDefinitionRepository) so the same UI components can work against either AppBuilder or TenantApplication API by switching the base URL or client.

---

## DTOs

### TenantApplicationDto

```csharp
namespace Datarizen.TenantApplication.Application.DTOs;

public sealed record TenantApplicationDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? ApplicationId { get; init; }
    public int? Major { get; init; }
    public int? Minor { get; init; }
    public int? Patch { get; init; }
    public string Version => (Major is not null && Minor is not null && Patch is not null) ? $"{Major}.{Minor}.{Patch}" : string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsCustom { get; init; }
    public string Status { get; init; } = string.Empty;
    public Dictionary<string, object> Configuration { get; init; } = new();
    public DateTime? InstalledAt { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? DeactivatedAt { get; init; }
    public DateTime? UninstalledAt { get; init; }
    public List<TenantApplicationEnvironmentDto> Environments { get; init; } = new();

    public static TenantApplicationDto FromDomain(TenantApplication tenantApp)
    {
        return new TenantApplicationDto
        {
            Id = tenantApp.Id,
            TenantId = tenantApp.TenantId,
            ApplicationId = tenantApp.ApplicationId,
            Major = tenantApp.Major,
            Minor = tenantApp.Minor,
            Patch = tenantApp.Patch,
            Name = tenantApp.Name,
            Slug = tenantApp.Slug,
            Description = tenantApp.Description,
            IsCustom = tenantApp.IsCustom,
            Status = tenantApp.Status.ToString(),
            Configuration = tenantApp.Configuration,
            InstalledAt = tenantApp.InstalledAt,
            ActivatedAt = tenantApp.ActivatedAt,
            DeactivatedAt = tenantApp.DeactivatedAt,
            UninstalledAt = tenantApp.UninstalledAt,
            Environments = tenantApp.Environments
                .Select(TenantApplicationEnvironmentDto.FromDomain)
                .ToList()
        };
    }
}
```

### TenantApplicationEnvironmentDto

```csharp
namespace Datarizen.TenantApplication.Application.DTOs;

public sealed record TenantApplicationEnvironmentDto
{
    public Guid Id { get; init; }
    public Guid TenantApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string EnvironmentType { get; init; } = string.Empty;
    public Guid? ApplicationReleaseId { get; init; }
    public string? ReleaseVersion { get; init; }
    public bool IsActive { get; init; }
    public DateTime? DeployedAt { get; init; }
    public Guid? DeployedBy { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static TenantApplicationEnvironmentDto FromDomain(TenantApplicationEnvironment env)
    {
        return new TenantApplicationEnvironmentDto
        {
            Id = env.Id,
            TenantApplicationId = env.TenantApplicationId,
            Name = env.Name,
            EnvironmentType = env.EnvironmentType.ToString(),
            ApplicationReleaseId = env.ApplicationReleaseId,
            ReleaseVersion = env.ReleaseVersion,
            IsActive = env.IsActive,
            DeployedAt = env.DeployedAt,
            DeployedBy = env.DeployedBy,
            Configuration = env.Configuration,  // or from ConfigurationJson
            CreatedAt = env.CreatedAt,
            UpdatedAt = env.UpdatedAt
        };
    }
}
```

---

## Validators

### InstallApplicationCommandValidator

```csharp
namespace Datarizen.TenantApplication.Application.Commands.InstallApplication;

public sealed class InstallApplicationCommandValidator 
    : AbstractValidator<InstallApplicationCommand>
{
    public InstallApplicationCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("Application ID is required");

        RuleFor(x => x.Major)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Major version must be >= 0");

        RuleFor(x => x.Minor)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minor version must be >= 0");

        RuleFor(x => x.Patch)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Patch version must be >= 0");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Slug is required")
            .MaximumLength(100)
            .WithMessage("Slug cannot exceed 100 characters");
    }
}
```

### CreateEnvironmentCommandValidator

```csharp
namespace Datarizen.TenantApplication.Application.Commands.CreateEnvironment;

public sealed class CreateEnvironmentCommandValidator 
    : AbstractValidator<CreateEnvironmentCommand>
{
    public CreateEnvironmentCommandValidator()
    {
        RuleFor(x => x.TenantApplicationId)
            .NotEmpty()
            .WithMessage("Tenant application ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Environment name is required")
            .MaximumLength(100)
            .WithMessage("Environment name cannot exceed 100 characters");

        RuleFor(x => x.EnvironmentType)
            .NotEmpty()
            .WithMessage("Environment type is required")
            .Must(type => new[] { "Development", "Staging", "Production" }.Contains(type))
            .WithMessage("Environment type must be Development, Staging, or Production");
    }
}
```

---

## Application Services

### IApplicationReleaseValidator

```csharp
namespace Datarizen.TenantApplication.Application.Services;

public interface IApplicationReleaseValidator
{
    Task<bool> ValidateReleaseExistsAsync(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        CancellationToken cancellationToken = default);
}
```

---

## Service Registration

**File**: `TenantApplication.Application/DependencyInjection.cs`

```csharp
namespace Datarizen.TenantApplication.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantApplicationApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

---

## Success Criteria

- ✅ All commands use Major/Minor/Patch instead of Version string
- ✅ Configuration management per tenant and environment
- ✅ Application lifecycle commands (Install, Activate, Deactivate, Uninstall)
- ✅ Environment management commands
- ✅ DTOs updated to match new domain model
- ✅ Validators enforce version format
- ✅ All handlers use ITenantApplicationRepository
- ✅ Proper error handling for missing entities

---

## Dependencies

- `TenantApplication.Domain` - Entities, repositories, domain events
- `AppBuilder.Contracts` - ApplicationRelease, ApplicationSchema DTOs
- `AppRuntime.Contracts` - ICompatibilityChecker
- `Tenant.Contracts` - Tenant validation
- `BuildingBlocks.Kernel` - Result<T>, Error, Guard
- `BuildingBlocks.Application` - ICommand, IQuery, ICommandHandler, IQueryHandler
- `FluentValidation` - Input validation
- `MediatR` - CQRS implementation

---

## File Structure

```
TenantApplication.Application/
├── Commands/
│   ├── InstallApplication/
│   │   ├── InstallApplicationCommand.cs
│   │   ├── InstallApplicationCommandHandler.cs
│   │   └── InstallApplicationCommandValidator.cs
│   ├── UpdateConfiguration/
│   ├── UpgradeApplication/
│   ├── ActivateApplication/
│   ├── DeactivateApplication/
│   ├── UninstallApplication/
│   ├── CreateEnvironment/
│   │   ├── CreateEnvironmentCommand.cs
│   │   ├── CreateEnvironmentCommandHandler.cs
│   │   └── CreateEnvironmentCommandValidator.cs
│   └── UpdateEnvironmentConfiguration/
├── Queries/
│   ├── GetTenantApplication/
│   ├── GetTenantApplications/
│   ├── GetApplicationTenants/
│   └── GetEnvironment/
├── DTOs/
│   ├── TenantApplicationDto.cs
│   ├── TenantApplicationEnvironmentDto.cs
│   └── (TenantApplicationMigrationDto.cs when migrations are implemented)
├── Services/
│   └── IApplicationReleaseValidator.cs
├── Validators/
│   └── (per-command validators)
└── DependencyInjection.cs
```





