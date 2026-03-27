# Datarizen AI Context - Module Domain Layer Implementation Guide

## Overview

This guide provides step-by-step instructions for implementing the **Domain Layer** of a new module. The Domain Layer contains the core business logic and is the heart of Clean Architecture.

**Target Audience**: AI coding assistants helping developers create new modules.

**Prerequisites**:
- Module name decided (e.g., "Identity", "Tenant", "Product")
- Business requirements documented
- Entity relationships mapped
- Primary key type decided (Guid, string, int, etc.)

**Time Estimate**: 8-12 hours for a typical module with 5-7 entities

---

## Domain Layer Principles

### Core Rules

1. **Zero Infrastructure Dependencies**
   - ❌ No EF Core, no database concerns
   - ❌ No HTTP, no external services
   - ✅ Only BuildingBlocks.Kernel and BuildingBlocks.Infrastructure (for IRepository interface)

2. **Result Pattern Over Exceptions**
   - ❌ Don't throw exceptions for business rule violations
   - ✅ Return `Result<T>` from all factory methods and business operations
   - ✅ Use `Error.Validation()`, `Error.NotFound()`, `Error.Conflict()`, `Error.Failure()`

3. **Immutability and Encapsulation**
   - ✅ Private setters on all properties
   - ✅ Factory methods for creation (`Create()`, not constructors)
   - ✅ Business methods for mutations (`Update()`, `Activate()`, etc.)
   - ✅ Domain events for all state changes

---

## Project Structure

```
/server/src/Product/{ModuleName}
  /{ModuleName}.Domain
    {ModuleName}.Domain.csproj
    /Entities
      {EntityName}.cs
      {ChildEntityName}.cs
      README.md
    /ValueObjects
      {ValueObjectName}.cs
      README.md
    /Events
      {EntityName}CreatedEvent.cs
      {EntityName}UpdatedEvent.cs
      README.md
    /Repositories
      I{EntityName}Repository.cs
      README.md
    /Services
      I{DomainServiceName}.cs
      README.md
    /Enums
      {EnumName}.cs
    /Specifications
      {EntityName}Specification.cs
      README.md
    README.md
```

---

## Step-by-Step Implementation

### Step 1: Create Project Structure (15 minutes)

#### 1.1: Create Domain Project

```bash
cd server/src/Product/{ModuleName}
dotnet new classlib -n {ModuleName}.Domain -f net10.0
```

#### 1.2: Update Project File

**File**: `{ModuleName}.Domain/{ModuleName}.Domain.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Datarizen.{ModuleName}.Domain</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Infrastructure\BuildingBlocks.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**Why BuildingBlocks.Infrastructure?**
- Needed for `IRepository<TEntity, TKey>` interface
- Interface has no EF Core dependencies
- Follows Dependency Inversion Principle

#### 1.3: Create Folder Structure

```bash
cd {ModuleName}.Domain
mkdir Entities ValueObjects Events Repositories Services Enums Specifications
```

---

### Step 2: Define Enums (30 minutes)

**Location**: `{ModuleName}.Domain/Enums/`

**Pattern**: Use enums for fixed sets of values that are part of the domain model.

**Guidelines**:
- ✅ Use XML documentation for each value
- ✅ Start numbering from 1 (not 0) to avoid default value confusion
- ✅ Use PascalCase for enum names and values
- ✅ Keep enums simple (no behavior, just values)

**Example Structure**:
```csharp
namespace Datarizen.{ModuleName}.Domain.Enums;

/// <summary>
/// Represents the {description of what this enum represents}.
/// </summary>
public enum {EnumName}
{
    /// <summary>
    /// {Description of first value}.
    /// </summary>
    {FirstValue} = 1,

    /// <summary>
    /// {Description of second value}.
    /// </summary>
    {SecondValue} = 2
}
```

---

### Step 3: Define Value Objects (1-2 hours)

**Location**: `{ModuleName}.Domain/ValueObjects/`

**Pattern**: Value objects encapsulate domain concepts with validation and equality by value.

**All value objects must**:
- Inherit from `ValueObject` base class
- Use `Create()` factory method returning `Result<T>`
- Have private constructor
- Be immutable (init-only or readonly properties)
- Implement `GetEqualityComponents()`

**Template**:

```csharp
namespace Datarizen.{ModuleName}.Domain.ValueObjects;

using BuildingBlocks.Kernel;

/// <summary>
/// Represents {description of what this value object represents}.
/// </summary>
public sealed class {ValueObjectName} : ValueObject
{
    /// <summary>
    /// Gets the {property description}.
    /// </summary>
    public string Value { get; init; }

