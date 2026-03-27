# AppBuilder Module - Domain Layer (MVP v2)

## Overview

Application builder for creating no-code applications with entity modeling, navigation, pages, and data sources. **Scope: platform applications only.** AppBuilder edits only the `appbuilder` schema. When a tenant has the AppBuilder feature, **editing of that tenant’s applications** (custom or forked) is performed by the **TenantApplication** module (tenant-scoped API and `tenantapplication` schema); the same “AppBuilder” UX can target either AppBuilder API (platform) or TenantApplication API (tenant).

---

## Entities

### ApplicationDefinition (Aggregate Root)

**Purpose**: Represents a no-code application being built

**Base Class**: `AggregateRoot<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `Name: string` - Application name
- `Description: string` - Documentation
- `Slug: string` - URL-friendly identifier (unique)
- `Status: ApplicationStatus` - Draft, Released, Archived
- `CurrentVersion: string?` - Latest released version (e.g., "1.0.0")
- `IsPublic: bool` - Whether visible in tenant catalog
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Factory Method**:
```csharp
public static Result<ApplicationDefinition> Create(
    string name,
    string description,
    string slug,
    bool isPublic)
```

**Business Methods**:
- `Update(string name, string description)` - Update metadata
- `CreateRelease(string version, string releaseNotes, Guid userId)` - Create immutable release
- `Archive()` - Mark as archived

**Domain Events**:
- `ApplicationDefinitionCreatedEvent`
- `ApplicationDefinitionUpdatedEvent`
- `ApplicationReleasedEvent`
- `ApplicationDefinitionArchivedEvent`

---

### ApplicationRelease (Entity)

**Purpose**: Immutable snapshot of application at specific version

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `ApplicationDefinitionId: Guid` - Foreign key
- `Version: string` - Full semantic version (e.g., "1.0.0")
- `Major: int` - Major version number
- `Minor: int` - Minor version number
- `Patch: int` - Patch version number
- `ReleaseNotes: string` - Change description
- `NavigationJson: string` - JSON snapshot of navigation definitions
- `PageJson: string` - JSON snapshot of page definitions
- `DataSourceJson: string` - JSON snapshot of data source definitions
- `EntityJson: string` - JSON snapshot of entity definitions
- `ReleasedAt: DateTime` - When released
- `ReleasedBy: Guid` - User who released
- `IsActive: bool` - Whether available in catalog

**Factory Method**:
```csharp
public static Result<ApplicationRelease> Create(
    Guid applicationDefinitionId,
    string version,
    int major,
    int minor,
    int patch,
    string releaseNotes,
    string navigationJson,
    string pageJson,
    string dataSourceJson,
    string entityJson,
    Guid releasedBy)
```

**Business Methods**:
- `Activate()` - Make available in catalog
- `Deactivate()` - Remove from catalog

**Domain Events**:
- `ApplicationReleaseCreatedEvent`
- `ApplicationReleaseActivatedEvent`
- `ApplicationReleaseDeactivatedEvent`

---

### ApplicationSnapshot (Entity)

**Purpose**: JSON projection of all events for a release

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `ApplicationReleaseId: Guid` - Foreign key
- `SnapshotData: string` - JSON projection of all ApplicationEvents
- `CreatedAt: DateTime`

**Factory Method**:
```csharp
public static Result<ApplicationSnapshot> Create(
    Guid applicationReleaseId,
    string snapshotData)
```

**Domain Events**:
- `ApplicationSnapshotCreatedEvent`

---

### EntityDefinition (Entity)

**Purpose**: Database entity/table definition

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `ApplicationDefinitionId: Guid` - Foreign key
- `Name: string` - Entity name (e.g., "Customer")
- `DisplayName: string` - User-friendly name (e.g., "Customers")
- `PrimaryKeyAttributeId: Guid?` - Reference to primary key property
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Collections**:
- `Attributes: ICollection<PropertyDefinition>` - Entity properties
- `Relations: ICollection<RelationDefinition>` - Entity relationships

**Factory Method**:
```csharp
public static Result<EntityDefinition> Create(
    Guid applicationDefinitionId,
    string name,
    string displayName)
