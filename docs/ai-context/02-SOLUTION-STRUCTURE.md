# Datarizen AI Context - Solution Structure

## Repository Layout

```
/
  README.md                              # Project overview, quick start
  LICENSE                                # License file
  .gitignore                            # Git ignore rules
  docker-compose.monolith.yml           # Multi-tenant monolith deployment
  docker-compose.multiapp.yml           # Multi-tenant multi-app deployment
  docker-compose.microservices.yml      # Multi-tenant microservices deployment
  docker-compose.dev.yml                # Local development environment
  
  /server                               # Backend .NET solution
  /client                               # Frontend (mirrors server/ as single root)
    /apps                               # Deployable applications (3 apps, extensible)
      /builder                          # App Builder (visual editor)
      /dashboard                        # SaaS multi-tenant dashboard
      /runtime                          # End-user app renderer
      # Optional: /admin, /analytics, etc. — same MF-ready structure
    /packages                           # Shared frontend packages
      /contracts                        # Shared contracts (Builder ↔ Runtime)
      /design                           # Shared design system (tokens, components)
  /docs                                 # Documentation
  /infrastructure                       # Infrastructure as Code
  /scripts                              # Automation scripts
```

## Server Structure (`/server`)

### Solution File
```
/server
  Datarizen.sln                         # Main solution file
```

### Source Code (`/server/src`)

#### Orchestration & Infrastructure
```
/server/src
  /AppHost
    Datarizen.AppHost.csproj            # .NET Aspire orchestration
    Program.cs                          # Topology-aware service composition
    appsettings.json                    # Aspire configuration
  
  /ServiceDefaults
    Datarizen.ServiceDefaults.csproj    # Shared hosting defaults
    Extensions.cs                       # Logging, health checks, telemetry
  
  /ApiGateway
    Datarizen.ApiGateway.csproj         # YARP reverse proxy
    Program.cs                          # Gateway startup
    appsettings.json                    # Route configuration
    /Configuration
      routes.json                       # Topology-aware routing
```

#### Hosts (Deployment Containers)
```
/server/src/Hosts
  /MonolithHost
    Datarizen.MonolithHost.csproj       # Single host for all modules
    Program.cs                          # Loads all modules
    appsettings.json                    # Monolith configuration
  
  /Host1
    Datarizen.Host1.csproj              # Multi-app host 1
    Program.cs                          # Loads configured modules
    appsettings.json                    # Module assignment
  
  /Host2
    Datarizen.Host2.csproj              # Multi-app host 2
    Program.cs
    appsettings.json
  
  /Host3
    Datarizen.Host3.csproj              # Multi-app host 3
    Program.cs
    appsettings.json
```

**Host Responsibilities**:
- Load modules based on configuration
- Register module services in DI container
- Configure inter-module communication (MediatR, HTTP, gRPC)
- Apply middleware pipeline
- Host Web API endpoints

**Configuration Example** (`appsettings.json`):
```json
{
  "Deployment": {
    "Topology": "DistributedApp",
    "LoadedModules": ["Module1", "Module2"]
  }
}
```

#### Platform Modules (`/server/src/Modules`)

Each module follows Clean Architecture with vertical slice organization:

