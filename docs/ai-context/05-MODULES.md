# Datarizen AI Context - Module Template

## What is a Module?

A **module** is a self-contained business capability with:
- Clear business boundary (e.g., TenantManagement, UserManagement, ProductCatalog)
- Own database schema
- Independent deployment capability
- Well-defined public contracts
- Internal implementation hidden from other modules

## When to Create a New Module?

### Create New Module When:
- ✅ Represents distinct business capability
- ✅ Has different scaling requirements
- ✅ Owned by different team
- ✅ Has independent release cycle
- ✅ Requires data isolation

### Extend Existing Module When:
- ❌ Closely related to existing module's domain
- ❌ Shares same aggregate roots
- ❌ Always deployed together
- ❌ No clear business boundary

## Module Project Structure

Every module consists of **7 projects**:

```
/server/src/Product/{ModuleName}
  /{ModuleName}.Module
    {ModuleName}.Module.csproj
    {ModuleName}Module.cs
  
  /{ModuleName}.Api
    {ModuleName}.Api.csproj
    /Controllers
      {EntityName}Controller.cs
    /Filters
      ValidationFilter.cs
  
  /{ModuleName}.Domain
    {ModuleName}.Domain.csproj
    /Entities
    /ValueObjects
    /Events
    /Specifications
  
  /{ModuleName}.Application
    {ModuleName}.Application.csproj
    /Commands
    /Queries
    /DTOs
    /Mappers
    /Behaviors
  
  /{ModuleName}.Infrastructure
    {ModuleName}.Infrastructure.csproj
    /Persistence
    /ExternalServices
    /BackgroundJobs
  
  /{ModuleName}.Migrations
    {ModuleName}.Migrations.csproj
    /Migrations
    README.md
  
  /{ModuleName}.Contracts
    {ModuleName}.Contracts.csproj
    /Events
    /DTOs
    /Interfaces
```

### Project Dependencies

```
Module (startup)
  ↓ references
  ├─ Api
  ├─ Domain
  ├─ Application
  ├─ Infrastructure
  └─ Contracts

Api
  ↓ references
  ├─ Application
  └─ Contracts

Infrastructure
  ↓ references
  ├─ Domain
  └─ Application

Application
  ↓ references
  └─ Domain

Migrations
  ↓ references
  └─ Infrastructure

Contracts
  (no dependencies)

Domain
  (no dependencies except BuildingBlocks.Kernel)
```

**Dependency Rules (MUST ENFORCE)**:

| Layer | CAN Reference | CANNOT Reference |
|-------|---------------|------------------|
| **Domain** | BuildingBlocks.Kernel only | Application, Infrastructure, Api, other modules |
| **Application** | Domain, BuildingBlocks.Kernel | Infrastructure, Api, other modules |
| **Infrastructure** | Domain, Application, BuildingBlocks.* | Api, other modules |
| **Api** | Application, Contracts, BuildingBlocks.Web | Domain, Infrastructure, other modules |
| **Contracts** | None (pure DTOs/interfaces) | Any project |
| **Module** | All module projects, BuildingBlocks.Web | Other modules |

### Controller Conventions

- **Location**: Controllers live in `{ModuleName}.Api/Controllers`.
- **Route prefix**: Use `api/{modulename}` (lowercase, e.g., `api/tenant`, `api/identity`) to make module boundaries explicit.
- **Flow**: Controllers should be thin and delegate work to Application layer via MediatR commands/queries.
- **Naming**: `{EntityName}Controller` (e.g., `TenantController`, `UserController`)

## Layer 1: Module Project (Startup)

**Purpose**: Module registration, configuration, and startup logic.

### The `IModule` Contract

All modules implement `IModule` interface from `BuildingBlocks.Web.Modules`:

**File**: `{ModuleName}.Module/{ModuleName}Module.cs`

