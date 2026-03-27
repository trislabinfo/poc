# Datarizen AI Context - Request Flow

## Overview

This document describes how HTTP requests flow through the Datarizen system, from client to response. Understanding this flow is critical for implementing new modules, debugging issues, and ensuring consistent behavior across all deployment topologies.

**Target Audience**: AI coding assistants helping developers build modules.

**Key Principle**: The same request flow works across all deployment topologies (Monolith, Multi-App, Microservices) with minimal configuration changes.

---

## High-Level Request Flow

```
Client Request
    ↓
API Gateway (YARP) - Optional, only in Multi-App/Microservices
    ↓
Host (Kestrel Web Server)
    ↓
ASP.NET Core Middleware Pipeline
    ↓
Controller (Module.Api)
    ↓
MediatR Pipeline (Behaviors)
    ↓
Command/Query Handler (Module.Application)
    ↓
Domain Logic (Module.Domain)
    ↓
Repository (Module.Infrastructure)
    ↓
Database (PostgreSQL/SQL Server)
    ↓
Response (back through the pipeline)
```

---

## Detailed Request Flow

### Stage 1: Client Request

**Example**:
```http
POST https://api.datarizen.com/api/{module}/{endpoint}
Headers:
  - X-Tenant-ID: {tenantId}
  - Authorization: Bearer {jwt-token}
  - X-Correlation-ID: {correlationId} (optional)
  - Content-Type: application/json
Body:
  { ...request payload... }
```

**Key Points**:
- Client can be web app, mobile app, or another service
- Tenant ID can be in header, subdomain, or JWT claim
- Correlation ID is optional (generated if not provided)

---

### Stage 2: API Gateway (Multi-App/Microservices Only)

**Project**: `Datarizen.ApiGateway`

**Responsibilities**:
1. Route matching (e.g., `/api/identity/**` → `controlpanel` cluster)
2. Service discovery (resolve host address)
3. Load balancing (if multiple instances)
4. Request forwarding

**Example Configuration** (`appsettings.json`):
```json
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "controlpanel",
        "Match": {
          "Path": "/api/identity/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "controlpanel": {
        "Destinations": {
          "destination1": {
            "Address": "http://controlpanel:8080"
          }
        }
      }
    }
  }
}
```

**Topology Support**:
- ❌ **Monolith**: No API Gateway (direct to host)
- ✅ **Multi-App**: Routes to appropriate host
- ✅ **Microservices**: Routes to appropriate service

---

### Stage 3: Host (Kestrel Web Server)

**Projects**: `Datarizen.ControlPanel`, `Datarizen.TenantApp`, etc.

**Responsibilities**:
1. Receive HTTP request
2. Initialize ASP.NET Core pipeline
3. Load configured modules
4. Route to appropriate controller

**Module Loading** (`Program.cs`):
```csharp
var builder = WebApplication.CreateBuilder(args);

// Load modules based on topology configuration
builder.AddModule<IdentityModule>();
builder.AddModule<TenantModule>();
// ... other modules

var app = builder.Build();

// Use modules
app.UseModule<IdentityModule>();
app.UseModule<TenantModule>();
// ... other modules

app.Run();
```

---

### Stage 4: ASP.NET Core Middleware Pipeline

**Location**: `BuildingBlocks.Web/Middleware/`

**Execution Order** (CRITICAL):
```
1. CorrelationIdMiddleware          (FIRST - generates/extracts correlation ID)
2. TenantResolutionMiddleware       (extracts tenant context)
3. ExceptionHandlingMiddleware      (wraps everything in try/catch)
4. Authentication Middleware        (ASP.NET Core built-in)
5. Authorization Middleware         (ASP.NET Core built-in)
6. Routing Middleware               (ASP.NET Core built-in)
7. Endpoint Middleware              (ASP.NET Core built-in)
```

#### 4.1: CorrelationIdMiddleware

**Purpose**: Generate or extract correlation ID for request tracing.

**Logic**:
```csharp
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    // Extract or generate correlation ID
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();
    
    // Store in HttpContext for access by other components
    context.Items["CorrelationId"] = correlationId;
    
    // Add to logging scope (appears in ALL logs for this request)
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            return Task.CompletedTask;
        });
        
        await next(context);
    }
}
```

**Output**:
- `HttpContext.Items["CorrelationId"]` = correlation ID
- All logs include `correlationId` field
- Response includes `X-Correlation-ID` header

