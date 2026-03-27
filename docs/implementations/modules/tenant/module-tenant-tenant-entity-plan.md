# Tenant Module – Vertical Slice Implementation Plan (Create Tenant + GetById)

**Target:** Full vertical slice for Tenant with **Create** and **GetById**. Tenant has **Name** and **Slug** only (plus Id, CreatedAt, UpdatedAt from base Entity).  
**Conventions:** Same as Identity: schema from `TenantModule.SchemaName`, layer extensions at project root, no EF migrations in-app (FluentMigrator only), `Result<T>`, MediatR, FluentValidation, Ardalis.Specification where needed.

**Order of implementation:** Domain → Infrastructure → Application → API → Migrations → Module.

---

## Phase 1: Domain Layer

**Goal:** Tenant aggregate, repository interface, optional domain event. No dependencies on other modules.

### 1.1 Tenant entity

- **File:** `Tenant.Domain/Entities/Tenant.cs`
- **Content:**
  - `Tenant` : `AggregateRoot<Guid>` (from `BuildingBlocks.Kernel.Domain`).
  - Properties: `Id`, `Name` (string), `Slug` (string), `CreatedAt`, `UpdatedAt` (from base); private parameterless ctor for EF.
  - Static factory: `Result<Tenant> Create(string name, string slug, IDateTimeProvider dateTimeProvider)`:
    - Validate: name/slug not null-or-white-space; slug format (e.g. lowercase, no spaces; allow `[a-z0-9-]+`); name/slug length (e.g. name ≤ 200, slug ≤ 100).
    - Set `Id = Guid.NewGuid()`, `CreatedAt = dateTimeProvider.UtcNow`.
    - Optionally raise a `TenantCreatedEvent(Id, Name, Slug, CreatedAt)` (if event exists).
  - Use `Guard` and `Result`/`Error` from `BuildingBlocks.Kernel.Results`.
- **References:** `Tenant.Domain` already references `BuildingBlocks.Kernel`; add no extra packages.

### 1.2 Domain event

- **File:** `Tenant.Domain/Events/TenantCreatedEvent.cs`
- **Content:** Record implementing `IDomainEvent` (from Kernel) with `TenantId`, `Name`, `Slug`, `OccurredAt`. Used in `Tenant.Create` if you add events.

### 1.3 Repository interface

- **File:** `Tenant.Domain/Repositories/ITenantRepository.cs`
- **Content:** `interface ITenantRepository : IRepository<Tenant, Guid>` (from `BuildingBlocks.Infrastructure.Persistence`). Add no extra methods for this slice; `GetByIdAsync` comes from the base interface. Optionally add `Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)` and `Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)` for Create validation.
- **Project:** Ensure `Tenant.Domain` references `BuildingBlocks.Infrastructure` (for `IRepository`) if not already.

### 1.4 Domain project references

- **File:** `Tenant.Domain/Tenant.Domain.csproj`
- **Action:** Ensure `<ProjectReference Include="..\..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />`. If `ITenantRepository` extends `IRepository`, add `<ProjectReference Include="..\..\..\BuildingBlocks\Infrastructure\BuildingBlocks.Infrastructure.csproj" />`.

**Acceptance:** Domain builds; `Tenant.Create` validates and returns `Result<Tenant>`; `ITenantRepository` compiles.

---

## Phase 2: Infrastructure Layer

**Goal:** Persist Tenant in DB; schema from module; register DbContext, repository, UoW.

### 2.1 DbContext options extension (schema from module)

- **File:** `Tenant.Infrastructure/Data/TenantDbContextOptionsExtension.cs`
- **Content:** Same pattern as Identity: class implementing `IDbContextOptionsExtension`, holds `string Schema`; nested `ExtensionInfo` with `LogFragment`, `GetServiceProviderHashCode`, `ShouldUseSameServiceProvider`, `PopulateDebugInfo`. `ApplyServices`/`Validate` empty.
- **File:** `Tenant.Infrastructure/Data/TenantDbContextOptionsBuilderExtensions.cs`
- **Content:** Extension method `UseTenantSchema(this DbContextOptionsBuilder<TenantDbContext> optionsBuilder, string schema)` that adds/updates `TenantDbContextOptionsExtension`.

### 2.2 TenantDbContext