```
/server/src/Modules/{ModuleName}
  /{ModuleName}.Domain
    Datarizen.{ModuleName}.Domain.csproj
    /Entities                           # Aggregate roots, entities
    /ValueObjects                       # Value objects
    /Events                             # Domain events
    /Interfaces                         # Repository interfaces
    /Exceptions                         # Domain exceptions
  
  /{ModuleName}.Application
    Datarizen.{ModuleName}.Application.csproj
    /Commands                           # CQRS commands
      /{CommandName}
        {CommandName}Command.cs
        {CommandName}CommandHandler.cs
        {CommandName}CommandValidator.cs
    /Queries                            # CQRS queries
      /{QueryName}
        {QueryName}Query.cs
        {QueryName}QueryHandler.cs
    /DTOs                               # Data transfer objects
    /Mappings                           # AutoMapper profiles
    /Behaviors                          # MediatR pipeline behaviors
  
  /{ModuleName}.Infrastructure
    Datarizen.{ModuleName}.Infrastructure.csproj
    /Persistence
      {ModuleName}DbContext.cs          # EF Core DbContext
      /Configurations                   # Entity configurations
      /Repositories                     # Repository implementations
    /ExternalServices                   # Third-party integrations
    /BackgroundJobs                     # Hangfire/Quartz jobs
  
  /{ModuleName}.Migrations
    Datarizen.{ModuleName}.Migrations.csproj
    /Migrations                         # EF Core migrations
    README.md                           # Migration instructions
  
  /{ModuleName}.Contracts
    Datarizen.{ModuleName}.Contracts.csproj
    /Events                             # Integration events
    /DTOs                               # Public DTOs
    /Interfaces                         # Public service interfaces
```

**Module Layer Rules**:
- **Domain**: No dependencies on other layers or modules
- **Application**: Depends on Domain only
- **Infrastructure**: Depends on Domain and Application
- **Migrations**: Depends on Infrastructure (for DbContext)
- **Contracts**: No dependencies (pure interfaces/DTOs)

**Optional Layers**:
- Simple modules can skip **Migrations** if no database
- Simple modules can skip **Contracts** if no inter-module communication
- Very simple modules can combine **Application** and **Infrastructure**

#### Building Blocks (`/server/src/BuildingBlocks`)

Shared infrastructure components used across modules:

```
/server/src/BuildingBlocks
  /Kernel
    Datarizen.BuildingBlocks.Kernel.csproj
    /Domain
      Entity.cs                         # Base entity class
      ValueObject.cs                    # Base value object
      DomainEvent.cs                    # Base domain event
      AggregateRoot.cs                  # Aggregate root base
    /Application
      Result.cs                         # Result<T> pattern
      Error.cs                          # Error handling
      PagedList.cs                      # Pagination
    /Exceptions
      DomainException.cs
      ValidationException.cs
  
  /Infrastructure
    Datarizen.BuildingBlocks.Infrastructure.csproj
    /EventBus
      IEventBus.cs                      # Event bus interface
      InMemoryEventBus.cs               # In-process events
      RabbitMqEventBus.cs               # Distributed events
    /Caching
      ICacheService.cs
      RedisCacheService.cs
    /BackgroundJobs
      IBackgroundJobScheduler.cs
      HangfireJobScheduler.cs
    /UnitOfWork
      IUnitOfWork.cs
      UnitOfWork.cs
    /Outbox
      OutboxMessage.cs                  # Transactional outbox pattern
      OutboxProcessor.cs
  
  /Contracts
    Datarizen.BuildingBlocks.Contracts.csproj
    /Messaging
      IIntegrationEvent.cs              # Integration event marker
      ICommand.cs                       # Command marker
      IQuery.cs                         # Query marker
    /Pagination
      PagedRequest.cs
      PagedResponse.cs
```

#### Capabilities (`/server/src/Capabilities`)

Cross-cutting concerns and non-business-logic features:

