# Feature Module - Complete Vertical Implementation Plan

**Status**: 🆕 New - Complete Vertical Slice (All Layers)  
**Last Updated**: 2026-02-11  
**Estimated Total Time**: ~40 hours  
**Related Documents**: 
- `docs/implementations/module-tenant-tenant-entity-plan.md` (Reference implementation)
- `docs/implementations/module-identity-domain-layer-plan.md` (Domain patterns)
- `docs/implementations/module-identity-infrastructure-layer-plan.md` (Infrastructure patterns)
- `docs/ai-context/05-MODULES.md` (Module architecture)
- `docs/ai-context/07-DB-MIGRATIONS.md` (Migration patterns)

---

## Overview

The **Feature module** manages feature definitions and feature flags with **tenant-scoped configuration**. This enables runtime feature toggles, A/B testing, gradual rollouts, and tenant-specific feature enablement.

**Philosophy**: 
- ✅ Features are global definitions (e.g., "advanced-analytics", "custom-branding")
- ✅ FeatureFlags are tenant-scoped instances (e.g., Tenant A has "advanced-analytics" enabled, Tenant B does not)
- ✅ Support hierarchical feature flags (global → tenant → user)
- ✅ Integration with Tenant module for multi-tenancy
- ✅ Clean separation of concerns across all layers

**Success Criteria**:
- ✅ Complete CRUD operations for Features and FeatureFlags
- ✅ Tenant-scoped feature flag evaluation
- ✅ User-scoped feature flag overrides
- ✅ All layers implemented (Domain → Application → Infrastructure → API → Migrations)
- ✅ Full vertical integration with Tenant module
- ✅ Works in all three deployment topologies (Monolith, MultiApp, Microservices)

---

## Module Dependencies

**Migration Dependencies**: `["Tenant"]`  
**Runtime Dependencies**: 
- `Tenant.Contracts` (for TenantId validation and tenant context)
- `BuildingBlocks.Kernel` (base classes, Result<T>, Guard)
- `BuildingBlocks.Infrastructure` (Repository, UnitOfWork, IDateTimeProvider)

**Schema Name**: `feature`

---

## Domain Model

### Entity Relationship

```
Feature (1) ──────< (many) FeatureFlag
   │                          │
   │                          ├─ TenantId (nullable)
   │                          ├─ UserId (nullable)
   │                          └─ IsEnabled
   │
   ├─ Code (unique)
   ├─ Name
   ├─ Category
   └─ IsGloballyEnabled
```

**Relationship Explanation**:
- One **Feature** (e.g., "advanced-analytics") can have **many FeatureFlags**
- Each **FeatureFlag** references exactly one **Feature** via `FeatureId`
- FeatureFlags provide tenant-specific or user-specific overrides
- Example: Feature "advanced-analytics" might have:
  - FeatureFlag for Tenant A (enabled)
  - FeatureFlag for Tenant B (disabled)
  - FeatureFlag for User X in Tenant A (disabled - user override)

### Entities

#### 1. Feature (Aggregate Root)
**Purpose**: Global feature definition that can be enabled/disabled per tenant.

**Relationship**: A Feature has **one-to-many** relationship with FeatureFlag entities. Each Feature can have zero or more FeatureFlags (one per tenant/user scope).

**Properties**:
- `Id` (Guid) - Primary key
- `Code` (string) - Unique feature code (e.g., "advanced-analytics", "custom-branding")
- `Name` (string) - Display name
- `Description` (string) - Feature description
- `Category` (string) - Feature category (e.g., "Analytics", "Customization", "Integration")
- `IsGloballyEnabled` (bool) - Default state for new tenants
- `CreatedAt` (DateTime) - Inherited from Entity<Guid>
- `UpdatedAt` (DateTime?) - Inherited from Entity<Guid>

**Validation Rules**:
- Code: Required, 3-100 characters, lowercase-kebab-case format `^[a-z0-9]+(?:-[a-z0-9]+)*$`
- Name: Required, 3-200 characters
- Description: Optional, max 1000 characters
- Category: Required, 3-100 characters

**Factory Method**:
```csharp
public static Result<Feature> Create(
    string code,
    string name,
    string description,
    string category,
    bool isGloballyEnabled,
    IDateTimeProvider dateTimeProvider)
```

**Domain Events**:
- `FeatureCreatedEvent(Guid FeatureId, string Code, string Name, DateTime OccurredOn)`
- `FeatureUpdatedEvent(Guid FeatureId, string Code, DateTime OccurredOn)`
- `FeatureDeletedEvent(Guid FeatureId, string Code, DateTime OccurredOn)`

---

#### 2. FeatureFlag (Aggregate Root)
**Purpose**: Tenant-scoped (or user-scoped) feature flag instance. Multiple FeatureFlags can reference the same Feature (one per tenant/user).

**Properties**:
- `Id` (Guid) - Primary key
- `FeatureId` (Guid) - Foreign key to Feature
- `TenantId` (Guid?) - Nullable: null = global override, non-null = tenant-specific
- `UserId` (Guid?) - Nullable: null = tenant-level, non-null = user-specific override
- `IsEnabled` (bool) - Whether the feature is enabled for this scope
- `Configuration` (string?) - Optional JSON configuration for the feature
- `CreatedAt` (DateTime) - Inherited from Entity<Guid>
- `UpdatedAt` (DateTime?) - Inherited from Entity<Guid>

**Validation Rules**:
- FeatureId: Required, must be valid Guid
- TenantId and UserId: At least one must be null (cannot be both user-scoped AND tenant-scoped)
- Configuration: Optional, max 10000 characters, must be valid JSON if provided

**Factory Method**:
```csharp
public static Result<FeatureFlag> Create(
    Guid featureId,
    Guid? tenantId,
    Guid? userId,
    bool isEnabled,
    string? configuration,
    IDateTimeProvider dateTimeProvider)
```

**Domain Events**:
- `FeatureFlagCreatedEvent(Guid FeatureFlagId, Guid FeatureId, Guid? TenantId, Guid? UserId, bool IsEnabled, DateTime OccurredOn)`
- `FeatureFlagToggledEvent(Guid FeatureFlagId, Guid FeatureId, bool IsEnabled, DateTime OccurredOn)`
- `FeatureFlagDeletedEvent(Guid FeatureFlagId, Guid FeatureId, DateTime OccurredOn)`

---

### Repository Interfaces

#### IFeatureRepository
```csharp
public interface IFeatureRepository : IRepository<Feature, Guid>
{
    Task<Feature?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feature>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
```

#### IFeatureFlagRepository
```csharp
public interface IFeatureFlagRepository : IRepository<FeatureFlag, Guid>
{
    // Get flags by Feature (one-to-many relationship)
    Task<IEnumerable<FeatureFlag>> GetAllByFeatureAsync(Guid featureId, CancellationToken cancellationToken = default);

    // Get specific flag instances
    Task<FeatureFlag?> GetByFeatureAndTenantAsync(Guid featureId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<FeatureFlag?> GetByFeatureAndUserAsync(Guid featureId, Guid userId, CancellationToken cancellationToken = default);

    // Get all flags for a tenant
    Task<IEnumerable<FeatureFlag>> GetAllByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    // Check existence
    Task<bool> ExistsAsync(Guid featureId, Guid? tenantId, Guid? userId, CancellationToken cancellationToken = default);
}
```

