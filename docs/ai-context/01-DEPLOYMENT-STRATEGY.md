# Datarizen AI Context - Deployment Strategy

## Deployment Topologies

Datarizen supports 5 deployment topologies to serve different use cases:

### 1. Multi-Tenant Monolith
**Use Case**: Local development, testing, demos

**Architecture**:
```
┌─────────────────────────────────────────┐
│  Single Host (All Modules)              │
│  ┌───────────────────────────────────┐  │
│  │ Module1, Module2, Module3,        │  │
│  │ Module4, Module5, Module6, ...    │  │
│  └───────────────────────────────────┘  │
│                                         │
│  Communication: In-Process (MediatR)    │
└─────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────┐
│  Shared Database                        │
│  - module1_schema                       │
│  - module2_schema                       │
│  - module3_schema                       │
│  + Per-Tenant App DBs (tenant_app_*)    │
└─────────────────────────────────────────┘
```

**Characteristics**:
- All modules in single process
- In-process communication (MediatR, domain events)
- Shared database with schema separation
- Simplest deployment
- Easiest debugging

### 2. Multi-Tenant Multi-App
**Use Case**: SaaS production deployment

**Architecture**:
```
┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────────┐
│  Host1               │  │  Host2               │  │  Host3               │
│  ┌────────────────┐  │  │  ┌────────────────┐  │  │  ┌────────────────┐  │
│  │ Module1        │  │  │  │ Module3        │  │  │  │ Module5        │  │
│  │ Module2        │  │  │  │ Module4        │  │  │  │ Module6        │  │
│  └────────────────┘  │  │  └────────────────┘  │  │  └────────────────┘  │
└──────────────────────┘  └──────────────────────┘  └──────────────────────┘
         │                         │                         │
         └─────────────────────────┴─────────────────────────┘
                                   ↓
         ┌─────────────────────────────────────────────────────┐
         │  API Gateway (YARP/Ocelot)                          │
         └─────────────────────────────────────────────────────┘
                                   ↓
         ┌─────────────────────────────────────────────────────┐
         │  Shared Database                                    │
         │  - module1_schema, module2_schema, ...              │
         │  + Per-Tenant App DBs (tenant_app_*)                │
         └─────────────────────────────────────────────────────┘
```

**Characteristics**:
- Modules grouped by concern into separate hosts
- In-process within host, HTTP/gRPC between hosts
- Pub/Sub for async events
- API Gateway for routing
- Horizontal scaling per host

### 3. Multi-Tenant Microservices
**Use Case**: Large-scale SaaS, independent scaling

**Architecture**:
```
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ Module1  │ │ Module2  │ │ Module3  │ │ Module4  │ │ Module5  │ ...
│ Service  │ │ Service  │ │ Service  │ │ Service  │ │ Service  │
└──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘
     │            │            │            │            │
     └────────────┴────────────┴────────────┴────────────┘
                              ↓
     ┌─────────────────────────────────────────────────────┐
     │  API Gateway + Service Discovery                    │
     └─────────────────────────────────────────────────────┘
                              ↓
     ┌─────────────────────────────────────────────────────┐
     │  Shared Database                                    │
     │  - module1_schema, module2_schema, ...              │
     │  + Per-Tenant App DBs (tenant_app_*)                │
     └─────────────────────────────────────────────────────┘
```

**Characteristics**:
- Each module = independent service
- HTTP/gRPC for all inter-module calls
- Pub/Sub for async events
- Independent deployment and scaling
- Service mesh optional (Istio, Linkerd)

### 4. Single-Tenant Monolith
**Use Case**: On-premise customer deployment, air-gapped environments