- **File:** `Tenant.Infrastructure/Data/TenantDbContext.cs`
- **Content:**
  - Constructor `TenantDbContext(DbContextOptions<TenantDbContext> options)`; store options in field for OnModelCreating.
  - `DbSet<Tenant> Tenants => Set<Tenant>();`
  - `OnModelCreating`: read schema from options extension (same pattern as Identity), `HasDefaultSchema(schema)`, `ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly)`.
  - `ConfigureConventions`: `Properties<string>().HaveMaxLength(500)`.
- **Namespace:** `Tenant.Infrastructure.Data`.

### 2.3 Entity configuration

- **File:** `Tenant.Infrastructure/Data/Configurations/TenantConfiguration.cs`
- **Content:** `IEntityTypeConfiguration<Tenant>`: table `tenants`, columns `id`, `name`, `slug`, `created_at`, `updated_at` (snake_case); unique index on `slug`; ignore `DomainEvents`.

### 2.4 Repository implementation

- **File:** `Tenant.Infrastructure/Repositories/TenantRepository.cs`
- **Content:** `class TenantRepository : Repository<Tenant, Guid>, ITenantRepository` (from `BuildingBlocks.Infrastructure.Persistence`), constructor takes `TenantDbContext`. Implement `GetBySlugAsync` and `SlugExistsAsync` if added to interface.

### 2.5 Unit of Work

- **File:** `Tenant.Infrastructure/Data/TenantUnitOfWork.cs`
- **Content:** Implement `IUnitOfWork` (from `BuildingBlocks.Application.UnitOfWork`) by delegating to `TenantDbContext`: `BeginTransactionAsync`, `CommitTransactionAsync` (SaveChangesAsync + commit), `RollbackTransactionAsync`, `SaveChangesAsync`.

### 2.6 Service registration

- **File:** `Tenant.Infrastructure/TenantInfrastructureServiceCollectionExtensions.cs` (project root)
- **Content:** `AddTenantInfrastructure(IServiceCollection services, IConfiguration configuration, string schemaName)`: validate schemaName; get `DefaultConnection`; `AddDbContext<TenantDbContext>` with Npgsql, retry, no migrations history table; cast options to `DbContextOptionsBuilder<TenantDbContext>` and call `UseTenantSchema(schemaName)`; register `IUnitOfWork` → `TenantUnitOfWork`, `ITenantRepository` → `TenantRepository`.
- **Namespace:** `Tenant.Infrastructure`.

### 2.7 Project and packages

