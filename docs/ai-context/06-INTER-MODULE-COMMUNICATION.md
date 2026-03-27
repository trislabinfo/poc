# Datarizen AI Context - Inter-Module Communication

## Overview

Modules communicate using **topology-aware patterns** that work across all deployment models (Monolith, Multi-App, Microservices). The same module code adapts to different topologies through configuration and dependency injection.

---

## Communication Mechanisms by Topology

### Monolith Topology

**Modules in same process communicate via:**

1. **MediatR Commands/Queries** (synchronous, transactional)
2. **Domain Events** (in-memory event bus, synchronous)
3. **Direct Service Interfaces** (dependency injection)

**Benefits:**
- Immediate consistency
- Single database transaction
- No network latency
- Simple debugging

---

### Multi-App Topology

**Modules in same host:** Use MediatR (in-process)

**Modules in different hosts:** Use HTTP/gRPC + Message Bus

**Benefits:**
- Balance between simplicity and scalability
- Modules grouped by concern
- Independent host scaling

---

### Microservices Topology

**All modules communicate via:**

1. **HTTP/gRPC** (synchronous, request-response)
2. **Message Bus** (asynchronous, events)

**Benefits:**
- Independent deployment per module
- Independent scaling per module
- Technology diversity per module

---

## Pattern 1: Synchronous Request-Response

### In-Process Communication (MediatR)

**Use when:** Modules deployed in same host, need immediate response

**Module B defines command in Contracts:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Contracts/Commands/CreateEntityCommand.cs
using Datarizen.BuildingBlocks.Kernel.Application;

namespace Datarizen.ModuleB.Contracts.Commands;

public record CreateEntityCommand(
    string Name,
    string Description
) : ICommand<Result<Guid>>;
```

**Module B implements handler:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Application/Commands/CreateEntity/CreateEntityCommandHandler.cs
public class CreateEntityCommandHandler 
    : IRequestHandler<CreateEntityCommand, Result<Guid>>
{
    private readonly IRepository<Entity, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(
        CreateEntityCommand command, 
        CancellationToken cancellationToken)
    {
        var entity = Entity.Create(command.Name, command.Description);
        
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(entity.Id);
    }
}
```

**Module A sends command:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Application/Services/ModuleAService.cs
using Datarizen.ModuleB.Contracts.Commands;
using MediatR;

public class ModuleAService
{
    private readonly IMediator _mediator;

    public async Task<Result> ProcessAsync()
    {
        var result = await _mediator.Send(
            new CreateEntityCommand("Name", "Description"));
        
        if (result.IsFailure)
            return Result.Failure(result.Error);
        
        var entityId = result.Value;
        // Continue processing with entityId
        
        return Result.Success();
    }
}
```

**Topology Support:**
- ✅ Monolith: Direct in-process call
- ✅ Multi-App (same host): Direct in-process call
- ⚠️ Multi-App (different hosts): Use HTTP instead
- ⚠️ Microservices: Use HTTP instead

---

### Cross-Process Communication (HTTP)

**Use when:** Modules deployed in different hosts, need immediate response

**Module B exposes HTTP endpoint:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Module/Controllers/EntitiesController.cs
[ApiController]
[Route("api/module-b/entities")]
public class EntitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEntityRequest request)
    {
        var result = await _mediator.Send(
            new CreateEntityCommand(request.Name, request.Description));
        
        return result.ToActionResult();
    }
}
```

**Module B defines client interface in Contracts:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Contracts/Clients/IModuleBClient.cs
namespace Datarizen.ModuleB.Contracts.Clients;

public interface IModuleBClient
{
    Task<Result<Guid>> CreateEntityAsync(string name, string description, CancellationToken cancellationToken = default);
}
```

**Module A uses client interface:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Application/Services/ModuleAService.cs
using Datarizen.ModuleB.Contracts.Clients;

public class ModuleAService
{
    private readonly IModuleBClient _moduleBClient;

    public async Task<Result> ProcessAsync()
    {
        var result = await _moduleBClient.CreateEntityAsync("Name", "Description");
        
        if (result.IsFailure)
            return Result.Failure(result.Error);
        
        return Result.Success();
    }
}
```