**Architecture**:
```
┌─────────────────────────────────────────┐
│  Customer Infrastructure                │
│  ┌───────────────────────────────────┐  │
│  │ Single Host (All Modules)         │  │
│  │ ┌───────────────────────────────┐ │  │
│  │ │ Module1, Module2, Module3,    │ │  │
│  │ │ Module4, Module5, Module6     │ │  │
│  │ └───────────────────────────────┘ │  │
│  └───────────────────────────────────┘  │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ Customer Database                 │  │
│  │ - Single tenant context           │  │
│  │ - App operational DBs             │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Characteristics**:
- All modules in single process
- Tenant-specific modules may be disabled
- Customer manages infrastructure
- No SaaS features (billing, limits)
- Customer controls updates

### 5. Single-Tenant Multi-App
**Use Case**: Enterprise on-premise with separation of concerns

**Architecture**:
```
┌─────────────────────────────────────────────────────────┐
│  Customer Infrastructure                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │  Host1       │  │  Host2       │  │  Host3       │   │
│  │  ┌────────┐  │  │  ┌────────┐  │  │  ┌────────┐  │   │
│  │  │Module1 │  │  │  │Module3 │  │  │  │Module5 │  │   │
│  │  │Module2 │  │  │  │Module4 │  │  │  │Module6 │  │   │
│  │  └────────┘  │  │  └────────┘  │  │  └────────┘  │   │
│  └──────────────┘  └──────────────┘  └──────────────┘   │
│                                                         │
│  ┌────────────────────────────────────────────────────┐ │
│  │ Customer Database                                  │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Characteristics**:
- Modules grouped into separate hosts
- HTTP/gRPC between hosts
- Customer manages all components
- Separation for security/compliance

## Module Communication Patterns

### In-Process Communication (Monolith, within Multi-App hosts)

**MediatR Commands/Queries**:
```csharp
// Send command within same process
await mediator.Send(new CreateEntityCommand { ... });
```

**Domain Events**:
```csharp
// Publish event within same process
await publisher.Publish(new EntityCreatedEvent { ... });
```

### HTTP/gRPC Communication (Multi-App, Microservices)

**HTTP Client**:
```csharp
// Call another host/service
var response = await httpClient.PostAsync(
    "http://host2/api/resource", 
    content);
```

**gRPC**:
```csharp
// Call gRPC service
var response = await grpcClient.CreateResourceAsync(request);
```

### Pub/Sub Events (All topologies)

**Message Bus** (RabbitMQ, Azure Service Bus, Redis Streams):
```csharp
// Publish event to message bus
await messageBus.PublishAsync(new EntityCreatedEvent { ... });

// Subscribe to events from other modules
messageBus.Subscribe<EntityCreatedEvent>(async evt => { ... });
```

**Use Cases**:
- Cross-module notifications
- Eventual consistency
- Async workflows
- Audit logging

## Database Strategy

### Shared Database with Schema Separation

**Platform Schemas** (shared across all tenants):
```
datarizen_db
├── module1_schema
│   └── tables...
├── module2_schema
│   └── tables...
├── module3_schema
│   └── tables...
└── moduleN_schema
    └── tables...
```

**Tenant Application Databases** (per tenant app):
```
tenant_{tenantId}_app_{appId}_db
├── entity_data_table_1
├── entity_data_table_2
└── entity_data_table_N
```

**Rationale**:
- Platform metadata shared (tenants, users, app definitions)
- Tenant operational data isolated (security, compliance)
- Schema-level isolation for platform modules
- Database-level isolation for tenant apps

### Database Access Patterns

**Platform Modules**:
- Read/write to their own schema
- Read-only to tenant schema (for tenant context)
- No direct access to other module schemas

**Tenant Applications**:
- Each app gets dedicated database
- Connection string per tenant app
- Complete data isolation

## Database Migrations

**Strategy**:
- Each module owns its schema migrations
- Migrations in `{Module}.Migrations` project
- CI/CD pipeline handles migration execution
- Details covered in separate migration instructions

## .NET Aspire Orchestration

### Local Development

**AppHost Project** (`Datarizen.AppHost`):
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Read topology from configuration
var topology = builder.Configuration["Deployment:Topology"] 
    ?? "Monolith";