```
/server/src/Capabilities
  /MultiTenancy
    Datarizen.Capabilities.MultiTenancy.csproj
    /Middleware
      TenantResolutionMiddleware.cs     # Resolve tenant from request
    /Services
      ITenantService.cs
      TenantService.cs
    /Context
      ITenantContext.cs                 # Current tenant accessor
      TenantContext.cs
  
  /Authentication
    Datarizen.Capabilities.Authentication.csproj
    /Jwt
      JwtTokenGenerator.cs
      JwtTokenValidator.cs
    /Services
      IAuthenticationService.cs
  
  /Authorization
    Datarizen.Capabilities.Authorization.csproj
    /Policies
      TenantPolicyHandler.cs
      PermissionPolicyHandler.cs
    /Requirements
      TenantRequirement.cs
  
  /Auditing
    Datarizen.Capabilities.Auditing.csproj
    /Interceptors
      AuditInterceptor.cs               # EF Core interceptor
    /Services
      IAuditService.cs
  
  /FileStorage
    Datarizen.Capabilities.FileStorage.csproj
    /Abstractions
      IFileStorageService.cs
    /Providers
      LocalFileStorageService.cs
      AzureBlobStorageService.cs
      S3StorageService.cs
  
  /Notifications
    Datarizen.Capabilities.Notifications.csproj
    /Email
      IEmailService.cs
      SmtpEmailService.cs
    /Push
      IPushNotificationService.cs
    /InApp
      IInAppNotificationService.cs
  
  /Chat
    Datarizen.Capabilities.Chat.csproj
    /SignalR
      ChatHub.cs
    /Services
      IChatService.cs
  
  /FeatureFlags
    Datarizen.Capabilities.FeatureFlags.csproj
    /Services
      IFeatureFlagService.cs
```

**Capability Characteristics**:
- Reusable across modules
- No business logic
- Infrastructure/technical concerns
- Can be enabled/disabled per deployment

### Tests (`/server/tests`)

```
/server/tests
  /Unit
    /{ModuleName}.Domain.Tests
      Datarizen.{ModuleName}.Domain.Tests.csproj
      /Entities
      /ValueObjects
    
    /{ModuleName}.Application.Tests
      Datarizen.{ModuleName}.Application.Tests.csproj
      /Commands
      /Queries
      /Validators
  
  /Integration
    /{ModuleName}.Integration.Tests
      Datarizen.{ModuleName}.Integration.Tests.csproj
      /Api                              # API endpoint tests
      /Database                         # Database integration tests
      /ExternalServices                 # Third-party integration tests
  
  /E2E
    Datarizen.E2E.Tests.csproj
    /Scenarios                          # End-to-end user scenarios
    /Fixtures                           # Test data setup
  
  /Performance
    Datarizen.Performance.Tests.csproj
    /LoadTests                          # Load testing scenarios
    /BenchmarkTests                     # Benchmark.NET tests
```

**Testing Strategy**:
- **Unit Tests**: Fast, isolated, no dependencies
- **Integration Tests**: Database, message bus, external services
- **E2E Tests**: Full application flow, multiple modules
- **Performance Tests**: Load testing, benchmarking

## Client Structure (`/client`)

All frontend code lives under a single **client** root (mirroring **server**). This follows the same best practice as the backend: one root per domain.

- **`/client/apps/`** — Deployable applications (Builder, Dashboard, Runtime; extensible to more).
- **`/client/packages/`** — Shared packages (contracts, design) used by the apps.

Datarizen uses **three deployable frontend applications** (with room for a **fourth** if needed) plus **shared packages**. Each app is a **modular monolith** and is **micro-frontend ready**. See [Client Migration to Micro-Frontends](../implementations/client/client-migration-to-micro-frontends.md).

### Three-Application Model (Extensible to Four or More)

| App | Purpose | Users | Deployment |
|-----|---------|-------|------------|
| **Builder** | Visual editor, schema, workflows, AI assistant | Builders / internal | `builder.*` or path |
| **Dashboard** | Billing, orgs, tenant settings, RBAC | Admins / tenants | `dashboard.*` or path |
| **Runtime** | End-user app renderer (generated apps) | End users | `app.*` / tenant apps |

- **Builder** and **Runtime** share contracts (component schema, layout JSON, actions, validation) via `client/client/packages/contracts`.
- **Dashboard** mirrors backend modules (Tenant, Identity, User, Feature, AppBuilder, TenantApplication) as internal modules.
- Each app is built and deployed independently.
- **Micro-frontend ready:** All three apps are designed so they can later be split into a shell + remotes (or stay as a single deploy). A **fourth** application can be added under `/client/apps/{appName}` following the same modular, MF-ready structure.