**Note**: The `GetAllByFeatureAsync` method retrieves all FeatureFlags for a given Feature, demonstrating the one-to-many relationship.

#### IFeatureUnitOfWork
```csharp
public interface IFeatureUnitOfWork : IUnitOfWork
{
    // Inherits SaveChangesAsync from IUnitOfWork
}
```

---

## Phase 1: Domain Layer (8 hours)

### 1.1: Feature Entity (2 hours)

**File**: `Feature.Domain/Entities/Feature.cs`

**Tasks**:
- [ ] Create Feature class inheriting from `AggregateRoot<Guid>`
- [ ] Add all properties with private setters
- [ ] Add private parameterless constructor for EF Core
- [ ] Implement `Create` factory method with validation
- [ ] Implement `Update` method
- [ ] Implement `Enable` and `Disable` methods
- [ ] Raise domain events for all state changes
- [ ] Add XML documentation

**Validation in Create**:
```csharp
// Code validation
var codeRegex = new Regex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
if (!codeRegex.IsMatch(code))
    return Result<Feature>.Failure(Error.Validation("Feature.Code", "Code must be lowercase kebab-case"));

// Name validation
var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
if (nameResult.IsFailure) return Result<Feature>.Failure(nameResult.Error);

if (name.Length < 3 || name.Length > 200)
    return Result<Feature>.Failure(Error.Validation("Feature.Name", "Name must be 3-200 characters"));

// Category validation
var categoryResult = Guard.Against.NullOrWhiteSpace(category, nameof(category));
if (categoryResult.IsFailure) return Result<Feature>.Failure(categoryResult.Error);
```

**Methods**:
```csharp
public Result Update(string name, string description, string category, IDateTimeProvider dateTimeProvider)
{
    // Validate and update
    Name = name;
    Description = description;
    Category = category;
    UpdatedAt = dateTimeProvider.UtcNow;
    RaiseDomainEvent(new FeatureUpdatedEvent(Id, Code, UpdatedAt.Value));
    return Result.Success();
}

public Result Enable(IDateTimeProvider dateTimeProvider)
{
    IsGloballyEnabled = true;
    UpdatedAt = dateTimeProvider.UtcNow;
    return Result.Success();
}

public Result Disable(IDateTimeProvider dateTimeProvider)
{
    IsGloballyEnabled = false;
    UpdatedAt = dateTimeProvider.UtcNow;
    return Result.Success();
}
```

---

### 1.2: FeatureFlag Entity (2 hours)

**File**: `Feature.Domain/Entities/FeatureFlag.cs`

**Tasks**:
- [ ] Create FeatureFlag class inheriting from `AggregateRoot<Guid>`
- [ ] Add all properties with private setters
- [ ] Add private parameterless constructor for EF Core
- [ ] Implement `Create` factory method with validation
- [ ] Implement `Toggle` method
- [ ] Implement `UpdateConfiguration` method
- [ ] Raise domain events for all state changes
- [ ] Add XML documentation

**Validation in Create**:
```csharp
// FeatureId validation
if (featureId == Guid.Empty)
    return Result<FeatureFlag>.Failure(Error.Validation("FeatureFlag.FeatureId", "FeatureId is required"));

// Scope validation: cannot be both user-scoped AND tenant-scoped
if (tenantId.HasValue && userId.HasValue)
    return Result<FeatureFlag>.Failure(Error.Validation("FeatureFlag.Scope", "Cannot specify both TenantId and UserId"));

// Configuration validation (if provided, must be valid JSON)
if (!string.IsNullOrWhiteSpace(configuration))
{
    try
    {
        System.Text.Json.JsonDocument.Parse(configuration);
    }
    catch
    {
        return Result<FeatureFlag>.Failure(Error.Validation("FeatureFlag.Configuration", "Configuration must be valid JSON"));
    }
}
```

**Methods**:
```csharp
public Result Toggle(IDateTimeProvider dateTimeProvider)
{
    IsEnabled = !IsEnabled;
    UpdatedAt = dateTimeProvider.UtcNow;
    RaiseDomainEvent(new FeatureFlagToggledEvent(Id, FeatureId, IsEnabled, UpdatedAt.Value));
    return Result.Success();
}

public Result UpdateConfiguration(string? configuration, IDateTimeProvider dateTimeProvider)
{
    // Validate JSON if provided
    if (!string.IsNullOrWhiteSpace(configuration))
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(configuration);
        }
        catch
        {
            return Result.Failure(Error.Validation("FeatureFlag.Configuration", "Configuration must be valid JSON"));
        }
    }

    Configuration = configuration;
    UpdatedAt = dateTimeProvider.UtcNow;
    return Result.Success();
}
```

---

### 1.3: Domain Events (1 hour)

**File**: `Feature.Domain/Events/FeatureEvents.cs`

**Tasks**:
- [ ] Create all domain events as record types
- [ ] Implement `IDomainEvent` interface
- [ ] Add XML documentation

**Events**:
```csharp
public sealed record FeatureCreatedEvent(
    Guid FeatureId,
    string Code,
    string Name,
    DateTime OccurredOn) : IDomainEvent;

public sealed record FeatureUpdatedEvent(
    Guid FeatureId,
    string Code,
    DateTime OccurredOn) : IDomainEvent;

public sealed record FeatureDeletedEvent(
    Guid FeatureId,
    string Code,
    DateTime OccurredOn) : IDomainEvent;

public sealed record FeatureFlagCreatedEvent(
    Guid FeatureFlagId,
    Guid FeatureId,
    Guid? TenantId,
    Guid? UserId,
    bool IsEnabled,
    DateTime OccurredOn) : IDomainEvent;

public sealed record FeatureFlagToggledEvent(
    Guid FeatureFlagId,
    Guid FeatureId,
    bool IsEnabled,
    DateTime OccurredOn) : IDomainEvent;

public sealed record FeatureFlagDeletedEvent(
    Guid FeatureFlagId,
    Guid FeatureId,
    DateTime OccurredOn) : IDomainEvent;
```

---

### 1.4: Repository Interfaces (1 hour)

**File**: `Feature.Domain/Repositories/IFeatureRepository.cs`
**File**: `Feature.Domain/Repositories/IFeatureFlagRepository.cs`
**File**: `Feature.Domain/Repositories/IFeatureUnitOfWork.cs`

**Tasks**:
- [ ] Create IFeatureRepository interface
- [ ] Create IFeatureFlagRepository interface
- [ ] Create IFeatureUnitOfWork interface
- [ ] Add XML documentation

---

### 1.5: Domain Layer Testing (2 hours)

**File**: `Feature.Domain.Tests/Entities/FeatureTests.cs`
**File**: `Feature.Domain.Tests/Entities/FeatureFlagTests.cs`

