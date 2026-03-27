# AppBuilder Module - Application Layer

**Status**: ✅ Updated to align with new domain model  
**Last Updated**: 2026-02-11  
**Module**: AppBuilder  
**Layer**: Application  

---

## Overview

The Application layer implements CQRS commands/queries, DTOs, validators, and application services for the AppBuilder module.

**Key Changes from Domain Update**:
- ✅ Removed ApplicationSchema commands/queries
- ✅ Added EntityDefinition, PropertyDefinition, RelationDefinition commands/queries
- ✅ Renamed all *Component to *Definition
- ✅ Split SnapshotJson into NavigationJson, PageJson, DataSourceJson, EntityJson
- ✅ Added Major/Minor/Patch version fields
- ✅ Removed IconUrl and CreatedBy from ApplicationDefinition

---

## 1. DTOs (Data Transfer Objects)

### 1.1 ApplicationDefinition DTOs

```csharp
public record ApplicationDefinitionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public ApplicationStatus Status { get; init; }
    public string? CurrentVersion { get; init; }
    public bool IsPublic { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateApplicationDefinitionDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsPublic { get; init; }
}

public record UpdateApplicationDefinitionDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
```

---

### 1.2 EntityDefinition DTOs

```csharp
public record EntityDefinitionDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Dictionary<string, object> Attributes { get; init; } = new();
    public string? PrimaryKey { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateEntityDefinitionDto
{
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
    public string? PrimaryKey { get; init; }
}

public record UpdateEntityDefinitionDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
}
```

---

### 1.3 PropertyDefinition DTOs

```csharp
public record PropertyDefinitionDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public Dictionary<string, object> ValidationRules { get; init; } = new();
    public int Order { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreatePropertyDefinitionDto
{
    public Guid EntityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public Dictionary<string, object>? ValidationRules { get; init; }
    public int Order { get; init; }
}

public record UpdatePropertyDefinitionDto
{
    public string DisplayName { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public Dictionary<string, object>? ValidationRules { get; init; }
    public int Order { get; init; }
}
```

---

### 1.4 RelationDefinition DTOs

```csharp
public record RelationDefinitionDto
{
    public Guid Id { get; init; }
    public Guid SourceEntityId { get; init; }
    public Guid TargetEntityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public RelationType RelationType { get; init; }
    public bool CascadeDelete { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateRelationDefinitionDto
{
    public Guid SourceEntityId { get; init; }
    public Guid TargetEntityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public RelationType RelationType { get; init; }
    public bool CascadeDelete { get; init; }
}

public record UpdateRelationDefinitionDto
{
    public RelationType RelationType { get; init; }
    public bool CascadeDelete { get; init; }
}
```

---

### 1.5 NavigationDefinition DTOs

```csharp
public record NavigationDefinitionDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record CreateNavigationDefinitionDto
{
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
}

public record UpdateNavigationDefinitionDto
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
}
```

---

### 1.6 PageDefinition DTOs

```csharp
public record PageDefinitionDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record CreatePageDefinitionDto
{
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
}

public record UpdatePageDefinitionDto
{
    public string Name { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
}
```

---

### 1.7 DataSourceDefinition DTOs

```csharp
public record DataSourceDefinitionDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DataSourceType Type { get; init; }
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record CreateDataSourceDefinitionDto
{
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DataSourceType Type { get; init; }
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
}

public record UpdateDataSourceDefinitionDto
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object> ConfigurationJson { get; init; } = new();
}
```

---

### 1.8 ApplicationRelease DTOs