### Shared Contracts Package (`/client/client/packages/contracts`)

**Critical:** Builder and Runtime must share a single contract package so that app definitions and runtime rendering stay in sync.

```
/client/client/packages/contracts
  package.json
  tsconfig.json
  /src
    /schema                    # Component schema, layout JSON
      component-schema.ts
      layout-schema.ts
      page-schema.ts
    /actions                   # Action definitions
      action-types.ts
    /validation                # Validation rules
      validation-rules.ts
    index.ts                   # Public API
```

- Versioned independently; Builder and Runtime depend on same major version.
- Used by both Builder (editing) and Runtime (rendering).
- See [Client Migration to Micro-Frontends](../implementations/client/client-migration-to-micro-frontends.md) for contract versioning and compatibility.

### Dashboard Application (`/client/apps/dashboard`)

SaaS multi-tenant dashboard: billing, organizations, settings, tenant management. Modular monolith mirroring backend modules; **micro-frontend ready** so it can be split into shell + remotes when needed (e.g. many teams, independent module deployments).

```
/client/apps/dashboard
  package.json
  vite.config.ts
  tsconfig.json
  .env.development
  .env.production
  
  /public
    /assets
      /images
      /fonts
  
  /src
    main.ts
    App.svelte
    
    /modules                            # Backend module APIs + components (MF-ready)
      /tenant
        index.ts                        # Module entry point (export all)
        /api
        /components
        /stores
        /routes
      /identity
        index.ts
        ...
      /user
      /feature
      /appBuilder
      /tenantApplication
        ...
    
    /features                           # User-facing pages
      /billing
      /org-settings
      /apps
        ...
    
    /shell                              # Optional; for MF: module loader, routing, layout
      /module-loader
      /routing
      /layout
    
    /shared
      /components
        /ui
        /layout
      /utils
        apiClient.ts
        auth.ts
      /stores
        authStore.ts
        tenantStore.ts
      /types
      /constants
    
    /styles
      global.css
      variables.css
      themes.css
```

**Organization:** Modules mirror backend; each module has an `index.ts` entry point and is self-contained (no cross-module imports). Features are user-centric pages. **MF-ready:** when scaling requires it, Dashboard can become a shell loading remote modules (e.g. tenant-client, identity-client) via Module Federation—see [Client Migration to Micro-Frontends](../implementations/client/client-migration-to-micro-frontends.md).

### Builder Application (`/client/apps/builder`)

Visual editor: canvas, component palette, data model, workflow editor, AI assistant. Modular monolith; **micro-frontend ready** so it can be split into shell + remotes when needed.

```
/client/apps/builder
  package.json
  vite.config.ts
  tsconfig.json
  .env.development
  .env.production
  
  /public
    /assets
  
  /src
    main.ts
    App.svelte
    
    /features                           # Domain features (vertical slices, MF-ready)
      /canvas
        index.ts                        # Feature entry point
        /components
        /stores
        /types
      /component-palette
        index.ts
        ...
      /data-model
      /workflow-editor
      /ai-assistant
      /marketplace                     # Optional; plugin ecosystem
        ...
    
    /shell                              # Optional; for MF: module loader, routing, layout
      /module-loader
      /routing
      /layout
    
    /lib                                # Builder-specific libs
    /shared
      /components
      /utils
      /stores
      /types
    /styles
```

**MF-ready:** Each feature has an `index.ts` entry point and is self-contained. When you have 3+ teams or a plugin/marketplace, extract features (e.g. workflow-editor, ai-assistant, marketplace) as remotes loaded by the Builder shell. See [Client Migration to Micro-Frontends](../implementations/client/client-migration-to-micro-frontends.md).

### Runtime Application (`/client/apps/runtime`)