---

#### 4.2: TenantResolutionMiddleware

**Purpose**: Extract tenant context from request.

**Logic**:
```csharp
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    Guid? tenantId = null;
    
    // Strategy 1: Extract from header
    if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue))
    {
        tenantId = Guid.Parse(headerValue);
    }
    
    // Strategy 2: Extract from subdomain
    else if (context.Request.Host.Host.Contains("."))
    {
        var subdomain = context.Request.Host.Host.Split('.')[0];
        tenantId = await _tenantRepository.GetTenantIdBySubdomainAsync(subdomain);
    }
    
    // Strategy 3: Extract from JWT claim
    else if (context.User.Identity?.IsAuthenticated == true)
    {
        var claim = context.User.FindFirst("tenant_id");
        tenantId = Guid.Parse(claim?.Value);
    }
    
    // Validate tenant exists
    if (tenantId.HasValue)
    {
        var tenantExists = await _tenantRepository.ExistsAsync(tenantId.Value);
        if (!tenantExists)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found" });
            return;
        }
    }
    
    // Store in HttpContext and ITenantContext
    context.Items["TenantId"] = tenantId;
    _tenantContext.TenantId = tenantId;
    
    // Add to logging scope
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["TenantId"] = tenantId?.ToString() ?? "none"
    }))
    {
        await next(context);
    }
}
```

**Output**:
- `HttpContext.Items["TenantId"]` = tenant ID
- `ITenantContext.TenantId` = tenant ID
- All logs include `tenantId` field
- All database queries automatically scoped to tenant

---

#### 4.3: ExceptionHandlingMiddleware

**Purpose**: Catch unhandled exceptions and return consistent error responses.

**Logic**:
```csharp
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception occurred");
        
        // Send to error tracker (Sentry, Application Insights, etc.)
        await _errorTracker.TrackAsync(ex, new ErrorContext
        {
            CorrelationId = context.Items["CorrelationId"] as string,
            TenantId = context.Items["TenantId"] as Guid?,
            UserId = context.User.FindFirst("sub")?.Value,
            RequestPath = context.Request.Path,
            IpAddress = context.Connection.RemoteIpAddress?.ToString()
        });
        
        // Return ProblemDetails response
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred. Please try again later.",
            Instance = context.Request.Path
        };
        
        problemDetails.Extensions["correlationId"] = context.Items["CorrelationId"];
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

**Output**:
- Unhandled exceptions logged with full context
- Errors sent to error tracker (Sentry)
- Client receives `ProblemDetails` JSON response
- Correlation ID included in response

---

#### 4.4: Authentication Middleware (ASP.NET Core)

**Purpose**: Validate JWT token and populate `HttpContext.User`.

**Configuration**:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });
```

**Output**:
- `HttpContext.User` populated with claims
- `HttpContext.User.Identity.IsAuthenticated` = true/false
- Claims available: `sub` (user ID), `email`, `tenant_id`, `role`, etc.

---

#### 4.5: Routing Middleware (ASP.NET Core)

**Purpose**: Match request to controller and action.

**Logic** (built-in ASP.NET Core):
```csharp
// Match route pattern
POST /api/{module}/{endpoint}
    ↓
Controller: {Module}Controller
Action: {Endpoint}(...)
```

---

### Stage 5: Controller (Module.Api)

**Location**: `{ModuleName}.Api/Controllers/`

**Responsibilities**:
1. Receive HTTP request
2. Map request DTO to command/query
3. Send command/query to MediatR
4. Convert `Result<T>` to `IActionResult`