```csharp
using BuildingBlocks.Web.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {ModuleName}.Module;

/// <summary>
/// Module composition root for {ModuleName}.
/// </summary>
public sealed class {ModuleName}Module : IModule
{
    public string ModuleName => "{ModuleName}";
    public string SchemaName => "{modulename}";  // lowercase, no "_schema" suffix

    public string[] GetMigrationDependencies()
    {
        // Return module names this module depends on
        // Example: return new[] { "Tenant", "Identity" };
        return Array.Empty<string>();
    }

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register in order: Domain → Application → Infrastructure → Api
        
        // 1. Domain services (if any)
        // services.AddScoped<I{EntityName}DomainService, {EntityName}DomainService>();
        
        // 2. Application services (MediatR, validators, mappers)
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof({ModuleName}.Application.AssemblyMarker).Assembly));
        
        // 3. Infrastructure services (DbContext, repositories)
        // services.AddDbContext<{ModuleName}DbContext>(...);
        // services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        
        // 4. Api controllers
        services.AddControllers()
            .AddApplicationPart(typeof({ModuleName}.Api.Controllers.{EntityName}Controller).Assembly);

        return services;
    }

    public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
    {
        // Optional: Add module-specific middleware
        // Example: app.UseMiddleware<{ModuleName}TenantContextMiddleware>();
        return app;
    }
}
```

**Responsibilities**:
- **Identification**: `ModuleName` and `SchemaName` for routing and schema ownership
- **Migration ordering**: `GetMigrationDependencies()` declares prerequisite modules
- **Two-phase startup**:
  - `RegisterServices(...)`: DI registrations (Domain → Application → Infrastructure → API)
  - `ConfigureMiddleware(...)`: Optional module-specific middleware

## Layer 2: Domain Project

**Purpose**: Core business logic, entities, value objects, domain events.

**Dependencies**: `BuildingBlocks.Kernel` only (no other layers, no vendor packages)

### Primary Key Type Decision

**Choose primary key type based on requirements**:

| Type | Use When | Example |
|------|----------|---------|
| `Guid` | Distributed systems, offline generation, security | `Entity<Guid>` |
| `int` | Simple sequential IDs, performance-critical | `Entity<int>` |
| `string` | Natural keys, external system IDs | `Entity<string>` |
| Composite | Multi-column keys (rare, avoid if possible) | Custom implementation |

**Recommendation**: Use `Guid` for most entities unless you have specific performance requirements.

### Entity Base Classes

**Choose the appropriate base class**:

```csharp
// 1. Simple entity (no audit, no tenant)
public class {EntityName} : Entity<Guid>
{
    // Your properties
}

// 2. Auditable entity (created/modified tracking)
public class {EntityName} : AuditableEntity<Guid>
{
    // Inherits: CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
}

// 3. Tenant-scoped entity (multi-tenant isolation)
public class {EntityName} : TenantScopedEntity<Guid>
{
    // Inherits: TenantId (automatic filtering)
}

// 4. Auditable + Tenant-scoped
public class {EntityName} : AuditableTenantScopedEntity<Guid>
{
    // Inherits: TenantId, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
}
```

**Decision Tree**:
1. Does this entity need tenant isolation? → Use `TenantScopedEntity<TKey>` or `AuditableTenantScopedEntity<TKey>`
2. Does this entity need audit trail? → Use `AuditableEntity<TKey>` or `AuditableTenantScopedEntity<TKey>`
3. Otherwise → Use `Entity<TKey>`

### Value Objects

**All value objects must**:
- Inherit from `ValueObject` base class
- Use `Create()` factory method for validation
- Return `Result<T>` from factory
- Be immutable (init-only or readonly properties)

```csharp
public sealed class {ValueObjectName} : ValueObject
{
    public string Value { get; init; }

    private {ValueObjectName}(string value)
    {
        Value = value;
    }

    public static Result<{ValueObjectName}> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<{ValueObjectName}>.Failure(Error.Validation(
                "{ModuleName}.{ValueObjectName}.Empty",
                "{ValueObjectName} cannot be empty"));

        // Additional validation...

        return Result<{ValueObjectName}>.Success(new {ValueObjectName}(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### Domain Events

**Naming Convention**: `{EntityName}{Action}Event`

**Examples**: `TenantCreatedEvent`, `UserPasswordChangedEvent`, `OrderCancelledEvent`

```csharp
public sealed record {EntityName}{Action}Event(
    Guid {EntityName}Id,
    // Additional event data
    DateTime OccurredAt
) : IDomainEvent;
```

**Event Payload Guidelines**:
- Include entity ID (always)
- Include changed data (what changed, not entire entity)
- Include timestamp (when it happened)
- Include actor (who triggered it, if relevant)
- Keep payload minimal (only data needed by subscribers)

### Error Handling

**Use appropriate error types**:

| Error Type | Use When | Example |
|------------|----------|---------|
| `Error.Validation()` | Input validation failed | Invalid email format |
| `Error.NotFound()` | Entity doesn't exist | User not found |
| `Error.Conflict()` | Business rule violation | Email already exists |
| `Error.Unauthorized()` | Permission denied | Cannot delete other user's data |
| `Error.Failure()` | General business logic error | Cannot process payment |

```csharp
// Validation error
if (string.IsNullOrWhiteSpace(email))
    return Result.Failure(Error.Validation(
        "{ModuleName}.{EntityName}.InvalidEmail",
        "Email address is required"));