**Module A implements HTTP client:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/ExternalServices/ModuleBHttpClient.cs
using Datarizen.ModuleB.Contracts.Clients;

public class ModuleBHttpClient : IModuleBClient
{
    private readonly HttpClient _httpClient;

    public ModuleBHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ModuleB");
    }

    public async Task<Result<Guid>> CreateEntityAsync(
        string name, 
        string description, 
        CancellationToken cancellationToken = default)
    {
        var request = new { Name = name, Description = description };
        
        var response = await _httpClient.PostAsJsonAsync(
            "/api/module-b/entities", 
            request, 
            cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            return Result<Guid>.Failure(new Error(error.Code, error.Message));
        }
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(cancellationToken: cancellationToken);
        return Result<Guid>.Success(result.Data);
    }
}
```

**Module A implements MediatR client (for monolith):**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/ExternalServices/ModuleBMediatorClient.cs
using Datarizen.ModuleB.Contracts.Clients;
using Datarizen.ModuleB.Contracts.Commands;
using MediatR;

public class ModuleBMediatorClient : IModuleBClient
{
    private readonly IMediator _mediator;

    public ModuleBMediatorClient(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<Guid>> CreateEntityAsync(
        string name, 
        string description, 
        CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(
            new CreateEntityCommand(name, description), 
            cancellationToken);
    }
}
```

**Topology-aware registration:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var topology = configuration["Deployment:Topology"];
    
    if (topology == "Monolith" || topology == "SingleTenantMonolith")
    {
        // In-process: Use MediatR client
        services.AddScoped<IModuleBClient, ModuleBMediatorClient>();
    }
    else
    {
        // Cross-process: Use HTTP client
        services.AddHttpClient("ModuleB", client =>
        {
            var baseUrl = configuration["Modules:ModuleB:BaseUrl"] ?? "http://localhost:5002";
            client.BaseAddress = new Uri(baseUrl);
        });
        
        services.AddScoped<IModuleBClient, ModuleBHttpClient>();
    }
    
    return services;
}
```

**Topology Support:**
- ⚠️ Monolith: Use MediatR instead
- ✅ Multi-App (different hosts): HTTP between hosts
- ✅ Microservices: HTTP between services

---

## Pattern 2: Asynchronous Events

### In-Process Events (Domain Events)

**Use when:** Modules in same host, need to react to changes

**Module B defines domain event:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Domain/Events/EntityCreatedEvent.cs
using Datarizen.BuildingBlocks.Kernel.Domain;

namespace Datarizen.ModuleB.Domain.Events;

public record EntityCreatedEvent(
    Guid EntityId,
    string Name,
    DateTime CreatedAt
) : DomainEvent;
```

**Module B entity raises event:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Domain/Entities/Entity.cs
public class Entity : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    
    public static Entity Create(string name, string description)
    {
        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            Name = name
        };
        
        entity.RaiseDomainEvent(new EntityCreatedEvent(
            entity.Id,
            entity.Name,
            DateTime.UtcNow));
        
        return entity;
    }
}
```

**Module A subscribes to event:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Application/EventHandlers/EntityCreatedEventHandler.cs
using Datarizen.ModuleB.Domain.Events;
using MediatR;

namespace Datarizen.ModuleA.Application.EventHandlers;

public class EntityCreatedEventHandler : INotificationHandler<EntityCreatedEvent>
{
    private readonly IRepository<RelatedEntity, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task Handle(EntityCreatedEvent notification, CancellationToken cancellationToken)
    {
        var relatedEntity = RelatedEntity.Create(notification.EntityId, notification.Name);
        
        await _repository.AddAsync(relatedEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

**Topology Support:**
- ✅ Monolith: In-memory event bus
- ✅ Multi-App (same host): In-memory event bus
- ⚠️ Multi-App (different hosts): Use integration events instead
- ⚠️ Microservices: Use integration events instead

---

### Cross-Process Events (Integration Events)

**Use when:** Modules in different hosts, eventual consistency acceptable

**Module B defines integration event in Contracts:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Contracts/Events/EntityCreatedIntegrationEvent.cs
using Datarizen.BuildingBlocks.Contracts.Messaging;

namespace Datarizen.ModuleB.Contracts.Events;

public record EntityCreatedIntegrationEvent(
    Guid EntityId,
    string Name,
    DateTime CreatedAt,
    Guid? TenantId
) : IIntegrationEvent;
```