```csharp
public record ApplicationReleaseDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public int Major { get; init; }
    public int Minor { get; init; }
    public int Patch { get; init; }
    public string Version => $"{Major}.{Minor}.{Patch}";
    public string? ReleaseNotes { get; init; }
    public bool IsActive { get; init; }
    public DateTime ReleasedAt { get; init; }
}

public record ApplicationReleaseDetailDto : ApplicationReleaseDto
{
    public string NavigationJson { get; init; } = string.Empty;
    public string PageJson { get; init; } = string.Empty;
    public string DataSourceJson { get; init; } = string.Empty;
    public string EntityJson { get; init; } = string.Empty;
}

public record CreateApplicationReleaseDto
{
    public Guid ApplicationId { get; init; }
    public int Major { get; init; }
    public int Minor { get; init; }
    public int Patch { get; init; }
    public string? ReleaseNotes { get; init; }
}
```

---

## 2. Commands

### 2.1 ApplicationDefinition Commands

```csharp
public record CreateApplicationDefinitionCommand(
    string Name,
    string Description,
    string Slug,
    bool IsPublic
) : IRequest<Result<Guid>>;

public record UpdateApplicationDefinitionCommand(
    Guid Id,
    string Name,
    string Description
) : IRequest<Result>;

public record DeleteApplicationDefinitionCommand(
    Guid Id
) : IRequest<Result>;

public record ArchiveApplicationDefinitionCommand(
    Guid Id
) : IRequest<Result>;
```

---

### 2.2 EntityDefinition Commands

```csharp
public record CreateEntityDefinitionCommand(
    Guid ApplicationId,
    string Name,
    string DisplayName,
    string? Description,
    Dictionary<string, object>? Attributes,
    string? PrimaryKey
) : IRequest<Result<Guid>>;

public record UpdateEntityDefinitionCommand(
    Guid Id,
    string DisplayName,
    string? Description,
    Dictionary<string, object>? Attributes
) : IRequest<Result>;

public record DeleteEntityDefinitionCommand(
    Guid Id
) : IRequest<Result>;
```

---

### 2.3 PropertyDefinition Commands

```csharp
public record CreatePropertyDefinitionCommand(
    Guid EntityId,
    string Name,
    string DisplayName,
    string DataType,
    bool IsRequired,
    string? DefaultValue,
    Dictionary<string, object>? ValidationRules,
    int Order
) : IRequest<Result<Guid>>;

public record UpdatePropertyDefinitionCommand(
    Guid Id,
    string DisplayName,
    bool IsRequired,
    string? DefaultValue,
    Dictionary<string, object>? ValidationRules,
    int Order
) : IRequest<Result>;

public record DeletePropertyDefinitionCommand(
    Guid Id
) : IRequest<Result>;

public record ReorderPropertiesCommand(
    Guid EntityId,
    List<Guid> PropertyIds
) : IRequest<Result>;
```

---

### 2.4 RelationDefinition Commands

```csharp
public record CreateRelationDefinitionCommand(
    Guid SourceEntityId,
    Guid TargetEntityId,
    string Name,
    RelationType RelationType,
    bool CascadeDelete
) : IRequest<Result<Guid>>;

public record UpdateRelationDefinitionCommand(
    Guid Id,
    RelationType RelationType,
    bool CascadeDelete
) : IRequest<Result>;

public record DeleteRelationDefinitionCommand(
    Guid Id
) : IRequest<Result>;
```

---

### 2.5 NavigationDefinition Commands

```csharp
public record CreateNavigationDefinitionCommand(
    Guid ApplicationId,
    string Name,
    Dictionary<string, object> ConfigurationJson
) : IRequest<Result<Guid>>;

public record UpdateNavigationDefinitionCommand(
    Guid Id,
    string Name,
    Dictionary<string, object> ConfigurationJson
) : IRequest<Result>;

public record DeleteNavigationDefinitionCommand(
    Guid Id
) : IRequest<Result>;
```

---

### 2.6 PageDefinition Commands

```csharp
public record CreatePageDefinitionCommand(
    Guid ApplicationId,
    string Name,
    string Route,
    Dictionary<string, object> ConfigurationJson
) : IRequest<Result<Guid>>;

public record UpdatePageDefinitionCommand(
    Guid Id,
    string Name,
    string Route,
    Dictionary<string, object> ConfigurationJson
) : IRequest<Result>;

public record DeletePageDefinitionCommand(
    Guid Id
) : IRequest<Result>;
```