// Not found error
if (entity == null)
    return Result.Failure(Error.NotFound(
        "{ModuleName}.{EntityName}.NotFound",
        $"{EntityName} with ID {id} was not found"));

// Conflict error
if (await _repository.ExistsAsync(spec))
    return Result.Failure(Error.Conflict(
        "{ModuleName}.{EntityName}.AlreadyExists",
        $"{EntityName} with this email already exists"));
```

**Error Code Convention**: `{ModuleName}.{EntityName}.{ErrorReason}`

## Layer 3: Application Project

**Purpose**: Use cases, commands, queries, DTOs, validation.

**Dependencies**: Domain, BuildingBlocks.Kernel

### Commands and Queries (CQRS)

**Naming Conventions**:
- Commands: `{Action}{EntityName}Command` (e.g., `CreateTenantCommand`, `UpdateUserCommand`)
- Queries: `Get{EntityName}Query`, `List{EntityName}Query` (e.g., `GetTenantQuery`, `ListUsersQuery`)

**Command Example**:
```csharp
public sealed record Create{EntityName}Command(
    // Command parameters
) : ICommand<Result<Guid>>;  // Returns entity ID

public sealed class Create{EntityName}CommandHandler : ICommandHandler<Create{EntityName}Command, Result<Guid>>
{
    private readonly IRepository<{EntityName}, Guid> _repository;

    public async Task<Result<Guid>> Handle(Create{EntityName}Command command, CancellationToken ct)
    {
        // 1. Create entity using factory method
        var entityResult = {EntityName}.Create(/* parameters */);
        if (entityResult.IsFailure)
            return Result<Guid>.Failure(entityResult.Error);

        // 2. Save to repository
        await _repository.AddAsync(entityResult.Value, ct);

        // 3. Return ID
        return Result<Guid>.Success(entityResult.Value.Id);
    }
}
```

**Query Example**:
```csharp
public sealed record Get{EntityName}Query(Guid Id) : IQuery<Result<{EntityName}Dto>>;

public sealed class Get{EntityName}QueryHandler : IQueryHandler<Get{EntityName}Query, Result<{EntityName}Dto>>
{
    private readonly IRepository<{EntityName}, Guid> _repository;

    public async Task<Result<{EntityName}Dto>> Handle(Get{EntityName}Query query, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(query.Id, ct);
        if (entity == null)
            return Result<{EntityName}Dto>.Failure(Error.NotFound(
                "{ModuleName}.{EntityName}.NotFound",
                $"{EntityName} with ID {query.Id} was not found"));

        // Manual mapping (no AutoMapper)
        var dto = new {EntityName}Dto
        {
            Id = entity.Id,
            // Map other properties
        };

        return Result<{EntityName}Dto>.Success(dto);
    }
}
```

### DTOs

**Naming Convention**: `{EntityName}Dto`, `Create{EntityName}Dto`, `Update{EntityName}Dto`

**Guidelines**:
- DTOs are in Application layer (not Contracts unless shared with other modules)
- Use records for immutability
- Include only data needed for the use case
- No business logic in DTOs

```csharp
public sealed record {EntityName}Dto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    // Other properties
}
```

### Validation

**Two types of validation**:

1. **Domain Validation** (business rules) - in Domain layer using `Result<T>`
2. **Input Validation** (request format) - in Application layer using FluentValidation

**FluentValidation Example**:
```csharp
public sealed class Create{EntityName}CommandValidator : AbstractValidator<Create{EntityName}Command>
{
    public Create{EntityName}CommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
    }
}
```

**Validation Execution Order**:
1. FluentValidation (input format) - runs first via MediatR pipeline behavior
2. Domain validation (business rules) - runs in command handler

### Mapping

**Use manual mapping (no AutoMapper)**:

```csharp
public static class {EntityName}Mapper
{
    public static {EntityName}Dto ToDto(this {EntityName} entity)
    {
        return new {EntityName}Dto
        {
            Id = entity.Id,
            Name = entity.Name.Value,  // Unwrap value objects
            // Map other properties
        };
    }