End-user app renderer: loads application structure and tenant configuration from the backend, then renders UI. Performance-sensitive; **micro-frontend ready** so it can be split into shell + remotes if needed (e.g. plugin renderers, multiple runtime modes).

**Backend dependencies:** The runtime client does not own application definitions. The client calls **only the Runtime BFF** (single backend surface). The Runtime BFF performs authentication and delegates as follows (Pattern 1): it calls the **TenantApplication API** for app resolution and (with AppBuilder) for snapshot; it calls the **Runtime API** (AppRuntime) for compatibility and for all other runtime-related executions (datasource execution, engines).
- **TenantApplication API:** Resolve tenant + app + environment (e.g. by URL) → `ApplicationReleaseId` + tenant **configuration** (merged). Application resolution is always performed by TenantApplication; AppBuilder never resolves. Snapshot for tenant custom/forked releases.
- **AppBuilder:** Load application **structure** (snapshot) for platform releases (for the `ApplicationReleaseId` from resolve).
- **Runtime API (AppRuntime):** Compatibility check and runtime execution—verify the current runtime can execute the release; datasource execution; navigation/page/datasource engines.

**Contract-first:** The runtime consumes the same application-definition shape as the Builder. Use `client/client/packages/contracts` for schema, types, and validation; keep the client a thin renderer over contract-shaped data. **Compatibility and versioning** for all feature types (datasource, workflow, validation rules, and future engines) follow the [Compatibility and Versioning Framework](../implementations/client/compatibility-and-versioning-framework.md): one pattern (definition schema version + engine support matrix + client adapters) so the client UI and Runtime API stay aligned across versions. See [Runtime Client Implementation Plan](../implementations/client/runtime-client-impl-plan.md) and [Runtime Server Implementation Plan](../implementations/client/runtime-server-impl-plan.md) for client and backend implementation plans.

**Flow:** Runtime client → **Runtime BFF** (auth) → TenantApplication API (resolve, tenant snapshot) / AppBuilder (platform snapshot) / Runtime API (AppRuntime) (compatibility, datasource execution, engines). Client calls BFF for all runtime needs; BFF delegates to TenantApplication API and Runtime API as above. Multi-tenant versioning is handled by the backend: each tenant environment pins an `ApplicationReleaseId`, so different tenants can run different app versions; the runtime supports a range of release versions via the compatibility framework (single compatibility check, schema-version adapters per feature type).

```
/client/apps/runtime
  package.json
  vite.config.ts
  tsconfig.json
  .env.development
  .env.production
  
  /src
    main.ts
    App.svelte
    
    /loaders                             # API clients (MF-ready boundary)
      index.ts
      resolveLoader.ts                   # Runtime BFF: resolve by URL
      structureLoader.ts                 # Runtime BFF: get snapshot
      compatibilityLoader.ts             # Runtime BFF: compatibility check
    /renderer                            # Dynamic renderer (MF-ready boundary)
      index.ts
      /components
      ...
    /shared
      /components                        # Runtime component set
      /utils
    /shell                               # Optional; for MF: load remote renderers/plugins
      /module-loader
      /routing
    /styles
```

- Depends on `client/client/packages/contracts` for schema and validation; aligns with backend DTOs (ApplicationSnapshotDto, ResolvedApplicationDto).
- **MF-ready:** Clear boundaries (loaders, renderer); when needed, Runtime can become a shell loading remote renderers or plugin bundles via Module Federation.

### Micro-Frontend Ready Design (All Applications)

All three applications—and any future fourth—follow the same design principles so that **any** of them can move to micro-frontends without a rewrite:

- **Entry points:** Each module or feature has an `index.ts` that exports its public API (components, routes, stores). No cross-module or cross-feature imports; shared code lives in `/shared` or `/lib`.
- **Optional shell:** Each app can include a `/src/shell` (module-loader, routing, layout) used only when running as host for remotes; when running as a single app, shell code is unused or used for local “virtual” loading.
- **Shared dependencies:** Clearly list and pin shared deps (Svelte, contracts, etc.) so that when an app becomes a host or remote, federation config stays consistent.
- **Same migration pattern:** Builder (features as remotes), Dashboard (backend modules as remotes), and Runtime (e.g. renderer/loaders/plugins as remotes) all use the same migration steps—see [Client Migration to Micro-Frontends](../implementations/client/client-migration-to-micro-frontends.md).