**Module B publishes to message bus:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Application/Commands/CreateEntity/CreateEntityCommandHandler.cs
using Datarizen.BuildingBlocks.Infrastructure.EventBus;
using Datarizen.ModuleB.Contracts.Events;

public class CreateEntityCommandHandler : IRequestHandler<CreateEntityCommand, Result<Guid>>
{
    private readonly IRepository<Entity, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ITenantContext _tenantContext;

    public async Task<Result<Guid>> Handle(
        CreateEntityCommand command, 
        CancellationToken cancellationToken)
    {
        var entity = Entity.Create(command.Name, command.Description);
        
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Publish integration event
        await _eventBus.PublishAsync(
            new EntityCreatedIntegrationEvent(
                entity.Id,
                entity.Name,
                DateTime.UtcNow,
                _tenantContext.TenantId
            ),
            cancellationToken);
        
        return Result<Guid>.Success(entity.Id);
    }
}
```

**Module A defines event handler:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/EventHandlers/EntityCreatedIntegrationEventHandler.cs
using Datarizen.BuildingBlocks.Infrastructure.EventBus;
using Datarizen.ModuleB.Contracts.Events;

public class EntityCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<EntityCreatedIntegrationEvent>
{
    private readonly IRepository<RelatedEntity, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public async Task HandleAsync(
        EntityCreatedIntegrationEvent @event, 
        CancellationToken cancellationToken)
    {
        // Set tenant context from event
        if (@event.TenantId.HasValue)
            _tenantContext.SetTenantId(@event.TenantId.Value);
        
        var relatedEntity = RelatedEntity.Create(@event.EntityId, @event.Name);
        
        await _repository.AddAsync(relatedEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

**Module A registers subscription:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Module/ModuleAModule.cs
public static IApplicationBuilder UseModule(this IApplicationBuilder app)
{
    var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
    
    eventBus.Subscribe<EntityCreatedIntegrationEvent, EntityCreatedIntegrationEventHandler>();
    
    return app;
}
```

**Topology Support:**
- ⚠️ Monolith: Use domain events instead
- ✅ Multi-App (different hosts): Message bus between hosts
- ✅ Microservices: Message bus between services

---

## Pattern 3: Data Sharing Between Modules

### Service Interface Pattern

**Use when:** Need real-time data from another module

**Module B defines query service in Contracts:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Contracts/Services/IModuleBQueryService.cs
namespace Datarizen.ModuleB.Contracts.Services;

public interface IModuleBQueryService
{
    Task<Result<EntityDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<List<EntityDto>>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
}

public record EntityDto(Guid Id, string Name, string Description);
```

**Module B implements service:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Infrastructure/Services/ModuleBQueryService.cs
public class ModuleBQueryService : IModuleBQueryService
{
    private readonly IRepository<Entity, Guid> _repository;

    public async Task<Result<EntityDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (entity == null)
            return Result<EntityDto>.Failure(new Error("ModuleB.NotFound", "Entity not found"));
        
        return Result<EntityDto>.Success(new EntityDto(entity.Id, entity.Name, entity.Description));
    }
}
```

**Module B registers service:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddScoped<IModuleBQueryService, ModuleBQueryService>();
    
    return services;
}
```

**Module A uses service (in-process):**

```csharp
// server/src/Modules/ModuleA/ModuleA.Application/Queries/GetCombinedData/GetCombinedDataQueryHandler.cs
using Datarizen.ModuleB.Contracts.Services;

public class GetCombinedDataQueryHandler : IRequestHandler<GetCombinedDataQuery, Result<CombinedDataDto>>
{
    private readonly IModuleBQueryService _moduleBQueryService;
    private readonly IRepository<ModuleAEntity, Guid> _repository;