---

### 2.7 DataSourceDefinition Commands

```csharp
public record CreateDataSourceDefinitionCommand(
    Guid ApplicationId,
    string Name,
    DataSourceType Type,
    Dictionary<string, object> ConfigurationJson
) : IRequest<Result<Guid>>;

public record UpdateDataSourceDefinitionCommand(
    Guid Id,
    string Name,
    Dictionary<string, object> ConfigurationJson
) : IRequest<Result>;

public record DeleteDataSourceDefinitionCommand(
    Guid Id
) : IRequest<Result>;
```

---

### 2.8 ApplicationRelease Commands

```csharp
public record CreateApplicationReleaseCommand(
    Guid ApplicationId,
    int Major,
    int Minor,
    int Patch,
    string? ReleaseNotes
) : IRequest<Result<Guid>>;

public record ActivateApplicationReleaseCommand(
    Guid Id
) : IRequest<Result>;

public record DeactivateApplicationReleaseCommand(
    Guid Id
) : IRequest<Result>;
```

---

## 3. Queries

### 3.1 ApplicationDefinition Queries

```csharp
public record GetApplicationDefinitionByIdQuery(
    Guid Id
) : IRequest<Result<ApplicationDefinitionDto>>;

public record GetApplicationDefinitionBySlugQuery(
    string Slug
) : IRequest<Result<ApplicationDefinitionDto>>;

public record GetAllApplicationDefinitionsQuery(
    ApplicationStatus? Status = null
) : IRequest<Result<List<ApplicationDefinitionDto>>>;
```

---

### 3.2 EntityDefinition Queries

```csharp
public record GetEntityDefinitionByIdQuery(
    Guid Id
) : IRequest<Result<EntityDefinitionDto>>;

public record GetEntitiesByApplicationQuery(
    Guid ApplicationId
) : IRequest<Result<List<EntityDefinitionDto>>>;

public record GetEntityWithPropertiesQuery(
    Guid EntityId
) : IRequest<Result<EntityDefinitionWithPropertiesDto>>;

public record EntityDefinitionWithPropertiesDto : EntityDefinitionDto
{
    public List<PropertyDefinitionDto> Properties { get; init; } = new();
    public List<RelationDefinitionDto> Relations { get; init; } = new();
}
```

---

### 3.3 PropertyDefinition Queries

```csharp
public record GetPropertyDefinitionByIdQuery(
    Guid Id
) : IRequest<Result<PropertyDefinitionDto>>;

public record GetPropertiesByEntityQuery(
    Guid EntityId
) : IRequest<Result<List<PropertyDefinitionDto>>>;
```

---

### 3.4 RelationDefinition Queries

```csharp
public record GetRelationDefinitionByIdQuery(
    Guid Id
) : IRequest<Result<RelationDefinitionDto>>;

public record GetRelationsByEntityQuery(
    Guid EntityId
) : IRequest<Result<List<RelationDefinitionDto>>>;
```

---

### 3.5 NavigationDefinition Queries

```csharp
public record GetNavigationDefinitionByIdQuery(
    Guid Id
) : IRequest<Result<NavigationDefinitionDto>>;

public record GetNavigationsByApplicationQuery(
    Guid ApplicationId
) : IRequest<Result<List<NavigationDefinitionDto>>>;
```

---

### 3.6 PageDefinition Queries

```csharp
public record GetPageDefinitionByIdQuery(
    Guid Id
) : IRequest<Result<PageDefinitionDto>>;

public record GetPagesByApplicationQuery(
    Guid ApplicationId
) : IRequest<Result<List<PageDefinitionDto>>>;
```

---

### 3.7 DataSourceDefinition Queries

