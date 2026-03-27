# Current Deployment Strategy

## Overview

Datarizen currently uses **.NET Aspire** for local development orchestration. The deployment strategy is focused on developer productivity and local testing, with Docker containers for infrastructure services and direct process execution for application hosts.

---

## Current Architecture

### Development Environment

The current deployment uses Aspire's AppHost to orchestrate:

1. **Infrastructure Services** (Docker containers):
   - **PostgreSQL** (`postgres:16.6-alpine`) - Database server
   - **Redis** (`redis:7.4-alpine`) - Caching and session storage
   - **RabbitMQ** (`rabbitmq:4.0-management-alpine`) - Message broker
   - **pgAdmin** (`dpage/pgadmin4:9.12.0`) - Database administration UI
   - **Redis Commander** - Redis management UI

2. **Application Hosts** (direct .NET processes):
   - **MonolithHost** - Single process containing all modules (Monolith topology)
   - **MultiAppControlPanelHost** - Control panel service (DistributedApp topology)
   - **MultiAppRuntimeHost** - Runtime service (DistributedApp topology)
   - **MultiAppAppBuilderHost** - App builder service (DistributedApp topology)
   - **IdentityServiceHost** - Identity microservice (Microservices topology)
   - **TenantServiceHost** - Tenant microservice (Microservices topology)
   - **AppBuilderServiceHost** - App builder microservice (Microservices topology)
   - **TenantApplicationServiceHost** - Tenant application microservice (Microservices topology)
   - **ApiGateway** - API gateway for routing (DistributedApp/Microservices topologies)

### Deployment Topologies

The system supports three deployment topologies, selectable via configuration or command-line:

#### 1. Monolith
- Single process (`MonolithHost`) containing all modules
- In-process communication via MediatR
- Fixed ports: HTTPS `8443`, HTTP `8080`
- All modules share the same process and memory space

#### 2. DistributedApp
- Multiple hosts grouped by concern:
  - `controlpanel` - Control panel functionality
  - `runtime` - Application runtime
  - `appbuilder` - Application builder
  - `gateway` - API gateway for routing
- HTTP communication between hosts
- Service discovery via Aspire's `WithReference()`

#### 3. Microservices
- Each module as independent service:
  - `identity` - Identity service
  - `tenant` - Tenant service
  - `appbuilder` - App builder service
  - `tenantapplication` - Tenant application service
  - `gateway` - API gateway
- HTTP communication between all services
- Full service isolation

---

## Current Deployment Process

### Local Development Workflow

1. **Setup Environment**
   ```powershell
   cd server\src\AppHost
   .\scripts\setup-dev-environment.ps1
   ```
   - Initializes .NET user secrets
   - Sets PostgreSQL and RabbitMQ credentials
   - Creates pgAdmin configuration files
   - Verifies Docker is running

2. **Run Database Migrations**
   ```powershell
   dotnet run --project server/src/MigrationRunner
   ```
   - Creates database schemas and tables
   - Required before starting the API

3. **Start AppHost**
   ```powershell
   cd server\src\AppHost
   dotnet run --launch-profile https
   ```
   - Starts infrastructure containers (PostgreSQL, Redis, RabbitMQ)
   - Launches application hosts based on selected topology
   - Opens Aspire Dashboard for monitoring

4. **Access Services**
   - **Aspire Dashboard**: `https://localhost:8433` (login URL printed in console)
   - **Monolith API**: `https://localhost:8443` or `http://localhost:8080`
   - **pgAdmin**: `http://localhost:5050`
   - **Redis Commander**: Via Aspire Dashboard
   - **RabbitMQ Management**: Via Aspire Dashboard

### Container Lifecycle

- **Development**: Containers use `ContainerLifetime.Session` - stop when AppHost stops
- **Production**: Containers use `ContainerLifetime.Persistent` - keep running after AppHost stops

### Data Persistence

Infrastructure services use Docker volumes:
- `dr-development-db` - PostgreSQL data
- `dr-development-cache` - Redis data
- `dr-development-messaging` - RabbitMQ data

---

## Containerization Status

### Current State