**Example**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class EntitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EntitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 400)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEntityRequest request,
        CancellationToken ct)
    {
        // Map request DTO to command
        var command = new CreateEntityCommand(
            request.Name,
            request.Description);
        
        // Send to MediatR pipeline
        var result = await _mediator.Send(command, ct);
        
        // Convert Result<T> to IActionResult
        return result.ToActionResult();
        // Success → 200 OK with ApiResponse<T>
        // Failure → 400/404/409/500 with ApiResponse<T> or ProblemDetails
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EntityDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<EntityDto>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetEntityByIdQuery(id);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<EntityDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var query = new GetAllEntitiesQuery(request);
        var result = await _mediator.Send(query, ct);
        return result.ToPagedActionResult();
    }
}
```

**Key Points**:
- Controllers are THIN (no business logic)
- All work delegated to MediatR handlers
- Use `.ToActionResult()` extension method for consistent responses
- Use `[ProducesResponseType]` for OpenAPI documentation

---

### Stage 6: MediatR Pipeline (Behaviors)

**Location**: `BuildingBlocks.Application/Behaviors/`

**Execution Order** (CRITICAL):
```
1. LoggingContextBehavior       (FIRST - enriches logging context)
2. LoggingBehavior              (logs request/response)
3. PerformanceBehavior          (measures execution time)
4. ValidationBehavior           (validates request)
5. AuthorizationBehavior        (checks permissions)
6. TransactionBehavior          (wraps in database transaction)
7. AuditLoggingBehavior         (logs audit trail)
8. HANDLER                      (actual business logic)
```

#### 6.1: LoggingContextBehavior

**Purpose**: Enrich logging context with request metadata.

**Logic**:
```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;
    var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as Guid?;
    var userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
    var requestPath = _httpContextAccessor.HttpContext?.Request.Path.Value;
    
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId ?? "none",
        ["TenantId"] = tenantId?.ToString() ?? "none",
        ["UserId"] = userId ?? "anonymous",
        ["RequestPath"] = requestPath ?? "unknown",
        ["RequestType"] = typeof(TRequest).Name
    }))
    {
        return await next();
    }
}
```

**Output**:
- All subsequent logs include: `correlationId`, `tenantId`, `userId`, `requestPath`, `requestType`

---

#### 6.2: LoggingBehavior

**Purpose**: Log request and response.

**Logic**:
```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    var requestName = typeof(TRequest).Name;
    
    _logger.LogInformation("Handling {RequestName}", requestName);
    
    var stopwatch = Stopwatch.StartNew();
    var response = await next();
    stopwatch.Stop();
    
    _logger.LogInformation(
        "Handled {RequestName} in {ElapsedMs}ms",
        requestName,
        stopwatch.ElapsedMilliseconds);
    
    return response;
}
```

**Output**:
- Log entry: "Handling {RequestName}"
- Log entry: "Handled {RequestName} in {ElapsedMs}ms"

---

#### 6.3: PerformanceBehavior

**Purpose**: Warn if request takes too long.

**Logic**:
```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    var stopwatch = Stopwatch.StartNew();
    var response = await next();
    stopwatch.Stop();
    
    if (stopwatch.ElapsedMilliseconds > 500) // Threshold: 500ms
    {
        _logger.LogWarning(
            "Long running request: {RequestName} took {ElapsedMs}ms",
            typeof(TRequest).Name,
            stopwatch.ElapsedMilliseconds);
    }
    
    return response;
}
```

**Output**:
- Warning log if request > 500ms

---

#### 6.4: ValidationBehavior

**Purpose**: Validate request using FluentValidation.

**Logic**:
```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    // Find all validators for TRequest
    var validators = _serviceProvider.GetServices<IValidator<TRequest>>();
    
    if (!validators.Any())
    {
        return await next();
    }
    
    // Run all validators
    var context = new ValidationContext<TRequest>(request);
    var validationResults = await Task.WhenAll(
        validators.Select(v => v.ValidateAsync(context, cancellationToken)));
    
    // Collect all errors
    var failures = validationResults
        .SelectMany(r => r.Errors)
        .Where(f => f != null)
        .ToList();
    
    if (failures.Any())
    {
        throw new ValidationException(failures);
    }
    
    return await next();
}
```

**Output**:
- Throws `ValidationException` if validation fails
- Caught by `ExceptionHandlingMiddleware` → 400 Bad Request

---

#### 6.5: AuthorizationBehavior

**Purpose**: Check if user has permission to execute request.

**Logic**:
```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    // Only apply to requests implementing IAuthorizedRequest
    if (request is not IAuthorizedRequest authorizedRequest)
    {
        return await next();
    }
    
    var user = _httpContextAccessor.HttpContext?.User;
    
    if (user?.Identity?.IsAuthenticated != true)
    {
        throw new UnauthorizedException("User is not authenticated");
    }
    
    // Check permissions
    var requiredPermissions = authorizedRequest.RequiredPermissions;
    var userPermissions = await _permissionService.GetUserPermissionsAsync(
        Guid.Parse(user.FindFirst("sub")!.Value),
        cancellationToken);
    
    var hasAllPermissions = requiredPermissions.All(p => userPermissions.Contains(p));
    
    if (!hasAllPermissions)
    {
        throw new ForbiddenException("User does not have required permissions");
    }
    
    return await next();
}
```

**Output**:
- Throws `UnauthorizedException` → 401 Unauthorized
- Throws `ForbiddenException` → 403 Forbidden

---

#### 6.6: TransactionBehavior

**Purpose**: Wrap command in database transaction.

**Logic**:
```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    // Only apply to commands implementing ITransactionalCommand
    if (request is not ITransactionalCommand)
    {
        return await next();
    }
    
    await _unitOfWork.BeginTransactionAsync(cancellationToken);
    
    try
    {
        var response = await next();
        await _unitOfWork.CommitTransactionAsync(cancellationToken);
        return response;
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        throw;
    }
}
```

**Output**:
- Database transaction started before handler
- Committed on success, rolled back on exception

---

#### 6.7: AuditLoggingBehavior

**Purpose**: Log audit trail for sensitive operations.

**Logic**:
```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    // Only apply to requests implementing IAuditedRequest
    if (request is not IAuditedRequest auditedRequest)
    {
        return await next();
    }
    
    var httpContext = _httpContextAccessor.HttpContext;
    var userId = httpContext?.User.FindFirst("sub")?.Value;
    var tenantId = httpContext?.Items["TenantId"] as Guid?;
    
    // Log audit entry BEFORE handler
    await _auditLogger.LogAsync(
        action: auditedRequest.AuditAction,
        userId: userId != null ? Guid.Parse(userId) : null,
        tenantId: tenantId,
        details: request,
        cancellationToken: cancellationToken);
    
    var response = await next();
    
    // Log audit entry AFTER handler (with result)
    await _auditLogger.LogAsync(
        action: $"{auditedRequest.AuditAction}.Completed",
        userId: userId != null ? Guid.Parse(userId) : null,
        tenantId: tenantId,
        details: new { request, response },
        cancellationToken: cancellationToken);
    
    return response;
}
```

**Output**:
- Audit log entry created in database
- Includes: action, user ID, tenant ID, timestamp, IP address, details

---

### Stage 7: Command/Query Handler (Module.Application)

**Location**: `{ModuleName}.Application/Commands/` or `{ModuleName}.Application/Queries/`

**Responsibilities**:
1. Execute business logic
2. Call domain methods
3. Use repositories for data access
4. Publish domain events
5. Return `Result<T>`

**Example (Command Handler)**:
```csharp
public class CreateEntityCommandHandler : IRequestHandler<CreateEntityCommand, Result<Guid>>
{
    private readonly IRepository<Entity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(
        CreateEntityCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Create domain entity (returns Result<Entity>)
        var entityResult = Entity.Create(command.Name, command.Description);
        
        if (entityResult.IsFailure)
        {
            return Result<Guid>.Failure(entityResult.Error);
        }
        
        // 2. Add to repository
        await _repository.AddAsync(entityResult.Value, cancellationToken);
        
        // 3. Save changes (publishes domain events to outbox)
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // 4. Return success with entity ID
        return Result<Guid>.Success(entityResult.Value.Id);
    }
}
```

**Example (Query Handler)**:
```csharp
public class GetEntityByIdQueryHandler : IRequestHandler<GetEntityByIdQuery, Result<EntityDto>>
{
    private readonly IRepository<Entity> _repository;