**When to adopt MF in a given app:** When that app has 3+ teams, independent release cycles per area, plugin/marketplace needs, or bundle size problems. You can adopt MF in one, two, or all three apps independently. A **fourth** application (e.g. admin, analytics, white-label portal) can be added under `/client/apps/{appName}` with the same modular, MF-ready structure and migrated to MF when needed.

### Mobile Application (`/client/apps/mobile`) - Future

```
/client/apps/mobile
  # React Native, Flutter, or .NET MAUI
  # Structure TBD when implemented
```

## Documentation (`/docs`)

```
/docs
  /ai-context                           # AI assistant context
    00-OVERVIEW.md
    01-DEPLOYMENT-STRATEGY.md
    02-SOLUTION-STRUCTURE.md
    03-MODULE-TEMPLATE.md
    04-DATABASE-STRATEGY.md
    05-TESTING-STRATEGY.md
    ...
  
  /architecture                         # Architecture decisions
    /adr                                # Architecture Decision Records
      001-modular-monolith.md
      002-cqrs-pattern.md
      003-multi-tenancy.md
    /diagrams                           # Architecture diagrams
      system-context.puml
      deployment-topologies.puml
  
  /api                                  # API documentation
    /openapi
      swagger.json
    /postman
      collections/
  
  /deployment                           # Deployment guides
    /kubernetes
      production-deployment.md
      scaling-guide.md
    /docker
      docker-compose-guide.md
    /on-premise
      installation-guide.md
```

## Infrastructure (`/infrastructure`)

Infrastructure as Code for different deployment targets:

```
/infrastructure
  /kubernetes
    /monolith
      deployment.yaml
      service.yaml
      ingress.yaml
      configmap.yaml
      secrets.yaml
    
    /multiapp
      /host1
        deployment.yaml
        service.yaml
      /host2
        deployment.yaml
        service.yaml
      /host3
        deployment.yaml
        service.yaml
      /gateway
        deployment.yaml
        service.yaml
        ingress.yaml
    
    /microservices
      /{moduleName}
        deployment.yaml
        service.yaml
      /gateway
        deployment.yaml
        ingress.yaml
    
    /shared
      namespace.yaml
      postgres.yaml
      redis.yaml
      rabbitmq.yaml
  
  /terraform
    /azure
      main.tf
      variables.tf
      outputs.tf
      /modules
        /aks
        /database
        /storage
    
    /aws
      main.tf
      variables.tf
      /modules
        /eks
        /rds
        /s3
  
  /helm
    /datarizen
      Chart.yaml
      values.yaml
      /templates
        deployment.yaml
        service.yaml
        ingress.yaml
```

## Scripts (`/scripts`)

Automation scripts for development, deployment, and operations:

```
/scripts
  /setup
    setup-dev-environment.sh            # Setup local dev environment
    install-dependencies.sh             # Install all dependencies
    create-databases.sh                 # Create databases
  
  /deployment
    deploy-monolith.sh                  # Deploy monolith topology
    deploy-multiapp.sh                  # Deploy multi-app topology
    deploy-microservices.sh             # Deploy microservices topology
    rollback.sh                         # Rollback deployment
  
  /database
    run-migrations.sh                   # Run all module migrations
    seed-data.sh                        # Seed test data
    backup-database.sh                  # Backup databases
    restore-database.sh                 # Restore databases
  
  /development
    start-aspire.sh                     # Start Aspire orchestration
    start-docker-compose.sh             # Start Docker Compose
    generate-module.sh                  # Generate new module scaffold
    generate-migration.sh               # Generate EF Core migration
  
  /ci-cd
    build.sh                            # Build all projects
    test.sh                             # Run all tests
    publish.sh                          # Publish artifacts
```