**Tasks**:
- [ ] Test Feature.Create with valid data
- [ ] Test Feature.Create with invalid code format
- [ ] Test Feature.Update
- [ ] Test Feature.Enable/Disable
- [ ] Test FeatureFlag.Create with valid data
- [ ] Test FeatureFlag.Create with both TenantId and UserId (should fail)
- [ ] Test FeatureFlag.Toggle
- [ ] Test FeatureFlag.UpdateConfiguration with invalid JSON (should fail)
- [ ] Verify domain events are raised

**Deliverable**: Domain layer complete with entities, events, repository interfaces, and tests.

---

## Phase 2: Application Layer (10 hours)

### 2.1: DTOs (1 hour)

**File**: `Feature.Application/DTOs/FeatureDto.cs`

```csharp
public sealed record FeatureDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    bool IsGloballyEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

**File**: `Feature.Application/DTOs/FeatureFlagDto.cs`

```csharp
public sealed record FeatureFlagDto(
    Guid Id,
    Guid FeatureId,
    Guid? TenantId,
    Guid? UserId,
    bool IsEnabled,
    string? Configuration,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

**File**: `Feature.Application/DTOs/FeatureWithFlagDto.cs`

```csharp
public sealed record FeatureWithFlagDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    bool IsGloballyEnabled,
    bool IsEnabledForTenant,
    string? Configuration,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

---

### 2.2: Mappers (1 hour)

**File**: `Feature.Application/Mappers/FeatureMapper.cs`

```csharp
public static class FeatureMapper
{
    public static FeatureDto ToDto(Feature.Domain.Entities.Feature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        return new FeatureDto(
            feature.Id,
            feature.Code,
            feature.Name,
            feature.Description,
            feature.Category,
            feature.IsGloballyEnabled,
            feature.CreatedAt,
            feature.UpdatedAt);
    }
}
```

**File**: `Feature.Application/Mappers/FeatureFlagMapper.cs`

```csharp
public static class FeatureFlagMapper
{
    public static FeatureFlagDto ToDto(Feature.Domain.Entities.FeatureFlag featureFlag)
    {
        ArgumentNullException.ThrowIfNull(featureFlag);
        return new FeatureFlagDto(
            featureFlag.Id,
            featureFlag.FeatureId,
            featureFlag.TenantId,
            featureFlag.UserId,
            featureFlag.IsEnabled,
            featureFlag.Configuration,
            featureFlag.CreatedAt,
            featureFlag.UpdatedAt);
    }
}
```

---

### 2.3: Commands - Feature (3 hours)

#### CreateFeatureCommand

**File**: `Feature.Application/Commands/Features/CreateFeature/CreateFeatureCommand.cs`

```csharp
public sealed record CreateFeatureCommand(
    string Code,
    string Name,
    string Description,
    string Category,
    bool IsGloballyEnabled) : ICommand<Guid>;
```

**File**: `Feature.Application/Commands/Features/CreateFeature/CreateFeatureCommandValidator.cs`

```csharp
public sealed class CreateFeatureCommandValidator : AbstractValidator<CreateFeatureCommand>
{
    public CreateFeatureCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(3, 100)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Code must be lowercase kebab-case");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(3, 200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Category)
            .NotEmpty()
            .Length(3, 100);
    }
}
```

**File**: `Feature.Application/Commands/Features/CreateFeature/CreateFeatureCommandHandler.cs`

```csharp
public sealed class CreateFeatureCommandHandler : IRequestHandler<CreateFeatureCommand, Result<Guid>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeatureUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateFeatureCommandHandler(
        IFeatureRepository featureRepository,
        IFeatureUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _featureRepository = featureRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        // Check if code already exists
        var exists = await _featureRepository.CodeExistsAsync(request.Code, cancellationToken);
        if (exists)
            return Result<Guid>.Failure(Error.Conflict("Feature.CodeAlreadyExists", "A feature with this code already exists"));

        // Create feature
        var featureResult = Feature.Domain.Entities.Feature.Create(
            request.Code,
            request.Name,
            request.Description,
            request.Category,
            request.IsGloballyEnabled,
            _dateTimeProvider);

        if (featureResult.IsFailure)
            return Result<Guid>.Failure(featureResult.Error);

        // Save
        await _featureRepository.AddAsync(featureResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(featureResult.Value.Id);
    }
}
```

#### UpdateFeatureCommand

**File**: `Feature.Application/Commands/Features/UpdateFeature/UpdateFeatureCommand.cs`

```csharp
public sealed record UpdateFeatureCommand(
    Guid Id,
    string Name,
    string Description,
    string Category) : ICommand<Unit>;