- **File:** `Tenant.Infrastructure/Tenant.Infrastructure.csproj`
- **Action:** References: `Tenant.Domain`, `BuildingBlocks.Kernel`, `BuildingBlocks.Infrastructure` (and `BuildingBlocks.Application` if needed for `IUnitOfWork`). Remove reference to `Tenant.Application`. Packages: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Ardalis.Specification`, `Ardalis.Specification.EntityFrameworkCore`, `Microsoft.Extensions.Configuration.Abstractions` (same as Identity).

**Acceptance:** `Tenant.Infrastructure` builds; `AddTenantInfrastructure` registers DbContext (schema from parameter), repository, and UoW.

---

## Phase 3: Application Layer

**Goal:** CreateTenant command, GetTenantById query, DTOs, validators, specifications.

### 3.1 DTOs

- **File:** `Tenant.Application/DTOs/TenantDto.cs`
- **Content:** `record TenantDto(Guid Id, string Name, string Slug, DateTime CreatedAt, DateTime? UpdatedAt)`.

### 3.2 Mapper

- **File:** `Tenant.Application/Mappers/TenantMapper.cs`
- **Content:** Static `ToDto(Tenant tenant)` returning `TenantDto` (Id, Name, Slug, CreatedAt, UpdatedAt).

### 3.3 Create tenant command and handler

- **File:** `Tenant.Application/Commands/CreateTenant/CreateTenantCommand.cs`
- **Content:** Record `CreateTenantCommand(string Name, string Slug)` implementing `IRequest<Result<Guid>>` and `ITransactionalCommand`.
- **File:** `Tenant.Application/Commands/CreateTenant/CreateTenantCommandHandler.cs`
- **Content:** Inject `ITenantRepository`, `IDateTimeProvider`. Handle: validate (e.g. slug uniqueness via `SlugExistsAsync`); `Tenant.Create(name, slug, dateTimeProvider)`; `AddAsync`; return `Result.Success(tenant.Id)`. Transaction handled by existing pipeline.

### 3.4 Create tenant validator

- **File:** `Tenant.Application/Validators/CreateTenantCommandValidator.cs`
- **Content:** FluentValidation `AbstractValidator<CreateTenantCommand>`: Name and Slug not empty; Slug format (e.g. `[a-z0-9-]+`); length limits matching domain.

### 3.5 Get tenant by id query and handler

- **File:** `Tenant.Application/Queries/GetTenantById/GetTenantByIdQuery.cs`
- **Content:** Record `GetTenantByIdQuery(Guid TenantId)` implementing `IRequest<Result<TenantDto>>`.
- **File:** `Tenant.Application/Queries/GetTenantById/GetTenantByIdQueryHandler.cs`
- **Content:** Inject `ITenantRepository`; `GetByIdAsync(request.TenantId)`; if null return `Result.Failure(Error.NotFound(...))`; else return `Result.Success(TenantMapper.ToDto(tenant))`.

### 3.6 Specification (optional)

- **File:** `Tenant.Application/Specifications/TenantSpecifications.cs`
- **Content:** E.g. `TenantByIdSpec : Specification<Tenant>` with `Query.Where(t => t.Id == id)`. Use in GetTenantById if you prefer specification over `GetByIdAsync`.

### 3.7 Application service registration

- **File:** `Tenant.Application/TenantApplicationServiceCollectionExtensions.cs` (project root)
- **Content:** `AddTenantApplication(IServiceCollection services)`: `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTenantCommand).Assembly))`, `AddValidatorsFromAssembly(typeof(CreateTenantCommandValidator).Assembly)`.
- **Namespace:** `Tenant.Application`.

### 3.8 Application project references

- **File:** `Tenant.Application/Tenant.Application.csproj`
- **Action:** References: `Tenant.Domain`, `BuildingBlocks.Kernel`, `BuildingBlocks.Application` (MediatR). Packages: MediatR, FluentValidation, FluentValidation.DependencyInjectionExtensions, Ardalis.Specification (if specs used).

**Acceptance:** Application layer builds; CreateTenant and GetTenantById run via MediatR; validators run for CreateTenant.

---

## Phase 4: API Layer

**Goal:** HTTP endpoints for Create and GetById; controller uses MediatR only; API registers its own controllers.

### 4.1 Request/response models

- **File:** `Tenant.Api/Models/CreateTenantRequest.cs`
- **Content:** Record or class with `Name`, `Slug` (for binding from body).
- **File:** `Tenant.Api/Models/TenantResponse.cs` (or reuse DTO)
- **Content:** Same shape as `TenantDto` (Id, Name, Slug, CreatedAt, UpdatedAt) for response.

### 4.2 Controller

- **File:** `Tenant.Api/Controllers/TenantController.cs`
- **Content:**
  - Replace Create stub: `[HttpPost]` action that accepts `CreateTenantRequest`, sends `new CreateTenantCommand(request.Name, request.Slug)` via `IMediator`, maps `Result<Guid>` to 201 Created (with location) or 400/409.
  - Replace GetById stub: `[HttpGet("{id:guid}")]` sends `GetTenantByIdQuery(id)`, maps `Result<TenantDto>` to 200 OK or 404. Keep ping if desired.
- **Dependencies:** Inject `IMediator` only (no repository in API).

### 4.3 Result → IActionResult mapping

- **Action:** Add a small helper or extension to map `Result<T>` to `IActionResult` (e.g. success → Ok/Created, failure → BadRequest/NotFound/Conflict by error type). Reuse pattern from BuildingBlocks or Identity if present; otherwise implement locally in Api.

### 4.4 API service registration

- **File:** `Tenant.Api/TenantApiServiceCollectionExtensions.cs` (project root)
- **Content:** `AddTenantApi(IServiceCollection services)`: `services.AddControllers().AddApplicationPart(typeof(TenantController).Assembly)`; return services.
- **Namespace:** `Tenant.Api`.

### 4.5 API project references

- **File:** `Tenant.Api/Tenant.Api.csproj`
- **Action:** References: `Tenant.Application` (for MediatR and DTOs if exposed), `Tenant.Contracts` if used. Framework: `Microsoft.AspNetCore.App`.

**Acceptance:** `POST /api/tenant` creates a tenant and returns 201 with location; `GET /api/tenant/{id}` returns tenant or 404.

---

## Phase 5: Migrations (FluentMigrator)

**Goal:** Schema `tenant` and table `tenants` with id, name, slug, created_at, updated_at; unique slug. Migrations run outside the app.

### 5.1 Create tenant schema

- **File:** `Tenant.Migrations/Migrations/Schema/20250116000000_CreateTenantSchema.cs`
- **Content:** Migration with `Up`: `Create.Schema("tenant")`; `Down`: `Delete.Schema("tenant")`. Namespace consistent with existing (e.g. `Tenant.Migrations.Migrations.Schema` or match Identity style).

### 5.2 Create tenants table

- **File:** `Tenant.Migrations/Migrations/Schema/20250116001000_CreateTenantsTable.cs`
- **Content:** Migration: `Create.Table("tenants").InSchema("tenant")` with columns: `id` (Guid, PK), `name` (string, not null), `slug` (string, not null), `created_at` (DateTime, not null), `updated_at` (DateTime, nullable). Unique constraint on `slug` within schema.

### 5.3 Migrations project

- **File:** `Tenant.Migrations/Tenant.Migrations.csproj`
- **Action:** Reference `BuildingBlocks.Migrations` and `FluentMigrator.Runner.Postgres`; no reference to Tenant.Infrastructure or Tenant.Domain (migrations are DB-only). Ensure MigrationRunner knows Tenant.Migrations assembly and schema name (from existing discovery / module list).

**Acceptance:** Running the migration runner creates schema `tenant` and table `tenants`; app can read/write via EF.

---

## Phase 6: Module Composition

**Goal:** TenantModule only calls layer extensions; no direct registration of MediatR/validators/controllers.

### 6.1 TenantModule

- **File:** `Tenant.Module/TenantModule.cs`
- **Content:** In `RegisterServices`: call `services.AddTenantApplication();`, `services.AddTenantInfrastructure(configuration, SchemaName);`, `services.AddTenantApi();`. Remove any direct `AddControllers().AddApplicationPart` and TODO. Usings: `Tenant.Api`, `Tenant.Application`, `Tenant.Infrastructure`, `BuildingBlocks.Web.Modules`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.AspNetCore.Builder`.