    public async Task<Result<EntityDto>> Handle(
        GetEntityByIdQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Build specification
        var spec = new EntityByIdSpecification(query.Id);
        
        // 2. Query repository
        var entity = await _repository.GetBySpecificationAsync(
            spec,
            cancellationToken);
        
        if (entity is null)
        {
            return Result<EntityDto>.Failure(
                Error.NotFound("Entity.NotFound", "Entity not found"));
        }
        
        // 3. Map to response DTO
        var response = new EntityDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.CreatedAt);
        
        return Result<EntityDto>.Success(response);
    }
}
```

**Key Points**:
- ✅ Always return `Result<T>` (never throw exceptions for business rules)
- ✅ Always check `.IsFailure` before accessing `.Value`
- ✅ Propagate errors using `Result<T>.Failure(error)`
- ✅ Use `IDateTimeProvider` for deterministic timestamps
- ✅ Domain events automatically processed to outbox on `SaveChangesAsync()`

---

### Stage 8: Domain Logic (Module.Domain)

**Location**: `{ModuleName}.Domain/Entities/`, `{ModuleName}.Domain/ValueObjects/`

**Responsibilities**:
1. Enforce business rules
2. Raise domain events
3. Encapsulate state changes

**Example**:
```csharp
public sealed class Entity : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    private Entity() { } // EF Core
    
    // Factory method (returns Result<T>)
    public static Result<Entity> Create(string name, string description)
    {
        // Validate business rules
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Entity>.Failure(
                Error.Validation("Entity.NameRequired", "Name is required"));
        }
        
        if (name.Length > 100)
        {
            return Result<Entity>.Failure(
                Error.Validation("Entity.NameTooLong", "Name must be <= 100 characters"));
        }
        
        // Create entity
        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        
        // Raise domain event
        entity.RaiseDomainEvent(new EntityCreatedEvent(entity.Id, entity.Name));
        
        return Result<Entity>.Success(entity);
    }
    
    // Business method (returns Result)
    public Result UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(
                Error.Validation("Entity.NameRequired", "Name is required"));
        }
        
        if (newName.Length > 100)
        {
            return Result.Failure(
                Error.Validation("Entity.NameTooLong", "Name must be <= 100 characters"));
        }
        
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new EntityNameUpdatedEvent(Id, newName));
        
        return Result.Success();
    }
}