    public static List<{EntityName}Dto> ToDtoList(this IEnumerable<{EntityName}> entities)
    {
        return entities.Select(ToDto).ToList();
    }
}
```

### Pagination

**For list queries, always use pagination**:

```csharp
public sealed record List{EntityName}Query(
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<Result<PagedResult<{EntityName}Dto>>>;

public sealed class List{EntityName}QueryHandler : IQueryHandler<List{EntityName}Query, Result<PagedResult<{EntityName}Dto>>>
{
    public async Task<Result<PagedResult<{EntityName}Dto>>> Handle(List{EntityName}Query query, CancellationToken ct)
    {
        var spec = new {EntityName}PaginationSpecification(query.PageNumber, query.PageSize);
        var entities = await _repository.ListAsync(spec, ct);
        var totalCount = await _repository.CountAsync(ct);

        var dtos = entities.ToDtoList();
        var pagedResult = new PagedResult<{EntityName}Dto>(dtos, totalCount, query.PageNumber, query.PageSize);

        return Result<PagedResult<{EntityName}Dto>>.Success(pagedResult);
    }
}
```

## Layer 4: Infrastructure Project

**Purpose**: Database access, external services, background jobs.

**Dependencies**: Domain, Application, BuildingBlocks.Infrastructure

### DbContext

**File**: `{ModuleName}.Infrastructure/Persistence/{ModuleName}DbContext.cs`

```csharp
public sealed class {ModuleName}DbContext : DbContext
{
    public {ModuleName}DbContext(DbContextOptions<{ModuleName}DbContext> options) : base(options) { }

    public DbSet<{EntityName}> {EntityName}s => Set<{EntityName}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("{modulename}");  // lowercase schema name
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({ModuleName}DbContext).Assembly);
    }
}
```

### Entity Configuration

**File**: `{ModuleName}.Infrastructure/Persistence/Configurations/{EntityName}Configuration.cs`

```csharp
public sealed class {EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>
{
    public void Configure(EntityTypeBuilder<{EntityName}> builder)
    {
        builder.ToTable("{entityname}s");  // lowercase table name, plural
        
        builder.HasKey(x => x.Id);
        
        // Value object mapping
        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(x => x.Email.Value).IsUnique();
    }
}
```

### Repository Pattern

**Use generic repository from BuildingBlocks.Infrastructure**:

```csharp
// In RegisterServices():
services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
```

**Only create custom repository if you need custom queries**:

```csharp
public interface I{EntityName}Repository : IRepository<{EntityName}, Guid>
{
    Task<{EntityName}?> GetByEmailAsync(string email, CancellationToken ct);
}

public sealed class {EntityName}Repository : Repository<{EntityName}, Guid>, I{EntityName}Repository
{
    public {EntityName}Repository({ModuleName}DbContext context) : base(context) { }

    public async Task<{EntityName}?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await _context.{EntityName}s
            .FirstOrDefaultAsync(x => x.Email.Value == email, ct);
    }
}
```

### Specifications (Ardalis.Specification)

**Use specifications for complex queries**:

```csharp
public sealed class {EntityName}ByEmailSpecification : Specification<{EntityName}>
{
    public {EntityName}ByEmailSpecification(string email)
    {
        Query.Where(x => x.Email.Value == email);
    }
}

// Usage in handler:
var spec = new {EntityName}ByEmailSpecification(email);
var entity = await _repository.FirstOrDefaultAsync(spec, ct);
```

**When to use Specification vs direct LINQ**:
- ✅ Use Specification: Reusable queries, complex filtering, pagination
- ❌ Use direct LINQ: Simple one-off queries in custom repository methods

## Layer 5: Api Project

**Purpose**: HTTP endpoints, request/response models, validation filters.

**Dependencies**: Application, Contracts, BuildingBlocks.Web

### Controller Template

```csharp
[ApiController]
[Route("api/{modulename}")]  // lowercase module name
public sealed class {EntityName}Controller : ControllerBase
{
    private readonly ISender _sender;