```

**File**: `Feature.Application/Commands/Features/UpdateFeature/UpdateFeatureCommandHandler.cs`

```csharp
public sealed class UpdateFeatureCommandHandler : IRequestHandler<UpdateFeatureCommand, Result<Unit>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeatureUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Unit>> Handle(UpdateFeatureCommand request, CancellationToken cancellationToken)
    {
        var feature = await _featureRepository.GetByIdAsync(request.Id, cancellationToken);
        if (feature == null)
            return Result<Unit>.Failure(Error.NotFound("Feature.NotFound", "Feature not found"));

        var updateResult = feature.Update(request.Name, request.Description, request.Category, _dateTimeProvider);
        if (updateResult.IsFailure)
            return Result<Unit>.Failure(updateResult.Error);

        _featureRepository.Update(feature);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
```

#### DeleteFeatureCommand

**File**: `Feature.Application/Commands/Features/DeleteFeature/DeleteFeatureCommand.cs`

```csharp
public sealed record DeleteFeatureCommand(Guid Id) : ICommand<Unit>;
```

**File**: `Feature.Application/Commands/Features/DeleteFeature/DeleteFeatureCommandHandler.cs`

```csharp
public sealed class DeleteFeatureCommandHandler : IRequestHandler<DeleteFeatureCommand, Result<Unit>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeatureUnitOfWork _unitOfWork;

    public async Task<Result<Unit>> Handle(DeleteFeatureCommand request, CancellationToken cancellationToken)
    {
        var feature = await _featureRepository.GetByIdAsync(request.Id, cancellationToken);
        if (feature == null)
            return Result<Unit>.Failure(Error.NotFound("Feature.NotFound", "Feature not found"));

        _featureRepository.Delete(feature);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

### 2.4: Commands - FeatureFlag (2 hours)

#### CreateFeatureFlagCommand

**File**: `Feature.Application/Commands/FeatureFlags/CreateFeatureFlag/CreateFeatureFlagCommand.cs`

```csharp
public sealed record CreateFeatureFlagCommand(
    Guid FeatureId,
    Guid? TenantId,
    Guid? UserId,
    bool IsEnabled,
    string? Configuration) : ICommand<Guid>;
```

**File**: `Feature.Application/Commands/FeatureFlags/CreateFeatureFlag/CreateFeatureFlagCommandHandler.cs`

```csharp
public sealed class CreateFeatureFlagCommandHandler : IRequestHandler<CreateFeatureFlagCommand, Result<Guid>>
{
    private readonly IFeatureFlagRepository _featureFlagRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeatureUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Guid>> Handle(CreateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        // Verify feature exists
        var feature = await _featureRepository.GetByIdAsync(request.FeatureId, cancellationToken);
        if (feature == null)
            return Result<Guid>.Failure(Error.NotFound("Feature.NotFound", "Feature not found"));

        // Check if flag already exists for this scope
        var exists = await _featureFlagRepository.ExistsAsync(
            request.FeatureId,
            request.TenantId,
            request.UserId,
            cancellationToken);
        if (exists)
            return Result<Guid>.Failure(Error.Conflict("FeatureFlag.AlreadyExists", "Feature flag already exists for this scope"));

        // Create feature flag
        var flagResult = FeatureFlag.Create(
            request.FeatureId,
            request.TenantId,
            request.UserId,
            request.IsEnabled,
            request.Configuration,
            _dateTimeProvider);

        if (flagResult.IsFailure)
            return Result<Guid>.Failure(flagResult.Error);

        await _featureFlagRepository.AddAsync(flagResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(flagResult.Value.Id);
    }
}
```

#### ToggleFeatureFlagCommand

**File**: `Feature.Application/Commands/FeatureFlags/ToggleFeatureFlag/ToggleFeatureFlagCommand.cs`

```csharp
public sealed record ToggleFeatureFlagCommand(Guid Id) : ICommand<Unit>;
```

---

### 2.5: Queries - Feature (1.5 hours)

#### GetFeatureByIdQuery

**File**: `Feature.Application/Queries/Features/GetFeatureById/GetFeatureByIdQuery.cs`

```csharp
public sealed record GetFeatureByIdQuery(Guid Id) : IQuery<FeatureDto>;
```

**File**: `Feature.Application/Queries/Features/GetFeatureById/GetFeatureByIdQueryHandler.cs`

```csharp
public sealed class GetFeatureByIdQueryHandler : IRequestHandler<GetFeatureByIdQuery, Result<FeatureDto>>
{
    private readonly IFeatureRepository _featureRepository;

    public async Task<Result<FeatureDto>> Handle(GetFeatureByIdQuery request, CancellationToken cancellationToken)
    {
        var feature = await _featureRepository.GetByIdAsync(request.Id, cancellationToken);
        if (feature == null)
            return Result<FeatureDto>.Failure(Error.NotFound("Feature.NotFound", "Feature not found"));

        return Result<FeatureDto>.Success(FeatureMapper.ToDto(feature));
    }
}
```

#### GetAllFeaturesQuery

**File**: `Feature.Application/Queries/Features/GetAllFeatures/GetAllFeaturesQuery.cs`

```csharp
public sealed record GetAllFeaturesQuery : IQuery<IEnumerable<FeatureDto>>;
```

#### GetFeaturesByCategoryQuery

**File**: `Feature.Application/Queries/Features/GetFeaturesByCategory/GetFeaturesByCategoryQuery.cs`

```csharp
public sealed record GetFeaturesByCategoryQuery(string Category) : IQuery<IEnumerable<FeatureDto>>;
```

---

### 2.6: Queries - FeatureFlag (1.5 hours)

#### GetFeatureFlagsForTenantQuery

**File**: `Feature.Application/Queries/FeatureFlags/GetFeatureFlagsForTenant/GetFeatureFlagsForTenantQuery.cs`

```csharp
public sealed record GetFeatureFlagsForTenantQuery(Guid TenantId) : IQuery<IEnumerable<FeatureFlagDto>>;
```

**File**: `Feature.Application/Queries/FeatureFlags/GetFeatureFlagsForTenant/GetFeatureFlagsForTenantQueryHandler.cs`

```csharp
public sealed class GetFeatureFlagsForTenantQueryHandler
    : IRequestHandler<GetFeatureFlagsForTenantQuery, Result<IEnumerable<FeatureFlagDto>>>
{
    private readonly IFeatureFlagRepository _featureFlagRepository;

    public async Task<Result<IEnumerable<FeatureFlagDto>>> Handle(
        GetFeatureFlagsForTenantQuery request,
        CancellationToken cancellationToken)
    {
        var flags = await _featureFlagRepository.GetAllByTenantAsync(request.TenantId, cancellationToken);
        var dtos = flags.Select(FeatureFlagMapper.ToDto);
        return Result<IEnumerable<FeatureFlagDto>>.Success(dtos);
    }
}
```

#### IsFeatureEnabledQuery

**File**: `Feature.Application/Queries/FeatureFlags/IsFeatureEnabled/IsFeatureEnabledQuery.cs`

```csharp
public sealed record IsFeatureEnabledQuery(
    string FeatureCode,
    Guid? TenantId,
    Guid? UserId) : IQuery<bool>;
```

**File**: `Feature.Application/Queries/FeatureFlags/IsFeatureEnabled/IsFeatureEnabledQueryHandler.cs`

```csharp
public sealed class IsFeatureEnabledQueryHandler : IRequestHandler<IsFeatureEnabledQuery, Result<bool>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeatureFlagRepository _featureFlagRepository;

    public async Task<Result<bool>> Handle(IsFeatureEnabledQuery request, CancellationToken cancellationToken)
    {
        // Get feature by code
        var feature = await _featureRepository.GetByCodeAsync(request.FeatureCode, cancellationToken);
        if (feature == null)
            return Result<bool>.Failure(Error.NotFound("Feature.NotFound", $"Feature '{request.FeatureCode}' not found"));

        // Priority: User-specific > Tenant-specific > Global default

        // 1. Check user-specific flag
        if (request.UserId.HasValue)
        {
            var userFlag = await _featureFlagRepository.GetByFeatureAndUserAsync(feature.Id, request.UserId.Value, cancellationToken);
            if (userFlag != null)
                return Result<bool>.Success(userFlag.IsEnabled);
        }

        // 2. Check tenant-specific flag
        if (request.TenantId.HasValue)
        {
            var tenantFlag = await _featureFlagRepository.GetByFeatureAndTenantAsync(feature.Id, request.TenantId.Value, cancellationToken);
            if (tenantFlag != null)
                return Result<bool>.Success(tenantFlag.IsEnabled);
        }

        // 3. Fall back to global default
        return Result<bool>.Success(feature.IsGloballyEnabled);
    }
}
```

---

### 2.7: Application Module Registration (30 minutes)

**File**: `Feature.Application/FeatureApplicationModule.cs`

```csharp
using System.Reflection;
using BuildingBlocks.Application.Modules;
using Feature.Application.Commands.Features.CreateFeature;

namespace Feature.Application;

public sealed class FeatureApplicationModule : IApplicationModule
{
    public Assembly ApplicationAssembly => typeof(CreateFeatureCommand).Assembly;
}
```

**File**: `Feature.Application/FeatureApplicationServiceCollectionExtensions.cs`

```csharp
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Feature.Application;

public static class FeatureApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureApplication(this IServiceCollection services)
    {
        // Register transaction behavior (if needed)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviors.FeatureTransactionBehavior<,>));

        return services;
    }
}
```

**Deliverable**: Application layer complete with commands, queries, DTOs, mappers, and validators.

---

## Phase 3: Infrastructure Layer (8 hours)

### 3.1: DbContext (2 hours)

**File**: `Feature.Infrastructure/Data/FeatureDbContext.cs`

```csharp
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Feature.Infrastructure.Data;

public class FeatureDbContext : BaseModuleDbContext
{
    public FeatureDbContext(DbContextOptions<FeatureDbContext> options)
        : base(options)
    {
    }

    protected override string SchemaName => "feature";

    public DbSet<Feature.Domain.Entities.Feature> Features => Set<Feature.Domain.Entities.Feature>();
    public DbSet<Feature.Domain.Entities.FeatureFlag> FeatureFlags => Set<Feature.Domain.Entities.FeatureFlag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeatureDbContext).Assembly);
    }
}
```

---

### 3.2: Entity Configurations (2 hours)

**File**: `Feature.Infrastructure/Data/Configurations/FeatureConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feature.Infrastructure.Data.Configurations;

public class FeatureConfiguration : IEntityTypeConfiguration<Feature.Domain.Entities.Feature>
{
    public void Configure(EntityTypeBuilder<Feature.Domain.Entities.Feature> builder)
    {
        builder.ToTable("features");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(f => f.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(f => f.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.IsGloballyEnabled)
            .HasColumnName("is_globally_enabled")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(f => f.Code)
            .HasDatabaseName("ix_features_code")
            .IsUnique();

        builder.HasIndex(f => f.Category)
            .HasDatabaseName("ix_features_category");
    }
}
```

**File**: `Feature.Infrastructure/Data/Configurations/FeatureFlagConfiguration.cs`

```csharp
public class FeatureFlagConfiguration : IEntityTypeConfiguration<Feature.Domain.Entities.FeatureFlag>
{
    public void Configure(EntityTypeBuilder<Feature.Domain.Entities.FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");

        builder.HasKey(ff => ff.Id);

        builder.Property(ff => ff.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(ff => ff.FeatureId)
            .HasColumnName("feature_id")
            .IsRequired();

        builder.Property(ff => ff.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(ff => ff.UserId)
            .HasColumnName("user_id");

        builder.Property(ff => ff.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(ff => ff.Configuration)
            .HasColumnName("configuration")
            .HasMaxLength(10000);

        builder.Property(ff => ff.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ff => ff.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(ff => ff.FeatureId)
            .HasDatabaseName("ix_feature_flags_feature_id");

        builder.HasIndex(ff => ff.TenantId)
            .HasDatabaseName("ix_feature_flags_tenant_id");

        builder.HasIndex(ff => ff.UserId)
            .HasDatabaseName("ix_feature_flags_user_id");

        // Unique constraint: one flag per feature per scope
        // Note: This uses a partial unique index to handle NULL values properly
        // - Global flags: (feature_id) WHERE tenant_id IS NULL AND user_id IS NULL
        // - Tenant flags: (feature_id, tenant_id) WHERE user_id IS NULL
        // - User flags: (feature_id, tenant_id, user_id) WHERE user_id IS NOT NULL
        builder.HasIndex(ff => new { ff.FeatureId, ff.TenantId, ff.UserId })
            .HasDatabaseName("uq_feature_flags_scope")
            .IsUnique()
            .HasFilter("user_id IS NOT NULL"); // User-level flags must be unique

        // Additional partial unique indexes for NULL handling
        builder.HasIndex(ff => new { ff.FeatureId, ff.TenantId })
            .HasDatabaseName("uq_feature_flags_tenant_scope")
            .IsUnique()
            .HasFilter("user_id IS NULL AND tenant_id IS NOT NULL"); // Tenant-level flags

        builder.HasIndex(ff => ff.FeatureId)
            .HasDatabaseName("uq_feature_flags_global_scope")
            .IsUnique()
            .HasFilter("tenant_id IS NULL AND user_id IS NULL"); // Global flags
    }
}
```

---

### 3.3: Repository Implementations (2 hours)

**File**: `Feature.Infrastructure/Repositories/FeatureRepository.cs`

```csharp
using BuildingBlocks.Infrastructure.Persistence;
using Feature.Domain.Repositories;
using Feature.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Feature.Infrastructure.Repositories;

public class FeatureRepository : Repository<Feature.Domain.Entities.Feature, Guid>, IFeatureRepository
{
    private readonly FeatureDbContext _context;

    public FeatureRepository(FeatureDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<Feature.Domain.Entities.Feature?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Features
            .FirstOrDefaultAsync(f => f.Code == code, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Features
            .AnyAsync(f => f.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<Feature.Domain.Entities.Feature>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _context.Features
            .Where(f => f.Category == category)
            .ToListAsync(cancellationToken);
    }
}
```

**File**: `Feature.Infrastructure/Repositories/FeatureFlagRepository.cs`

```csharp
public class FeatureFlagRepository : Repository<Feature.Domain.Entities.FeatureFlag, Guid>, IFeatureFlagRepository
{
    private readonly FeatureDbContext _context;

    public FeatureFlagRepository(FeatureDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Feature.Domain.Entities.FeatureFlag>> GetAllByFeatureAsync(
        Guid featureId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .Where(ff => ff.FeatureId == featureId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Feature.Domain.Entities.FeatureFlag?> GetByFeatureAndTenantAsync(
        Guid featureId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .FirstOrDefaultAsync(
                ff => ff.FeatureId == featureId && ff.TenantId == tenantId && ff.UserId == null,
                cancellationToken);
    }

    public async Task<Feature.Domain.Entities.FeatureFlag?> GetByFeatureAndUserAsync(
        Guid featureId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .FirstOrDefaultAsync(
                ff => ff.FeatureId == featureId && ff.UserId == userId,
                cancellationToken);
    }

    public async Task<IEnumerable<Feature.Domain.Entities.FeatureFlag>> GetAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .Where(ff => ff.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid featureId,
        Guid? tenantId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .AnyAsync(
                ff => ff.FeatureId == featureId && ff.TenantId == tenantId && ff.UserId == userId,
                cancellationToken);
    }
}
```

---

### 3.4: Unit of Work (30 minutes)

**File**: `Feature.Infrastructure/Data/FeatureUnitOfWork.cs`

```csharp
using BuildingBlocks.Kernel.Persistence;
using Feature.Domain.Repositories;

namespace Feature.Infrastructure.Data;

public class FeatureUnitOfWork : IFeatureUnitOfWork
{
    private readonly FeatureDbContext _context;

    public FeatureUnitOfWork(FeatureDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

### 3.5: Infrastructure Registration (1.5 hours)

**File**: `Feature.Infrastructure/FeatureInfrastructureServiceCollectionExtensions.cs`

```csharp
using Feature.Domain.Repositories;
using Feature.Infrastructure.Data;
using Feature.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Feature.Infrastructure;

public static class FeatureInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string schemaName)
    {
        // DbContext
        services.AddDbContext<FeatureDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schemaName);
            });
        });

        // Repositories
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();

        // Unit of Work
        services.AddScoped<IFeatureUnitOfWork, FeatureUnitOfWork>();

        return services;
    }
}
```

**Deliverable**: Infrastructure layer complete with DbContext, configurations, repositories, and DI registration.

---

## Phase 4: API Layer (6 hours)

### 4.1: Feature Controller (3 hours)

**File**: `Feature.Api/Controllers/FeatureController.cs`

```csharp
using Feature.Application.Commands.Features.CreateFeature;
using Feature.Application.Commands.Features.UpdateFeature;
using Feature.Application.Commands.Features.DeleteFeature;
using Feature.Application.Queries.Features.GetFeatureById;
using Feature.Application.Queries.Features.GetAllFeatures;
using Feature.Application.Queries.Features.GetFeaturesByCategory;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Feature.Api.Controllers;