    public async Task<Result<CombinedDataDto>> Handle(
        GetCombinedDataQuery query, 
        CancellationToken cancellationToken)
    {
        var moduleAEntity = await _repository.GetByIdAsync(query.Id, cancellationToken);
        
        if (moduleAEntity == null)
            return Result<CombinedDataDto>.Failure(new Error("NotFound", "Entity not found"));
        
        var moduleBResult = await _moduleBQueryService.GetByIdAsync(
            moduleAEntity.RelatedEntityId, 
            cancellationToken);
        
        if (moduleBResult.IsFailure)
            return Result<CombinedDataDto>.Failure(moduleBResult.Error);
        
        var combined = new CombinedDataDto
        {
            ModuleAData = ModuleADto.FromEntity(moduleAEntity),
            ModuleBData = moduleBResult.Value
        };
        
        return Result<CombinedDataDto>.Success(combined);
    }
}
```

**Module A implements HTTP wrapper (cross-process):**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/ExternalServices/ModuleBQueryHttpClient.cs
using Datarizen.ModuleB.Contracts.Services;

public class ModuleBQueryHttpClient : IModuleBQueryService
{
    private readonly HttpClient _httpClient;

    public async Task<Result<EntityDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/module-b/entities/{id}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            return Result<EntityDto>.Failure(new Error("ModuleB.Error", "Failed to get entity"));
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EntityDto>>(cancellationToken: cancellationToken);
        return Result<EntityDto>.Success(result.Data);
    }
}
```

**Module A registers topology-aware implementation:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var topology = configuration["Deployment:Topology"];
    
    if (topology == "Monolith" || topology == "SingleTenantMonolith")
    {
        // In-process: Module B's service already registered in DI
        // No additional registration needed
    }
    else
    {
        // Cross-process: Register HTTP wrapper
        services.AddHttpClient("ModuleB", client =>
        {
            var baseUrl = configuration["Modules:ModuleB:BaseUrl"] ?? "http://localhost:5002";
            client.BaseAddress = new Uri(baseUrl);
        });
        
        services.AddScoped<IModuleBQueryService, ModuleBQueryHttpClient>();
    }
    
    return services;
}
```

**Topology Support:**
- ✅ Monolith: Direct service call via DI
- ✅ Multi-App (same host): Direct service call via DI
- ✅ Multi-App (different hosts): HTTP wrapper
- ✅ Microservices: HTTP wrapper

---

### Denormalized Data Pattern

**Use when:** Frequent access to another module's data, eventual consistency acceptable

**Module A stores cached copy:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Domain/Entities/EntityBCache.cs
public class EntityBCache : Entity<Guid>
{
    public Guid OriginalEntityId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime LastSyncedAt { get; private set; }
    
    public static EntityBCache Create(Guid originalEntityId, string name, string description)
    {
        return new EntityBCache
        {
            Id = Guid.NewGuid(),
            OriginalEntityId = originalEntityId,
            Name = name,
            Description = description,
            LastSyncedAt = DateTime.UtcNow
        };
    }
    
    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        LastSyncedAt = DateTime.UtcNow;
    }
}
```

**Module A subscribes to Module B events:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/EventHandlers/EntityBCreatedEventHandler.cs
using Datarizen.ModuleB.Contracts.Events;