// Infrastructure
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// Conditionally add hosts based on topology
switch (topology)
{
    case "Monolith":
    case "SingleTenantMonolith":
        builder.AddProject<MonolithHost>("monolith-host")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitmq);
        break;

    case "DistributedApp":
    case "SingleTenantMultiApp":
        var host1 = builder.AddProject<Host1>("host1")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitmq);

        var host2 = builder.AddProject<Host2>("host2")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitmq);

        var host3 = builder.AddProject<Host3>("host3")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitmq);

        builder.AddProject<ApiGateway>("api-gateway")
            .WithReference(host1)
            .WithReference(host2)
            .WithReference(host3);
        break;

    case "MultiTenantMicroservices":
        // Add each module as separate service
        var module1 = builder.AddProject<Module1Service>("module1-service")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitmq);

        var module2 = builder.AddProject<Module2Service>("module2-service")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitmq);

        // ... more modules

        builder.AddProject<ApiGateway>("api-gateway")
            .WithReference(module1)
            .WithReference(module2);
        break;
}

builder.Build().Run();
```

**Topology Selection**:
```bash
# Set via environment variable
export Deployment__Topology=DistributedApp

# Or via appsettings.Development.json
{
  "Deployment": {
    "Topology": "DistributedApp"
  }
}

# Run Aspire
dotnet run --project src/AppHost
```

### Local Testing with Different Topologies

#### Option 1: Aspire (Recommended for Development)

**Switch Topology**:
```bash
# Test monolith
dotnet run --project src/AppHost -- --Deployment:Topology=Monolith

# Test multi-app
dotnet run --project src/AppHost -- --Deployment:Topology=DistributedApp

# Test microservices
dotnet run --project src/AppHost -- --Deployment:Topology=MultiTenantMicroservices
```

**Benefits**:
- Fast startup
- Integrated dashboard
- Service discovery automatic
- Easy debugging (attach to any service)

#### Option 2: Docker Compose

**Generate from Aspire**:
```bash
# Aspire can generate docker-compose.yml
dotnet publish src/AppHost -r linux-x64 --self-contained

# Or manually create docker-compose files per topology
```

**docker-compose.monolith.yml**:
```yaml
services:
  postgres:
    image: postgres:15
  redis:
    image: redis:7
  rabbitmq:
    image: rabbitmq:3-management
  
  monolith-host:
    build: ./src/Hosts/MonolithHost
    environment:
      - Deployment__Topology=Monolith
    depends_on:
      - postgres
      - redis
      - rabbitmq
```

**docker-compose.multiapp.yml**:
```yaml
services:
  postgres:
    image: postgres:15
  redis:
    image: redis:7
  rabbitmq:
    image: rabbitmq:3-management
  
  host1:
    build: ./src/Hosts/Host1
    environment:
      - Deployment__Topology=DistributedApp
  
  host2:
    build: ./src/Hosts/Host2
    environment:
      - Deployment__Topology=DistributedApp
  
  host3:
    build: ./src/Hosts/Host3
    environment:
      - Deployment__Topology=DistributedApp
  
  api-gateway:
    build: ./src/ApiGateway
    ports:
      - "8080:8080"
    depends_on:
      - host1
      - host2
      - host3
```

**Run**:
```bash
# Test monolith
docker-compose -f docker-compose.monolith.yml up

# Test multi-app
docker-compose -f docker-compose.multiapp.yml up

# Test microservices
docker-compose -f docker-compose.microservices.yml up
```

#### Option 3: Local Kubernetes (k3d/minikube)

**Setup k3d cluster**:
```bash
# Create local cluster
k3d cluster create datarizen-local

# Build and load images
docker build -t datarizen/host1:local ./src/Hosts/Host1
k3d image import datarizen/host1:local -c datarizen-local
```

**Kubernetes manifests per topology**:

**k8s/monolith/deployment.yaml**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: monolith-host
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: monolith
        image: datarizen/monolith-host:local
        env:
        - name: Deployment__Topology
          value: "Monolith"
```

**k8s/multiapp/deployment.yaml**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: host1
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: host1
        image: datarizen/host1:local
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: host2
# ... similar for host2, host3
```

**Deploy**:
```bash
# Test monolith
kubectl apply -f k8s/monolith/

# Test multi-app
kubectl apply -f k8s/multiapp/

