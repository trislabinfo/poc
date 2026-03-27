# Module Code Generalization Implementation Plan

**Status**: 🆕 New - Code Generalization & Base Infrastructure  
**Last Updated**: 2025-02-07  
**Estimated Total Time**: ~32 hours  
**Related Documents**: 
- `docs/implementations/module-identity-domain-layer-plan.md`
- `docs/implementations/module-identity-application-layer-plan.md`
- `docs/implementations/module-identity-infrastructure-layer-plan.md`
- `docs/ai-context/05-MODULES.md`

---

## Overview

This plan implements **common base classes and utilities** to eliminate repetitive code across modules. After analyzing the Identity and Tenant modules, we identified significant duplication in:

- API controllers (CRUD operations)
- Application layer (command/query handlers)
- Infrastructure layer (DbContext, repositories)
- Module registration
- Validation patterns

**Philosophy**: 
- ✅ Write once, use everywhere
- ✅ Convention over configuration
- ✅ Maintain flexibility for custom behavior
- ✅ Zero breaking changes to existing modules

**Success Criteria**:
- ✅ New modules require 50% less boilerplate code
- ✅ Existing modules can opt-in to base classes incrementally
- ✅ All base classes are well-documented with examples
- ✅ Base classes support all three deployment topologies

---

## Plan Assumptions and Clarifications

### 1. Result → IActionResult Mapping

**Decision**: Map `Error.Type` to HTTP status codes as follows:

- `Error.Validation` → `400 Bad Request`
- `Error.NotFound` → `404 Not Found`
- `Error.Conflict` → `409 Conflict`
- `Error.Unauthorized` → `401 Unauthorized`
- `Error.Forbidden` → `403 Forbidden`
- `Error.Failure` → `500 Internal Server Error`

**Implementation**: `BaseCrudController` will have a protected method `ToActionResult(Result<T>)` that performs this mapping.

---

### 2. AutoMapper vs Manual Mapping

**Decision**: **Use manual mapping** (static mapper classes) for now.

**Rationale**:
- Simpler to understand and debug
- No additional dependencies
- Easier to customize per module
- Can migrate to AutoMapper later if needed

**Pattern**:
```csharp
public static class TenantMapper
{
    public static TenantResponse ToResponse(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        return new TenantResponse(tenant.Id, tenant.Name, tenant.Slug, tenant.CreatedAt);
    }
}
```

---

### 3. Generic Constraints

**Decision**: All base classes use `where TEntity : Entity<TId>` constraint.

**Rationale**:
- Ensures entities inherit from `Entity<TId>` base class
- Provides access to `Id`, `CreatedAt`, `UpdatedAt` properties
- Enforces domain model consistency

---

### 4. Unit of Work Pattern

**Decision**: Each module has its own `IUnitOfWork` implementation scoped to its `DbContext`.

**Example**:
```csharp
// Tenant module registration  
services.AddScoped<IUnitOfWork, TenantUnitOfWork>();

// Identity module registration  
services.AddScoped<IUnitOfWork, IdentityUnitOfWork>();
```

**Limitation**: This approach assumes **single-module transactions**. Cross-module transactions require a different approach (e.g., distributed transactions, saga pattern, or integration events).

**Rationale**: Maintains module isolation and aligns with microservices principles.

---

### 5. Migration Dependency Order

**Problem**: Should `GetMigrationDependencies()` be part of `IModule` interface, or should migration order be configured externally?

**Current State**: 
- `IModule` interface has `GetMigrationDependencies()`
- `MigrationRunner` uses this method to resolve dependency order via topological sort

**Decision**: **Keep `GetMigrationDependencies()` in `IModule` interface**.

**Rationale**:
- Migration dependencies are intrinsic to the module (e.g., Identity depends on Tenant)
- Allows modules to declare their own dependencies
- MigrationRunner can automatically resolve order
- Simpler than maintaining external configuration

**Action**: Keep current approach. `IModule.GetMigrationDependencies()` returns array of module names.

---

### 6. Schema Configuration Approach

**Problem**: How is schema name applied to `DbContext`?

**Old Approach** (extension method):
```csharp
options.UseNpgsql(connectionString);
((DbContextOptionsBuilder<TenantDbContext>)options).UseTenantSchema(schemaName);
```

**New Approach** (in DbContext):
```csharp
public class TenantDbContext : DbContext
{
    private readonly string _schemaName;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, string schemaName) 
        : base(options)
    {
        _schemaName = schemaName;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schemaName);
        // ... entity configurations
    }
}
```

**Decision**: **Move schema configuration to `DbContext` type** (new approach).

**Rationale**:
- Schema is intrinsic to the module, not a configuration option
- Simpler registration (no need for `UseTenantSchema()` extension)
- Consistent with other `DbContext` configuration
- Easier to understand and maintain

**Action**: Remove `UseTenantSchema()` extension methods. Schema is now defined in derived `DbContext` classes.

