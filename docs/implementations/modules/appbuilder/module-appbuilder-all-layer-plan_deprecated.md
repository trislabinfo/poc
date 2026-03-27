# AppBuilder Module - Complete Implementation Plan

## Overview

The **AppBuilder** module enables administrators to build, configure, and manage custom applications within the Datarizen platform. It provides the infrastructure for creating application definitions, managing versions, configuring components, and defining application-specific settings.

### Purpose

- **Application Management**: Create and manage standalone application definitions
- **Version Control**: Track application versions and configurations
- **Component Configuration**: Define which modules/features are enabled per application
- **Settings Management**: Store application-specific configuration and metadata
- **Integration**: Connect with Feature module for feature flag management
- **Tenant Linking**: Applications are linked to tenants via a separate TenantApplication module

### Key Concepts

- **Application**: A tenant-scoped application definition with metadata and configuration
- **ApplicationRelease**: Immutable versioned snapshots of application configuration (replaces "ApplicationVersion")
- **NavigationComponent**: Navigation menu items for the application UI
- **PageComponent**: Page definitions with routes and content
- **ApplicationSetting**: Key-value configuration settings per application
- **Release Process**: Process of creating immutable application releases for deployment

### Module Dependencies

**Migration Dependencies**: `["Tenant", "Feature"]`

- **Tenant Module**: Applications are tenant-scoped (migration dependency)
- **Feature Module**: Applications can enable/disable features via components (migration dependency)
- **Identity Module**: User permissions for application management (runtime dependency only)
- **TenantApplication Module** (separate): Links applications to tenants (runtime dependency only, no migration dependency)

**Note**: AppBuilder does not have a migration dependency on TenantApplication. The TenantApplication module references AppBuilder's ApplicationRelease, but AppBuilder's migrations can run independently.

---

## Architecture

### Domain Model

```
Application (Aggregate Root)
├── Id: Guid
├── Name: string
├── Slug: string (unique globally)
├── Description: string
├── Status: ApplicationStatus (Draft, Released, Archived)
├── CurrentReleaseId: Guid? (FK to latest release)
├── CreatedAt: DateTime
├── UpdatedAt: DateTime
└── DomainEvents: List<IDomainEvent>

ApplicationRelease (Aggregate Root)
├── Id: Guid
├── ApplicationId: Guid (FK to appbuilder.applications)
├── Version: string (semver: 1.0.0, 1.1.0, 2.0.0)
├── ReleaseNotes: string
├── ReleasedAt: DateTime
├── ReleasedBy: Guid (UserId)
├── IsActive: bool (only one active release per application)
├── CreatedAt: DateTime
└── DomainEvents: List<IDomainEvent>

NavigationComponent
├── Id: Guid
├── ApplicationReleaseId: Guid (FK to appbuilder.application_releases)
├── Label: string (e.g., "Dashboard", "Reports")
├── Icon: string (e.g., "dashboard", "chart-bar")
├── Route: string (e.g., "/dashboard", "/reports")
├── ParentId: Guid? (for nested navigation)
├── DisplayOrder: int
├── IsVisible: bool
├── CreatedAt: DateTime

PageComponent
├── Id: Guid
├── ApplicationReleaseId: Guid (FK to appbuilder.application_releases)
├── Title: string (e.g., "Dashboard", "User Profile")
├── Route: string (e.g., "/dashboard", "/profile")
├── Layout: string (e.g., "default", "full-width", "sidebar")
├── Content: string (JSON - simple content definition)
├── CreatedAt: DateTime

ApplicationSetting
├── Id: Guid
├── ApplicationId: Guid (FK to appbuilder.applications)
├── Key: string
├── Value: string
├── DataType: SettingDataType (String, Number, Boolean, JSON)
├── IsEncrypted: bool
├── CreatedAt: DateTime
├── UpdatedAt: DateTime
```

### Release Process Workflow

```
1. CREATE APPLICATION (Draft)
   ↓
2. ADD NAVIGATION COMPONENTS
   ↓
3. ADD PAGE COMPONENTS
   ↓
4. RELEASE APPLICATION (v1.0.0)
   ├─ Creates ApplicationRelease
   ├─ Snapshots all components
   ├─ Sets Application.Status = Released
   ├─ Sets Application.CurrentReleaseId
   └─ Application becomes IMMUTABLE
   ↓
5. APPLICATION AVAILABLE TO TENANTS
   ↓
6. BUG FOUND?
   ├─ NO → Continue using
   └─ YES → Create new version
       ↓
       7. MODIFY APPLICATION (creates new draft)
          ↓
       8. FIX COMPONENTS
          ↓
       9. RELEASE NEW VERSION (v1.0.1 or v1.1.0)
          └─ Repeat from step 4
```

**Key Rules**:
- ✅ Application starts in **Draft** status
- ✅ Only **Draft** applications can be modified
- ✅ **Release** creates immutable snapshot (ApplicationRelease)
- ✅ After release, Application.Status = **Released** (immutable)
- ✅ To fix bugs, create new version and release again
- ✅ Only one **active** release per application at a time
- ✅ Components (Navigation, Page) belong to ApplicationRelease (not Application)

### Enums

```csharp
public enum ApplicationStatus
{
    Draft = 0,      // Can be modified
    Released = 1,   // Immutable, has active release
    Archived = 2    // No longer available
}

public enum SettingDataType
{
    String = 0,
    Number = 1,
    Boolean = 2,
    JSON = 3
}
```