```

**Business Methods**:
- `Update(string name, string displayName)` - Update entity metadata
- `AddProperty(PropertyDefinition property)` - Add property to entity
- `RemoveProperty(Guid propertyId)` - Remove property from entity
- `AddRelation(RelationDefinition relation)` - Add relationship
- `RemoveRelation(Guid relationId)` - Remove relationship
- `SetPrimaryKey(Guid attributeId)` - Set primary key

**Domain Events**:
- `EntityDefinitionCreatedEvent`
- `EntityDefinitionUpdatedEvent`
- `PropertyAddedToEntityEvent`
- `PropertyRemovedFromEntityEvent`
- `RelationAddedToEntityEvent`
- `RelationRemovedFromEntityEvent`

---

### PropertyDefinition (Entity)

**Purpose**: Entity property/field definition

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `EntityDefinitionId: Guid` - Foreign key
- `Name: string` - Property name (e.g., "firstName")
- `DisplayName: string` - User-friendly name (e.g., "First Name")
- `DataType: PropertyDataType` - String, Number, Boolean, DateTime, etc.
- `IsRequired: bool` - Whether field is mandatory
- `DefaultValue: string?` - Default value as JSON
- `ValidationRules: string?` - Validation rules as JSON
- `Order: int` - Display order
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Factory Method**:
```csharp
public static Result<PropertyDefinition> Create(
    Guid entityDefinitionId,
    string name,
    string displayName,
    PropertyDataType dataType,
    bool isRequired,
    int order)
```

**Business Methods**:
- `Update(string name, string displayName, bool isRequired)` - Update property
- `SetDefaultValue(string defaultValue)` - Set default value
- `SetValidationRules(string validationRules)` - Set validation rules
- `UpdateOrder(int order)` - Change display order

**Domain Events**:
- `PropertyDefinitionCreatedEvent`
- `PropertyDefinitionUpdatedEvent`

---

### RelationDefinition (Entity)

**Purpose**: Entity relationship definition

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `SourceEntityId: Guid` - Source entity foreign key
- `TargetEntityId: Guid` - Target entity foreign key
- `Name: string` - Relation name (e.g., "orders")
- `RelationType: RelationType` - OneToMany, ManyToOne, ManyToMany
- `CascadeDelete: bool` - Whether to cascade delete
- `CreatedAt: DateTime`

**Factory Method**:
```csharp
public static Result<RelationDefinition> Create(
    Guid sourceEntityId,
    Guid targetEntityId,
    string name,
    RelationType relationType,
    bool cascadeDelete)
```

**Business Methods**:
- `UpdateCascadeDelete(bool cascadeDelete)` - Update cascade behavior

**Domain Events**:
- `RelationDefinitionCreatedEvent`
- `RelationDefinitionUpdatedEvent`

---

### ApplicationEvent (Entity)

**Purpose**: Event-sourced changes to application definition

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `ApplicationDefinitionId: Guid` - Foreign key
- `EventType: string` - Type of change (e.g., "NavigationItemAdded")
- `EventData: string` - JSON payload
- `Sequence: int` - Order of events
- `CreatedAt: DateTime`
- `CreatedBy: Guid`

**Factory Method**:
```csharp
public static Result<ApplicationEvent> Create(
    Guid applicationDefinitionId,
    string eventType,
    string eventData,
    int sequence,
    Guid createdBy)
```

**Domain Events**:
- `ApplicationEventAppendedEvent`

---

### NavigationDefinition (Entity)

**Purpose**: Navigation structure (sidebar, breadcrumbs, etc.)

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `ApplicationDefinitionId: Guid` - Foreign key
- `Name: string` - Navigation name
- `ConfigurationJson: string` - Navigation items as JSON
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Factory Method**:
```csharp
public static Result<NavigationDefinition> Create(
    Guid applicationDefinitionId,
    string name,
    string configurationJson)
```

**Business Methods**:
- `Update(string name, string configurationJson)` - Update navigation

**Domain Events**:
- `NavigationDefinitionCreatedEvent`
- `NavigationDefinitionUpdatedEvent`

---

### PageDefinition (Entity)

**Purpose**: Page layout and widgets

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `ApplicationDefinitionId: Guid` - Foreign key
- `Name: string` - Page name
- `Route: string` - URL route
- `ConfigurationJson: string` - Page layout as JSON
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Factory Method**:
```csharp
public static Result<PageDefinition> Create(
    Guid applicationDefinitionId,
    string name,
    string route,
    string configurationJson)
```

**Business Methods**:
- `Update(string name, string route, string configurationJson)` - Update page

**Domain Events**:
- `PageDefinitionCreatedEvent`
- `PageDefinitionUpdatedEvent`

---

### DataSourceDefinition (Entity)

**Purpose**: External data connections

**Base Class**: `Entity<Guid>`

**Properties**:
- `Id: Guid` - Primary key
- `ApplicationDefinitionId: Guid` - Foreign key
- `Name: string` - Data source name
- `Type: DataSourceType` - API, Database, Entity
- `ConfigurationJson: string` - Connection details as JSON
- `CreatedAt: DateTime`
- `UpdatedAt: DateTime?`

**Factory Method**:
```csharp
public static Result<DataSourceDefinition> Create(
    Guid applicationDefinitionId,
    string name,
    DataSourceType type,
    string configurationJson)
