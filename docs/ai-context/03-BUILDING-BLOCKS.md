# Datarizen AI Context - Building Blocks

## Overview

Building Blocks are shared infrastructure components used across all modules. They provide common functionality for domain modeling, application logic, API responses, caching, messaging, and data access.

## Project Structure

```
/server/src/BuildingBlocks
  /Kernel
    Datarizen.BuildingBlocks.Kernel.csproj
    /Domain                             # Domain primitives
    /Application                        # Application patterns
    /Exceptions                         # Common exceptions
  
  /Contracts
    Datarizen.BuildingBlocks.Contracts.csproj
    /Api                                # API response DTOs
    /Messaging                          # Message contracts
    /Pagination                         # Pagination contracts
  
  /Infrastructure
    Datarizen.BuildingBlocks.Infrastructure.csproj
    /EventBus                           # Event bus implementations
    /Caching                            # Cache service
    /BackgroundJobs                     # Job scheduling
    /UnitOfWork                         # Transaction management
    /Outbox                             # Transactional outbox
  
  /Web
    Datarizen.BuildingBlocks.Web.csproj
    /Extensions                         # Result → IActionResult converters
    /Filters                            # Global filters
    /Middleware                         # Common middleware
```

## Kernel (Domain & Application Primitives)

### Domain Layer

**Base Entity**:
```csharp
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**Value Object**:
```csharp
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;
        
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }
    
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}
```

**Aggregate Root**:
```csharp
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    // Aggregate roots can raise domain events
}
```

**Domain Event**:
```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

### Application Layer

**Result Pattern**:
```csharp
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }
    
    private Result(T value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        Value = value;
    }
    
    public static Result<T> Success(T value) => new(value, true, Error.None);
    public static new Result<T> Failure(Error error) => new(default, false, error);
}
```

**Error**:
```csharp
public record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    
    public static Error Failure(string code, string message) => 
        new(code, message, ErrorType.Failure);
    
    public static Error Validation(string code, string message) => 
        new(code, message, ErrorType.Validation);
    
    public static Error NotFound(string code, string message) => 
        new(code, message, ErrorType.NotFound);
    
    public static Error Conflict(string code, string message) => 
        new(code, message, ErrorType.Conflict);
    
    public static Error Unauthorized(string code, string message) => 
        new(code, message, ErrorType.Unauthorized);
    
    public static Error Forbidden(string code, string message) => 
        new(code, message, ErrorType.Forbidden);
}

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
```

**Paged List**:
```csharp
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    
    public PagedList(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items.ToList().AsReadOnly();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
    
    public static PagedList<T> Empty() => new([], 1, 10, 0);
}
```

### Exceptions

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationException(Dictionary<string, string[]> errors) 
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }
}
```

## Contracts (Public APIs)

### API Response DTOs

**ApiResponse**:
```csharp
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(ApiError error) => new() { Success = false, Error = error };
}

public class ApiResponse
{
    public bool Success { get; init; }
    public ApiError? Error { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    public static ApiResponse Ok() => new() { Success = true };
    public static ApiResponse Fail(ApiError error) => new() { Success = false, Error = error };
}
```

**ApiError**:
```csharp
public record ApiError(
    string Code,
    string Message,
    ErrorType Type,
    Dictionary<string, string[]>? ValidationErrors = null
);
```

**Paginated Response**:
```csharp
public class PagedApiResponse<T>
{
    public bool Success { get; init; }
    public PagedData<T>? Data { get; init; }
    public ApiError? Error { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    public static PagedApiResponse<T> Ok(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount) =>
        new() { Success = true, Data = new PagedData<T>(items, pageNumber, pageSize, totalCount) };
    
    public static PagedApiResponse<T> Fail(ApiError error) =>
        new() { Success = false, Error = error };
}

public class PagedData<T>
{
    public IEnumerable<T> Items { get; init; }
    public PaginationMetadata Pagination { get; init; }
    
    public PagedData(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        Pagination = new PaginationMetadata(pageNumber, pageSize, totalCount);
    }
}

public class PaginationMetadata
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    
    public PaginationMetadata(int pageNumber, int pageSize, int totalCount)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
    }
}
```

### Pagination Request

```csharp
public record PagedRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    
    public int PageNumber { get; init; } = 1;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}
```

### Messaging Contracts

```csharp
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public interface ICommand { }

