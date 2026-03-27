# BuildingBlocks.Infrastructure

Infrastructure services and implementations shared across all modules.

## Structure

- `EventBus/`
  - `IEventBus`
  - `InMemoryEventBus`
  - `RabbitMqEventBus`
- `Caching/`
  - `ICacheService`
  - `RedisCacheService`
- `BackgroundJobs/`
  - `IBackgroundJobScheduler`
- `UnitOfWork/`
  - `IUnitOfWork`
  - `UnitOfWork`
- `Outbox/`
  - `OutboxMessage`
  - `OutboxProcessor`
- `Repositories/`
  - `IRepository`
  - `Repository`

## Dependencies

- `BuildingBlocks.Kernel`
- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `StackExchange.Redis`
- `RabbitMQ.Client`

The project centralizes cross-cutting infrastructure concerns and should remain free of domain-specific logic.