```

**Business Methods**:
- `Update(string name, string configurationJson)` - Update data source

**Domain Events**:
- `DataSourceDefinitionCreatedEvent`
- `DataSourceDefinitionUpdatedEvent`

---

## Value Objects

### ApplicationStatus (Enum)
```csharp
public enum ApplicationStatus
{
    Draft = 0,
    Released = 1,
    Archived = 2
}
```

### PropertyDataType (Enum)
```csharp
public enum PropertyDataType
{
    String = 0,
    Number = 1,
    Boolean = 2,
    DateTime = 3,
    Date = 4,
    Time = 5,
    Json = 6
}
```

### RelationType (Enum)
```csharp
public enum RelationType
{
    OneToMany = 0,
    ManyToOne = 1,
    ManyToMany = 2
}
```

### DataSourceType (Enum)
```csharp
public enum DataSourceType
{
    Entity = 0,
    RestApi = 1,
    Database = 2,
    GraphQL = 3
}
```

---

## Domain Events

All events are `record` types inheriting from `IDomainEvent`:

```csharp
public record ApplicationDefinitionCreatedEvent(Guid ApplicationDefinitionId) : IDomainEvent;
public record ApplicationDefinitionUpdatedEvent(Guid ApplicationDefinitionId) : IDomainEvent;
public record ApplicationReleasedEvent(Guid ApplicationDefinitionId, Guid ApplicationReleaseId, string Version) : IDomainEvent;
public record ApplicationDefinitionArchivedEvent(Guid ApplicationDefinitionId) : IDomainEvent;

public record ApplicationReleaseCreatedEvent(Guid ApplicationReleaseId) : IDomainEvent;
public record ApplicationReleaseActivatedEvent(Guid ApplicationReleaseId) : IDomainEvent;
public record ApplicationReleaseDeactivatedEvent(Guid ApplicationReleaseId) : IDomainEvent;

public record NavigationDefinitionCreatedEvent(Guid NavigationDefinitionId) : IDomainEvent;
public record NavigationDefinitionUpdatedEvent(Guid NavigationDefinitionId) : IDomainEvent;

public record PageDefinitionCreatedEvent(Guid PageDefinitionId) : IDomainEvent;
public record PageDefinitionUpdatedEvent(Guid PageDefinitionId) : IDomainEvent;