[ApiController]
[Route("api/feature/features")]
public sealed class FeatureController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeatureController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all features
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetAllFeaturesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(statusCode: 500, detail: result.Error.Message);
    }

    /// <summary>
    /// Get feature by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetFeatureByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error.Message });
    }

    /// <summary>
    /// Get features by category
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken cancellationToken)
    {
        var query = new GetFeaturesByCategoryQuery(category);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(statusCode: 500, detail: result.Error.Message);
    }

    /// <summary>
    /// Create a new feature
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFeatureCommand command,
        CancellationToken cancellationToken)
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

    /// <summary>
    /// Update an existing feature
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFeatureRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFeatureCommand(id, request.Name, request.Description, request.Category);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
                ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
                _ => Problem(statusCode: 500, detail: result.Error.Message)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Delete a feature
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteFeatureCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error.Message });
    }
}

public sealed record UpdateFeatureRequest(string Name, string Description, string Category);
```

---

### 4.2: FeatureFlag Controller (3 hours)

**File**: `Feature.Api/Controllers/FeatureFlagController.cs`

```csharp
using Feature.Application.Commands.FeatureFlags.CreateFeatureFlag;
using Feature.Application.Commands.FeatureFlags.ToggleFeatureFlag;
using Feature.Application.Queries.FeatureFlags.GetFeatureFlagsForTenant;
using Feature.Application.Queries.FeatureFlags.IsFeatureEnabled;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Feature.Api.Controllers;

