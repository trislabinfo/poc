# Datarizen AI Context - Server Coding Conventions

## Overview

This document defines coding standards and best practices for the Datarizen .NET backend. Following these conventions ensures consistency, maintainability, and adherence to industry-standard .NET practices.

---

## Table of Contents

1. [Naming Conventions](#naming-conventions)
2. [File Organization](#file-organization)
3. [Code Structure](#code-structure)
4. [Dependency Injection](#dependency-injection)
5. [Async/Await Best Practices](#asyncawait-best-practices)
6. [Error Handling](#error-handling)
7. [LINQ and Collections](#linq-and-collections)
8. [Null Handling](#null-handling)
9. [Comments and Documentation](#comments-and-documentation)
10. [Testing Conventions](#testing-conventions)
11. [Performance Best Practices](#performance-best-practices)
12. [Security Best Practices](#security-best-practices)
13. [Code Formatting](#code-formatting)
14. [NuGet Package Management](#nuget-package-management)
15. [Entity Framework Core Conventions](#entity-framework-core-conventions)
16. [API Design Conventions](#api-design-conventions)
17. [Logging Conventions](#logging-conventions)

---

## Naming Conventions

### General Rules

- **PascalCase**: Classes, methods, properties, public members, namespaces, enums
- **camelCase**: Local variables, parameters, private fields (without underscore for locals)
- **_camelCase**: Private fields (prefix with underscore)
- **IPascalCase**: Interfaces (prefix with `I`)
- **SCREAMING_SNAKE_CASE**: Avoid (use PascalCase for constants)

### Classes and Interfaces

```csharp
✅ Good:
public class OrderService { }
public class OrderRepository { }
public interface IOrderRepository { }
public abstract class BaseEntity { }

❌ Bad:
public class orderService { }
public class Order_Service { }
public interface OrderRepository { } // Missing I prefix
```

### Methods and Properties

```csharp
✅ Good:
public async Task<Result<Order>> GetOrderAsync(Guid orderId) { }
public string CustomerName { get; set; }
public bool IsActive { get; private set; }
public decimal CalculateTotal() { }

❌ Bad:
public async Task<Result<Order>> getOrder(Guid orderId) { }
public string customer_name { get; set; }
public bool is_active { get; set; }
```

### Fields and Variables

```csharp
✅ Good:
// Private fields with underscore
private readonly IOrderRepository _orderRepository;
private readonly ILogger<OrderService> _logger;
private int _retryCount;

// Local variables without underscore
public void ProcessOrder(Order order)
{
    var orderTotal = order.CalculateTotal();
    var discountAmount = CalculateDiscount(orderTotal);
    var finalAmount = orderTotal - discountAmount;
}

❌ Bad:
private readonly IOrderRepository orderRepository; // Missing underscore
private readonly ILogger<OrderService> _Logger; // Wrong casing
var OrderTotal = order.CalculateTotal(); // Wrong casing for local
```

### Constants and Enums

```csharp
✅ Good:
public const int MaxRetryAttempts = 3;
private const string DefaultCurrency = "USD";

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled
}

❌ Bad:
public const int MAX_RETRY_ATTEMPTS = 3; // Use PascalCase
public enum OrderStatus
{
    pending, // Use PascalCase
    processing
}
```

### Namespaces

```csharp
✅ Good:
namespace Datarizen.Orders.Domain.Entities;
namespace Datarizen.Orders.Application.Commands;
namespace Datarizen.BuildingBlocks.Kernel.Domain;

❌ Bad:
namespace Datarizen.orders.domain.entities; // Wrong casing
namespace Datarizen_Orders_Domain; // Use dots, not underscores
```

---

## File Organization

### One Class Per File

```
✅ Good:
/Domain/Entities/Order.cs          (contains Order class)
/Domain/Entities/OrderItem.cs      (contains OrderItem class)
/Domain/ValueObjects/Money.cs      (contains Money class)

❌ Bad:
/Domain/Entities/OrderEntities.cs  (contains Order, OrderItem, OrderStatus)
```

### File Naming

- File name **must match** the primary class/interface name exactly
- Use PascalCase for file names
- Group related files in feature folders

```
✅ Good:
/Application/Commands/CreateOrder/CreateOrderCommand.cs
/Application/Commands/CreateOrder/CreateOrderCommandHandler.cs
/Application/Commands/CreateOrder/CreateOrderCommandValidator.cs

❌ Bad:
/Application/Commands/create-order-command.cs
/Application/Commands/CreateOrderCmd.cs
/Application/Commands/OrderCommands.cs (multiple classes)
```

### Folder Structure

```
✅ Good (feature folders):
/Orders
  /Domain
    /Entities
      Order.cs
      OrderItem.cs
    /ValueObjects
      Money.cs
      Address.cs
    /Events
      OrderCreatedEvent.cs
  /Application
    /Commands
      /CreateOrder
        CreateOrderCommand.cs
        CreateOrderCommandHandler.cs
        CreateOrderCommandValidator.cs
      /UpdateOrder
        UpdateOrderCommand.cs
        UpdateOrderCommandHandler.cs
    /Queries
      /GetOrderById
        GetOrderByIdQuery.cs
        GetOrderByIdQueryHandler.cs

❌ Bad (layer folders):
/Commands
  CreateOrderCommand.cs
  UpdateOrderCommand.cs
/Handlers
  CreateOrderCommandHandler.cs
  UpdateOrderCommandHandler.cs
```

---

## Code Structure

### Namespace Organization

```csharp
// Use file-scoped namespaces (.NET 10+)
namespace Datarizen.Orders.Domain.Entities;

public class Order
{
    // Implementation
}
```

### Using Directives

```csharp
// Order: System namespaces first, then third-party, then project namespaces
// Alphabetically within each group
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;

using Datarizen.BuildingBlocks.Kernel.Domain;
using Datarizen.Orders.Domain.Entities;
using Datarizen.Orders.Domain.ValueObjects;
```

### Class Member Order

```csharp
public class OrderService : IOrderService
{
    // 1. Constants
    private const int MaxOrderItems = 100;
    private const string DefaultCurrency = "USD";
    
    // 2. Static fields
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    
    // 3. Private fields (readonly first, then mutable)
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private int _retryCount;
    
    // 4. Constructor(s)
    public OrderService(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // 5. Public properties
    public string ServiceName => "OrderService";
    public int RetryCount => _retryCount;
    
    // 6. Public methods
    public async Task<Result<Order>> CreateOrderAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
    
    public async Task<Result<Order>> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
    
    // 7. Protected methods (if applicable)
    protected virtual void OnOrderCreated(Order order)
    {
        // Implementation
    }
    
    // 8. Private methods
    private decimal CalculateDiscount(decimal total)
    {
        // Implementation
    }
    
    private bool ValidateOrder(Order order)
    {
        // Implementation
    }
}
```

---

## Dependency Injection

### Constructor Injection (Preferred)

```csharp
✅ Good:
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}

❌ Bad (property injection):
public class OrderService
{
    public IOrderRepository OrderRepository { get; set; } // Avoid
    public ILogger<OrderService> Logger { get; set; } // Avoid
}
```

### Service Registration

```csharp
✅ Good (extension methods):
public static class DependencyInjection
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories (Scoped - per request)
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Services (Scoped - per request)
        services.AddScoped<IOrderService, OrderService>();
        
        // Background services (Singleton)
        services.AddSingleton<IOrderBackgroundService, OrderBackgroundService>();
        
        // MediatR
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
}

❌ Bad (registering in Program.cs):
// Don't register module services directly in Program.cs
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

### Service Lifetimes

- **Transient**: Created each time requested (stateless, lightweight)
- **Scoped**: Created once per request (repositories, DbContext, UnitOfWork)
- **Singleton**: Created once for application lifetime (caches, background services)

```csharp
// Transient - lightweight, stateless
services.AddTransient<IEmailSender, EmailSender>();

// Scoped - per HTTP request
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddScoped<OrderDbContext>();

// Singleton - application lifetime
services.AddSingleton<IMemoryCache, MemoryCache>();
services.AddSingleton<IOrderBackgroundService, OrderBackgroundService>();
```

---

## Async/Await Best Practices

### Always Use Async Suffix

```csharp
✅ Good:
public async Task<Order> GetOrderAsync(Guid orderId)
public async Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)

❌ Bad:
public async Task<Order> GetOrder(Guid orderId) // Missing Async suffix
public async Task<Order> Get(Guid orderId) // Missing Async suffix
```

### Avoid Async Void

```csharp
✅ Good:
public async Task ProcessOrderAsync()
{
    await _orderRepository.SaveAsync();
}

❌ Bad (only for event handlers):
public async void ProcessOrder() // Avoid - exceptions can't be caught
{
    await _orderRepository.SaveAsync();
}

✅ Exception (event handlers only):
private async void OnButtonClick(object sender, EventArgs e)
{
    try
    {
        await ProcessOrderAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing order");
    }
}
```

### Use ConfigureAwait(false) in Libraries

```csharp
// In library code (not ASP.NET Core controllers/handlers)
public async Task<Order> GetOrderAsync(Guid orderId)
{
    var order = await _orderRepository
        .GetByIdAsync(orderId)
        .ConfigureAwait(false);
    
    return order;
}

// In ASP.NET Core controllers/handlers - ConfigureAwait not needed
public async Task<IActionResult> GetOrder(Guid orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    return Ok(order);
}
```

### Always Pass CancellationToken

```csharp
✅ Good:
public async Task<Result<Order>> CreateOrderAsync(
    CreateOrderCommand command,
    CancellationToken cancellationToken = default)
{
    var order = Order.Create(command.CustomerId);
    
    await _orderRepository.AddAsync(order, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    return Result<Order>.Success(order);
}

❌ Bad:
public async Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
{
    // No cancellation support
    await _orderRepository.AddAsync(order);
    await _unitOfWork.SaveChangesAsync();
}
```

### Avoid Task.Result and Task.Wait()

```csharp
✅ Good:
public async Task<Order> GetOrderAsync(Guid orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    return order;
}

❌ Bad (can cause deadlocks):
public Order GetOrder(Guid orderId)
{
    var order = _orderRepository.GetByIdAsync(orderId).Result; // Deadlock risk
    return order;
}

public Order GetOrder2(Guid orderId)
{
    var task = _orderRepository.GetByIdAsync(orderId);
    task.Wait(); // Deadlock risk
    return task.Result;
}
```

---

## Error Handling

### Use Result Pattern (Not Exceptions for Flow Control)

```csharp
✅ Good (Result pattern):
public Result<Order> CreateOrder(CreateOrderRequest request)
{
    if (string.IsNullOrWhiteSpace(request.CustomerName))
        return Result<Order>.Failure(OrderErrors.InvalidCustomerName);
    
    if (request.Items.Count == 0)
        return Result<Order>.Failure(OrderErrors.NoItems);
    
    var order = Order.Create(request.CustomerName, request.Items);
    return Result<Order>.Success(order);
}

❌ Bad (exceptions for flow control):
public Order CreateOrder(CreateOrderRequest request)
{
    if (string.IsNullOrWhiteSpace(request.CustomerName))
        throw new InvalidOperationException("Customer name is required");
    
    if (request.Items.Count == 0)
        throw new InvalidOperationException("Order must have items");
    
    return Order.Create(request.CustomerName, request.Items);
}
```

### Use Exceptions for Exceptional Cases

```csharp
✅ Good (truly exceptional - infrastructure failure):
public async Task<Order> GetOrderAsync(Guid orderId)
{
    try
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        return order;
    }
    catch (DbException ex)
    {
        _logger.LogError(ex, "Database error retrieving order {OrderId}", orderId);
        throw; // Re-throw infrastructure exceptions
    }
}

✅ Better (use Result for business logic):
public async Task<Result<Order>> GetOrderAsync(Guid orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    
    if (order is null)
        return Result<Order>.Failure(OrderErrors.NotFound);
    
    return Result<Order>.Success(order);
}
```

### Custom Exception Types

```csharp
✅ Good (domain exceptions):
public class OrderNotFoundException : Exception
{
    public Guid OrderId { get; }
    
    public OrderNotFoundException(Guid orderId)
        : base($"Order with ID {orderId} was not found")
    {
        OrderId = orderId;
    }
}

// Usage
if (order is null)
    throw new OrderNotFoundException(orderId);
```

### Global Exception Handling

```csharp
// In Program.cs or middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;
        
        var response = exception switch
        {
            OrderNotFoundException ex => new { Status = 404, Message = ex.Message },
            ValidationException ex => new { Status = 400, Message = ex.Message },
            _ => new { Status = 500, Message = "An error occurred" }
        };
        
        context.Response.StatusCode = response.Status;
        await context.Response.WriteAsJsonAsync(response);
    });
});
```

---

## LINQ and Collections

### Use LINQ Method Syntax (Preferred)

```csharp
✅ Good (method syntax):
var activeOrders = orders
    .Where(o => o.Status == OrderStatus.Active)
    .OrderByDescending(o => o.CreatedAt)
    .Select(o => new OrderDto
    {
        Id = o.Id,
        CustomerName = o.CustomerName
    })
    .ToList();

❌ Bad (query syntax):
var activeOrders = (from o in orders
                    where o.Status == OrderStatus.Active
                    orderby o.CreatedAt descending
                    select new OrderDto
                    {
                        Id = o.Id,
                        CustomerName = o.CustomerName
                    }).ToList();
```

### Prefer IEnumerable<T> for Return Types

```csharp
✅ Good:
public IEnumerable<Order> GetActiveOrders()
{
    return _orders.Where(o => o.Status == OrderStatus.Active);
}

❌ Bad (forces materialization):
public List<Order> GetActiveOrders()
{
    return _orders.Where(o => o.Status == OrderStatus.Active).ToList();
}
```

### Use Collection Expressions (.NET 10+)

```csharp
✅ Good (collection expressions):
var numbers = [1, 2, 3, 4, 5];
var combined = [..firstList, ..secondList];
var filtered = [..items.Where(x => x.IsActive)];

❌ Old style:
var numbers = new List<int> { 1, 2, 3, 4, 5 };
var combined = firstList.Concat(secondList).ToList();
```

### Avoid Multiple Enumeration

```csharp
✅ Good:
var activeOrders = orders.Where(o => o.Status == OrderStatus.Active).ToList();
var count = activeOrders.Count;
var total = activeOrders.Sum(o => o.Total);

❌ Bad (enumerates twice):
var activeOrders = orders.Where(o => o.Status == OrderStatus.Active);
var count = activeOrders.Count(); // Enumerates
var total = activeOrders.Sum(o => o.Total); // Enumerates again
```

---

## Null Handling

### Enable Nullable Reference Types

```csharp
// In .csproj or Directory.Build.props
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>

// In code
#nullable enable

public class OrderService
{
    // Non-nullable
    private readonly IOrderRepository _orderRepository;
    
    // Nullable
    private string? _cachedCustomerName;
    
    public Order? FindOrder(Guid orderId)
    {
        return _orderRepository.GetById(orderId);
    }
    
    public Order GetOrder(Guid orderId)
    {
        return _orderRepository.GetById(orderId) 
            ?? throw new OrderNotFoundException(orderId);
    }
}
```

### Null Checks with Pattern Matching

```csharp
✅ Good (pattern matching):
if (order is null)
    return Result.Failure(OrderErrors.NotFound);

if (order is not null)
    ProcessOrder(order);

❌ Bad (old style):
if (order == null)
    return Result.Failure(OrderErrors.NotFound);

if (order != null)
    ProcessOrder(order);
```

### Null-Coalescing and Null-Conditional Operators

```csharp
✅ Good:
var customerName = order.CustomerName ?? "Unknown";
var itemCount = order?.Items?.Count ?? 0;
var firstItem = order?.Items?.FirstOrDefault();

// Null-coalescing assignment
_cache ??= new Dictionary<string, Order>();
```

### Argument Null Checking

```csharp
✅ Good (.NET 10+):
public OrderService(IOrderRepository orderRepository)
{
    ArgumentNullException.ThrowIfNull(orderRepository);
    _orderRepository = orderRepository;
}

✅ Good (older style):
public OrderService(IOrderRepository orderRepository)
{
    _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
}

❌ Bad:
public OrderService(IOrderRepository orderRepository)
{
    if (orderRepository == null)
        throw new ArgumentNullException(nameof(orderRepository));
    _orderRepository = orderRepository;
}
```

---

## Comments and Documentation

### XML Documentation for Public APIs

```csharp
/// <summary>
/// Creates a new order for the specified customer.
/// </summary>
/// <param name="customerId">The unique identifier of the customer.</param>
/// <param name="items">The list of items to include in the order.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>A result containing the created order or an error.</returns>
/// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
public async Task<Result<Order>> CreateOrderAsync(
    Guid customerId,
    List<OrderItem> items,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(items);
    
    // Implementation
}
```

### Inline Comments (Use Sparingly)

```csharp
✅ Good (explains WHY, not WHAT):
// Retry logic needed due to occasional database deadlocks under high load
await RetryPolicy.ExecuteAsync(() => SaveOrderAsync(order));

// Cache customer data for 5 minutes to reduce database load
_cache.Set(customerId, customer, TimeSpan.FromMinutes(5));

❌ Bad (explains WHAT - code is self-explanatory):
// Loop through items
foreach (var item in order.Items)
{
    // Add item price to total
    total += item.Price;
}

// Check if order is null
if (order is null)
    return;
```

### TODO Comments

```csharp
✅ Good (actionable TODOs):
// TODO: Implement caching for frequently accessed orders (Issue #123)
// TODO: Add validation for maximum order amount (Sprint 5)

❌ Bad (vague TODOs):
// TODO: Fix this
// TODO: Optimize
```

---

## Testing Conventions

### Test Class Naming

```csharp
// Pattern: {ClassUnderTest}Tests
public class OrderServiceTests { }
public class CreateOrderCommandHandlerTests { }
public class OrderRepositoryTests { }
```

### Test Method Naming

```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
[Fact]
public async Task CreateOrderAsync_WithValidData_ReturnsSuccess()
{
    // Arrange
    var command = new CreateOrderCommand("Customer1", items);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
}

[Fact]
public async Task CreateOrderAsync_WithInvalidCustomer_ReturnsFailure()
{
    // Arrange
    var command = new CreateOrderCommand("", items);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(OrderErrors.InvalidCustomerName);
}

[Fact]
public async Task CreateOrderAsync_WithNoItems_ReturnsFailure()
{
    // Arrange, Act, Assert
}
```

### Use FluentAssertions

```csharp
✅ Good (FluentAssertions):
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();
result.Value.CustomerName.Should().Be("John Doe");
result.Value.Items.Should().HaveCount(3);

❌ Bad (xUnit assertions):
Assert.True(result.IsSuccess);
Assert.NotNull(result.Value);
Assert.Equal("John Doe", result.Value.CustomerName);
Assert.Equal(3, result.Value.Items.Count);
```

### Arrange-Act-Assert Pattern

```csharp
[Fact]
public async Task GetOrderByIdAsync_WhenOrderExists_ReturnsOrder()
{
    // Arrange
    var orderId = Guid.NewGuid();
    var expectedOrder = new Order { Id = orderId, CustomerName = "John" };
    _mockRepository.Setup(r => r.GetByIdAsync(orderId, default))
        .ReturnsAsync(expectedOrder);
    
    // Act
    var result = await _service.GetOrderByIdAsync(orderId);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().BeEquivalentTo(expectedOrder);
}
```

---

## Performance Best Practices

### Avoid Premature Optimization

```csharp
✅ Good (readable first, optimize if needed):
var activeOrders = orders
    .Where(o => o.Status == OrderStatus.Active)
    .OrderByDescending(o => o.CreatedAt)
    .ToList();

// Profile first, then optimize if this is a bottleneck
```

### Use Span<T> and Memory<T> for Performance-Critical Code

```csharp
public void ProcessLargeData(ReadOnlySpan<byte> data)
{
    // Zero-allocation processing
    for (int i = 0; i < data.Length; i++)
    {
        ProcessByte(data[i]);
    }
}
```

### Avoid String Concatenation in Loops

```csharp
✅ Good:
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.Append(item.Name);
    sb.Append(", ");
}
var result = sb.ToString();

❌ Bad:
var result = "";
foreach (var item in items)
{
    result += item.Name + ", "; // Creates new string each iteration
}
```

### Use ValueTask<T> for Hot Paths

```csharp
// When result is often synchronous (e.g., cached)
public ValueTask<Order?> GetCachedOrderAsync(Guid orderId)
{
    if (_cache.TryGetValue(orderId, out var order))
        return new ValueTask<Order?>(order); // Synchronous path
    
    return new ValueTask<Order?>(LoadOrderAsync(orderId)); // Async path
}

private async Task<Order?> LoadOrderAsync(Guid orderId)
{
    var order = await _repository.GetByIdAsync(orderId);
    _cache[orderId] = order;
    return order;
}
```

---

## Security Best Practices

### Never Trust User Input

```csharp
✅ Good:
public Result<Order> CreateOrder(CreateOrderRequest request)
{
    // Validate
    if (string.IsNullOrWhiteSpace(request.CustomerName))
        return Result<Order>.Failure(OrderErrors.InvalidCustomerName);
    
    if (request.CustomerName.Length > 200)
        return Result<Order>.Failure(OrderErrors.CustomerNameTooLong);
    
    // Sanitize
    var sanitizedName = request.CustomerName.Trim();
    
    // Process
    var order = Order.Create(sanitizedName);
    return Result<Order>.Success(order);
}

❌ Bad:
public Order CreateOrder(CreateOrderRequest request)
{
    // No validation or sanitization
    return Order.Create(request.CustomerName);
}
```

### Use Parameterized Queries (EF Core Does This)

```csharp
✅ Good (EF Core - parameterized automatically):
var orders = await _context.Orders
    .Where(o => o.CustomerId == customerId)
    .ToListAsync();

❌ Bad (raw SQL without parameters - SQL injection risk):
var sql = $"SELECT * FROM Orders WHERE CustomerId = '{customerId}'";
var orders = await _context.Orders.FromSqlRaw(sql).ToListAsync();

✅ Good (raw SQL with parameters):
var orders = await _context.Orders
    .FromSqlInterpolated($"SELECT * FROM Orders WHERE CustomerId = {customerId}")
    .ToListAsync();
```

### Protect Sensitive Data

```csharp
✅ Good:
public class User
{
    public string Email { get; set; }
    
    [JsonIgnore] // Don't serialize password hash
    public string PasswordHash { get; set; }
}

// Log without sensitive data
_logger.LogInformation("User {UserId} logged in", user.Id);

❌ Bad:
_logger.LogInformation("User {Email} logged in with password {Password}", 
    user.Email, user.Password); // Never log passwords!
```

---

## Code Formatting

### Use EditorConfig

```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Naming conventions
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = begins_with_i

dotnet_naming_rule.private_fields_should_be_prefixed_with_underscore.severity = warning
dotnet_naming_rule.private_fields_should_be_prefixed_with_underscore.symbols = private_field
dotnet_naming_rule.private_fields_should_be_prefixed_with_underscore.style = begins_with_underscore

# Code style
csharp_prefer_braces = true:warning
csharp_using_directive_placement = outside_namespace:warning
csharp_prefer_simple_using_statement = true:suggestion
csharp_style_namespace_declarations = file_scoped:warning
```

### Consistent Brace Style (Allman)

```csharp
✅ Good (Allman style):
public class Order
{
    public void Process()
    {
        if (IsValid())
        {
            Save();
        }
        else
        {
            LogError();
        }
    }
}

❌ Bad (mixed styles):
public class Order {
    public void Process() {
        if (IsValid()) {
            Save();
        } else {
            LogError();
        }
    }
}
```

### Line Length

```csharp
✅ Good (max 120 characters):
var order = await _orderRepository.GetByIdAsync(
    orderId, 
    cancellationToken);

❌ Bad (too long):
var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken, includeItems: true, includeCustomer: true, includePayments: true);
```

---

## NuGet Package Management

### Central Package Management (CPM)

All NuGet packages are managed centrally via `server/Directory.Packages.props` to ensure version consistency across all projects.

### Directory.Packages.props Structure

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- Aspire -->
    <PackageVersion Include="Aspire.Hosting.AppHost" Version="13.1.0" />
    <PackageVersion Include="Aspire.Hosting.PostgreSQL" Version="13.1.0" />
    
    <!-- Framework / Extensions -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.2" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.2" />
    
    <!-- Data / Messaging -->
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageVersion Include="RabbitMQ.Client" Version="7.2.0" />
    
    <!-- CQRS / Messaging -->
    <PackageVersion Include="MediatR" Version="14.0.0" />
  </ItemGroup>
</Project>
```

### Project File References (No Version)

```xml
✅ Good (no version specified):
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="MediatR" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
</ItemGroup>

❌ Bad (version in project file):
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.2" />
  <PackageReference Include="MediatR" Version="14.0.0" />
</ItemGroup>
```

### Adding New Packages

1. **Add to Directory.Packages.props first**
   ```xml
   <PackageVersion Include="Serilog.AspNetCore" Version="8.0.0" />
   ```

2. **Reference in project file (without version)**
   ```xml
   <PackageReference Include="Serilog.AspNetCore" />
   ```

### Updating Package Versions

```bash
# Update single package in Directory.Packages.props
# Change version number in Directory.Packages.props
<PackageVersion Include="MediatR" Version="14.1.0" />

# Restore packages
dotnet restore

# All projects using MediatR will now use version 14.1.0
```

### Package Organization in Directory.Packages.props

Group packages by category with comments:

```xml
<ItemGroup>
  <!-- Aspire -->
  <PackageVersion Include="Aspire.Hosting.AppHost" Version="13.1.0" />
  
  <!-- Framework / Extensions -->
  <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.2" />
  
  <!-- Observability -->
  <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.0" />
  
  <!-- Data / Messaging -->
  <PackageVersion Include="Npgsql" Version="10.0.1" />
  
  <!-- API / Tooling -->
  <PackageVersion Include="Swashbuckle.AspNetCore" Version="10.1.2" />
  
  <!-- CQRS / Messaging -->
  <PackageVersion Include="MediatR" Version="14.0.0" />
  
  <!-- Migrations -->
  <PackageVersion Include="FluentMigrator.Runner.Postgres" Version="8.0.1" />
</ItemGroup>
```

### Benefits of Central Package Management

- ✅ **Version Consistency**: All projects use the same package versions
- ✅ **Easy Updates**: Update version in one place, affects all projects
- ✅ **Reduced Conflicts**: No version mismatches between projects
- ✅ **Cleaner Project Files**: No version clutter in individual `.csproj` files
- ✅ **Better Dependency Management**: Clear overview of all dependencies

### Verification

```bash
# Check if CPM is enabled
cat server/Directory.Packages.props | grep ManagePackageVersionsCentrally

# List all packages and versions
dotnet list package

# List with transitive dependencies
dotnet list package --include-transitive

# Check for outdated packages
dotnet list package --outdated
```

---

### NuGet Security Auditing

Starting with .NET 8+, NuGet includes built-in security vulnerability scanning.

#### Enable Security Auditing

Security auditing is **already enabled** in all Datarizen projects via `Directory.Build.props`:

```xml
<PropertyGroup>
  <NuGetAudit>true</NuGetAudit>
  <NuGetAuditLevel>low</NuGetAuditLevel>
  <NuGetAuditMode>all</NuGetAuditMode>
</PropertyGroup>
```

#### Check for Vulnerabilities

```bash
# Check all projects for vulnerabilities
dotnet restore

# List all packages with known vulnerabilities
dotnet list package --vulnerable

# Include transitive dependencies
dotnet list package --vulnerable --include-transitive

# Check specific severity levels
dotnet list package --vulnerable --severity high
dotnet list package --vulnerable --severity critical
```

#### Understanding Vulnerability Reports

```bash
# Example output:
Project `MonolithHost` has the following vulnerable packages
   [net10.0]:
   Top-level Package      Requested   Resolved   Severity   Advisory URL
   > System.Text.Json     8.0.0       8.0.0      High       https://github.com/advisories/GHSA-xxxx
```

#### Fix Vulnerabilities

**Option 1: Update Package Version (Recommended)**

1. Check for newer version:
   ```bash
   dotnet list package --outdated
   ```

2. Update in `Directory.Packages.props`:
   ```xml
   <!-- Before -->
   <PackageVersion Include="System.Text.Json" Version="8.0.0" />
   
   <!-- After -->
   <PackageVersion Include="System.Text.Json" Version="8.0.5" />
   ```

3. Restore and verify:
   ```bash
   dotnet restore
   dotnet list package --vulnerable
   ```

**Option 2: Suppress Known Vulnerabilities (Use Sparingly)**

Only suppress if:
- Vulnerability doesn't apply to your usage
- Fix is not yet available
- You have compensating controls

Create `nuget.config` or update existing:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  
  <vulnerabilities>
    <suppress>
      <!-- Suppress specific CVE -->
      <vulnerability id="CVE-2024-12345" />
    </suppress>
  </vulnerabilities>
</configuration>
```

Or suppress in project file:

```xml
<PropertyGroup>
  <NuGetAuditSuppress>CVE-2024-12345;CVE-2024-67890</NuGetAuditSuppress>
</PropertyGroup>
```

**Option 3: Update Transitive Dependencies**

If vulnerability is in a transitive dependency:

```bash
# Identify the package chain
dotnet list package --include-transitive | grep VulnerablePackage

# Add direct reference to force newer version
```

In `Directory.Packages.props`:

```xml
<!-- Force newer version of transitive dependency -->
<PackageVersion Include="System.Text.Json" Version="8.0.5" />
```

In project file:

```xml
<ItemGroup>
  <!-- Explicitly reference to override transitive version -->
  <PackageReference Include="System.Text.Json" />
</ItemGroup>
```

#### Audit Severity Levels

Configure minimum severity level in `Directory.Build.props`:

```xml
<PropertyGroup>
  <!-- Options: low, moderate, high, critical -->
  <NuGetAuditLevel>moderate</NuGetAuditLevel>
</PropertyGroup>
```

- **low**: All vulnerabilities (most verbose)
- **moderate**: Moderate, high, and critical only
- **high**: High and critical only
- **critical**: Critical only

#### CI/CD Integration

Add vulnerability checks to your build pipeline:

```yaml
# .github/workflows/build.yml
- name: Check for Vulnerabilities
  run: |
    dotnet restore
    dotnet list package --vulnerable --include-transitive
    
    # Fail build if vulnerabilities found
    if dotnet list package --vulnerable --include-transitive | grep -q "has the following vulnerable"; then
      echo "❌ Vulnerabilities found!"
      exit 1
    fi
```

#### Regular Maintenance

```bash
# Weekly/Monthly routine:

# 1. Check for outdated packages
dotnet list package --outdated

# 2. Check for vulnerabilities
dotnet list package --vulnerable --include-transitive

# 3. Update packages in Directory.Packages.props
# (Update versions one at a time, test after each)

# 4. Restore and test
dotnet restore
dotnet build
dotnet test
```

#### Best Practices

- ✅ **Enable NuGetAudit** in all projects (already done via `Directory.Build.props`)
- ✅ **Check vulnerabilities** before every release
- ✅ **Update packages regularly** (at least monthly)
- ✅ **Review transitive dependencies** - they can have vulnerabilities too
- ✅ **Document suppressions** - explain why vulnerability is suppressed
- ✅ **Automate checks** in CI/CD pipeline
- ❌ **Don't ignore warnings** - investigate and fix or suppress with justification
- ❌ **Don't suppress without understanding** - know what you're suppressing

#### Troubleshooting

**Issue**: `dotnet restore` shows vulnerability warnings but build succeeds

```bash
# This is expected - warnings don't fail the build by default
# To fail on vulnerabilities, set:
```

```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <!-- Or specifically for NuGet audit -->
  <WarningsAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsAsErrors>
</PropertyGroup>
```

**Issue**: Cannot find newer version without vulnerability

```bash
# Check package on nuget.org
# Check GitHub advisories: https://github.com/advisories
# Consider alternative packages
# Suppress with justification if no fix available
```

**Issue**: Vulnerability in package you don't directly reference

```bash
# Find which package depends on it
dotnet list package --include-transitive | grep VulnerablePackage

# Update the parent package or add direct reference to override
```

#### Example Workflow

```bash
# 1. Check current state
dotnet list package --vulnerable --include-transitive

# Output:
# Project `MonolithHost` has the following vulnerable packages
#    [net10.0]:
#    Top-level Package      Requested   Resolved   Severity   Advisory URL
#    > Npgsql               9.0.0       9.0.0      High       https://github.com/advisories/GHSA-xxxx

# 2. Check for updates
dotnet list package --outdated

# Output:
# Project `MonolithHost` has the following updates to its packages
#    [net10.0]:
#    Top-level Package      Requested   Resolved   Latest
#    > Npgsql               9.0.0       9.0.0      10.0.1

# 3. Update in Directory.Packages.props
# Change: <PackageVersion Include="Npgsql" Version="9.0.0" />
# To:     <PackageVersion Include="Npgsql" Version="10.0.1" />

# 4. Restore and verify
dotnet restore
dotnet list package --vulnerable --include-transitive

# Output: (no vulnerabilities found)

# 5. Test
dotnet build
dotnet test
```

---

## Entity Framework Core Conventions

### DbContext Configuration

```csharp
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

### Entity Configuration (Fluent API)

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(o => o.Total)
            .HasPrecision(18, 2);
        
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(o => o.CustomerId);
    }
}
```

### Repository Pattern

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    void Update(Order order);
    void Delete(Order order);
}

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;
    
    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }
    
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }
    
    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }
}
```

### Unit of Work Pattern

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly OrderDbContext _context;
    
    public UnitOfWork(OrderDbContext context)
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

## API Design Conventions

### Module API Layer

- **Controllers belong in `{Module}.Api`** projects under `server/src/Product/{Module}`.
- **Module projects (`{Module}.Module`)** should focus on registration/startup concerns.
- Prefer a clear module route prefix like `api/tenant`, `api/identity`, etc.

### Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;
    
    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : NotFound(result.Error);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetOrder), new { id = result.Value.Id }, result.Value)
            : BadRequest(result.Error);
    }
}
```

### API Response Format

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public List<string>? Errors { get; set; }
    
    public static ApiResponse<T> SuccessResponse(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }
    
    public static ApiResponse<T> ErrorResponse(string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error
        };
    }
}
```

---

## Logging Conventions

### Structured Logging

```csharp
✅ Good (structured logging):
_logger.LogInformation(
    "Order {OrderId} created for customer {CustomerId} with total {Total}",
    order.Id,
    order.CustomerId,
    order.Total);

❌ Bad (string interpolation):
_logger.LogInformation($"Order {order.Id} created for customer {order.CustomerId}");
```

### Log Levels

```csharp
// Trace - Very detailed, typically only enabled in development
_logger.LogTrace("Entering method GetOrderAsync with orderId: {OrderId}", orderId);

// Debug - Detailed information for debugging
_logger.LogDebug("Cache miss for order {OrderId}", orderId);

// Information - General informational messages
_logger.LogInformation("Order {OrderId} created successfully", order.Id);

// Warning - Unexpected but recoverable situations
_logger.LogWarning("Order {OrderId} has no items", order.Id);

// Error - Error events that might still allow the application to continue
_logger.LogError(ex, "Failed to create order for customer {CustomerId}", customerId);

// Critical - Critical failures that require immediate attention
_logger.LogCritical(ex, "Database connection failed");
```

### Don't Log Sensitive Data

```csharp
✅ Good:
_logger.LogInformation("User {UserId} logged in", user.Id);

❌ Bad:
_logger.LogInformation("User {Email} logged in with password {Password}", 
    user.Email, user.Password);
```

---

## Summary Checklist

### Naming
- [ ] PascalCase for classes, methods, properties
- [ ] camelCase for local variables and parameters
- [ ] _camelCase for private fields
- [ ] IPascalCase for interfaces

### Files
- [ ] One class per file
- [ ] File name matches class name
- [ ] Feature folder organization

### Code Structure
- [ ] File-scoped namespaces
- [ ] Ordered using directives
- [ ] Consistent member ordering

### Dependency Injection
- [ ] Constructor injection
- [ ] Extension methods for registration
- [ ] Appropriate service lifetimes

### Async/Await
- [ ] Async suffix on methods
- [ ] Avoid async void
- [ ] Pass CancellationToken
- [ ] Avoid Task.Result/Wait()

### Error Handling
- [ ] Use Result pattern
- [ ] Exceptions for exceptional cases
- [ ] Global exception handling

### Collections
- [ ] LINQ method syntax
- [ ] IEnumerable<T> for returns
- [ ] Collection expressions (.NET 10+)

### Null Handling
- [ ] Nullable reference types enabled
- [ ] Pattern matching for null checks
- [ ] ArgumentNullException.ThrowIfNull

### Documentation
- [ ] XML docs for public APIs
- [ ] Meaningful inline comments (WHY not WHAT)

### Testing
- [ ] Arrange-Act-Assert pattern
- [ ] Descriptive test names
- [ ] FluentAssertions

### Performance
- [ ] Profile before optimizing
- [ ] Avoid string concatenation in loops
- [ ] Use Span<T> for hot paths

### Security
- [ ] Validate user input
- [ ] Parameterized queries
- [ ] Don't log sensitive data

### NuGet
- [ ] Central package management
- [ ] Regular vulnerability checks
- [ ] Update packages regularly

### Entity Framework
- [ ] Fluent API configuration
- [ ] Repository pattern
- [ ] Unit of Work pattern

### API Design
- [ ] RESTful conventions
- [ ] Proper HTTP status codes
- [ ] Consistent response format

### Logging
- [ ] Structured logging
- [ ] Appropriate log levels
- [ ] No sensitive data in logs