---

## Phase 1: Domain Layer (10 hours)

### 1.1: Application Entity (3 hours)

**File**: `AppBuilder.Domain/Entities/Application.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppBuilder.Domain.Events;

namespace AppBuilder.Domain.Entities;

public sealed class Application : Entity<Guid>, IAggregateRoot
{
    private Application() { } // EF Core

    private Application(
        Guid id,
        string name,
        string slug,
        string description,
        ApplicationStatus status,
        DateTime createdAt)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Description = description;
        Status = status;
        CreatedAt = createdAt;
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ApplicationStatus Status { get; private set; }
    public Guid? CurrentReleaseId { get; private set; }

    public static Result<Application> Create(
        string name,
        string slug,
        string description,
        IDateTimeProvider dateTimeProvider)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 200)
            return Result<Application>.Failure(Error.Validation("Application.InvalidName", "Name must be between 3 and 200 characters"));

        if (string.IsNullOrWhiteSpace(slug) || !System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            return Result<Application>.Failure(Error.Validation("Application.InvalidSlug", "Slug must be lowercase kebab-case"));

        var application = new Application(
            Guid.NewGuid(),
            name,
            slug,
            description ?? string.Empty,
            ApplicationStatus.Draft,
            dateTimeProvider.UtcNow);

        application.AddDomainEvent(new ApplicationCreatedEvent(application.Id, application.Name));

        return Result<Application>.Success(application);
    }

    public Result<Unit> Update(string name, string description, IDateTimeProvider dateTimeProvider)
    {
        // Can only update Draft applications
        if (Status != ApplicationStatus.Draft)
            return Result<Unit>.Failure(Error.Validation("Application.CannotModifyReleased", "Cannot modify a released application. Create a new version instead."));

        if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 200)
            return Result<Unit>.Failure(Error.Validation("Application.InvalidName", "Name must be between 3 and 200 characters"));

        Name = name;
        Description = description ?? string.Empty;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new ApplicationUpdatedEvent(Id, Name));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> Release(Guid releaseId, IDateTimeProvider dateTimeProvider)
    {
        if (Status == ApplicationStatus.Archived)
            return Result<Unit>.Failure(Error.Validation("Application.CannotReleaseArchived", "Cannot release an archived application"));

        if (Status != ApplicationStatus.Draft)
            return Result<Unit>.Failure(Error.Validation("Application.AlreadyReleased", "Application is already released"));

        Status = ApplicationStatus.Released;
        CurrentReleaseId = releaseId;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new ApplicationReleasedEvent(Id, releaseId));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Application> CreateNewVersion(IDateTimeProvider dateTimeProvider)
    {
        if (Status != ApplicationStatus.Released)
            return Result<Application>.Failure(Error.Validation("Application.MustBeReleased", "Only released applications can have new versions created"));

        var newVersion = new Application(
            Guid.NewGuid(),
            Name,
            Slug,
            Description,
            ApplicationStatus.Draft,
            dateTimeProvider.UtcNow);

        newVersion.AddDomainEvent(new ApplicationNewVersionCreatedEvent(Id, newVersion.Id));

        return Result<Application>.Success(newVersion);
    }

    public Result<Unit> Archive(IDateTimeProvider dateTimeProvider)
    {
        Status = ApplicationStatus.Archived;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new ApplicationArchivedEvent(Id));

        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 1.2: ApplicationRelease Entity (3 hours)

**File**: `AppBuilder.Domain/Entities/ApplicationRelease.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppBuilder.Domain.Events;

namespace AppBuilder.Domain.Entities;

public sealed class ApplicationRelease : Entity<Guid>, IAggregateRoot
{
    private ApplicationRelease() { } // EF Core

    private ApplicationRelease(
        Guid id,
        Guid applicationId,
        string version,
        string releaseNotes,
        Guid releasedBy,
        DateTime releasedAt)
    {
        Id = id;
        ApplicationId = applicationId;
        Version = version;
        ReleaseNotes = releaseNotes;
        ReleasedBy = releasedBy;
        ReleasedAt = releasedAt;
        IsActive = true;
        CreatedAt = releasedAt;
    }