- **Infrastructure**: Fully containerized (PostgreSQL, Redis, RabbitMQ, pgAdmin)
- **Application Hosts**: **Not containerized** - run as direct .NET processes
- **Dockerfile**: Only `MonolithHost` has a Dockerfile (`server/src/Hosts/MonolithHost/Dockerfile`)

### Dockerfile Details

The existing `MonolithHost/Dockerfile`:
- Uses Alpine-based images for smaller size
- Multi-stage build (SDK for build, runtime for execution)
- Creates non-root user (`appuser`)
- Exposes port `8080`
- Sets `ASPNETCORE_URLS=http://+:8080`

### Missing Containerization

The following hosts **do not have Dockerfiles**:
- `MultiAppControlPanelHost`
- `MultiAppRuntimeHost`
- `MultiAppAppBuilderHost`
- `IdentityServiceHost`
- `TenantServiceHost`
- `AppBuilderServiceHost`
- `TenantApplicationServiceHost`
- `ApiGateway`

---

## Configuration Management

### Secrets Management

- **Development**: Uses .NET User Secrets (stored outside source code)
- **Production**: Intended to use Azure Key Vault, AWS Secrets Manager, or environment variables

### Parameter Injection

Aspire injects connection strings and service URLs via:
- `WithReference()` - Service discovery between hosts
- `PublishAsConnectionString()` - Database connection strings
- Environment variables for configuration

### Configuration Sources

1. `appsettings.json` - Base configuration
2. `appsettings.Development.json` - Development overrides
3. User Secrets - Sensitive values (passwords, API keys)
4. Environment variables - Runtime overrides
5. Command-line arguments - Topology selection

---

## Service Discovery

### Aspire Service Discovery

When running under Aspire, services discover each other via:
- `WithReference(postgres)` - Injects connection string
- `WithReference(redis)` - Injects Redis connection
- `WithReference(rabbitMq)` - Injects RabbitMQ connection
- `WithReference(identity)` - Injects Identity service URL (Microservices topology)

### Configuration Keys

Aspire injects configuration under `Services:<serviceName>`:
- `Services:identity:https` - HTTPS endpoint URL
- `Services:identity:http` - HTTP endpoint URL
- `Services:postgres` - PostgreSQL connection string

### Fallback Configuration

When running **without Aspire** (standalone or production):
- Services must use explicit configuration (`Services:Identity:BaseUrl` in `appsettings.json`)
- Connection strings must be provided via environment variables or config files

---

## Limitations of Current Approach

### Development-Only Focus

1. **No Production Deployment Path**: Current setup is optimized for local development only
2. **No Container Orchestration**: Application hosts run as processes, not containers
3. **No Kubernetes Support**: No manifests or Helm charts for K8s deployment
4. **No CI/CD Integration**: No automated build/push/deploy pipeline
5. **Manual Infrastructure Management**: Containers started/stopped manually via AppHost

### Scalability Constraints

1. **Single Instance**: No horizontal scaling support
2. **No Load Balancing**: Direct process execution, no load balancer
3. **No Health Checks**: Basic health endpoints exist but not integrated with orchestration
4. **No Auto-Recovery**: Process failures require manual restart

### Production Readiness Gaps

1. **No High Availability**: Single point of failure for each service
2. **No Rolling Updates**: Cannot update services without downtime
3. **No Resource Limits**: No CPU/memory constraints
4. **No Monitoring Integration**: Aspire Dashboard is development-only
5. **No Secret Management**: User Secrets not suitable for production

---

## Migration Path to Kubernetes

To move from the current Aspire-based local development to Kubernetes deployment:

1. **Add Kubernetes Hosting Integration** to AppHost
2. **Create Dockerfiles** for all application hosts
3. **Generate Kubernetes Manifests** using `aspire publish`
4. **Test Locally** with minikube or Docker Desktop Kubernetes
5. **Deploy to AKS** with Azure Container Registry (ACR)

See `k8s_deployment.md` for detailed implementation plan.

---

## Related Documentation

- `server/src/AppHost/README.md` - AppHost setup and usage
- `docs/ai-context/01-DEPLOYMENT-STRATEGY.md` - Deployment topologies overview
- `server/src/Hosts/MonolithHost/Dockerfile` - Example Dockerfile
- `server/src/AppHost/Program.cs` - Aspire orchestration code
