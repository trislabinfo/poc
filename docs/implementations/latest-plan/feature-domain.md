# Feature Module - Domain Layer

## Overview

Feature management system with support for feature flags. Features can have multiple flags for granular control.

---

## Entities

### Feature (Aggregate Root)

**Purpose**: Represents a system feature that can be enabled/disabled

**Base Class**: `AggregateRoot<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `Name: string` - Feature name (e.g., "Notification", "AppBuilder")
- `Description: string` - Feature description
- `IsEnabled: bool` - Whether feature is globally enabled
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Collections**:
- `FeatureFlags: ICollection<FeatureFlag>` - Feature flags

**Factory Method**:
```csharp
public static Result<Feature> Create(
    string name,
    string description,
    bool isEnabled)
```

**Business Methods**:
- `Enable()` - Enable feature globally
- `Disable()` - Disable feature globally
- `AddFlag(FeatureFlag flag)` - Add feature flag
- `RemoveFlag(Guid flagId)` - Remove feature flag

**Domain Events**:
- `FeatureCreatedEvent`
- `FeatureEnabledEvent`
- `FeatureDisabledEvent`
- `FeatureFlagAddedEvent`
- `FeatureFlagRemovedEvent`

---

### FeatureFlag (Entity)

**Purpose**: Granular feature flag within a feature

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `FeatureId: Guid` - Foreign key
- `Name: string` - Flag name (e.g., "SmsNotification", "EmailNotification")
- `Description: string` - Flag description
- `IsEnabled: bool` - Whether flag is enabled
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Factory Method**:
```csharp
public static Result<FeatureFlag> Create(
    Guid featureId,
    string name,
    string description,
    bool isEnabled)
```

**Business Methods**:
- `Enable()` - Enable flag
- `Disable()` - Disable flag

**Domain Events**:
- `FeatureFlagCreatedEvent`
- `FeatureFlagEnabledEvent`
- `FeatureFlagDisabledEvent`

---

## Examples

### Notification Feature
```
Feature: Notification (IsEnabled = true)
├── FeatureFlag: SmsNotification (IsEnabled = true)
├── FeatureFlag: EmailNotification (IsEnabled = true)
└── FeatureFlag: PushNotification (IsEnabled = false)
```

### AppBuilder Feature
```
Feature: AppBuilder (IsEnabled = true)
├── FeatureFlag: EntityModeling (IsEnabled = true)
├── FeatureFlag: PageBuilder (IsEnabled = true)
├── FeatureFlag: NavigationBuilder (IsEnabled = true)
└── FeatureFlag: DataSourceBuilder (IsEnabled = false)
```

---

## Repository Interfaces

### IFeatureRepository
```csharp
public interface IFeatureRepository
{
    Task<Feature?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Feature?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Feature>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Feature>> GetEnabledFeaturesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Feature feature, CancellationToken cancellationToken = default);
    void Update(Feature feature);
    void Remove(Feature feature);
}
```

### IFeatureFlagRepository
```csharp
public interface IFeatureFlagRepository
{
    Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FeatureFlag>> GetByFeatureAsync(Guid featureId, CancellationToken cancellationToken = default);
    Task<FeatureFlag?> GetByNameAsync(Guid featureId, string name, CancellationToken cancellationToken = default);
    Task AddAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
    void Update(FeatureFlag flag);
    void Remove(FeatureFlag flag);
}
```

---

## Notes

- **TenantFeature** is NOT part of Feature module
- **TenantFeature** belongs to Tenant module
- Feature module only manages platform-level features
- Tenant module manages tenant-specific feature assignments