## Naming Conventions

### Projects
- **Format**: `Datarizen.{Category}.{Name}.csproj`
- **Examples**:
  - `Datarizen.AppHost.csproj`
  - `Datarizen.TenantManagement.Domain.csproj`
  - `Datarizen.BuildingBlocks.Kernel.csproj`
  - `Datarizen.Capabilities.MultiTenancy.csproj`

### Namespaces
- **Format**: `Datarizen.{Category}.{Name}`
- **Examples**:
  - `Datarizen.TenantManagement.Domain.Entities`
  - `Datarizen.BuildingBlocks.Infrastructure.EventBus`
  - `Datarizen.Capabilities.Authentication.Jwt`

### Files
- **Classes**: `{ClassName}.cs` (PascalCase)
- **Interfaces**: `I{InterfaceName}.cs` (PascalCase with I prefix)
- **Tests**: `{ClassName}Tests.cs`

### Folders
- **PascalCase** for all folders
- Plural for collections: `Entities`, `Commands`, `Queries`
- Singular for single concept: `Domain`, `Application`

## Dependency Rules

### Module Dependencies
```
┌─────────────────────────────────────────┐
│  Contracts (No dependencies)            │
└─────────────────────────────────────────┘
                    ↑
┌─────────────────────────────────────────┐
│  Domain (No dependencies)                │
└─────────────────────────────────────────┘
                    ↑
┌─────────────────────────────────────────┐
│  Application (→ Domain)                  │
└─────────────────────────────────────────┘
                    ↑
┌─────────────────────────────────────────┐
│  Infrastructure (→ Domain, Application)  │
└─────────────────────────────────────────┘
                    ↑
┌─────────────────────────────────────────┐
│  Migrations (→ Infrastructure)           │
└─────────────────────────────────────────┘
```

### Cross-Module Communication
- ✅ **Allowed**: Module A → Module B.Contracts
- ❌ **Forbidden**: Module A → Module B.Domain/Application/Infrastructure
- ✅ **Allowed**: All modules → BuildingBlocks
- ✅ **Allowed**: All modules → Capabilities

### Host Dependencies
- Hosts reference all modules they load
- Hosts reference BuildingBlocks and Capabilities
- Hosts configure DI container and middleware

## Configuration Files

### Root Level
- `docker-compose.*.yml` - Docker Compose configurations per topology
- `.gitignore` - Git ignore rules
- `README.md` - Project overview
- `LICENSE` - License file

### Server Level (`/server`)
- `Datarizen.sln` - Solution file
- `Directory.Build.props` - Shared MSBuild properties
- `Directory.Packages.props` - Central package management
- `.editorconfig` - Code style rules

### Client Level (`/clients`)
- **Dashboard** (`/client/apps/dashboard`): `package.json`, `vite.config.ts`, `tsconfig.json`, `.env.*`
- **Builder** (`/client/apps/builder`): `package.json`, `vite.config.ts`, `tsconfig.json`, `.env.*`
- **Runtime** (`/client/apps/runtime`): `package.json`, `vite.config.ts`, `tsconfig.json`, `.env.*`
- **Shared contracts** (`/client/client/packages/contracts`): `package.json`, `tsconfig.json`
- **Shared design** (`/client/packages/design`): `package.json`, tokens, styles

## Build Output

```
/server
  /bin                                  # Build output (gitignored)
  /obj                                  # Intermediate files (gitignored)
  /artifacts                            # Published artifacts
    /monolith
    /multiapp
    /microservices

/client/apps/dashboard
  /node_modules
  /dist

/client/apps/builder
  /node_modules
  /dist

/client/apps/runtime
  /node_modules
  /dist

/client/packages/contracts
  /node_modules
  /dist
```