    public {EntityName}Controller(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new {entityname}.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] Create{EntityName}Command command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error);
    }

    /// <summary>
    /// Gets {entityname} by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof({EntityName}Dto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new Get{EntityName}Query(id);
        var result = await _sender.Send(query, ct);
        
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error);
    }

    /// <summary>
    /// Lists all {entityname}s with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<{EntityName}Dto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct)
    {
        var query = new List{EntityName}Query(pageNumber, pageSize);
        var result = await _sender.Send(query, ct);
        
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error);
    }

    private IActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: error.Code, detail: error.Message);
    }
}
```

## Layer 6: Migrations Project

**Purpose**: EF Core migrations for this module's schema.

**Dependencies**: Infrastructure

### Migration Naming Convention

`{Timestamp}_{Action}{EntityName}` (e.g., `20250101120000_CreateTenantTable`, `20250102140000_AddUserEmailIndex`)

### Creating Migrations

```bash
# From module's Migrations project directory
dotnet ef migrations add {MigrationName} --project ../{ModuleName}.Migrations --startup-project ../../../Hosts/MonolithHost
```

### Migration Dependencies

Declare in `{ModuleName}Module.GetMigrationDependencies()`:

```csharp
public string[] GetMigrationDependencies()
{
    // This module's migrations run AFTER these modules
    return new[] { "Tenant", "Identity" };
}
```

## Layer 7: Contracts Project

**Purpose**: Public API for other modules (events, DTOs, interfaces).

**Dependencies**: None (pure contracts)

### Integration Events

**Use for cross-module communication**:

```csharp
public sealed record {EntityName}{Action}IntegrationEvent(
    Guid {EntityName}Id,
    // Event data
    DateTime OccurredAt
) : IIntegrationEvent;
```

**Difference from Domain Events**:
- **Domain Events**: Internal to module, in-memory, synchronous
- **Integration Events**: Cross-module, message bus, asynchronous

## Multi-Tenancy Considerations

**Platform Data** (in module schema):
- Tenant-agnostic configuration
- Shared reference data
- Module metadata
- Example: Feature definitions, system settings

**Tenant Data** (in tenant app databases):
- Tenant-specific operational data
- Isolated per tenant
- Accessed via tenant context
- Example: User profiles, orders, invoices

**Decision**: Does this entity belong to a specific tenant or is it shared across all tenants?

## Testing Structure

```
/server/tests/Unit/{ModuleName}.Domain.Tests
  /Entities
    {EntityName}Tests.cs
  /ValueObjects
    {ValueObjectName}Tests.cs

/server/tests/Integration/{ModuleName}.Integration.Tests
  /Api
    {EntityName}ApiTests.cs
  /Database
    {EntityName}RepositoryTests.cs
  /Fixtures
    {ModuleName}TestFixture.cs
```

## Summary

A complete module consists of:

1. **Module Project** - Implements `IModule`, registers services in order: Domain → Application → Infrastructure → Api
2. **Domain** - Entities (choose base class: `Entity<TKey>`, `AuditableEntity<TKey>`, `TenantScopedEntity<TKey>`), value objects (factory pattern), domain events
3. **Application** - Commands/queries (CQRS), DTOs, manual mappers, FluentValidation
4. **Infrastructure** - DbContext (schema = lowercase module name), entity configurations, generic repositories (custom only if needed)
5. **Api** - Controllers (route = `api/{modulename}`), thin layer delegating to MediatR
6. **Migrations** - EF Core migrations, declare dependencies via `GetMigrationDependencies()`
7. **Contracts** - Integration events, shared DTOs (only if needed by other modules)

**Key Principles**:
- **Dependency Direction**: Domain ← Application ← Infrastructure ← Api (never reverse)
- **Primary Keys**: Choose `Guid`, `int`, or `string` based on requirements (prefer `Guid`)
- **Tenant Isolation**: Use `TenantScopedEntity<TKey>` for tenant-specific data
- **Audit Trail**: Use `AuditableEntity<TKey>` for created/modified tracking
- **Error Handling**: Use `Result<T>` pattern with appropriate error types
- **Validation**: FluentValidation for input, domain validation for business rules
- **Mapping**: Manual mapping (no AutoMapper)
- **Pagination**: Always paginate list queries
- **Specifications**: Use for reusable complex queries
- **Events**: Domain events (internal), integration events (cross-module)