### 6.2 Module project references

- **File:** `Tenant.Module/Tenant.Module.csproj`
- **Action:** References: `Tenant.Api`, `Tenant.Application`, `Tenant.Infrastructure`, `BuildingBlocks.Web`, `BuildingBlocks.Kernel` (and others already present). Fix path to Tenant.Contracts if wrong (e.g. `..\Tenant.Contracts`).

**Acceptance:** Host that loads TenantModule can resolve `ITenantRepository`, `TenantDbContext`, MediatR handlers; Create and GetById work end-to-end.

---

## Implementation Order Summary (for AI agent)

1. **Domain:** Tenant entity + `Create`, optional event, `ITenantRepository` (+ optional GetBySlug/SlugExists). Fix Domain csproj references.
2. **Infrastructure:** Options extension + `UseTenantSchema`, `TenantDbContext`, `TenantConfiguration`, `TenantRepository`, `TenantUnitOfWork`, `TenantInfrastructureServiceCollectionExtensions`; fix csproj (EF, Npgsql, schema from module).
3. **Application:** `TenantDto`, `TenantMapper`, `CreateTenantCommand` + handler + validator, `GetTenantByIdQuery` + handler, optional specs, `TenantApplicationServiceCollectionExtensions`; fix csproj (MediatR, FluentValidation).
4. **API:** Create/GetById request/response types, `TenantController` using MediatR only, result mapping, `TenantApiServiceCollectionExtensions`; fix Api csproj.
5. **Migrations:** Schema migration, tenants table migration; ensure MigrationRunner includes Tenant.
6. **Module:** `TenantModule` only calls `AddTenantApplication`, `AddTenantInfrastructure(configuration, SchemaName)`, `AddTenantApi`.

---

## Conventions to Follow

- **Schema:** Always use schema name from `TenantModule.SchemaName` (`"tenant"`); pass it into `AddTenantInfrastructure`; do not hardcode in DbContext.
- **Naming:** Snake_case columns and indexes; table name `tenants`.
- **Errors:** Use `Error.Validation`, `Error.NotFound`, `Error.Conflict` from Kernel.Results.
- **Slug:** Normalize (e.g. lowercase) in domain or application and enforce uniqueness in repository + command handler.
- **No EF Migrations in app:** Do not add `MigrationsHistoryTable`; FluentMigrator is the only migration runner.