public record DataSourceDefinitionCreatedEvent(Guid DataSourceDefinitionId) : IDomainEvent;
public record DataSourceDefinitionUpdatedEvent(Guid DataSourceDefinitionId) : IDomainEvent;
```

---

## Repository Interfaces

### IApplicationDefinitionRepository
```csharp
public interface IApplicationDefinitionRepository : IRepository<ApplicationDefinition, Guid>
{
    Task<ApplicationDefinition?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
}
```

### IApplicationReleaseRepository
```csharp
public interface IApplicationReleaseRepository : IRepository<ApplicationRelease, Guid>
{
    Task<List<ApplicationRelease>> GetByApplicationDefinitionIdAsync(Guid applicationDefinitionId, CancellationToken cancellationToken = default);
    Task<ApplicationRelease?> GetActiveReleaseAsync(Guid applicationDefinitionId, CancellationToken cancellationToken = default);
    Task<ApplicationRelease?> GetByVersionAsync(Guid applicationDefinitionId, int major, int minor, int patch, CancellationToken cancellationToken = default);
}
```

### INavigationDefinitionRepository
```csharp
public interface INavigationDefinitionRepository : IRepository<NavigationDefinition, Guid>
{
    Task<List<NavigationDefinition>> GetByApplicationDefinitionIdAsync(Guid applicationDefinitionId, CancellationToken cancellationToken = default);
}
```

### IPageDefinitionRepository
```csharp
public interface IPageDefinitionRepository : IRepository<PageDefinition, Guid>
{
    Task<List<PageDefinition>> GetByApplicationDefinitionIdAsync(Guid applicationDefinitionId, CancellationToken cancellationToken = default);
    Task<PageDefinition?> GetByRouteAsync(Guid applicationDefinitionId, string route, CancellationToken cancellationToken = default);
}
```

### IDataSourceDefinitionRepository
```csharp
public interface IDataSourceDefinitionRepository : IRepository<DataSourceDefinition, Guid>
{
    Task<List<DataSourceDefinition>> GetByApplicationDefinitionIdAsync(Guid applicationDefinitionId, CancellationToken cancellationToken = default);
}
```

### IEntityDefinitionRepository
```csharp
public interface IEntityDefinitionRepository : IRepository<EntityDefinition, Guid>
{
    Task<List<EntityDefinition>> GetByApplicationDefinitionIdAsync(Guid applicationDefinitionId, CancellationToken cancellationToken = default);
    Task<EntityDefinition?> GetByNameAsync(Guid applicationDefinitionId, string name, CancellationToken cancellationToken = default);
}
```

### IPropertyDefinitionRepository
```csharp
public interface IPropertyDefinitionRepository : IRepository<PropertyDefinition, Guid>
{
    Task<List<PropertyDefinition>> GetByEntityDefinitionIdAsync(Guid entityDefinitionId, CancellationToken cancellationToken = default);
    Task<PropertyDefinition?> GetByNameAsync(Guid entityDefinitionId, string name, CancellationToken cancellationToken = default);
}
```

### IRelationDefinitionRepository
```csharp
public interface IRelationDefinitionRepository : IRepository<RelationDefinition, Guid>
{
    Task<List<RelationDefinition>> GetBySourceEntityIdAsync(Guid sourceEntityId, CancellationToken cancellationToken = default);
    Task<List<RelationDefinition>> GetByTargetEntityIdAsync(Guid targetEntityId, CancellationToken cancellationToken = default);
}
```

### IAppBuilderUnitOfWork
```csharp
public interface IAppBuilderUnitOfWork : IUnitOfWork
{
    IApplicationDefinitionRepository ApplicationDefinitions { get; }
    IApplicationReleaseRepository ApplicationReleases { get; }
    IEntityDefinitionRepository EntityDefinitions { get; }
    IPropertyDefinitionRepository PropertyDefinitions { get; }
    IRelationDefinitionRepository RelationDefinitions { get; }
    INavigationDefinitionRepository NavigationDefinitions { get; }
    IPageDefinitionRepository PageDefinitions { get; }
    IDataSourceDefinitionRepository DataSourceDefinitions { get; }
}
```

---

## Domain Services

None required for MVP v2. All business logic is encapsulated in entities.

---

## Validation Rules

### ApplicationDefinition
- ✅ Name: Required, max 200 characters
- ✅ Slug: Required, lowercase alphanumeric + hyphens, unique
- ✅ Description: Required, max 1000 characters

### ApplicationRelease
- ✅ Version: Required, semantic version format (e.g., "1.0.0")
- ✅ ReleaseNotes: Required, max 5000 characters
- ✅ Cannot create release if ApplicationDefinition is Archived

### EntityDefinition
- ✅ Name: Required, max 100 characters, unique per application
- ✅ DisplayName: Required, max 200 characters

### PropertyDefinition
- ✅ Name: Required, max 100 characters, unique per entity
- ✅ DisplayName: Required, max 200 characters
- ✅ DataType: Required
- ✅ Order: Non-negative

### RelationDefinition
- ✅ Name: Required, max 100 characters, unique per source entity
- ✅ RelationType: Required

### NavigationDefinition
- ✅ Name: Required, max 200 characters
- ✅ ConfigurationJson: Required, valid JSON

### PageDefinition
- ✅ Name: Required, max 200 characters
- ✅ Route: Required, unique per application
- ✅ ConfigurationJson: Required, valid JSON

### DataSourceDefinition
- ✅ Name: Required, max 200 characters
- ✅ Type: Required (DataSourceType)
- ✅ ConfigurationJson: Required, valid JSON

---

## Design Decisions

✅ **Immutable Releases** - Once released, ApplicationRelease cannot be modified; snapshot data (NavigationJson, PageJson, DataSourceJson, EntityJson) is stored on the release  
✅ **Normalized Definitions** - EntityDefinition, PropertyDefinition, RelationDefinition, NavigationDefinition, PageDefinition, DataSourceDefinition are first-class entities  
✅ **Result Pattern** - All factory methods and business methods return `Result<T>`  
✅ **Domain Events** - All state changes raise domain events for audit and integration  
✅ **Guard Clauses** - All validation uses `Guard` class from BuildingBlocks.Kernel  

---

## Future Enhancements

### Phase 2: Advanced Features
- Entity modeling (custom entities, fields, relations)
- Workflow engine (approval processes, state machines)
- Role-based access control per application
- Application templates (pre-built applications)

### Phase 3: Enterprise Features
- Multi-language support
- Application versioning (rollback to previous release)
- Application cloning (duplicate existing application)
- Application marketplace (share applications across tenants)

---

## Dependencies

- `BuildingBlocks.Kernel` - Base classes, Result<T>, Guard
- `BuildingBlocks.Infrastructure` - IRepository, IUnitOfWork

---

## Schema

**Schema Name**: `appbuilder`

**Tables** (aligned with appbuilder-migrations.md):
- `application_definitions`
- `entity_definitions`
- `property_definitions`
- `relation_definitions`
- `navigation_definitions`
- `page_definitions`
- `datasource_definitions`
- `application_releases`