---

### 7. MediatR/FluentValidation Registration Strategy

**Problem**: `AddBuildingBlocks()` already registers MediatR and FluentValidation. How does module registration interact with this?

**Decision**: **Modules do NOT register MediatR/FluentValidation** - they rely on host's `AddBuildingBlocks()`.

**Rationale**:
- Avoids duplicate registrations
- Centralizes pipeline behavior configuration
- Simpler module code

**Module Registration Pattern**:
```csharp
public class TenantModule : BaseModule
{
    protected override string ModuleName => "Tenant";
    protected override string SchemaName => "tenant";

    protected override void RegisterModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<TenantDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IRepository<Tenant, Guid>, Repository<Tenant, Guid>>();

        // UnitOfWork
        services.AddScoped<IUnitOfWork, TenantUnitOfWork>();

        // Module-specific services (if any)
    }
}
```

**Host Registration Pattern**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register BuildingBlocks (includes MediatR, FluentValidation, etc.)
builder.Services.AddBuildingBlocks(builder.Configuration);

// 2. Register modules
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<IdentityModule>(builder.Configuration);

var app = builder.Build();

// 3. Use modules
app.UseModule<TenantModule>();
app.UseModule<IdentityModule>();
```

---

### 8. Testing Framework

**Decision**: Use **xUnit + FluentAssertions + NSubstitute**.

**Rationale**:
- xUnit: Industry standard for .NET
- FluentAssertions: Readable assertions
- NSubstitute: Simpler syntax than Moq

**Example**:
```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsSuccess()
{
    // Arrange
    var repository = Substitute.For<IRepository<Tenant, Guid>>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var handler = new CreateTenantCommandHandler(repository, unitOfWork, new FakeDateTimeProvider());

    // Act
    var result = await handler.Handle(new CreateTenantCommand("Test", "test"), CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    await repository.Received(1).AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
}
```

---

### 9. Paging Strategy

**Decision**: Use `PagedResponse<T>` from `BuildingBlocks.Kernel`.

**Location**: `BuildingBlocks.Kernel/Results/PagedResponse.cs`

**Pattern**:
```csharp
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

---

## Implementation Plan

### Phase 1: API Layer Base Classes (8 hours)

#### 1.1: Create BaseCrudController (3 hours)

**Location**: `BuildingBlocks.Web/Controllers/BaseCrudController.cs`

**Purpose**: Eliminate repetitive CRUD endpoint code in module controllers.

**Tasks**:

1. **Create base controller class**
   - [ ] Define generic parameters: `TEntity`, `TId`, `TCreateRequest`, `TUpdateRequest`, `TResponse`
   - [ ] Add protected `IMediator` property
   - [ ] Add protected `ToActionResult(Result<T>)` method
   - [ ] Implement standard CRUD endpoints (GET, POST, PUT, DELETE)

2. **Add XML documentation**
   - [ ] Document all public methods
   - [ ] Add usage examples
   - [ ] Document generic constraints

3. **Create unit tests**
   - [ ] Test `ToActionResult()` mapping for all error types
   - [ ] Test endpoint routing
   - [ ] Test request/response serialization

**Example Implementation**:
```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Kernel.Results;

namespace BuildingBlocks.Web.Controllers;

/// <summary>
/// Base controller providing standard CRUD endpoints.
/// </summary>
[ApiController]
public abstract class BaseCrudController<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse> 
    : ControllerBase
    where TEntity : class
{
    protected IMediator Mediator { get; }

    protected BaseCrudController(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Type switch
        {
            ErrorType.Validation => BadRequest(new { errors = result.Error.Message }),
            ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
            ErrorType.Conflict => Conflict(new { error = result.Error.Message }),
            ErrorType.Unauthorized => Unauthorized(new { error = result.Error.Message }),
            ErrorType.Forbidden => StatusCode(403, new { error = result.Error.Message }),
            _ => StatusCode(500, new { error = result.Error.Message })
        };
    }

    [HttpGet("{id}")]
    public virtual async Task<IActionResult> GetById(TId id)
    {
        var result = await HandleGetByIdAsync(id);
        return ToActionResult(result);
    }

    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] TCreateRequest request)
    {
        var result = await HandleCreateAsync(request);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(TId id, [FromBody] TUpdateRequest request)
    {
        var result = await HandleUpdateAsync(id, request);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(TId id)
    {
        var result = await HandleDeleteAsync(id);
        return ToActionResult(result);
    }

    protected abstract Task<Result<TResponse>> HandleGetByIdAsync(TId id);
    protected abstract Task<Result<TResponse>> HandleCreateAsync(TCreateRequest request);
    protected abstract Task<Result<TResponse>> HandleUpdateAsync(TId id, TUpdateRequest request);
    protected abstract Task<Result<Unit>> HandleDeleteAsync(TId id);
}