public class EntityBCreatedEventHandler : IIntegrationEventHandler<EntityCreatedIntegrationEvent>
{
    private readonly IRepository<EntityBCache, Guid> _cacheRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task HandleAsync(EntityCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        var cache = EntityBCache.Create(@event.EntityId, @event.Name, "");
        
        await _cacheRepository.AddAsync(cache, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

**Module A queries its own cache:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Application/Queries/GetEntityB/GetEntityBQueryHandler.cs
public class GetEntityBQueryHandler : IRequestHandler<GetEntityBQuery, Result<EntityBDto>>
{
    private readonly IRepository<EntityBCache, Guid> _cacheRepository;

    public async Task<Result<EntityBDto>> Handle(GetEntityBQuery query, CancellationToken cancellationToken)
    {
        var cache = await _cacheRepository.FirstOrDefaultAsync(
            c => c.OriginalEntityId == query.EntityId, 
            cancellationToken);
        
        if (cache == null)
            return Result<EntityBDto>.Failure(new Error("NotFound", "Entity not found"));
        
        return Result<EntityBDto>.Success(new EntityBDto
        {
            Id = cache.OriginalEntityId,
            Name = cache.Name,
            Description = cache.Description
        });
    }
}
```

**Topology Support:**
- ✅ Monolith: Works (reduces queries)
- ✅ Multi-App: Works (reduces cross-host calls)
- ✅ Microservices: Works (reduces cross-service calls)

---

## Pattern 4: Junction Tables Across Modules

**Use when:** Many-to-many relationship between entities in different modules

**Module A owns its junction table:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Domain/Entities/EntityAEntityBLink.cs
public class EntityAEntityBLink : Entity<Guid>
{
    public Guid EntityAId { get; private set; }
    public Guid EntityBId { get; private set; } // Reference, not FK
    public DateTime LinkedAt { get; private set; }
    public Guid? LinkedBy { get; private set; }
    
    public EntityA EntityA { get; private set; } = null!;
    
    public static EntityAEntityBLink Create(Guid entityAId, Guid entityBId, Guid? linkedBy)
    {
        return new EntityAEntityBLink
        {
            Id = Guid.NewGuid(),
            EntityAId = entityAId,
            EntityBId = entityBId,
            LinkedAt = DateTime.UtcNow,
            LinkedBy = linkedBy
        };
    }
}
```

**Module B owns its junction table:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Domain/Entities/EntityBEntityALink.cs
public class EntityBEntityALink : Entity<Guid>
{
    public Guid EntityBId { get; private set; }
    public Guid EntityAId { get; private set; } // Reference, not FK
    public string Role { get; private set; } = string.Empty;
    public DateTime AddedAt { get; private set; }
    
    public EntityB EntityB { get; private set; } = null!;
    
    public static EntityBEntityALink Create(Guid entityBId, Guid entityAId, string role)
    {
        return new EntityBEntityALink
        {
            Id = Guid.NewGuid(),
            EntityBId = entityBId,
            EntityAId = entityAId,
            Role = role,
            AddedAt = DateTime.UtcNow
        };
    }
}
```

**Module A creates link and publishes event:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Application/Commands/LinkEntityB/LinkEntityBCommandHandler.cs
public class LinkEntityBCommandHandler : IRequestHandler<LinkEntityBCommand, Result>
{
    private readonly IRepository<EntityAEntityBLink, Guid> _linkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public async Task<Result> Handle(LinkEntityBCommand command, CancellationToken cancellationToken)
    {
        var link = EntityAEntityBLink.Create(command.EntityAId, command.EntityBId, command.LinkedBy);
        
        await _linkRepository.AddAsync(link, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _eventBus.PublishAsync(
            new EntityALinkedToEntityBEvent(command.EntityAId, command.EntityBId, link.LinkedAt),
            cancellationToken);
        
        return Result.Success();
    }
}
```

**Module B subscribes and creates its link:**

```csharp
// server/src/Modules/ModuleB/ModuleB.Infrastructure/EventHandlers/EntityALinkedToEntityBEventHandler.cs
public class EntityALinkedToEntityBEventHandler 
    : IIntegrationEventHandler<EntityALinkedToEntityBEvent>
{
    private readonly IRepository<EntityBEntityALink, Guid> _linkRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task HandleAsync(EntityALinkedToEntityBEvent @event, CancellationToken cancellationToken)
    {
        var link = EntityBEntityALink.Create(@event.EntityBId, @event.EntityAId, "DefaultRole");
        
        await _linkRepository.AddAsync(link, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

**Topology Support:**
- ✅ Monolith: Works (domain events for sync)
- ✅ Multi-App: Works (integration events for sync)
- ✅ Microservices: Works (integration events for sync)

---

## Pattern 5: Multi-Step Workflows (Saga Pattern)

### Orchestration Saga

**Use when:** Centralized coordinator manages workflow

**Coordinator orchestrates all steps:**

```csharp
// server/src/Modules/Orders/Orders.Application/Sagas/CreateOrderSaga.cs
public class CreateOrderSaga
{
    private readonly IInventoryClient _inventoryClient;
    private readonly IPaymentClient _paymentClient;
    private readonly IMediator _mediator;

    public async Task<Result<Guid>> ExecuteAsync(CreateOrderCommand command)
    {
        Guid? reservationId = null;
        Guid? paymentId = null;
        
        try
        {
            // Step 1: Reserve inventory
            var reserveResult = await _inventoryClient.ReserveAsync(
                command.ProductId, 
                command.Quantity);
            
            if (reserveResult.IsFailure)
                return Result<Guid>.Failure(reserveResult.Error);
            
            reservationId = reserveResult.Value;
            
            // Step 2: Process payment
            var paymentResult = await _paymentClient.ProcessAsync(
                command.Amount, 
                command.PaymentMethod);
            
            if (paymentResult.IsFailure)
            {
                await _inventoryClient.ReleaseAsync(reservationId.Value);
                return Result<Guid>.Failure(paymentResult.Error);
            }
            
            paymentId = paymentResult.Value;
            
            // Step 3: Create order
            var createResult = await _mediator.Send(
                new CreateOrderInternalCommand(
                    command.ProductId,
                    command.Quantity,
                    reservationId.Value,
                    paymentId.Value));
            
            if (createResult.IsFailure)
            {
                await _paymentClient.RefundAsync(paymentId.Value);
                await _inventoryClient.ReleaseAsync(reservationId.Value);
                return Result<Guid>.Failure(createResult.Error);
            }
            
            return Result<Guid>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            if (paymentId.HasValue)
                await _paymentClient.RefundAsync(paymentId.Value);
            
            if (reservationId.HasValue)
                await _inventoryClient.ReleaseAsync(reservationId.Value);
            
            return Result<Guid>.Failure(new Error("Saga.Failed", ex.Message));
        }
    }
}
```

**Topology Support:**
- ✅ Monolith: Works (in-process calls)
- ✅ Multi-App: Works (HTTP calls)
- ✅ Microservices: Works (HTTP calls)

---

### Choreography Saga

**Use when:** Decentralized, event-driven workflow

**Step 1: Orders module starts:**

```csharp
// server/src/Modules/Orders/Orders.Application/Commands/StartOrder/StartOrderCommandHandler.cs
public class StartOrderCommandHandler : IRequestHandler<StartOrderCommand, Result<Guid>>
{
    private readonly IRepository<Order, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public async Task<Result<Guid>> Handle(StartOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var order = Order.Create(orderId, command.ProductId, command.Quantity, "Pending");
        
        await _repository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _eventBus.PublishAsync(
            new OrderStartedEvent(orderId, command.ProductId, command.Quantity),
            cancellationToken);
        
        return Result<Guid>.Success(orderId);
    }
}
```

**Step 2: Inventory module reacts:**

```csharp
// server/src/Modules/Inventory/Inventory.Infrastructure/EventHandlers/OrderStartedEventHandler.cs
public class OrderStartedEventHandler : IIntegrationEventHandler<OrderStartedEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly IEventBus _eventBus;

    public async Task HandleAsync(OrderStartedEvent @event, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.ReserveAsync(@event.ProductId, @event.Quantity);
        
        if (result.IsSuccess)
        {
            await _eventBus.PublishAsync(
                new InventoryReservedEvent(@event.OrderId, result.Value),
                cancellationToken);
        }
        else
        {
            await _eventBus.PublishAsync(
                new InventoryReservationFailedEvent(@event.OrderId, result.Error.Message),
                cancellationToken);
        }
    }
}
```

**Step 3: Payment module reacts:**

```csharp
// server/src/Modules/Payment/Payment.Infrastructure/EventHandlers/InventoryReservedEventHandler.cs
public class InventoryReservedEventHandler : IIntegrationEventHandler<InventoryReservedEvent>
{
    private readonly IPaymentService _paymentService;
    private readonly IEventBus _eventBus;

    public async Task HandleAsync(InventoryReservedEvent @event, CancellationToken cancellationToken)
    {
        var result = await _paymentService.ProcessAsync(@event.OrderId);
        
        if (result.IsSuccess)
        {
            await _eventBus.PublishAsync(
                new PaymentProcessedEvent(@event.OrderId, result.Value),
                cancellationToken);
        }
        else
        {
            await _eventBus.PublishAsync(
                new PaymentFailedEvent(@event.OrderId, result.Error.Message),
                cancellationToken);
        }
    }
}
```

**Step 4: Orders module finalizes:**

```csharp
// server/src/Modules/Orders/Orders.Infrastructure/EventHandlers/PaymentProcessedEventHandler.cs
public class PaymentProcessedEventHandler : IIntegrationEventHandler<PaymentProcessedEvent>
{
    private readonly IRepository<Order, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task HandleAsync(PaymentProcessedEvent @event, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(@event.OrderId, cancellationToken);
        
        if (order != null)
        {
            order.Confirm(@event.PaymentId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
```

**Compensation on failure:**

```csharp
// server/src/Modules/Orders/Orders.Infrastructure/EventHandlers/PaymentFailedEventHandler.cs
public class PaymentFailedEventHandler : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IRepository<Order, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public async Task HandleAsync(PaymentFailedEvent @event, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(@event.OrderId, cancellationToken);
        
        if (order != null)
        {
            order.Cancel(@event.Reason);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            await _eventBus.PublishAsync(
                new OrderCancelledEvent(@event.OrderId),
                cancellationToken);
        }
    }
}
```

**Topology Support:**
- ⚠️ Monolith: Adds complexity (use orchestration)
- ✅ Multi-App: Works well
- ✅ Microservices: Works well

---

## Pattern 6: Guaranteed Event Delivery (Transactional Outbox)

**Use when:** Must guarantee event publication even if message bus unavailable

**Outbox table:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Domain/Entities/OutboxMessage.cs
public class OutboxMessage : Entity<Guid>
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
```

**DbContext saves events to outbox:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/Persistence/ModuleADbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var domainEvents = ChangeTracker.Entries<IAggregateRoot>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();
    
    foreach (var domainEvent in domainEvents)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredAt = DateTime.UtcNow
        };
        
        OutboxMessages.Add(outboxMessage);
    }
    
    return await base.SaveChangesAsync(cancellationToken);
}
```

**Background job processes outbox:**

```csharp
// server/src/Modules/ModuleA/ModuleA.Infrastructure/BackgroundJobs/OutboxProcessorJob.cs
public class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ModuleADbContext>();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            
            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.OccurredAt)
                .Take(100)
                .ToListAsync(stoppingToken);
            