```csharp
public record GetDataSourceDefinitionByIdQuery(
    Guid Id
) : IRequest<Result<DataSourceDefinitionDto>>;

public record GetDataSourcesByApplicationQuery(
    Guid ApplicationId
) : IRequest<Result<List<DataSourceDefinitionDto>>>;
```

---

### 3.8 ApplicationRelease Queries

```csharp
public record GetApplicationReleaseByIdQuery(
    Guid Id
) : IRequest<Result<ApplicationReleaseDto>>;

public record GetApplicationReleaseDetailByIdQuery(
    Guid Id
) : IRequest<Result<ApplicationReleaseDetailDto>>;

public record GetReleasesByApplicationQuery(
    Guid ApplicationId
) : IRequest<Result<List<ApplicationReleaseDto>>>;

public record GetActiveReleaseByApplicationQuery(
    Guid ApplicationId
) : IRequest<Result<ApplicationReleaseDto>>;
```

---

## 4. Validators

### 4.1 CreateEntityDefinitionCommandValidator

```csharp
public class CreateEntityDefinitionCommandValidator 
    : AbstractValidator<CreateEntityDefinitionCommand>
{
    public CreateEntityDefinitionCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters")
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage("Name must start with a letter and contain only letters, numbers, and underscores");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("DisplayName is required")
            .MaximumLength(200)
            .WithMessage("DisplayName must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 1000 characters");
    }
}
```

---

### 4.2 CreatePropertyDefinitionCommandValidator

```csharp
public class CreatePropertyDefinitionCommandValidator 
    : AbstractValidator<CreatePropertyDefinitionCommand>
{
    private static readonly string[] ValidDataTypes = new[]
    {
        "String", "Number", "Boolean", "Date", "DateTime", 
        "Email", "Phone", "Url", "Json", "Uuid"
    };

    public CreatePropertyDefinitionCommandValidator()
    {
        RuleFor(x => x.EntityId)
            .NotEmpty()
            .WithMessage("EntityId is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters")
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage("Name must start with a letter and contain only letters, numbers, and underscores");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("DisplayName is required")
            .MaximumLength(200)
            .WithMessage("DisplayName must not exceed 200 characters");

        RuleFor(x => x.DataType)
            .NotEmpty()
            .WithMessage("DataType is required")
            .Must(dt => ValidDataTypes.Contains(dt))
            .WithMessage($"DataType must be one of: {string.Join(", ", ValidDataTypes)}");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Order must be non-negative");
    }
}
```

---

### 4.3 CreateApplicationReleaseCommandValidator

```csharp
public class CreateApplicationReleaseCommandValidator 
    : AbstractValidator<CreateApplicationReleaseCommand>
{
    public CreateApplicationReleaseCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required");

        RuleFor(x => x.Major)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Major version must be non-negative");

        RuleFor(x => x.Minor)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minor version must be non-negative");

        RuleFor(x => x.Patch)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Patch version must be non-negative");

        RuleFor(x => x.ReleaseNotes)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.ReleaseNotes))
            .WithMessage("ReleaseNotes must not exceed 5000 characters");
    }
}
```

---

## Success Criteria

- ✅ All DTOs created for new domain entities
- ✅ All commands/queries created
- ✅ All validators created
- ✅ Removed ApplicationSchema references
- ✅ Added EntityDefinition, PropertyDefinition, RelationDefinition
- ✅ Renamed *Component to *Definition
- ✅ Split SnapshotJson into 4 separate JSON fields
- ✅ Added Major/Minor/Patch version support
- ✅ Removed IconUrl and CreatedBy

---

## Dependencies

- `AppBuilder.Domain` - Entities, repositories, domain events
- `BuildingBlocks.Kernel` - Result<T>, Error, Guard
- `BuildingBlocks.Application` - ICommand, IQuery, ICommandHandler, IQueryHandler
- `FluentValidation` - Input validation
- `MediatR` - CQRS implementation

---

## File Structure