// Domain events
public record EntityCreatedEvent(Guid EntityId, string Name) : DomainEvent;
public record EntityNameUpdatedEvent(Guid EntityId, string NewName) : DomainEvent;
```

---

### Stage 9: Repository (Module.Infrastructure)

**Location**: `{ModuleName}.Infrastructure/Repositories/`

**Responsibilities**:
1. Data access abstraction
2. Query execution
3. Entity persistence

**Example**:
```csharp
public interface IEntityRepository : IRepository<Entity, Guid>
{
    Task<Entity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

public class EntityRepository : Repository<Entity, Guid>, IEntityRepository
{
    public EntityRepository(DbContext context) : base(context)
    {
    }
    
    public async Task<Entity?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.Name == name, cancellationToken);
    }
}
```

**Key Points**:
- Inherit from `Repository<TEntity, TKey>` (generic repository)
- All queries automatically tenant-scoped (via `IUnitOfWork`)
- Use specifications for complex queries

---

### Stage 10: Database (PostgreSQL/SQL Server)

**Responsibilities**:
1. Persist data
2. Enforce constraints
3. Execute queries

**Tenant Isolation**:
```sql
-- All queries automatically filtered by TenantId
SELECT * FROM module_schema.entities
WHERE tenant_id = 'tenant-123'
  AND name = 'example';
```

---

### Stage 11: Response (Back Through Pipeline)

**Flow**:
```
Database
    ↓
Repository (returns entity)
    ↓
Handler (returns Result<T>)
    ↓
MediatR Behaviors (reverse order)
    ↓
Controller (converts Result<T> to IActionResult)
    ↓
Middleware (adds headers, logs response)
    ↓
Host (serializes JSON)
    ↓
API Gateway (forwards response)
    ↓
Client (receives response)
```

**Success Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Example",
    "description": "Example description",
    "createdAt": "2024-01-15T10:30:00Z"
  },
  "error": null,
  "timestamp": "2024-01-15T10:30:00.123Z"
}
```

**Failure Response** (400 Bad Request):
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "Entity.NameRequired",
    "message": "Name is required",
    "type": "Validation"
  },
  "timestamp": "2024-01-15T10:30:00.123Z"
}
```

**Error Response** (500 Internal Server Error):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Please try again later.",
  "instance": "/api/entities",
  "correlationId": "abc-123",
  "timestamp": "2024-01-15T10:30:00.123Z"
}
```

---

## Topology-Specific Differences

### Monolith Topology

```
Client → Host → Middleware → Controller → MediatR → Handler → Domain → Repository → Database
```

**Characteristics**:
- No API Gateway
- All modules in same process
- In-process communication (MediatR)
- Single database connection
- Simplest debugging

---

### Multi-App Topology

```
Client → API Gateway → Host A (Module 1, 2) → ...
                     → Host B (Module 3, 4) → ...
```