[ApiController]
[Route("api/feature/flags")]
public sealed class FeatureFlagController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeatureFlagController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all feature flags for a tenant
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var query = new GetFeatureFlagsForTenantQuery(tenantId);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(statusCode: 500, detail: result.Error.Message);
    }

    /// <summary>
    /// Check if a feature is enabled for a tenant/user
    /// </summary>
    [HttpGet("enabled/{featureCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IsEnabled(
        string featureCode,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken)
    {
        var query = new IsFeatureEnabledQuery(featureCode, tenantId, userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(new { error = result.Error.Message })
                : Problem(statusCode: 500, detail: result.Error.Message);
        }

        return Ok(new { featureCode, isEnabled = result.Value, tenantId, userId });
    }

    /// <summary>
    /// Create a feature flag
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFeatureFlagCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
                ErrorType.Conflict => Conflict(new { error = result.Error.Message }),
                ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
                _ => Problem(statusCode: 500, detail: result.Error.Message)
            };
        }

        return CreatedAtAction(nameof(GetByTenant), new { tenantId = command.TenantId }, new { id = result.Value });
    }

    /// <summary>
    /// Toggle a feature flag
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken cancellationToken)
    {
        var command = new ToggleFeatureFlagCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error.Message });
    }
}
```

**Deliverable**: API layer complete with controllers for Features and FeatureFlags.

---

## Phase 5: Migrations Layer (4 hours)

### 5.1: Create Feature Schema (30 minutes)

**File**: `Feature.Migrations/Migrations/Schema/20260211000000_CreateFeatureSchema.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Feature.Migrations.Migrations.Schema;

[Migration(20260211000000, "Create feature schema")]
public class CreateFeatureSchema : Migration
{
    public override void Up()
    {
        Create.Schema("feature");
    }

    public override void Down()
    {
        Delete.Schema("feature");
    }
}
```

---

### 5.2: Create Features Table (1 hour)

**File**: `Feature.Migrations/Migrations/Schema/20260211001000_CreateFeaturesTable.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Feature.Migrations.Migrations.Schema;

[Migration(20260211001000, "Create features table")]
public class CreateFeaturesTable : Migration
{
    public override void Up()
    {
        Create.Table("features")
            .InSchema("feature")
            .WithColumn("id").AsGuid().PrimaryKey("pk_features")
            .WithColumn("code").AsString(100).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("category").AsString(100).NotNullable()
            .WithColumn("is_globally_enabled").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.UniqueConstraint("uq_features_code")
            .OnTable("features")
            .WithSchema("feature")
            .Column("code");

        Create.Index("ix_features_category")
            .OnTable("features")
            .WithSchema("feature")
            .OnColumn("category");
    }

    public override void Down()
    {
        Delete.Table("features").InSchema("feature");
    }
}
```

---

### 5.3: Create FeatureFlags Table (1.5 hours)

**File**: `Feature.Migrations/Migrations/Schema/20260211002000_CreateFeatureFlagsTable.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Feature.Migrations.Migrations.Schema;