    private {ValueObjectName}(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new instance of <see cref="{ValueObjectName}"/>.
    /// </summary>
    /// <param name="value">The {property description}.</param>
    /// <returns>A result containing the {ValueObjectName} or an error.</returns>
    public static Result<{ValueObjectName}> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<{ValueObjectName}>.Failure(Error.Validation(
                "{ModuleName}.{ValueObjectName}.Empty",
                "{ValueObjectName} cannot be empty"));

        // Add additional validation rules here
        // Example: length, format, regex, business rules

        return Result<{ValueObjectName}>.Success(new {ValueObjectName}(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
```

**Common Value Object Patterns**:

1. **Email Address**:
```csharp
public static Result<Email> Create(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return Result<Email>.Failure(Error.Validation(
            "{ModuleName}.Email.Empty",
            "Email address is required"));

    if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        return Result<Email>.Failure(Error.Validation(
            "{ModuleName}.Email.InvalidFormat",
            "Email address format is invalid"));

    return Result<Email>.Success(new Email(value.ToLowerInvariant()));
}
```

2. **Money/Amount**:
```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Result<Money>.Failure(Error.Validation(
                "{ModuleName}.Money.NegativeAmount",
                "Amount cannot be negative"));

        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money>.Failure(Error.Validation(
                "{ModuleName}.Money.InvalidCurrency",
                "Currency is required"));

        return Result<Money>.Success(new Money(amount, currency.ToUpperInvariant()));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

3. **Identifier/Code**:
```csharp
public static Result<{CodeName}> Create(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return Result<{CodeName}>.Failure(Error.Validation(
            "{ModuleName}.{CodeName}.Empty",
            "{CodeName} is required"));

    if (value.Length > 50)
        return Result<{CodeName}>.Failure(Error.Validation(
            "{ModuleName}.{CodeName}.TooLong",
            "{CodeName} cannot exceed 50 characters"));

    if (!Regex.IsMatch(value, @"^[A-Z0-9_-]+$"))
        return Result<{CodeName}>.Failure(Error.Validation(
            "{ModuleName}.{CodeName}.InvalidFormat",
            "{CodeName} must contain only uppercase letters, numbers, hyphens, and underscores"));

    return Result<{CodeName}>.Success(new {CodeName}(value.ToUpperInvariant()));
}
```

---

### Step 4: Define Domain Events (1 hour)

**Location**: `{ModuleName}.Domain/Events/`

**Pattern**: Domain events represent state changes in aggregate roots.

**Naming Convention**: `{EntityName}{Action}Event`

**Examples**: `{EntityName}CreatedEvent`, `{EntityName}UpdatedEvent`, `{EntityName}DeletedEvent`

**Template**:

```csharp
namespace Datarizen.{ModuleName}.Domain.Events;

using BuildingBlocks.Kernel;

/// <summary>
/// Event raised when a {entityname} is {action description}.
/// </summary>
/// <param name="{EntityName}Id">The ID of the {entityname}.</param>
/// <param name="OccurredAt">When the event occurred.</param>
public sealed record {EntityName}{Action}Event(
    Guid {EntityName}Id,
    DateTime OccurredAt
) : IDomainEvent;
```

**Event Payload Guidelines**:
- ✅ Include entity ID (always required)
- ✅ Include changed data (what changed, not entire entity)
- ✅ Include timestamp (OccurredAt)
- ✅ Include actor/user ID if relevant (who triggered it)
- ❌ Don't include entire entity (only relevant data)
- ❌ Don't include navigation properties

**Common Event Patterns**:

1. **Created Event**:
```csharp
public sealed record {EntityName}CreatedEvent(
    Guid {EntityName}Id,
    string Name,
    DateTime OccurredAt
) : IDomainEvent;
```

2. **Updated Event** (with changed fields):
```csharp
public sealed record {EntityName}UpdatedEvent(
    Guid {EntityName}Id,
    string? OldName,
    string? NewName,
    DateTime OccurredAt
) : IDomainEvent;
```

3. **Status Changed Event**:
```csharp
public sealed record {EntityName}StatusChangedEvent(
    Guid {EntityName}Id,
    {StatusEnum} OldStatus,
    {StatusEnum} NewStatus,
    DateTime OccurredAt
) : IDomainEvent;
```

4. **Deleted Event**:
```csharp
public sealed record {EntityName}DeletedEvent(
    Guid {EntityName}Id,
    DateTime OccurredAt
) : IDomainEvent;
```

---

### Step 5: Define Entities (3-4 hours)

**Location**: `{ModuleName}.Domain/Entities/`

**Pattern**: Entities are objects with identity and lifecycle.

#### 5.1: Choose Entity Base Class

**Decision Tree**:
1. Does this entity need tenant isolation? → Yes: Use `TenantScopedEntity<TKey>` or `AuditableTenantScopedEntity<TKey>`
2. Does this entity need audit trail? → Yes: Use `AuditableEntity<TKey>` or `AuditableTenantScopedEntity<TKey>`
3. Is this an aggregate root? → Yes: Use `AggregateRoot<TKey>` (or auditable/tenant-scoped variants)
4. Otherwise → Use `Entity<TKey>`

**Available Base Classes**:

| Base Class | Provides | Use When |
|------------|----------|----------|
| `Entity<TKey>` | Id, equality | Simple entity, no audit, no tenant |
| `AuditableEntity<TKey>` | Id, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy | Need audit trail |
| `TenantScopedEntity<TKey>` | Id, TenantId | Multi-tenant data |
| `AuditableTenantScopedEntity<TKey>` | Id, TenantId, audit fields | Multi-tenant + audit |
| `AggregateRoot<TKey>` | Id, domain events | Root of aggregate |

#### 5.2: Entity Template (Aggregate Root)

```csharp
namespace Datarizen.{ModuleName}.Domain.Entities;

using BuildingBlocks.Kernel;
using Datarizen.{ModuleName}.Domain.Events;
using Datarizen.{ModuleName}.Domain.ValueObjects;
using Datarizen.{ModuleName}.Domain.Enums;

/// <summary>
/// Represents a {entity description}.
/// </summary>
public sealed class {EntityName} : AggregateRoot<Guid>
{
    // Properties with private setters
    /// <summary>
    /// Gets the {property description}.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the {value object description}.
    /// </summary>
    public {ValueObjectName} {PropertyName} { get; private set; } = null!;

    /// <summary>
    /// Gets the {enum description}.
    /// </summary>
    public {EnumName} Status { get; private set; }

    // Navigation properties (for EF Core)
    private readonly List<{ChildEntityName}> _children = new();
    
    /// <summary>
    /// Gets the collection of {child entity description}.
    /// </summary>
    public IReadOnlyCollection<{ChildEntityName}> Children => _children.AsReadOnly();

    // Private parameterless constructor for EF Core
    private {EntityName}() { }

    // Private constructor for factory method
    private {EntityName}(
        Guid id,
        string name,
        {ValueObjectName} {propertyName},
        {EnumName} status)
    {
        Id = id;
        Name = name;
        {PropertyName} = {propertyName};
        Status = status;
    }

    /// <summary>
    /// Creates a new instance of <see cref="{EntityName}"/>.
    /// </summary>
    /// <param name="name">The {property description}.</param>
    /// <param name="{propertyName}">The {value object description}.</param>
    /// <returns>A result containing the {EntityName} or an error.</returns>
    public static Result<{EntityName}> Create(
        string name,
        {ValueObjectName} {propertyName})
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(name))
            return Result<{EntityName}>.Failure(Error.Validation(
                "{ModuleName}.{EntityName}.NameRequired",
                "Name is required"));

        if (name.Length > 100)
            return Result<{EntityName}>.Failure(Error.Validation(
                "{ModuleName}.{EntityName}.NameTooLong",
                "Name cannot exceed 100 characters"));

        // Create entity
        var entity = new {EntityName}(
            Guid.NewGuid(),
            name,
            {propertyName},
            {EnumName}.{DefaultValue});

        // Raise domain event
        entity.RaiseDomainEvent(new {EntityName}CreatedEvent(
            entity.Id,
            entity.Name,
            DateTime.UtcNow));

        return Result<{EntityName}>.Success(entity);
    }

    /// <summary>
    /// Updates the {entity description}.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(Error.Validation(
                "{ModuleName}.{EntityName}.NameRequired",
                "Name is required"));

        if (name.Length > 100)
            return Result.Failure(Error.Validation(
                "{ModuleName}.{EntityName}.NameTooLong",
                "Name cannot exceed 100 characters"));

        var oldName = Name;
        Name = name;

        RaiseDomainEvent(new {EntityName}UpdatedEvent(
            Id,
            oldName,
            Name,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Changes the status of the {entity description}.
    /// </summary>
    /// <param name="newStatus">The new status.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result ChangeStatus({EnumName} newStatus)
    {
        if (Status == newStatus)
            return Result.Failure(Error.Validation(
                "{ModuleName}.{EntityName}.StatusUnchanged",
                "Status is already set to the specified value"));

        var oldStatus = Status;
        Status = newStatus;

        RaiseDomainEvent(new {EntityName}StatusChangedEvent(
            Id,
            oldStatus,
            newStatus,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Adds a {child entity description}.
    /// </summary>
    /// <param name="child">The child entity to add.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result Add{ChildEntityName}({ChildEntityName} child)
    {
        if (_children.Any(c => c.Id == child.Id))
            return Result.Failure(Error.Conflict(
                "{ModuleName}.{EntityName}.ChildAlreadyExists",
                "Child entity already exists"));

        _children.Add(child);

        return Result.Success();
    }
}
```

#### 5.3: Child Entity Template

```csharp
namespace Datarizen.{ModuleName}.Domain.Entities;

using BuildingBlocks.Kernel;

/// <summary>
/// Represents a {child entity description}.
/// </summary>
public sealed class {ChildEntityName} : Entity<Guid>
{
    /// <summary>
    /// Gets the parent {EntityName} ID.
    /// </summary>
    public Guid {EntityName}Id { get; private set; }

    /// <summary>
    /// Gets the {property description}.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    // Navigation property
    public {EntityName} {EntityName} { get; private set; } = null!;

    private {ChildEntityName}() { }

    private {ChildEntityName}(Guid id, Guid {entityName}Id, string name)
    {
        Id = id;
        {EntityName}Id = {entityName}Id;
        Name = name;
    }

    public static Result<{ChildEntityName}> Create(Guid {entityName}Id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<{ChildEntityName}>.Failure(Error.Validation(
                "{ModuleName}.{ChildEntityName}.NameRequired",
                "Name is required"));

        return Result<{ChildEntityName}>.Success(new {ChildEntityName}(
            Guid.NewGuid(),
            {entityName}Id,
            name));
    }
}
```

---

### Step 6: Define Repository Interfaces (1 hour)

**Location**: `{ModuleName}.Domain/Repositories/`

**Pattern**: Repository interfaces define data access contracts.

**Guidelines**:
- ✅ One repository per aggregate root
- ✅ Inherit from `IRepository<TEntity, TKey>`
- ✅ Only add domain-specific query methods
- ❌ Don't duplicate base methods (GetByIdAsync, AddAsync, etc.)
- ❌ Don't return IQueryable (leaks EF Core abstraction)
- ✅ Return `Task<TEntity?>` for single results
- ✅ Return `Task<List<TEntity>>` for collections

**Template**:

```csharp
namespace Datarizen.{ModuleName}.Domain.Repositories;

using BuildingBlocks.Infrastructure;
using Datarizen.{ModuleName}.Domain.Entities;

/// <summary>
/// Repository interface for <see cref="{EntityName}"/> aggregate.
/// </summary>
public interface I{EntityName}Repository : IRepository<{EntityName}, Guid>
{
    /// <summary>
    /// Gets a {entityname} by {unique property}.
    /// </summary>
    /// <param name="{propertyName}">The {property description}.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The {entityname} if found; otherwise, null.</returns>
    Task<{EntityName}?> GetBy{PropertyName}Async(string {propertyName}, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a {entityname} exists with the specified {property}.
    /// </summary>
    /// <param name="{propertyName}">The {property description}.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists; otherwise, false.</returns>
    Task<bool> {PropertyName}ExistsAsync(string {propertyName}, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all {entityname}s with the specified status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of {entityname}s.</returns>
    Task<List<{EntityName}>> GetByStatusAsync({EnumName} status, CancellationToken cancellationToken = default);
}
```

**Common Repository Method Patterns**:

1. **Get by unique property**:
```csharp
Task<{EntityName}?> GetBy{PropertyName}Async(string {propertyName}, CancellationToken cancellationToken = default);
```

2. **Existence check**:
```csharp
Task<bool> {PropertyName}ExistsAsync(string {propertyName}, CancellationToken cancellationToken = default);
```

3. **Get by foreign key**:
```csharp
Task<List<{EntityName}>> GetBy{ForeignKeyEntity}IdAsync(Guid {foreignKeyEntity}Id, CancellationToken cancellationToken = default);
```

4. **Get filtered collection**:
```csharp
Task<List<{EntityName}>> GetActive{EntityName}sAsync(CancellationToken cancellationToken = default);
```

5. **Bulk operations**:
```csharp
Task<int> DeleteBy{ForeignKeyEntity}IdAsync(Guid {foreignKeyEntity}Id, CancellationToken cancellationToken = default);
```

---

### Step 7: Define Domain Service Interfaces (30 minutes)

**Location**: `{ModuleName}.Domain/Services/`

**Pattern**: Domain services encapsulate domain logic that doesn't belong to a single entity.

**When to Use**:
- Logic involves multiple entities
- Logic requires external dependencies (e.g., password hashing, encryption)
- Logic is a domain concept but not entity behavior
- Stateless operations

**Guidelines**:
- ✅ Define interface in Domain layer
- ✅ Implement in Infrastructure layer
- ✅ Use for cross-cutting domain concerns
- ✅ Keep interfaces simple and focused
- ✅ XML documentation on all methods

**Template**:

```csharp
namespace Datarizen.{ModuleName}.Domain.Services;

/// <summary>
/// Domain service for {service description}.
/// </summary>
public interface I{ServiceName}
{
    /// <summary>
    /// {Method description}.
    /// </summary>
    /// <param name="{paramName}">The {param description}.</param>
    /// <returns>{Return description}.</returns>
    Task<Result<{ReturnType}>> {MethodName}Async({ParamType} {paramName}, CancellationToken cancellationToken = default);
}
```

**Common Domain Service Patterns**:

1. **Password Hashing**:
```csharp
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

2. **Validation Service**:
```csharp
public interface I{EntityName}Validator
{
    Task<Result> ValidateAsync({EntityName} entity, CancellationToken cancellationToken = default);
}
```

3. **Policy Service**:
```csharp
public interface I{PolicyName}Policy
{
    Task<Result<bool>> CanExecuteAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default);
}
```

4. **Calculation Service**:
```csharp
public interface I{CalculationName}Calculator
{
    decimal Calculate({InputType} input);
}
```

---

### Step 8: Define Specifications (Optional, 30 minutes)

**Location**: `{ModuleName}.Domain/Specifications/`

**Pattern**: Specifications encapsulate reusable query logic using Ardalis.Specification.

**When to Use**:
- ✅ Complex filtering logic used in multiple places
- ✅ Pagination with filtering
- ✅ Include navigation properties
- ❌ Simple one-off queries (use repository methods instead)

**Template**:

```csharp
namespace Datarizen.{ModuleName}.Domain.Specifications;

using Ardalis.Specification;
using Datarizen.{ModuleName}.Domain.Entities;

/// <summary>
/// Specification for {description}.
/// </summary>
public sealed class {SpecificationName} : Specification<{EntityName}>
{
    public {SpecificationName}({ParamType} {paramName})
    {
        Query.Where(x => x.{Property} == {paramName});
        
        // Optional: Include navigation properties
        Query.Include(x => x.{NavigationProperty});
        
        // Optional: Ordering
        Query.OrderBy(x => x.{Property});
        
        // Optional: Pagination
        // Query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}
```

**Common Specification Patterns**:

1. **Filter by status**:
```csharp
public sealed class Active{EntityName}Specification : Specification<{EntityName}>
{
    public Active{EntityName}Specification()
    {
        Query.Where(x => x.Status == {EnumName}.Active);
    }
}
```

2. **Pagination with filter**:
```csharp
public sealed class {EntityName}PaginationSpecification : Specification<{EntityName}>
{
    public {EntityName}PaginationSpecification(int pageNumber, int pageSize, {EnumName}? status = null)
    {
        if (status.HasValue)
            Query.Where(x => x.Status == status.Value);
        
        Query.OrderBy(x => x.Name)
             .Skip((pageNumber - 1) * pageSize)
             .Take(pageSize);
    }
}
```

3. **Include related entities**:
```csharp
public sealed class {EntityName}WithChildrenSpecification : Specification<{EntityName}>
{
    public {EntityName}WithChildrenSpecification(Guid id)
    {
        Query.Where(x => x.Id == id)
             .Include(x => x.Children);
    }
}
```

---

### Step 9: Create README Files (1 hour)

#### 9.1: Module README

**File**: `server/src/Product/{ModuleName}/README.md`

**Required Sections**:
1. **Overview** - What this module does
2. **Domain Model** - Entities, value objects, relationships
3. **Current Features** - What works now
4. **Feature Roadmap** - Planned enhancements
5. **Dependencies** - What this module depends on
6. **Extension Points** - How to extend

#### 9.2: Domain Layer README

**File**: `server/src/Product/{ModuleName}/{ModuleName}.Domain/README.md`

**Required Sections**:
1. **Current Architecture** - List all entities, value objects, events, repositories, services, enums
2. **Design Decisions** - Why Result<T>, why domain events, etc.
3. **Migration Guide** - How to add new components

#### 9.3: Entities README

**File**: `server/src/Product/{ModuleName}/{ModuleName}.Domain/Entities/README.md`

**Required Sections**:
1. **Overview** - Purpose of this folder
2. **Aggregate Roots** - List with descriptions
3. **Child Entities** - List with ownership
4. **Design Patterns** - Patterns used

---

### Step 10: Build and Verify (30 minutes)

#### Build Project

```bash
cd server/src/Product/{ModuleName}/{ModuleName}.Domain
dotnet build
```

**Expected**: Zero compilation errors

#### Verification Checklist

**Project Structure**:
- [ ] All folders created (Entities, ValueObjects, Events, Repositories, Services, Enums, Specifications)
- [ ] Project file has correct references (BuildingBlocks.Kernel, BuildingBlocks.Infrastructure)
- [ ] Namespace follows convention: `Datarizen.{ModuleName}.Domain.*`

**Code Quality**:
- [ ] All public APIs have XML documentation
- [ ] All entities use Result<T> pattern in factory methods
- [ ] All entities raise domain events for state changes
- [ ] Repository interfaces inherit from `IRepository<TEntity, TKey>`
- [ ] No duplicate base methods in repositories
- [ ] Value objects use Create() factory pattern
- [ ] Domain events follow naming convention

**Documentation**:
- [ ] Module README exists and is complete
- [ ] Domain README exists and is complete
- [ ] Entities README exists and is complete

---

## Common Patterns

### Pattern 1: Simple Entity with Validation

**Use Case**: Entity with basic properties and validation

```csharp
public sealed class {EntityName} : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;

    private {EntityName}() { }

    private {EntityName}(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static Result<{EntityName}> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<{EntityName}>.Failure(Error.Validation(
                "{ModuleName}.{EntityName}.NameRequired",
                "Name is required"));

        return Result<{EntityName}>.Success(new {EntityName}(Guid.NewGuid(), name));
    }
}
```

### Pattern 2: Entity with Value Objects

**Use Case**: Entity using value objects for complex properties

```csharp
public sealed class {EntityName} : AggregateRoot<Guid>
{
    public {ValueObjectName} {PropertyName} { get; private set; } = null!;

    private {EntityName}() { }

    private {EntityName}(Guid id, {ValueObjectName} {propertyName})
    {
        Id = id;
        {PropertyName} = {propertyName};
    }

    public static Result<{EntityName}> Create({ValueObjectName} {propertyName})
    {
        var entity = new {EntityName}(Guid.NewGuid(), {propertyName});
        
        entity.RaiseDomainEvent(new {EntityName}CreatedEvent(
            entity.Id,
            DateTime.UtcNow));

        return Result<{EntityName}>.Success(entity);
    }

    public Result Update{PropertyName}({ValueObjectName} new{PropertyName})
    {
        {PropertyName} = new{PropertyName};

        RaiseDomainEvent(new {EntityName}UpdatedEvent(
            Id,
            DateTime.UtcNow));

        return Result.Success();
    }
}
```

### Pattern 3: Aggregate Root with Children

**Use Case**: Parent entity managing child entities

```csharp
public sealed class {ParentEntityName} : AggregateRoot<Guid>
{
    private readonly List<{ChildEntityName}> _children = new();
    public IReadOnlyCollection<{ChildEntityName}> Children => _children.AsReadOnly();

    public Result Add{ChildEntityName}({ChildEntityName} child)
    {
        if (_children.Any(c => c.Id == child.Id))
            return Result.Failure(Error.Conflict(
                "{ModuleName}.{ParentEntityName}.ChildAlreadyExists",
                "Child already exists"));

        _children.Add(child);

        RaiseDomainEvent(new {ChildEntityName}AddedEvent(
            Id,
            child.Id,
            DateTime.UtcNow));

        return Result.Success();
    }

    public Result Remove{ChildEntityName}(Guid childId)
    {
        var child = _children.FirstOrDefault(c => c.Id == childId);
        if (child == null)
            return Result.Failure(Error.NotFound(
                "{ModuleName}.{ParentEntityName}.ChildNotFound",
                "Child not found"));

        _children.Remove(child);

        RaiseDomainEvent(new {ChildEntityName}RemovedEvent(
            Id,
            childId,
            DateTime.UtcNow));

        return Result.Success();
    }
}
```

### Pattern 4: Tenant-Scoped Entity

**Use Case**: Multi-tenant entity with automatic tenant filtering

```csharp
public sealed class {EntityName} : AuditableTenantScopedEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;

    private {EntityName}() { }

    private {EntityName}(Guid id, Guid tenantId, string name)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
    }

    public static Result<{EntityName}> Create(Guid tenantId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<{EntityName}>.Failure(Error.Validation(
                "{ModuleName}.{EntityName}.NameRequired",
                "Name is required"));

        return Result<{EntityName}>.Success(new {EntityName}(
            Guid.NewGuid(),
            tenantId,
            name));
    }
}
```

---

## Final Checklist

**Domain Layer Checklist**:

1. ✅ Project structure created with all folders
2. ✅ Project file references BuildingBlocks.Kernel and BuildingBlocks.Infrastructure
3. ✅ Enums defined with XML documentation
4. ✅ Value objects inherit from `ValueObject`, have `Create()` factory, validation
5. ✅ Domain events use `record` types, inherit from `IDomainEvent`
6. ✅ Entities inherit from appropriate base class (`Entity<TKey>`, `AuditableEntity<TKey>`, `TenantScopedEntity<TKey>`, `AggregateRoot<TKey>`)
7. ✅ Entities have private setters, `Create()` factory, business methods
8. ✅ Entities raise domain events for state changes
9. ✅ Repository interfaces inherit from `IRepository<TEntity, TKey>`
10. ✅ Repository interfaces only define domain-specific methods
11. ✅ Domain service interfaces defined (implementation in Infrastructure)
12. ✅ All public APIs have XML documentation
13. ✅ README files created (Module, Domain, Entities)
14. ✅ Project builds with zero errors

**Time Estimate**: 8-12 hours for typical module

**Next Steps**: Proceed to Application Layer (Commands, Queries, DTOs, Mappers)

---

## Reference Documentation

- **Repository Pattern**: `server/src/BuildingBlocks/Infrastructure/README.md`
- **Module Structure**: `docs/ai-context/05-MODULES.md`
- **Building Blocks**: `docs/ai-context/03-BUILDING-BLOCKS.md`
- **Coding Conventions**: `docs/ai-context/08-SERVER-CODING-CONVENTIONS.md`


## Domain Events

### Base Class (BuildingBlocks.Kernel)

```csharp
// server/src/BuildingBlocks/Kernel/Domain/DomainEvent.cs
public abstract record DomainEvent(DateTime OccurredAt) : INotification;
```

**Key Points**:
- ✅ Record type for immutability
- ✅ Inherits from `INotification` (MediatR)
- ✅ `OccurredAt` timestamp required (from `IDateTimeProvider`)

### Implementation Examples

**Simple Domain Event**:

```csharp
// server/src/Product/Identity/Identity.Domain/Events/UserCreatedEvent.cs
public sealed record UserCreatedEvent(
    Guid UserId,
    string Email,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
```

**Domain Event with Multiple Properties**:

```csharp
// server/src/Product/Identity/Identity.Domain/Events/UserUpdatedEvent.cs
public sealed record UserUpdatedEvent(
    Guid UserId,
    string DisplayName,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
```

**Domain Event with Complex Data**:

```csharp
// Example: Order placed event
public sealed record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency,
    List<OrderItemDto> Items,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
```

### Raising Domain Events in Entities

```csharp
public class User : AggregateRoot<Guid>
{
    // ... properties ...

    public static Result<User> Create(
        Guid defaultTenantId,
        Email email,
        string displayName,
        IDateTimeProvider dateTimeProvider)
    {
        // ... validation ...

        var user = new User
        {
            Id = Guid.NewGuid(),
            DefaultTenantId = defaultTenantId,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = dateTimeProvider.UtcNow
        };

        // Raise domain event with timestamp
        user.AddDomainEvent(new UserCreatedEvent(
            user.Id,
            user.Email.Value,
            dateTimeProvider.UtcNow));

        return Result<User>.Success(user);
    }

    public Result Update(string displayName, IDateTimeProvider dateTimeProvider)
    {
        // ... validation ...

        DisplayName = displayName;
        UpdatedAt = dateTimeProvider.UtcNow;

        // Raise domain event
        user.AddDomainEvent(new UserUpdatedEvent(
            Id,
            DisplayName,
            dateTimeProvider.UtcNow));

        return Result.Success();
    }
}
```

### Domain Event Handlers

**In-Process Handler (Monolith)**:

```csharp
// server/src/Product/Identity/Identity.Application/Events/UserCreatedEventHandler.cs
public sealed class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IEmailService _emailService;

    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> _logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId} at {OccurredAt}", 
            notification.UserId, 
            notification.OccurredAt);

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(
            notification.Email,
            cancellationToken);
    }
}
```

**Cross-Module Handler (via Integration Events)**:

```csharp
// server/src/Product/Notifications/Notifications.Application/IntegrationEvents/UserCreatedIntegrationEventHandler.cs
public sealed class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly INotificationService _notificationService;

    public UserCreatedIntegrationEventHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Handle(UserCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        // Create notification for new user
        await _notificationService.CreateNotificationAsync(
            @event.UserId,
            "Welcome to Datarizen!",
            "Your account has been created successfully.",
            cancellationToken);
    }
}
```

### Domain Event Processing (Outbox Pattern)

```csharp
// In DbContext.SaveChangesAsync()
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Get all domain events from aggregate roots
    var domainEvents = ChangeTracker.Entries<IAggregateRoot>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();
    
    // Save domain events to outbox for reliable processing
    foreach (var domainEvent in domainEvents)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredAt = domainEvent.OccurredAt, // Use event's timestamp
            ProcessedAt = null
        };
        
        OutboxMessages.Add(outboxMessage);
    }
    
    // Clear domain events after saving to outbox
    foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
    {
        entry.Entity.ClearDomainEvents();
    }
    
    return await base.SaveChangesAsync(cancellationToken);
}
```

### Background Job Processing Outbox

```csharp
// server/src/BuildingBlocks/Infrastructure/Outbox/OutboxProcessor.cs
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            // Get unprocessed messages
            var messages = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.OccurredAt)
                .Take(10)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    // Deserialize and publish event
                    var eventType = Type.GetType(message.Type)!;
                    var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as INotification;

                    await publisher.Publish(domainEvent!, stoppingToken);

                    // Mark as processed
                    message.ProcessedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```


## Domain Events

### Base Class (BuildingBlocks.Kernel)

```csharp
// server/src/BuildingBlocks/Kernel/Domain/DomainEvent.cs
public abstract record DomainEvent(DateTime OccurredAt) : INotification;
```

**Key Points**:
- ✅ Record type for immutability
- ✅ Inherits from `INotification` (MediatR)
- ✅ `OccurredAt` timestamp required (from `IDateTimeProvider`)

### Implementation Examples

**Simple Domain Event**:

```csharp
// server/src/Product/Identity/Identity.Domain/Events/UserCreatedEvent.cs
public sealed record UserCreatedEvent(
    Guid UserId,
    string Email,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
```

**Domain Event with Multiple Properties**:

```csharp
// server/src/Product/Identity/Identity.Domain/Events/UserUpdatedEvent.cs
public sealed record UserUpdatedEvent(
    Guid UserId,
    string DisplayName,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
```

**Domain Event with Complex Data**:

```csharp
// Example: Order placed event
public sealed record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency,
    List<OrderItemDto> Items,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
```

### Raising Domain Events in Entities

```csharp
public class User : AggregateRoot<Guid>
{
    // ... properties ...

    public static Result<User> Create(
        Guid defaultTenantId,
        Email email,
        string displayName,
        IDateTimeProvider dateTimeProvider)
    {
        // ... validation ...

        var user = new User
        {
            Id = Guid.NewGuid(),
            DefaultTenantId = defaultTenantId,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = dateTimeProvider.UtcNow
        };

        // Raise domain event with timestamp
        user.AddDomainEvent(new UserCreatedEvent(
            user.Id,
            user.Email.Value,
            dateTimeProvider.UtcNow));

        return Result<User>.Success(user);
    }

    public Result Update(string displayName, IDateTimeProvider dateTimeProvider)
    {
        // ... validation ...

        DisplayName = displayName;
        UpdatedAt = dateTimeProvider.UtcNow;

        // Raise domain event
        user.AddDomainEvent(new UserUpdatedEvent(
            Id,
            DisplayName,
            dateTimeProvider.UtcNow));

        return Result.Success();
    }
}
```

### Domain Event Handlers

**In-Process Handler (Monolith)**:

```csharp
// server/src/Product/Identity/Identity.Application/Events/UserCreatedEventHandler.cs
public sealed class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IEmailService _emailService;

    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> _logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId} at {OccurredAt}", 
            notification.UserId, 
            notification.OccurredAt);

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(
            notification.Email,
            cancellationToken);
    }
}
```

**Cross-Module Handler (via Integration Events)**:

```csharp
// server/src/Product/Notifications/Notifications.Application/IntegrationEvents/UserCreatedIntegrationEventHandler.cs
public sealed class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly INotificationService _notificationService;

    public UserCreatedIntegrationEventHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Handle(UserCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        // Create notification for new user
        await _notificationService.CreateNotificationAsync(
            @event.UserId,
            "Welcome to Datarizen!",
            "Your account has been created successfully.",
            cancellationToken);
    }
}
```

### Domain Event Processing (Outbox Pattern)

```csharp
// In DbContext.SaveChangesAsync()
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Get all domain events from aggregate roots
    var domainEvents = ChangeTracker.Entries<IAggregateRoot>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();
    
    // Save domain events to outbox for reliable processing
    foreach (var domainEvent in domainEvents)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredAt = domainEvent.OccurredAt, // Use event's timestamp
            ProcessedAt = null
        };
        
        OutboxMessages.Add(outboxMessage);
    }
    
    // Clear domain events after saving to outbox
    foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
    {
        entry.Entity.ClearDomainEvents();
    }
    
    return await base.SaveChangesAsync(cancellationToken);
}
```

### Background Job Processing Outbox

```csharp
// server/src/BuildingBlocks/Infrastructure/Outbox/OutboxProcessor.cs
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            // Get unprocessed messages
            var messages = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.OccurredAt)
                .Take(10)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    // Deserialize and publish event
                    var eventType = Type.GetType(message.Type)!;
                    var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as INotification;

                    await publisher.Publish(domainEvent!, stoppingToken);

                    // Mark as processed
                    message.ProcessedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```