**Characteristics**:
- API Gateway routes requests
- Modules grouped by host
- In-process within host, HTTP between hosts
- Shared database (schema separation)
- Moderate complexity

---

### Microservices Topology

```
Client → API Gateway → Service 1 (Module 1) → ...
                     → Service 2 (Module 2) → ...
                     → Service 3 (Module 3) → ...
```

**Characteristics**:
- API Gateway routes requests
- Each module = independent service
- HTTP/gRPC for all inter-module calls
- Database per service (optional)
- Highest complexity, highest scalability

---

## Observability

### Logging

Every log entry includes:
```json
{
  "timestamp": "2024-01-15T10:30:00.123Z",
  "level": "Information",
  "message": "Handled CreateEntityCommand in 120ms",
  "correlationId": "abc-123",
  "tenantId": "tenant-123",
  "userId": "user-456",
  "requestPath": "/api/entities",
  "requestType": "CreateEntityCommand",
  "elapsedMs": 120
}
```

### Metrics

Collected via OpenTelemetry:
- Request count
- Request duration
- Error rate
- Database query duration
- Cache hit rate

### Tracing

Distributed tracing via OpenTelemetry:
- Trace ID = Correlation ID
- Spans for each stage (middleware, handler, repository, database)
- Can trace request across multiple services

---

## Error Handling

### Validation Errors (400 Bad Request)

```
Client → ... → ValidationBehavior
                      ↓
              ValidationException
                      ↓
              ExceptionHandlingMiddleware
                      ↓
              400 Bad Request (ProblemDetails)
```

### Business Rule Violations (400/409)

```
Client → ... → Handler
                  ↓
              Return Result.Failure(Error.Conflict(...))
                  ↓
              Controller.ToActionResult()
                  ↓
              409 Conflict (ApiResponse with error)
```

### Not Found (404)

```
Client → ... → Handler
                  ↓
              Return Result.Failure(Error.NotFound(...))
                  ↓
              Controller.ToActionResult()
                  ↓
              404 Not Found (ApiResponse with error)
```

### Unauthorized (401)

```
Client → ... → AuthorizationBehavior
                      ↓
              UnauthorizedException
                      ↓
              ExceptionHandlingMiddleware
                      ↓
              401 Unauthorized (ProblemDetails)
```

### Forbidden (403)

```
Client → ... → AuthorizationBehavior
                      ↓
              ForbiddenException
                      ↓
              ExceptionHandlingMiddleware
                      ↓
              403 Forbidden (ProblemDetails)
```

### Unhandled Exceptions (500)

```
Client → ... → Handler
                  ↓
              throw new Exception(...)
                  ↓
              ExceptionHandlingMiddleware
                  ↓
              Log error + Send to Sentry
                  ↓
              500 Internal Server Error (ProblemDetails)
```

---

## Performance Characteristics

| Stage | Typical Duration |
|-------|-----------------|
| API Gateway routing | 1-5ms |
| Middleware pipeline | 5-10ms |
| MediatR behaviors | 10-20ms |
| Command/query handler | 50-100ms |
| Database query | 20-50ms |
| Domain event handlers | 10-30ms |
| **Total** | **~100-200ms** |

**Optimization Tips**:
- Use caching for frequently accessed data
- Use specifications for complex queries (avoid N+1)
- Use async/await throughout
- Use connection pooling
- Use read replicas for queries
- Use CDN for static assets

---

## Summary

**Key Principles**:
1. **Consistent Flow**: Same flow across all topologies
2. **Middleware First**: Correlation ID, tenant resolution, exception handling
3. **Behaviors Second**: Validation, authorization, transactions, auditing
4. **Handlers Third**: Business logic orchestration
5. **Domain Fourth**: Business rules enforcement
6. **Repository Fifth**: Data access
7. **Always Return Result<T>**: Never throw exceptions for business rules
8. **Always Log Context**: Correlation ID, tenant ID, user ID in every log
9. **Always Handle Errors**: Catch exceptions, return ProblemDetails
10. **Always Trace Requests**: Use correlation ID for end-to-end tracing

**For AI Assistants**:
- When implementing a new module, follow this exact flow
- When debugging, trace through this flow step-by-step
- When optimizing, measure each stage separately
- When adding features, add behaviors (not middleware)
- When handling errors, use Result<T> pattern (not exceptions)