[Migration(20260211002000, "Create feature_flags table")]
public class CreateFeatureFlagsTable : Migration
{
    public override void Up()
    {
        Create.Table("feature_flags")
            .InSchema("feature")
            .WithColumn("id").AsGuid().PrimaryKey("pk_feature_flags")
            .WithColumn("feature_id").AsGuid().NotNullable()
            .WithColumn("tenant_id").AsGuid().Nullable()
            .WithColumn("user_id").AsGuid().Nullable()
            .WithColumn("is_enabled").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("configuration").AsString(10000).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        // Foreign key to features table
        Create.ForeignKey("fk_feature_flags_feature_id")
            .FromTable("feature_flags").InSchema("feature").ForeignColumn("feature_id")
            .ToTable("features").InSchema("feature").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Indexes
        Create.Index("ix_feature_flags_feature_id")
            .OnTable("feature_flags")
            .WithSchema("feature")
            .OnColumn("feature_id");

        Create.Index("ix_feature_flags_tenant_id")
            .OnTable("feature_flags")
            .WithSchema("feature")
            .OnColumn("tenant_id");

        Create.Index("ix_feature_flags_user_id")
            .OnTable("feature_flags")
            .WithSchema("feature")
            .OnColumn("user_id");

        // Unique constraints: one flag per feature per scope (using partial unique indexes for NULL handling)
        // User-level flags: (feature_id, tenant_id, user_id) WHERE user_id IS NOT NULL
        Execute.Sql(@"
            CREATE UNIQUE INDEX uq_feature_flags_user_scope
            ON feature.feature_flags (feature_id, tenant_id, user_id)
            WHERE user_id IS NOT NULL;
        ");

        // Tenant-level flags: (feature_id, tenant_id) WHERE user_id IS NULL AND tenant_id IS NOT NULL
        Execute.Sql(@"
            CREATE UNIQUE INDEX uq_feature_flags_tenant_scope
            ON feature.feature_flags (feature_id, tenant_id)
            WHERE user_id IS NULL AND tenant_id IS NOT NULL;
        ");

        // Global flags: (feature_id) WHERE tenant_id IS NULL AND user_id IS NULL
        Execute.Sql(@"
            CREATE UNIQUE INDEX uq_feature_flags_global_scope
            ON feature.feature_flags (feature_id)
            WHERE tenant_id IS NULL AND user_id IS NULL;
        ");
    }

    public override void Down()
    {
        Delete.Table("feature_flags").InSchema("feature");
    }
}
```

---

### 5.4: Seed Default Features (1 hour)

**File**: `Feature.Migrations/Migrations/Data/20260211100000_SeedDefaultFeatures.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Feature.Migrations.Migrations.Data;

[Migration(20260211100000, "Seed default features")]
public class SeedDefaultFeatures : Migration
{
    public override void Up()
    {
        // Analytics features
        Insert.IntoTable("features").InSchema("feature")
            .Row(new
            {
                id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                code = "advanced-analytics",
                name = "Advanced Analytics",
                description = "Access to advanced analytics dashboards and reports",
                category = "Analytics",
                is_globally_enabled = false,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.Parse("11111111-1111-1111-1111-111111111112"),
                code = "data-export",
                name = "Data Export",
                description = "Export data to CSV, Excel, and PDF formats",
                category = "Analytics",
                is_globally_enabled = true,
                created_at = DateTime.UtcNow
            });

        // Customization features
        Insert.IntoTable("features").InSchema("feature")
            .Row(new
            {
                id = Guid.Parse("22222222-2222-2222-2222-222222222221"),
                code = "custom-branding",
                name = "Custom Branding",
                description = "Customize logo, colors, and branding",
                category = "Customization",
                is_globally_enabled = false,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                code = "custom-domain",
                name = "Custom Domain",
                description = "Use your own domain name",
                category = "Customization",
                is_globally_enabled = false,
                created_at = DateTime.UtcNow
            });

        // Integration features
        Insert.IntoTable("features").InSchema("feature")
            .Row(new
            {
                id = Guid.Parse("33333333-3333-3333-3333-333333333331"),
                code = "api-access",
                name = "API Access",
                description = "Access to REST API for integrations",
                category = "Integration",
                is_globally_enabled = true,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.Parse("33333333-3333-3333-3333-333333333332"),
                code = "webhooks",
                name = "Webhooks",
                description = "Configure webhooks for real-time notifications",
                category = "Integration",
                is_globally_enabled = false,
                created_at = DateTime.UtcNow
            });
    }

    public override void Down()
    {
        Delete.FromTable("features").InSchema("feature").AllRows();
    }
}
```

**Deliverable**: Migrations layer complete with schema, tables, and seed data.

---

## Phase 6: Module Composition (2 hours)

### 6.1: Update FeatureModule (1 hour)

**File**: `Feature.Module/FeatureModule.cs`

```csharp
using BuildingBlocks.Web.Modules;
using Feature.Api.Controllers;
using Feature.Application;
using Feature.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Feature.Module;

/// <summary>
/// Feature module composition root (startup) and migration metadata.
/// </summary>
public sealed class FeatureModule : IModule
{
    public string ModuleName => "Feature";
    public string SchemaName => "feature";

    public string[] GetMigrationDependencies() => ["Tenant"];

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Domain: No registration needed (pure domain logic)

        // Application: MediatR handlers, validators, behaviors
        services.AddFeatureApplication();

        // Infrastructure: DbContext, repositories, unit of work
        services.AddFeatureInfrastructure(configuration, SchemaName);

        // API: Controllers
        services.AddControllers()
            .AddApplicationPart(typeof(FeatureController).Assembly);

        return services;
    }

    public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
    {
        // No module-specific middleware needed
        return app;
    }
}
```

---

### 6.2: Update Hosts (1 hour)

**Tasks**:
- [ ] Update `MonolithHost/Program.cs` - already loads FeatureModule
- [ ] Update `MultiAppAppBuilderHost/appsettings.json` - use `"Feature"` (not `"FeatureManagement"`)
- [ ] Verify FeatureModule is registered in all hosts

**File**: `MultiAppAppBuilderHost/appsettings.json` (verify)

```json
{
  "LoadedModules": [ "Feature" ]
}
```

**Note**: The module name should be `"Feature"` to match the module assembly name and IModule implementation class name.

---

### 6.3: Create FeatureServiceHost for Microservices (1 hour)

**File**: `Hosts/FeatureServiceHost/Feature.Service.Host.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Feature.Service.Host</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\BuildingBlocks\Web\BuildingBlocks.Web.csproj" />
    <ProjectReference Include="..\..\Product\Feature\Feature.Module\Feature.Module.csproj" />
    <ProjectReference Include="..\..\ServiceDefaults\ServiceDefaults.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" />
  </ItemGroup>
</Project>
```

**File**: `Hosts/FeatureServiceHost/Program.cs`

```csharp
using BuildingBlocks.Web.Extensions;
using Feature.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddBuildingBlocks();
builder.AddBuildingBlocksHealthChecks();

builder.Services.AddModule<FeatureModule>(builder.Configuration);

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

app.UseModule<FeatureModule>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

await app.RunAsync();
```

**File**: `Hosts/FeatureServiceHost/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datarizen;Username=postgres;Password=postgres"
  }
}
```

**Deliverable**: Module composition complete with FeatureModule registration and FeatureServiceHost for microservices.

---

## Phase 7: Integration with Tenant Module (2 hours)

### 7.1: Tenant Context Integration

**Purpose**: Ensure feature flags respect tenant boundaries and integrate with tenant resolution middleware.

**Tasks**:
- [ ] Verify `BuildingBlocks.Web` has `ITenantContext` interface
- [ ] Update `IsFeatureEnabledQuery` to use current tenant from context if not specified
- [ ] Add tenant validation in `CreateFeatureFlagCommand` handler

**File**: `Feature.Application/Queries/FeatureFlags/IsFeatureEnabled/IsFeatureEnabledQueryHandler.cs` (update)

```csharp
// Add ITenantContext injection
private readonly ITenantContext _tenantContext;

public IsFeatureEnabledQueryHandler(
    IFeatureRepository featureRepository,
    IFeatureFlagRepository featureFlagRepository,
    ITenantContext tenantContext)
{
    _featureRepository = featureRepository;
    _featureFlagRepository = featureFlagRepository;
    _tenantContext = tenantContext;
}