public interface IQuery<out TResponse> { }
```

## Infrastructure

### Event Bus

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}

// In-memory implementation for monolith
public class InMemoryEventBus : IEventBus
{
    // ... implementation
}

// Distributed implementation for microservices
public class RabbitMqEventBus : IEventBus
{
    // ... implementation
}
```

### Caching

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public class RedisCacheService : ICacheService
{
    // ... implementation
}
```

### Unit of Work

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### Background Jobs

```csharp
public interface IBackgroundJobScheduler
{
    void Enqueue(Expression<Action> methodCall);
    void Schedule(Expression<Action> methodCall, TimeSpan delay);
    void RecurringJob(string jobId, Expression<Action> methodCall, string cronExpression);
}

public class HangfireJobScheduler : IBackgroundJobScheduler
{
    // ... implementation
}
```

## Web (API Extensions)

### Result Extensions

Convert `Result<T>` to `IActionResult` with proper HTTP status codes:

```csharp
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(ApiResponse<T>.Ok(result.Value!));
        
        return result.Error.Type switch
        {
            ErrorType.Validation => new BadRequestObjectResult(
                ApiResponse<T>.Fail(new ApiError(result.Error.Code, result.Error.Message, result.Error.Type))),
            ErrorType.NotFound => new NotFoundObjectResult(
                ApiResponse<T>.Fail(new ApiError(result.Error.Code, result.Error.Message, result.Error.Type))),
            ErrorType.Conflict => new ConflictObjectResult(
                ApiResponse<T>.Fail(new ApiError(result.Error.Code, result.Error.Message, result.Error.Type))),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(
                ApiResponse<T>.Fail(new ApiError(result.Error.Code, result.Error.Message, result.Error.Type))),
            _ => new ObjectResult(
                ApiResponse.Fail(new ApiError(result.Error.Code, result.Error.Message, result.Error.Type)))
                { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
    
    public static IActionResult ToPagedActionResult<T>(this Result<PagedList<T>> result)
    {
        if (result.IsSuccess)
        {
            var pagedList = result.Value!;
            return new OkObjectResult(
                PagedApiResponse<T>.Ok(
                    pagedList.Items,
                    pagedList.PageNumber,
                    pagedList.PageSize,
                    pagedList.TotalCount));
        }
        
        return new BadRequestObjectResult(
            PagedApiResponse<T>.Fail(new ApiError(result.Error.Code, result.Error.Message, result.Error.Type)));
    }
}
```

### Usage Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class EntitiesController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EntityDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<EntityDto>), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetEntityByIdQuery(id));
        return result.ToActionResult();
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<EntityDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request)
    {
        var result = await _mediator.Send(new GetAllEntitiesQuery(request));
        return result.ToPagedActionResult();
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateEntityCommand command)
    {
        var result = await _mediator.Send(command);
        return result.ToActionResult();
    }
}
```

## Domain Patterns

### Domain Entity with Events

```csharp
public class Order : AggregateRoot<Guid>
{
    public string OrderNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    
    public static Result<Order> Create(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return Result<Order>.Failure(Error.Validation("INVALID_ORDER_NUMBER", "Order number is required"));
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, order.OrderNumber));
        
        return Result<Order>.Success(order);
    }
    
    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(Error.Conflict("ORDER_NOT_PENDING", "Only pending orders can be confirmed"));
        
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new OrderConfirmedEvent(Id));
        
        return Result.Success();
    }
}

public record OrderCreatedEvent(Guid OrderId, string OrderNumber) : DomainEvent;
public record OrderConfirmedEvent(Guid OrderId) : DomainEvent;
```

## Summary

**Building Blocks provide**:
- ✅ Domain primitives (Entity, ValueObject, AggregateRoot)
- ✅ Result pattern for error handling
- ✅ API response wrappers with consistent structure
- ✅ Pagination support (request/response)
- ✅ Event bus (in-memory and distributed)
- ✅ Caching abstraction
- ✅ Unit of Work pattern
- ✅ Transactional outbox
- ✅ Background job scheduling
- ✅ Extension methods for clean API responses

**All modules must**:
- Reference `BuildingBlocks.Kernel` for domain/application patterns
- Reference `BuildingBlocks.Contracts` for public APIs
- Use `Result<T>` internally, `ApiResponse<T>` externally
- Convert using `.ToActionResult()` extension methods
- Follow consistent error handling patterns
- Use domain events for state changes
- Implement pagination using `PagedList<T>` and `PagedRequest`