```
AppBuilder.Application/
├── Commands/
│   ├── CreateApplicationDefinitionCommand.cs
│   ├── UpdateApplicationDefinitionCommand.cs
│   ├── DeleteApplicationDefinitionCommand.cs
│   ├── ArchiveApplicationDefinitionCommand.cs
│   ├── CreateApplicationReleaseCommand.cs
│   ├── CreateEntityDefinitionCommand.cs
│   ├── UpdateEntityDefinitionCommand.cs
│   ├── DeleteEntityDefinitionCommand.cs
│   ├── CreatePropertyDefinitionCommand.cs
│   ├── UpdatePropertyDefinitionCommand.cs
│   ├── DeletePropertyDefinitionCommand.cs
│   ├── ReorderPropertiesCommand.cs
│   ├── CreateRelationDefinitionCommand.cs
│   ├── UpdateRelationDefinitionCommand.cs
│   ├── DeleteRelationDefinitionCommand.cs
│   ├── CreateNavigationDefinitionCommand.cs
│   ├── UpdateNavigationDefinitionCommand.cs
│   ├── DeleteNavigationDefinitionCommand.cs
│   ├── CreatePageDefinitionCommand.cs
│   ├── UpdatePageDefinitionCommand.cs
│   ├── DeletePageDefinitionCommand.cs
│   ├── CreateDataSourceDefinitionCommand.cs
│   ├── UpdateDataSourceDefinitionCommand.cs
│   └── DeleteDataSourceDefinitionCommand.cs
├── Queries/
│   ├── GetApplicationDefinitionByIdQuery.cs
│   ├── GetApplicationDefinitionBySlugQuery.cs
│   ├── GetAllApplicationDefinitionsQuery.cs
│   ├── GetApplicationReleaseByIdQuery.cs
│   ├── GetApplicationReleaseDetailByIdQuery.cs
│   ├── GetReleasesByApplicationQuery.cs
│   ├── GetActiveReleaseByApplicationQuery.cs
│   ├── GetEntityDefinitionByIdQuery.cs
│   ├── GetEntitiesByApplicationQuery.cs
│   ├── GetEntityWithPropertiesQuery.cs
│   ├── GetPropertyDefinitionByIdQuery.cs
│   ├── GetPropertiesByEntityQuery.cs
│   ├── GetRelationDefinitionByIdQuery.cs
│   ├── GetRelationsByEntityQuery.cs
│   ├── GetNavigationDefinitionByIdQuery.cs
│   ├── GetNavigationsByApplicationQuery.cs
│   ├── GetPageDefinitionByIdQuery.cs
│   ├── GetPagesByApplicationQuery.cs
│   ├── GetDataSourceDefinitionByIdQuery.cs
│   └── GetDataSourcesByApplicationQuery.cs
├── DTOs/
│   ├── ApplicationDefinitionDto.cs
│   ├── ApplicationReleaseDto.cs
│   ├── ApplicationReleaseDetailDto.cs
│   ├── EntityDefinitionDto.cs
│   ├── PropertyDefinitionDto.cs
│   ├── RelationDefinitionDto.cs
│   ├── NavigationDefinitionDto.cs
│   ├── PageDefinitionDto.cs
│   └── DataSourceDefinitionDto.cs
├── Validators/
│   ├── CreateApplicationDefinitionCommandValidator.cs
│   ├── CreateEntityDefinitionCommandValidator.cs
│   ├── CreatePropertyDefinitionCommandValidator.cs
│   ├── CreateApplicationReleaseCommandValidator.cs
│   └── (other command validators)
└── DependencyInjection.cs
```

---

## Next Steps

1. ✅ Implement all command handlers
2. ✅ Implement all query handlers
3. ✅ Implement EventProjectionService
4. ✅ Implement SchemaGenerationService
5. ✅ Implement FluentValidation validators
6. ✅ Implement domain event handlers
7. ✅ Add unit tests for all handlers
8. ✅ Add integration tests for complete workflows