public async Task<Result<bool>> Handle(IsFeatureEnabledQuery request, CancellationToken cancellationToken)
{
    // Use current tenant if not specified
    var tenantId = request.TenantId ?? _tenantContext.TenantId;

    // ... rest of implementation
}
```

---

### 7.2: Contracts for Inter-Module Communication

**File**: `Feature.Contracts/DTOs/FeatureEvaluationDto.cs`

```csharp
namespace Feature.Contracts.DTOs;

public sealed record FeatureEvaluationDto(
    string FeatureCode,
    bool IsEnabled,
    Guid? TenantId,
    Guid? UserId,
    string? Configuration);
```

**File**: `Feature.Contracts/Services/IFeatureEvaluationService.cs`

```csharp
namespace Feature.Contracts.Services;

/// <summary>
/// Service for evaluating feature flags from other modules.
/// </summary>
public interface IFeatureEvaluationService
{
    Task<bool> IsFeatureEnabledAsync(
        string featureCode,
        Guid? tenantId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<FeatureEvaluationDto?> GetFeatureEvaluationAsync(
        string featureCode,
        Guid? tenantId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
```

**File**: `Feature.Application/Services/FeatureEvaluationService.cs`

```csharp
using Feature.Application.Queries.FeatureFlags.IsFeatureEnabled;
using Feature.Contracts.DTOs;
using Feature.Contracts.Services;
using MediatR;

namespace Feature.Application.Services;

public sealed class FeatureEvaluationService : IFeatureEvaluationService
{
    private readonly IMediator _mediator;

    public FeatureEvaluationService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<bool> IsFeatureEnabledAsync(
        string featureCode,
        Guid? tenantId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new IsFeatureEnabledQuery(featureCode, tenantId, userId);
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess && result.Value;
    }

    public async Task<FeatureEvaluationDto?> GetFeatureEvaluationAsync(
        string featureCode,
        Guid? tenantId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new IsFeatureEnabledQuery(featureCode, tenantId, userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
            return null;

        return new FeatureEvaluationDto(featureCode, result.Value, tenantId, userId, null);
    }
}
```

**Update**: `Feature.Application/FeatureApplicationServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddFeatureApplication(this IServiceCollection services)
{
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviors.FeatureTransactionBehavior<,>));

    // Register contracts service
    services.AddScoped<IFeatureEvaluationService, FeatureEvaluationService>();

    return services;
}
```

---

## Phase 8: Testing & Validation (4 hours)

### 8.1: Integration Tests

**File**: `Feature.IntegrationTests/Features/CreateFeatureTests.cs`

```csharp
[Fact]
public async Task CreateFeature_WithValidData_ShouldSucceed()
{
    // Arrange
    var command = new CreateFeatureCommand(
        "test-feature",
        "Test Feature",
        "A test feature",
        "Testing",
        false);

    // Act
    var result = await _mediator.Send(command);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeEmpty();
}

[Fact]
public async Task CreateFeature_WithDuplicateCode_ShouldFail()
{
    // Arrange
    var command1 = new CreateFeatureCommand("duplicate", "Feature 1", "Desc", "Cat", false);
    var command2 = new CreateFeatureCommand("duplicate", "Feature 2", "Desc", "Cat", false);

    // Act
    await _mediator.Send(command1);
    var result = await _mediator.Send(command2);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Type.Should().Be(ErrorType.Conflict);
}
```

### 8.2: API Tests

**File**: `Feature.IntegrationTests/Api/FeatureControllerTests.cs`

```csharp
[Fact]
public async Task GetAll_ShouldReturnFeatures()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/feature/features");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

---

## Success Criteria Checklist

### Domain Layer
- [x] Feature entity with validation and factory method
- [x] FeatureFlag entity with scope validation
- [x] Domain events for all state changes
- [x] Repository interfaces defined
- [x] Unit tests for entities

### Application Layer
- [x] CRUD commands for Features
- [x] CRUD commands for FeatureFlags
- [x] Queries for feature evaluation
- [x] DTOs and mappers
- [x] FluentValidation validators
- [x] IFeatureEvaluationService for inter-module communication

### Infrastructure Layer
- [x] FeatureDbContext with schema configuration
- [x] Entity configurations for EF Core
- [x] Repository implementations
- [x] Unit of Work implementation
- [x] DI registration

### API Layer
- [x] FeatureController with CRUD endpoints
- [x] FeatureFlagController with evaluation endpoints
- [x] Proper HTTP status codes
- [x] Swagger documentation

### Migrations Layer
- [x] Schema creation migration
- [x] Features table migration
- [x] FeatureFlags table migration
- [x] Seed data migration
- [x] Foreign keys and indexes

### Module Composition
- [x] FeatureModule implements IModule
- [x] Layer registration in correct order
- [x] FeatureServiceHost for microservices topology
- [x] Integration with Tenant module

### Integration
- [x] Tenant-scoped feature flags
- [x] User-scoped feature flag overrides
- [x] Hierarchical evaluation (user > tenant > global)
- [x] Contracts for inter-module communication

---

## Deployment Topology Support

### Monolith
- ✅ All modules in single process
- ✅ Single database with `feature` schema
- ✅ Direct in-process service calls

### MultiApp
- ✅ Feature module is deployed **inside the AppBuilder host process** (not a separate process)
- ✅ Feature module is loaded via `LoadedModules` configuration in `MultiAppAppBuilderHost`
- ✅ Shared database with `feature` schema
- ✅ API Gateway routes `/api/feature/*` to AppBuilder host (port 5002)

### Microservices
- ✅ Dedicated `FeatureServiceHost`
- ✅ Can use separate database (or shared with schema isolation)
- ✅ HTTP/gRPC communication via service discovery

---

## Estimated Timeline

| Phase | Description | Time |
|-------|-------------|------|
| Phase 1 | Domain Layer | 8 hours |
| Phase 2 | Application Layer | 10 hours |
| Phase 3 | Infrastructure Layer | 8 hours |
| Phase 4 | API Layer | 6 hours |
| Phase 5 | Migrations Layer | 4 hours |
| Phase 6 | Module Composition | 2 hours |
| Phase 7 | Tenant Integration | 2 hours |
| Phase 8 | Testing & Validation | 4 hours |
| **Total** | **Complete Vertical Slice** | **44 hours** |

---

## Next Steps

After completing the Feature module:

1. **Create AppBuilder Module** - For building and configuring applications
2. **Create AppRuntime Module** - For running applications with feature flag evaluation
3. **Update AppHost** - Add FeatureServiceHost to microservices topology
4. **Integration Testing** - Test feature flag evaluation across all topologies
5. **Documentation** - Update API documentation and user guides

---

## Notes

- **Feature Codes**: Use lowercase-kebab-case for consistency (e.g., `advanced-analytics`, `custom-branding`)
- **Scope Hierarchy**: User-specific flags override tenant-specific flags, which override global defaults
- **Configuration**: Store feature-specific configuration as JSON in the `configuration` column
- **Performance**: Consider caching feature flag evaluations for frequently accessed features
- **Audit**: Domain events enable audit logging of all feature flag changes
- **Migration Dependencies**: Feature module depends on Tenant module (for TenantId foreign key validation)