            foreach (var message in messages)
            {
                try
                {
                    var eventType = Type.GetType(message.Type)!;
                    var @event = JsonSerializer.Deserialize(message.Content, eventType);
                    
                    await eventBus.PublishAsync(@event, stoppingToken);
                    
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.Error = ex.Message;
                }
            }
            
            await dbContext.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

**Topology Support:**
- ⚠️ Monolith: Optional (in-memory events simpler)
- ✅ Multi-App: Recommended
- ✅ Microservices: Recommended

---

## Communication Decision Guide

### Need immediate response?
- **Yes** → Synchronous (MediatR or HTTP)
- **No** → Asynchronous (Events)

### Same host deployment?
- **Yes** → MediatR (in-process)
- **No** → HTTP (cross-process)

### Need transactional consistency?
- **Yes** → Saga Pattern
- **No** → Integration Events

### Frequent data access?
- **Yes** → Denormalized Data
- **No** → Service Interface

### Must guarantee delivery?
- **Yes** → Transactional Outbox
- **No** → Direct event publishing

---

## Summary

**Topology-Aware Patterns:**

| Pattern | Monolith | Multi-App | Microservices |
|---------|----------|-----------|---------------|
| MediatR Commands | ✅ Primary | ✅ Same host | ⚠️ Not applicable |
| HTTP/gRPC | ⚠️ Not needed | ✅ Different hosts | ✅ Primary |
| Domain Events | ✅ Primary | ✅ Same host | ⚠️ Not applicable |
| Integration Events | ⚠️ Optional | ✅ Different hosts | ✅ Primary |
| Service Interface | ✅ DI | ✅ DI or HTTP | ✅ HTTP |
| Denormalized Data | ✅ Works | ✅ Recommended | ✅ Recommended |
| Junction Tables | ✅ Works | ✅ Works | ✅ Works |
| Orchestration Saga | ✅ Works | ✅ Works | ✅ Works |
| Choreography Saga | ⚠️ Complex | ✅ Works | ✅ Recommended |
| Transactional Outbox | ⚠️ Optional | ✅ Recommended | ✅ Recommended |

**Key Principles:**
- Same module code works across all topologies
- Communication mechanism changes via configuration
- Use Contracts for all inter-module communication
- Each module owns its own data and schema
- Use events for eventual consistency
- Use sagas for multi-step workflows
- Use transactional outbox for guaranteed delivery