    public Guid ApplicationId { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public string ReleaseNotes { get; private set; } = string.Empty;
    public DateTime ReleasedAt { get; private set; }
    public Guid ReleasedBy { get; private set; }
    public bool IsActive { get; private set; }

    public static Result<ApplicationRelease> Create(
        Guid applicationId,
        string version,
        string releaseNotes,
        Guid releasedBy,
        IDateTimeProvider dateTimeProvider)
    {
        // Validate version format (semver)
        if (string.IsNullOrWhiteSpace(version) || !System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"))
            return Result<ApplicationRelease>.Failure(Error.Validation("ApplicationRelease.InvalidVersion", "Version must be in semver format (e.g., 1.0.0)"));

        var release = new ApplicationRelease(
            Guid.NewGuid(),
            applicationId,
            version,
            releaseNotes ?? string.Empty,
            releasedBy,
            dateTimeProvider.UtcNow);

        release.AddDomainEvent(new ApplicationReleasedEvent(applicationId, release.Id, version));

        return Result<ApplicationRelease>.Success(release);
    }

    public Result<Unit> Activate()
    {
        if (IsActive)
            return Result<Unit>.Failure(Error.Validation("ApplicationRelease.AlreadyActive", "Release is already active"));

        IsActive = true;
        AddDomainEvent(new ReleaseActivatedEvent(Id, ApplicationId));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> Deactivate()
    {
        if (!IsActive)
            return Result<Unit>.Failure(Error.Validation("ApplicationRelease.AlreadyInactive", "Release is already inactive"));

        IsActive = false;
        AddDomainEvent(new ReleaseDeactivatedEvent(Id, ApplicationId));

        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 1.3: NavigationComponent Entity (2 hours)

**File**: `AppBuilder.Domain/Entities/NavigationComponent.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppBuilder.Domain.Events;

namespace AppBuilder.Domain.Entities;

public sealed class NavigationComponent : Entity<Guid>
{
    private NavigationComponent() { } // EF Core

    private NavigationComponent(
        Guid id,
        Guid applicationReleaseId,
        string label,
        string icon,
        string route,
        Guid? parentId,
        int displayOrder,
        DateTime createdAt)
    {
        Id = id;
        ApplicationReleaseId = applicationReleaseId;
        Label = label;
        Icon = icon;
        Route = route;
        ParentId = parentId;
        DisplayOrder = displayOrder;
        IsVisible = true;
        CreatedAt = createdAt;
    }

    public Guid ApplicationReleaseId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsVisible { get; private set; }

    public static Result<NavigationComponent> Create(
        Guid applicationReleaseId,
        string label,
        string icon,
        string route,
        Guid? parentId,
        int displayOrder,
        IDateTimeProvider dateTimeProvider)
    {
        if (applicationReleaseId == Guid.Empty)
            return Result<NavigationComponent>.Failure(Error.Validation("NavigationComponent.InvalidReleaseId", "Application Release ID is required"));

        if (string.IsNullOrWhiteSpace(label) || label.Length < 1 || label.Length > 100)
            return Result<NavigationComponent>.Failure(Error.Validation("NavigationComponent.InvalidLabel", "Label must be between 1 and 100 characters"));

        if (string.IsNullOrWhiteSpace(route) || !route.StartsWith("/"))
            return Result<NavigationComponent>.Failure(Error.Validation("NavigationComponent.InvalidRoute", "Route must start with /"));

        var component = new NavigationComponent(
            Guid.NewGuid(),
            applicationReleaseId,
            label,
            icon ?? string.Empty,
            route,
            parentId,
            displayOrder,
            dateTimeProvider.UtcNow);

        component.AddDomainEvent(new NavigationComponentCreatedEvent(component.Id, applicationReleaseId, label));

        return Result<NavigationComponent>.Success(component);
    }

    public Result<Unit> UpdateDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
            return Result<Unit>.Failure(Error.Validation("NavigationComponent.InvalidDisplayOrder", "Display order must be >= 0"));

        DisplayOrder = displayOrder;
        return Result<Unit>.Success(Unit.Value);
    }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
}
```

---

### 1.4: PageComponent Entity (2 hours)

**File**: `AppBuilder.Domain/Entities/PageComponent.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;
using AppBuilder.Domain.Events;

namespace AppBuilder.Domain.Entities;

public sealed class PageComponent : Entity<Guid>
{
    private PageComponent() { } // EF Core

    private PageComponent(
        Guid id,
        Guid applicationReleaseId,
        string title,
        string route,
        string layout,
        string content,
        DateTime createdAt)
    {
        Id = id;
        ApplicationReleaseId = applicationReleaseId;
        Title = title;
        Route = route;
        Layout = layout;
        Content = content;
        CreatedAt = createdAt;
    }

    public Guid ApplicationReleaseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;
    public string Layout { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty; // JSON

    public static Result<PageComponent> Create(
        Guid applicationReleaseId,
        string title,
        string route,
        string layout,
        string content,
        IDateTimeProvider dateTimeProvider)
    {
        if (applicationReleaseId == Guid.Empty)
            return Result<PageComponent>.Failure(Error.Validation("PageComponent.InvalidReleaseId", "Application Release ID is required"));

        if (string.IsNullOrWhiteSpace(title) || title.Length < 1 || title.Length > 200)
            return Result<PageComponent>.Failure(Error.Validation("PageComponent.InvalidTitle", "Title must be between 1 and 200 characters"));

        if (string.IsNullOrWhiteSpace(route) || !route.StartsWith("/"))
            return Result<PageComponent>.Failure(Error.Validation("PageComponent.InvalidRoute", "Route must start with /"));

        if (string.IsNullOrWhiteSpace(layout))
            return Result<PageComponent>.Failure(Error.Validation("PageComponent.InvalidLayout", "Layout is required"));

        var component = new PageComponent(
            Guid.NewGuid(),
            applicationReleaseId,
            title,
            route,
            layout,
            content ?? "{}",
            dateTimeProvider.UtcNow);

        component.AddDomainEvent(new PageComponentCreatedEvent(component.Id, applicationReleaseId, title));

        return Result<PageComponent>.Success(component);
    }

    public Result<Unit> UpdateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result<Unit>.Failure(Error.Validation("PageComponent.InvalidContent", "Content cannot be empty"));

        Content = content;
        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> UpdateLayout(string layout)
    {
        if (string.IsNullOrWhiteSpace(layout))
            return Result<Unit>.Failure(Error.Validation("PageComponent.InvalidLayout", "Layout cannot be empty"));

        Layout = layout;
        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 1.5: ApplicationSetting Entity (1.5 hours)

**File**: `AppBuilder.Domain/Entities/ApplicationSetting.cs`

```csharp
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Kernel.Time;

namespace AppBuilder.Domain.Entities;

public sealed class ApplicationSetting : Entity<Guid>
{
    private ApplicationSetting() { } // EF Core

    private ApplicationSetting(
        Guid id,
        Guid applicationId,
        string key,
        string value,
        SettingDataType dataType,
        bool isEncrypted,
        DateTime createdAt)
    {
        Id = id;
        ApplicationId = applicationId;
        Key = key;
        Value = value;
        DataType = dataType;
        IsEncrypted = isEncrypted;
        CreatedAt = createdAt;
    }

    public Guid ApplicationId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public SettingDataType DataType { get; private set; }
    public bool IsEncrypted { get; private set; }

    public static Result<ApplicationSetting> Create(
        Guid applicationId,
        string key,
        string value,
        SettingDataType dataType,
        bool isEncrypted,
        IDateTimeProvider dateTimeProvider)
    {
        if (applicationId == Guid.Empty)
            return Result<ApplicationSetting>.Failure(Error.Validation("ApplicationSetting.InvalidApplicationId", "Application ID is required"));

        if (string.IsNullOrWhiteSpace(key) || key.Length < 2 || key.Length > 100)
            return Result<ApplicationSetting>.Failure(Error.Validation("ApplicationSetting.InvalidKey", "Key must be between 2 and 100 characters"));

        var setting = new ApplicationSetting(
            Guid.NewGuid(),
            applicationId,
            key,
            value ?? string.Empty,
            dataType,
            isEncrypted,
            dateTimeProvider.UtcNow);

        return Result<ApplicationSetting>.Success(setting);
    }

    public Result<Unit> UpdateValue(string value, IDateTimeProvider dateTimeProvider)
    {
        Value = value ?? string.Empty;
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 1.6: Domain Events (1 hour)

**File**: `AppBuilder.Domain/Events/ApplicationCreatedEvent.cs`

```csharp
using BuildingBlocks.Kernel.Domain;

namespace AppBuilder.Domain.Events;

public sealed record ApplicationCreatedEvent(
    Guid ApplicationId,
    string Name) : IDomainEvent;
```

**File**: `AppBuilder.Domain/Events/ApplicationUpdatedEvent.cs`

```csharp
public sealed record ApplicationUpdatedEvent(
    Guid ApplicationId,
    string Name) : IDomainEvent;
```

**File**: `AppBuilder.Domain/Events/ApplicationReleasedEvent.cs`

```csharp
public sealed record ApplicationReleasedEvent(
    Guid ApplicationId,
    Guid ReleaseId,
    string Version) : IDomainEvent;
```

**File**: `AppBuilder.Domain/Events/ApplicationNewVersionCreatedEvent.cs`

```csharp
public sealed record ApplicationNewVersionCreatedEvent(
    Guid OriginalApplicationId,
    Guid NewApplicationId) : IDomainEvent;
```

**File**: `AppBuilder.Domain/Events/ApplicationArchivedEvent.cs`

```csharp
public sealed record ApplicationArchivedEvent(
    Guid ApplicationId) : IDomainEvent;
```

**File**: `AppBuilder.Domain/Events/ReleaseActivatedEvent.cs`

```csharp
public sealed record ReleaseActivatedEvent(
    Guid ReleaseId,
    Guid ApplicationId) : IDomainEvent;
```

**File**: `AppBuilder.Domain/Events/ReleaseDeactivatedEvent.cs`

```csharp
public sealed record ReleaseDeactivatedEvent(
    Guid ReleaseId,
    Guid ApplicationId) : IDomainEvent;
```

**File**: `AppBuilder.Domain/Events/NavigationComponentCreatedEvent.cs`

```csharp
public sealed record NavigationComponentCreatedEvent(
    Guid ComponentId,
    Guid ApplicationReleaseId,
    string Label) : IDomainEvent;
```

**File**: `AppBuilder.Domain/Events/PageComponentCreatedEvent.cs`

```csharp
public sealed record PageComponentCreatedEvent(
    Guid ComponentId,
    Guid ApplicationReleaseId,
    string Title) : IDomainEvent;
```

---

### 1.7: Repository Interfaces (1.5 hours)

**File**: `AppBuilder.Domain/Repositories/IApplicationRepository.cs`

```csharp
using BuildingBlocks.Kernel.Persistence;
using AppBuilder.Domain.Entities;

namespace AppBuilder.Domain.Repositories;

public interface IApplicationRepository : IRepository<Application, Guid>
{
    Task<Application?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Application>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Application>> GetByStatusAsync(ApplicationStatus status, CancellationToken cancellationToken = default);
}
```

**File**: `AppBuilder.Domain/Repositories/IApplicationReleaseRepository.cs`

```csharp
public interface IApplicationReleaseRepository : IRepository<ApplicationRelease, Guid>
{
    Task<IEnumerable<ApplicationRelease>> GetAllByApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<ApplicationRelease?> GetByVersionAsync(Guid applicationId, string version, CancellationToken cancellationToken = default);
    Task<ApplicationRelease?> GetActiveReleaseAsync(Guid applicationId, CancellationToken cancellationToken = default);
}
```

**File**: `AppBuilder.Domain/Repositories/INavigationComponentRepository.cs`

```csharp
public interface INavigationComponentRepository : IRepository<NavigationComponent, Guid>
{
    Task<IEnumerable<NavigationComponent>> GetAllByReleaseAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default);
    Task<NavigationComponent?> GetByRouteAsync(Guid applicationReleaseId, string route, CancellationToken cancellationToken = default);
}
```

**File**: `AppBuilder.Domain/Repositories/IPageComponentRepository.cs`

```csharp
public interface IPageComponentRepository : IRepository<PageComponent, Guid>
{
    Task<IEnumerable<PageComponent>> GetAllByReleaseAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default);
    Task<PageComponent?> GetByRouteAsync(Guid applicationReleaseId, string route, CancellationToken cancellationToken = default);
}
```

**File**: `AppBuilder.Domain/Repositories/IApplicationSettingRepository.cs`

```csharp
public interface IApplicationSettingRepository : IRepository<ApplicationSetting, Guid>
{
    Task<IEnumerable<ApplicationSetting>> GetAllByApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<ApplicationSetting?> GetByKeyAsync(Guid applicationId, string key, CancellationToken cancellationToken = default);
}
```

**File**: `AppBuilder.Domain/Repositories/IAppBuilderUnitOfWork.cs`

```csharp
using BuildingBlocks.Kernel.Persistence;

namespace AppBuilder.Domain.Repositories;

public interface IAppBuilderUnitOfWork : IUnitOfWork
{
}
```

**Deliverable**: Domain layer complete with entities, events, repository interfaces, and tests.

---

## Phase 2: Application Layer (12 hours)

### 2.1: DTOs (1.5 hours)

**File**: `AppBuilder.Application/DTOs/ApplicationDto.cs`

```csharp
namespace AppBuilder.Application.DTOs;

public sealed record ApplicationDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    ApplicationStatus Status,
    Guid? CurrentReleaseId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

**File**: `AppBuilder.Application/DTOs/ApplicationReleaseDto.cs`

```csharp
public sealed record ApplicationReleaseDto(
    Guid Id,
    Guid ApplicationId,
    string Version,
    string ReleaseNotes,
    Guid ReleasedBy,
    DateTime ReleasedAt,
    bool IsActive,
    DateTime CreatedAt);
```

**File**: `AppBuilder.Application/DTOs/NavigationComponentDto.cs`

```csharp
public sealed record NavigationComponentDto(
    Guid Id,
    Guid ApplicationReleaseId,
    string Label,
    string Icon,
    string Route,
    Guid? ParentId,
    int DisplayOrder,
    bool IsVisible,
    DateTime CreatedAt);
```

**File**: `AppBuilder.Application/DTOs/PageComponentDto.cs`

```csharp
public sealed record PageComponentDto(
    Guid Id,
    Guid ApplicationReleaseId,
    string Title,
    string Route,
    string Layout,
    string Content,
    DateTime CreatedAt);
```

**File**: `AppBuilder.Application/DTOs/ApplicationSettingDto.cs`

```csharp
public sealed record ApplicationSettingDto(
    Guid Id,
    Guid ApplicationId,
    string Key,
    string Value,
    SettingDataType DataType,
    bool IsEncrypted,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

---

### 2.2: Commands - Application (4 hours)

**File**: `AppBuilder.Application/Commands/Applications/CreateApplication/CreateApplicationCommand.cs`

```csharp
public sealed record CreateApplicationCommand(
    string Name,
    string Slug,
    string Description) : ICommand<Guid>;
```

**File**: `AppBuilder.Application/Commands/Applications/CreateApplication/CreateApplicationCommandHandler.cs`

```csharp
public sealed class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, Result<Guid>>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Guid>> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        // Check if slug already exists globally
        var exists = await _applicationRepository.SlugExistsAsync(request.Slug, cancellationToken);
        if (exists)
            return Result<Guid>.Failure(Error.Conflict("Application.SlugAlreadyExists", "An application with this slug already exists"));

        // Create application
        var applicationResult = Application.Create(
            request.Name,
            request.Slug,
            request.Description,
            _dateTimeProvider);

        if (applicationResult.IsFailure)
            return Result<Guid>.Failure(applicationResult.Error);

        await _applicationRepository.AddAsync(applicationResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(applicationResult.Value.Id);
    }
}
```

**File**: `AppBuilder.Application/Commands/Applications/UpdateApplication/UpdateApplicationCommand.cs`

```csharp
public sealed record UpdateApplicationCommand(
    Guid Id,
    string Name,
    string Description) : ICommand<Unit>;
```

**File**: `AppBuilder.Application/Commands/Applications/ReleaseApplication/ReleaseApplicationCommand.cs`

```csharp
public sealed record ReleaseApplicationCommand(
    Guid ApplicationId,
    string Version,
    string ReleaseNotes,
    Guid ReleasedBy) : ICommand<Guid>;
```

**File**: `AppBuilder.Application/Commands/Applications/ReleaseApplication/ReleaseApplicationCommandHandler.cs`

```csharp
public sealed class ReleaseApplicationCommandHandler : IRequestHandler<ReleaseApplicationCommand, Result<Guid>>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationReleaseRepository _releaseRepository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Guid>> Handle(ReleaseApplicationCommand request, CancellationToken cancellationToken)
    {
        // Get application
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return Result<Guid>.Failure(Error.NotFound("Application.NotFound", "Application not found"));

        // Create release
        var releaseResult = ApplicationRelease.Create(
            request.ApplicationId,
            request.Version,
            request.ReleaseNotes,
            request.ReleasedBy,
            _dateTimeProvider);

        if (releaseResult.IsFailure)
            return Result<Guid>.Failure(releaseResult.Error);

        // Release application
        var applicationReleaseResult = application.Release(releaseResult.Value.Id, _dateTimeProvider);
        if (applicationReleaseResult.IsFailure)
            return Result<Guid>.Failure(applicationReleaseResult.Error);

        await _releaseRepository.AddAsync(releaseResult.Value, cancellationToken);
        _applicationRepository.Update(application);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(releaseResult.Value.Id);
    }
}
```

**File**: `AppBuilder.Application/Commands/Applications/ArchiveApplication/ArchiveApplicationCommand.cs`

```csharp
public sealed record ArchiveApplicationCommand(Guid Id) : ICommand<Unit>;
```

---

### 2.3: Commands - ApplicationComponent (2 hours)

**File**: `AppBuilder.Application/Commands/Components/AddComponent/AddComponentCommand.cs`

```csharp
public sealed record AddComponentCommand(
    Guid ApplicationId,
    ComponentType ComponentType,
    string ComponentCode,
    bool IsEnabled,
    string? Configuration,
    int DisplayOrder) : ICommand<Guid>;
```

**File**: `AppBuilder.Application/Commands/Components/ToggleComponent/ToggleComponentCommand.cs`

```csharp
public sealed record ToggleComponentCommand(Guid Id) : ICommand<Unit>;
```

---

### 2.4: Queries (2.5 hours)

**File**: `AppBuilder.Application/Queries/Applications/GetApplicationById/GetApplicationByIdQuery.cs`

```csharp
public sealed record GetApplicationByIdQuery(Guid Id) : IQuery<ApplicationDto>;
```

**File**: `AppBuilder.Application/Queries/Applications/GetAllApplications/GetAllApplicationsQuery.cs`

```csharp
public sealed record GetAllApplicationsQuery() : IQuery<IEnumerable<ApplicationDto>>;
```

**File**: `AppBuilder.Application/Queries/Releases/GetReleasesByApplication/GetReleasesByApplicationQuery.cs`

```csharp
public sealed record GetReleasesByApplicationQuery(Guid ApplicationId) : IQuery<IEnumerable<ApplicationReleaseDto>>;
```

**File**: `AppBuilder.Application/Queries/Components/GetComponentsByApplication/GetComponentsByApplicationQuery.cs`

```csharp
public sealed record GetComponentsByApplicationQuery(Guid ApplicationId) : IQuery<IEnumerable<ApplicationComponentDto>>;
```

**Note**: `ApplicationComponentDto` is a union DTO that represents both NavigationComponent and PageComponent entities. The DTO includes a `ComponentType` discriminator field to distinguish between the two types. The query handler retrieves both NavigationComponents and PageComponents from the database and maps them to the unified DTO.

**Alternative Approach**: For type-safe queries, consider using separate queries:
- `GetNavigationComponentsByApplicationQuery` → `IEnumerable<NavigationComponentDto>`
- `GetPageComponentsByApplicationQuery` → `IEnumerable<PageComponentDto>`

**Deliverable**: Application layer complete with commands, queries, DTOs, and handlers.

---

## Phase 3: Infrastructure Layer (8 hours)

### 3.1: DbContext (2 hours)

**File**: `AppBuilder.Infrastructure/Data/AppBuilderDbContext.cs`

```csharp
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Data;

public class AppBuilderDbContext : BaseModuleDbContext
{
    public AppBuilderDbContext(DbContextOptions<AppBuilderDbContext> options)
        : base(options)
    {
    }

    protected override string SchemaName => "appbuilder";

    public DbSet<AppBuilder.Domain.Entities.Application> Applications => Set<AppBuilder.Domain.Entities.Application>();
    public DbSet<AppBuilder.Domain.Entities.ApplicationRelease> ApplicationReleases => Set<AppBuilder.Domain.Entities.ApplicationRelease>();
    public DbSet<AppBuilder.Domain.Entities.NavigationComponent> NavigationComponents => Set<AppBuilder.Domain.Entities.NavigationComponent>();
    public DbSet<AppBuilder.Domain.Entities.PageComponent> PageComponents => Set<AppBuilder.Domain.Entities.PageComponent>();
    public DbSet<AppBuilder.Domain.Entities.ApplicationSetting> ApplicationSettings => Set<AppBuilder.Domain.Entities.ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppBuilderDbContext).Assembly);
    }
}
```

---

### 3.2: Entity Configurations (3 hours)

**File**: `AppBuilder.Infrastructure/Data/Configurations/ApplicationConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppBuilder.Infrastructure.Data.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<AppBuilder.Domain.Entities.Application>
{
    public void Configure(EntityTypeBuilder<AppBuilder.Domain.Entities.Application> builder)
    {
        builder.ToTable("applications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id").IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(a => a.Status).HasColumnName("status").IsRequired();
        builder.Property(a => a.CurrentReleaseId).HasColumnName("current_release_id");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(a => a.Slug)
            .HasDatabaseName("uq_applications_slug")
            .IsUnique();

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("ix_applications_status");
    }
}
```

(Similar configurations for ApplicationRelease, NavigationComponent, PageComponent, ApplicationSetting)

---

### 3.3: Repository Implementations (2 hours)

**File**: `AppBuilder.Infrastructure/Repositories/ApplicationRepository.cs`

```csharp
using BuildingBlocks.Infrastructure.Persistence;
using AppBuilder.Domain.Repositories;
using AppBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class ApplicationRepository : Repository<AppBuilder.Domain.Entities.Application, Guid>, IApplicationRepository
{
    private readonly AppBuilderDbContext _context;

    public ApplicationRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<AppBuilder.Domain.Entities.Application?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .AnyAsync(a => a.Slug == slug, cancellationToken);
    }

    public async Task<IEnumerable<AppBuilder.Domain.Entities.Application>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AppBuilder.Domain.Entities.Application>> GetByStatusAsync(ApplicationStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .Where(a => a.Status == status)
            .ToListAsync(cancellationToken);
    }
}
```

**Deliverable**: Infrastructure layer complete with DbContext, configurations, repositories.

---

## Phase 4: API Layer (6 hours)

### 4.1: Application Controller (3 hours)

**File**: `AppBuilder.Api/Controllers/ApplicationController.cs`

```csharp
using AppBuilder.Application.Commands.Applications.CreateApplication;
using AppBuilder.Application.Commands.Applications.UpdateApplication;
using AppBuilder.Application.Commands.Applications.PublishApplication;
using AppBuilder.Application.Commands.Applications.ArchiveApplication;
using AppBuilder.Application.Queries.Applications.GetApplicationById;
using AppBuilder.Application.Queries.Applications.GetApplicationsByTenant;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

[ApiController]
[Route("api/appbuilder/applications")]
public sealed class ApplicationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("tenant/{tenantId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var query = new GetApplicationsByTenantQuery(tenantId);
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(statusCode: 500, detail: result.Error.Message);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetApplicationByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error.Message });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateApplicationCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
                ErrorType.Conflict => Conflict(new { error = result.Error.Message }),
                _ => Problem(statusCode: 500, detail: result.Error.Message)
            };
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPost("{id:guid}/release")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Release(Guid id, [FromBody] ReleaseRequest request, CancellationToken cancellationToken)
    {
        var command = new ReleaseApplicationCommand(id, request.Version, request.ReleaseNotes, request.ReleasedBy);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { releaseId = result.Value }) : NotFound(new { error = result.Error.Message });
    }

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var command = new ArchiveApplicationCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error.Message });
    }
}

public sealed record ReleaseRequest(string Version, string ReleaseNotes, Guid ReleasedBy);
```

**Deliverable**: API layer complete with controllers for Applications, Components, Settings.

---

## Phase 5: Migrations Layer (4 hours)

### 5.1: Create AppBuilder Schema

**File**: `AppBuilder.Migrations/Migrations/Schema/20260211200000_CreateAppBuilderSchema.cs`

```csharp
using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260211200000, "Create appbuilder schema")]
public class CreateAppBuilderSchema : Migration
{
    public override void Up()
    {
        Create.Schema("appbuilder");
    }

    public override void Down()
    {
        Delete.Schema("appbuilder");
    }
}
```

### 5.2: Create Applications Table

**File**: `AppBuilder.Migrations/Migrations/Schema/20260211201000_CreateApplicationsTable.cs`

```csharp
using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260211201000, "Create applications table")]
public class CreateApplicationsTable : Migration
{
    public override void Up()
    {
        Create.Table("applications")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_applications")
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("current_release_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.UniqueConstraint("uq_applications_slug")
            .OnTable("applications")
            .WithSchema("appbuilder")
            .Column("slug");

        Create.Index("ix_applications_status")
            .OnTable("applications")
            .WithSchema("appbuilder")
            .OnColumn("status");
    }

    public override void Down()
    {
        Delete.Table("applications").InSchema("appbuilder");
    }
}
```

### 5.3: Create ApplicationReleases, NavigationComponents, PageComponents, ApplicationSettings Tables

(Similar migrations for other tables with foreign keys to applications table)

**Note**: The `applications` table uses `current_release_id` column (not `current_version_id`) to reference the latest released version of the application.

**Deliverable**: Migrations layer complete with schema and table migrations.

---

## Phase 6: Module Composition (2 hours)

### 6.1: AppBuilderModule

**File**: `AppBuilder.Module/AppBuilderModule.cs`

```csharp
using BuildingBlocks.Web.Modules;
using AppBuilder.Api.Controllers;
using AppBuilder.Application;
using AppBuilder.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppBuilder.Module;

public sealed class AppBuilderModule : IModule
{
    public string ModuleName => "AppBuilder";
    public string SchemaName => "appbuilder";

    public string[] GetMigrationDependencies() => ["Feature"];

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAppBuilderApplication();
        services.AddAppBuilderInfrastructure(configuration, SchemaName);
        services.AddControllers()
            .AddApplicationPart(typeof(ApplicationController).Assembly);

        return services;
    }

    public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
    {
        return app;
    }
}
```

### 6.2: Create AppBuilderServiceHost for Microservices

**File**: `Hosts/AppBuilderServiceHost/AppBuilder.Service.Host.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>AppBuilder.Service.Host</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\..\\BuildingBlocks\\Web\\BuildingBlocks.Web.csproj" />
    <ProjectReference Include="..\\..\\Product\\AppBuilder\\AppBuilder.Module\\AppBuilder.Module.csproj" />
    <ProjectReference Include="..\\..\\ServiceDefaults\\ServiceDefaults.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" />
  </ItemGroup>
</Project>
```

**File**: `Hosts/AppBuilderServiceHost/Program.cs`

```csharp
using BuildingBlocks.Web.Extensions;
using AppBuilder.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddBuildingBlocks();
builder.AddBuildingBlocksHealthChecks();

builder.Services.AddModule<AppBuilderModule>(builder.Configuration);

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

app.UseModule<AppBuilderModule>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

await app.RunAsync();
```

**Deliverable**: Module composition complete with AppBuilderModule and AppBuilderServiceHost.

---

## Phase 7: Integration (2 hours)

### 7.1: TenantApplication Module (Separate Module)

**IMPORTANT**: Applications are **standalone entities** in the AppBuilder module. They are NOT directly linked to tenants.

- A separate **TenantApplication** module will handle the many-to-many relationship between Tenants and Applications
- This allows applications to be:
  - Shared across multiple tenants
  - Managed independently of tenant lifecycle
  - Reused and templated
- The TenantApplication module will contain:
  - `TenantApplication` entity with `TenantId` and `ApplicationId`
  - Tenant-specific application configuration overrides
  - Tenant-application permissions and access control

### 7.2: Feature Integration

- Applications can enable/disable features via ApplicationComponent
- Feature flags evaluated at runtime based on application configuration
- Integration via `IFeatureEvaluationService` from Feature.Contracts

### 7.3: Contracts for Inter-Module Communication

**File**: `AppBuilder.Contracts/DTOs/ApplicationConfigurationDto.cs`

```csharp
namespace AppBuilder.Contracts.DTOs;

public sealed record ApplicationConfigurationDto(
    Guid ApplicationId,
    string Name,
    string Slug,
    Dictionary<string, string> Settings,
    List<string> EnabledComponents);
```

**File**: `AppBuilder.Contracts/Services/IApplicationConfigurationService.cs`

```csharp
namespace AppBuilder.Contracts.Services;

public interface IApplicationConfigurationService
{
    Task<ApplicationConfigurationDto?> GetApplicationConfigurationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}
```

**Deliverable**: Integration complete with Feature and Identity modules. Tenant-Application linking handled by separate TenantApplication module.

---

## Phase 8: Testing & Validation (4 hours)

### Success Criteria Checklist

- [x] Application entity with validation and factory method
- [x] ApplicationRelease, NavigationComponent, PageComponent, ApplicationSetting entities
- [x] Domain events for all state changes
- [x] Repository interfaces and implementations
- [x] CRUD commands and queries (using Release terminology, not Publish)
- [x] DTOs and mappers
- [x] FluentValidation validators
- [x] AppBuilderDbContext with schema configuration
- [x] Entity configurations for EF Core
- [x] ApplicationController with CRUD endpoints
- [x] Migrations for schema and tables
- [x] AppBuilderModule implements IModule
- [x] AppBuilderServiceHost for microservices topology
- [x] Integration with Feature module (Tenant linking via separate TenantApplication module)

---

## Deployment Topology Support

### Monolith
- ✅ All modules in single process
- ✅ Single database with `appbuilder` schema

### MultiApp
- ✅ AppBuilder module in `MultiAppAppBuilderHost`
- ✅ Shared database with `appbuilder` schema
- ✅ API Gateway routes `/api/appbuilder/*` to AppBuilder host

### Microservices
- ✅ Dedicated `AppBuilderServiceHost`
- ✅ Can use separate database (or shared with schema isolation)
- ✅ HTTP/gRPC communication via service discovery

---

## Estimated Timeline

| Phase | Description | Time |
|-------|-------------|------|
| Phase 1 | Domain Layer | 10 hours |
| Phase 2 | Application Layer | 12 hours |
| Phase 3 | Infrastructure Layer | 8 hours |
| Phase 4 | API Layer | 6 hours |
| Phase 5 | Migrations Layer | 4 hours |
| Phase 6 | Module Composition | 2 hours |
| Phase 7 | Integration | 2 hours |
| Phase 8 | Testing & Validation | 4 hours |
| **Total** | **Complete Vertical Slice** | **48 hours** |

---

## Next Steps

After completing the AppBuilder module:

1. **Create AppRuntime Module** - For running applications with runtime configuration
2. **Update AppHost** - Add AppBuilderServiceHost to microservices topology
3. **Integration Testing** - Test application building and configuration across all topologies
4. **Documentation** - Update API documentation and user guides

---

## Notes

- **Application Slugs**: Use lowercase-kebab-case for consistency (e.g., `my-custom-app`)
- **Versioning**: Use semantic versioning (e.g., `1.0.0`, `1.1.0`, `2.0.0`)
- **Configuration**: Store application configuration as JSON in the `configuration` column
- **Components**: Link to Feature module for feature flag management
- **Settings**: Support encrypted settings for sensitive data (API keys, secrets)
- **Migration Dependencies**: AppBuilder module depends on Feature module only (Tenant linking via separate TenantApplication module)