# Test microservices
kubectl apply -f k8s/microservices/
```

**Benefits**:
- Production-like environment
- Test service mesh, ingress
- Resource limits, scaling

### Topology Testing Matrix

| Topology | Aspire | Docker Compose | Local K8s |
|----------|--------|----------------|-----------|
| Multi-Tenant Monolith | ✅ Primary | ✅ Supported | ✅ Supported |
| Multi-Tenant Multi-App | ✅ Primary | ✅ Supported | ✅ Supported |
| Multi-Tenant Microservices | ✅ Primary | ✅ Supported | ✅ Recommended |
| Single-Tenant Monolith | ✅ Primary | ✅ Supported | ✅ Supported |
| Single-Tenant Multi-App | ✅ Primary | ✅ Supported | ✅ Supported |

**Recommendation**:
- **Daily development**: Aspire (fastest, best DX)
- **Integration testing**: Docker Compose (consistent, CI-friendly)
- **Pre-production validation**: Local K8s (production parity)

## API Gateway

**Project**: `Datarizen.ApiGateway`

**Responsibilities**:
- Route requests to appropriate host/service
- Authentication/Authorization (JWT validation)
- Rate limiting
- Request/Response transformation
- Load balancing

**Technology**: YARP (Yet Another Reverse Proxy) or Ocelot

**Configuration** (topology-aware):
```json
{
  "Routes": [
    {
      "RouteId": "module1",
      "ClusterId": "host1",
      "Match": { "Path": "/api/module1/{**catch-all}" }
    },
    {
      "RouteId": "module2",
      "ClusterId": "host1",
      "Match": { "Path": "/api/module2/{**catch-all}" }
    },
    {
      "RouteId": "module3",
      "ClusterId": "host2",
      "Match": { "Path": "/api/module3/{**catch-all}" }
    }
  ],
  "Clusters": {
    "host1": {
      "Destinations": {
        "destination1": { "Address": "http://host1:8080" }
      }
    },
    "host2": {
      "Destinations": {
        "destination1": { "Address": "http://host2:8080" }
      }
    }
  }
}
```

## Database Server Hosting

### Multi-Tenant SaaS

**Managed Database Services**:
- **Azure Database for PostgreSQL** (Flexible Server)
- **AWS RDS for PostgreSQL**
- **Google Cloud SQL for PostgreSQL**

**Configuration**:
- High availability (zone redundancy)
- Automated backups
- Point-in-time restore
- Connection pooling (PgBouncer)
- Read replicas for scaling

### Single-Tenant On-Premise

**Customer-Managed**:
- Customer provides PostgreSQL server
- Customer handles backups, HA, scaling
- Datarizen provides schema scripts
- Customer controls access and security

**Deployment Options**:
- Bare metal PostgreSQL
- Docker container
- Kubernetes StatefulSet
- Customer's existing database infrastructure

### Connection String Management

**Configuration**:
```json
{
  "ConnectionStrings": {
    "Platform": "Host=postgres;Database=datarizen_db;Username=platform_user;Password=***",
    "TenantAppTemplate": "Host=postgres;Database=tenant_{tenantId}_app_{appId}_db;Username=tenant_{tenantId}_app_{appId}_user;Password=***"
  }
}
```

**Secrets Management**:
- Azure Key Vault (SaaS)
- Kubernetes Secrets (K8s deployments)
- Environment variables (Docker Compose)
- Customer secret manager (on-premise)

## Deployment Topology Configuration

**Configuration File** (`appsettings.json`):
```json
{
  "Deployment": {
    "Topology": "DistributedApp",
    "Tenancy": "MultiTenant",
    "Modules": {
      "Host1": ["Module1", "Module2"],
      "Host2": ["Module3", "Module4"],
      "Host3": ["Module5", "Module6"]
    }
  }
}
```

**Topology Values**:
- `Monolith`
- `DistributedApp`
- `MultiTenantMicroservices`
- `SingleTenantMonolith`
- `SingleTenantMultiApp`

**Module Loading**:
- Hosts read configuration at startup
- Load only assigned modules
- Register module services in DI container
- Configure inter-module communication based on topology