# Kubernetes Deployment Guide

## Overview

This document describes the steps needed to add Kubernetes deployment support to Datarizen, enabling local testing with minikube and production deployment to Azure Kubernetes Service (AKS).

---

## Current State vs. Target State

### Current State
- ✅ Aspire AppHost for local development orchestration
- ✅ Infrastructure services containerized (PostgreSQL, Redis, RabbitMQ)
- ✅ One Dockerfile exists (`MonolithHost`)
- ❌ Application hosts run as direct .NET processes (not containerized)
- ❌ No Kubernetes manifests or Helm charts
- ❌ No Kubernetes hosting integration in Aspire

### Target State
- ✅ Aspire AppHost continues to work for local development
- ✅ All application hosts have Dockerfiles
- ✅ Kubernetes manifests generated via `aspire publish`
- ✅ Local Kubernetes testing with minikube
- ✅ Production deployment to AKS with Azure Container Registry (ACR)

---

## Implementation Plan

### Phase 1: Add Kubernetes Hosting Integration

#### Step 1.1: Install Kubernetes Package

Add the Aspire Kubernetes hosting package to the AppHost project:

```powershell
cd server\src\AppHost
dotnet add package Aspire.Hosting.Kubernetes
```

Or use the Aspire CLI:
```powershell
aspire add kubernetes
```

#### Step 1.2: Register Kubernetes Environment

Modify `server/src/AppHost/Program.cs` to add Kubernetes environment support:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Kubernetes environment (optional - only when publishing to K8s)
var k8s = builder.AddKubernetesEnvironment("k8s")
    .WithProperties(k8s =>
    {
        k8s.HelmChartName = "datarizen";
        k8s.Namespace = "datarizen";
    });

// Existing infrastructure resources...
var postgres = builder.AddPostgres(...);
var redis = builder.AddRedis(...);
var rabbitMq = builder.AddRabbitMQ(...);

// Existing topology switch...
switch (topology)
{
    case "Monolith":
        builder.AddProject("monolith", "../Hosts/MonolithHost/Monolith.Host.csproj")
            // ... existing configuration ...
            // Optionally specify compute environment:
            // .WithComputeEnvironment(k8s);  // Only if you want K8s-specific config
            break;
    // ... other topologies ...
}
```

**Note**: The Kubernetes environment is optional and only used when publishing. The AppHost continues to work normally for local development.

---

### Phase 2: Containerize All Application Hosts

#### Step 2.1: Create Dockerfiles

Create Dockerfiles for all application hosts that don't have one yet:

**Required Dockerfiles:**
1. ✅ `server/src/Hosts/MonolithHost/Dockerfile` (already exists)
2. ❌ `server/src/Hosts/MultiAppControlPanelHost/Dockerfile`
3. ❌ `server/src/Hosts/MultiAppRuntimeHost/Dockerfile`
4. ❌ `server/src/Hosts/MultiAppAppBuilderHost/Dockerfile`
5. ❌ `server/src/Hosts/IdentityServiceHost/Dockerfile`
6. ❌ `server/src/Hosts/TenantServiceHost/Dockerfile`
7. ❌ `server/src/Hosts/AppBuilderServiceHost/Dockerfile`
8. ❌ `server/src/Hosts/TenantApplicationServiceHost/Dockerfile`
9. ❌ `server/src/ApiGateway/Dockerfile`

#### Step 2.2: Dockerfile Template

Use the existing `MonolithHost/Dockerfile` as a template. Each Dockerfile should:

- Use Alpine-based images for smaller size
- Multi-stage build (SDK for build, runtime for execution)
- Create non-root user
- Expose appropriate ports
- Set `ASPNETCORE_URLS` environment variable

**Example Dockerfile structure:**
```dockerfile
# Build stage - Alpine SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj files
COPY ["src/Hosts/<HostName>/<HostName>.csproj", "src/Hosts/<HostName>/"]
# Copy other project references...

# Restore
RUN dotnet restore "src/Hosts/<HostName>/<HostName>.csproj"

# Copy source
COPY . .

# Build & publish
WORKDIR "/src/src/Hosts/<HostName>"
RUN dotnet publish "<HostName>.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage - Alpine Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install dependencies
RUN apk add --no-cache \
    curl \
    icu-libs \
    tzdata

# Create non-root user
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser

# Copy app
COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "<HostName>.dll"]
```

#### Step 2.3: Update AppHost to Reference Dockerfiles

For each project that should be containerized, ensure Aspire can find the Dockerfile:

```csharp
builder.AddProject("monolith", "../Hosts/MonolithHost/Monolith.Host.csproj")
    .WithDockerfile("../Hosts/MonolithHost/Dockerfile", "..")  // Path relative to AppHost
    // ... other configuration ...
```

Or let Aspire auto-detect Dockerfiles in the project directory.

---

### Phase 3: Generate Kubernetes Manifests

#### Step 3.1: Install Aspire CLI

```powershell
dotnet tool install -g Aspire.Cli
```

Or update if already installed:
```powershell
dotnet tool update -g Aspire.Cli
```

#### Step 3.2: Generate Manifests

**Important:** Before publishing, ensure no AppHost processes are running, as they will lock the AppHost.exe file and prevent publishing.

**Option 1: Use the helper script (Recommended)**
```powershell
# From repository root
.\scripts\publish-k8s.ps1
```

**Option 2: Manual command**
```powershell
# Stop any running AppHost processes first
Get-Process -Name "AppHost" -ErrorAction SilentlyContinue | Stop-Process -Force

# From repository root or AppHost directory
aspire publish -e k8s -o k8s-artifacts
```

**Note:** The `-e k8s` parameter is **required** to specify the Kubernetes environment. Without it, bind mounts will cause the publish to fail.

This generates:
- Helm chart directly in `k8s-artifacts/` (Chart.yaml, values.yaml, templates/)
- Kubernetes YAML manifests in `k8s-artifacts/templates/`
- ConfigMaps and Secrets (parameterized)
- Services and Deployments/StatefulSets

#### Step 3.3: Review Generated Artifacts

The generated manifests will include:
- **Deployments** for application services
- **StatefulSets** for stateful services (PostgreSQL, Redis, RabbitMQ)
- **Services** for networking
- **ConfigMaps** for configuration
- **Secrets** (parameterized placeholders)

**Important**: The manifests contain **parameterized placeholders** (e.g., `${PG_PASSWORD}`) that must be resolved during deployment.

---

### Phase 4: Local Kubernetes Testing (minikube)

#### Step 4.1: Install minikube

**Windows (PowerShell as Administrator):**
```powershell
choco install minikube
```

#### Step 4.2: Start minikube Cluster

```powershell
minikube start --driver=docker --memory=4096 --cpus=2
```

Verify cluster is running:
```powershell
kubectl get nodes
```

#### Step 4.3: Configure Docker Environment

minikube uses its own Docker daemon. Configure your PowerShell session to use it:

```powershell
minikube docker-env | Invoke-Expression
```

**Important**: Keep this shell session open for building images. All `docker build` commands must run in this environment.

#### Step 4.4: Build Container Images

Build images using minikube's Docker daemon:

```powershell
# From repository root
docker build -t datarizen-monolith:local -f server/src/Hosts/MonolithHost/Dockerfile .
docker build -t datarizen-controlpanel:local -f server/src/Hosts/MultiAppControlPanelHost/Dockerfile .
docker build -t datarizen-runtime:local -f server/src/Hosts/MultiAppRuntimeHost/Dockerfile .
docker build -t datarizen-appbuilder:local -f server/src/Hosts/MultiAppAppBuilderHost/Dockerfile .
docker build -t datarizen-identity:local -f server/src/Hosts/IdentityServiceHost/Dockerfile .
docker build -t datarizen-tenant:local -f server/src/Hosts/TenantServiceHost/Dockerfile .
docker build -t datarizen-appbuilder-svc:local -f server/src/Hosts/AppBuilderServiceHost/Dockerfile .
docker build -t datarizen-tenantapp:local -f server/src/Hosts/TenantApplicationServiceHost/Dockerfile .
docker build -t datarizen-gateway:local -f server/src/ApiGateway/Dockerfile .
```

Or use the build script:
```powershell
.\scripts\build-images.ps1
```

#### Step 4.5: Create Values File for Helm

Create `k8s-artifacts/values-local.yaml`:

```yaml
# Image tags for local development
images:
  monolith:
    repository: datarizen-monolith
    tag: local
  controlpanel:
    repository: datarizen-controlpanel
    tag: local
  # ... other services ...

# Secrets (for local testing - use proper secrets management in production)
secrets:
  postgres:
    username: postgres
    password: "DevPassword123!"
  rabbitmq:
    username: admin
    password: "DevPassword123!"

# Configuration
config:
  environment: Development
  topology: Monolith  # or DistributedApp, Microservices
```

#### Step 4.6: Deploy to minikube

**Option A: Using Helm (recommended)**
```powershell
cd k8s-artifacts
helm install datarizen ./helm/datarizen -f values-local.yaml
```

**Option B: Using kubectl (if manifests are plain YAML)**
```powershell
# Substitute placeholders first (PowerShell variable substitution)
$content = Get-Content k8s-artifacts/k8s/deployment.yaml -Raw
$content = $content -replace '\$\{PG_PASSWORD\}', 'DevPassword123!'
# ... replace other placeholders ...
$content | kubectl apply -f -
```

#### Step 4.7: Verify Deployment

```powershell
# Check pods
kubectl get pods

# Check services
kubectl get services

# Check logs
kubectl logs -f deployment/datarizen-monolith

# Port forward to access services locally
kubectl port-forward service/datarizen-monolith 8080:8080
```

#### Step 4.8: Access Services

- **API**: `http://localhost:8080` (via port-forward)
- **PostgreSQL**: Port-forward to service
- **Redis**: Port-forward to service
- **RabbitMQ Management**: Port-forward to management port

---

### Phase 5: Prepare for Azure Kubernetes Service (AKS)

#### Step 5.1: Create Azure Container Registry (ACR)

```powershell
# Login to Azure
az login

# Create resource group
az group create --name datarizen-rg --location eastus

# Create ACR
az acr create --resource-group datarizen-rg --name datarizenacr --sku Basic

# Login to ACR
az acr login --name datarizenacr
```

#### Step 5.2: Build and Push Images to ACR

```powershell
# Build and push each image
az acr build --registry datarizenacr --image datarizen-monolith:latest -f server/src/Hosts/MonolithHost/Dockerfile .
az acr build --registry datarizenacr --image datarizen-controlpanel:latest -f server/src/Hosts/MultiAppControlPanelHost/Dockerfile .
# ... repeat for all services ...
```

Or use CI/CD pipeline (Azure DevOps, GitHub Actions) to automate this.

#### Step 5.3: Create AKS Cluster

```powershell
# Create AKS cluster
az aks create `
  --resource-group datarizen-rg `
  --name datarizen-aks `
  --node-count 2 `
  --enable-addons monitoring `
  --attach-acr datarizenacr `
  --generate-ssh-keys

# Get credentials
az aks get-credentials --resource-group datarizen-rg --name datarizen-aks
```

#### Step 5.4: Create Production Values File

Create `k8s-artifacts/values-aks.yaml`:

```yaml
# ACR image references
images:
  monolith:
    repository: datarizenacr.azurecr.io/datarizen-monolith
    tag: latest
  controlpanel:
    repository: datarizenacr.azurecr.io/datarizen-controlpanel
    tag: latest
  # ... other services ...

# Azure-managed services (recommended for production)
infrastructure:
  postgres:
    # Use Azure Database for PostgreSQL instead of in-cluster
    connectionString: "Host=your-postgres-server.postgres.database.azure.com;..."
  redis:
    # Use Azure Cache for Redis instead of in-cluster
    connectionString: "your-redis.redis.cache.windows.net:6380,ssl=true,..."

# Secrets (use Azure Key Vault CSI driver or sealed-secrets)
secrets:
  postgres:
    username: postgres
    password: "${POSTGRES_PASSWORD}"  # From Key Vault
  rabbitmq:
    username: admin
    password: "${RABBITMQ_PASSWORD}"  # From Key Vault

# Production configuration
config:
  environment: Production
  topology: Microservices  # or DistributedApp
  replicas:
    monolith: 2
    identity: 3
    tenant: 3
    # ... other services ...
```

#### Step 5.5: Deploy to AKS

```powershell
# Ensure you're connected to AKS
az aks get-credentials --resource-group datarizen-rg --name datarizen-aks

# Deploy using Helm
cd k8s-artifacts
helm install datarizen ./helm/datarizen -f values-aks.yaml --namespace datarizen --create-namespace

# Or upgrade if already deployed
helm upgrade datarizen ./helm/datarizen -f values-aks.yaml --namespace datarizen
```

#### Step 5.6: Configure Ingress (Optional)

For external access, configure Azure Application Gateway or NGINX Ingress:

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: datarizen-ingress
  annotations:
    kubernetes.io/ingress.class: azure/application-gateway
spec:
  rules:
  - host: api.datarizen.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: datarizen-gateway
            port:
              number: 80
```

---

## Infrastructure Considerations

### Option A: In-Cluster Infrastructure (Development/Testing)

Use the containers generated by Aspire:
- PostgreSQL StatefulSet
- Redis StatefulSet
- RabbitMQ StatefulSet

**Pros**: Simple, self-contained
**Cons**: Not production-ready, requires persistent volumes, manual backups

### Option B: Azure Managed Services (Production Recommended)

Replace in-cluster infrastructure with Azure services:
- **Azure Database for PostgreSQL** - Managed PostgreSQL
- **Azure Cache for Redis** - Managed Redis
- **Azure Service Bus** or **RabbitMQ on Azure** - Managed messaging

**Pros**: High availability, automatic backups, scaling, monitoring
**Cons**: Additional cost, requires network configuration

**Implementation**: Modify Helm values to use external connection strings instead of in-cluster services.

---

## Secret Management

### Development (minikube)

Use plain values in `values-local.yaml` (not recommended for production).

### Production (AKS)

**Option 1: Azure Key Vault CSI Driver**
```powershell
# Install Key Vault CSI driver
az aks enable-addons --addons azure-keyvault-secrets-provider --name datarizen-aks --resource-group datarizen-rg

# Create secret provider class
kubectl apply -f secret-provider-class.yaml
```

**Option 2: Sealed Secrets**
```powershell
# Install sealed-secrets controller
kubectl apply -f https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.24.0/controller.yaml

# Create sealed secret (PowerShell)
Get-Content secret.yaml | kubeseal | Out-File sealed-secret.yaml
```

**Option 3: External Secrets Operator**
Integrate with Azure Key Vault, AWS Secrets Manager, etc.

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Deploy

on:
  push:
    branches: [main]

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Login to ACR
        uses: azure/docker-login@v1
        with:
          login-server: datarizenacr.azurecr.io
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}
      
      - name: Build and push images
        run: |
          az acr build --registry datarizenacr --image datarizen-monolith:${{ github.sha }} -f server/src/Hosts/MonolithHost/Dockerfile .
          # ... repeat for all services ...
  
  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Configure kubectl
        uses: azure/aks-set-context@v1
        with:
          resource-group: datarizen-rg
          cluster-name: datarizen-aks
      
      - name: Deploy to AKS
        run: |
          helm upgrade --install datarizen ./k8s-artifacts/helm/datarizen \
            -f k8s-artifacts/values-aks.yaml \
            --set images.monolith.tag=${{ github.sha }} \
            --namespace datarizen
```

---

## Testing Checklist

### Local Kubernetes (minikube)

- [ ] All Dockerfiles build successfully
- [ ] Images load into minikube Docker daemon
- [ ] Helm chart installs without errors
- [ ] All pods start and become ready
- [ ] Services are accessible via port-forward
- [ ] Application hosts can connect to infrastructure (PostgreSQL, Redis, RabbitMQ)
- [ ] Health checks respond correctly
- [ ] Logs are accessible via `kubectl logs`

### AKS Deployment

- [ ] ACR images build and push successfully
- [ ] AKS cluster created and accessible
- [ ] Helm chart deploys to AKS
- [ ] Pods schedule and run on AKS nodes
- [ ] External access configured (if needed)
- [ ] Secrets managed securely (Key Vault or sealed-secrets)
- [ ] Monitoring and logging configured
- [ ] Backup strategy for persistent data

---

## Troubleshooting

### Images Not Found

**Problem**: Pods show `ImagePullBackOff` error.

**Solution**:
- For minikube: Ensure you're using minikube's Docker daemon (`minikube docker-env | Invoke-Expression`)
- For AKS: Verify ACR is attached (`az aks update --attach-acr datarizenacr`)
- Check image names and tags match in values file

### Connection Refused

**Problem**: Services can't connect to PostgreSQL/Redis/RabbitMQ.

**Solution**:
- Verify service names match (Kubernetes DNS: `postgres-service:5432`)
- Check network policies aren't blocking traffic
- Verify connection strings use Kubernetes service names, not `localhost`

### Secrets Not Found

**Problem**: Pods fail with missing secret errors.

**Solution**:
- Create secrets manually: `kubectl create secret generic postgres-secret --from-literal=password=...`
- Or use Helm values to inject secrets
- For production, use Key Vault CSI driver or sealed-secrets

---

## Next Steps

1. **Complete Phase 1**: Add Kubernetes hosting integration to AppHost
2. **Complete Phase 2**: Create Dockerfiles for all application hosts
3. **Complete Phase 3**: Generate and review Kubernetes manifests
4. **Complete Phase 4**: Test locally with minikube
5. **Complete Phase 5**: Deploy to AKS for production

---

## Related Documentation

- `current_deployment.md` - Current deployment strategy
- `server/src/AppHost/README.md` - AppHost setup
- `server/src/AppHost/Program.cs` - Aspire orchestration
- [.NET Aspire Kubernetes Integration](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/kubernetes-integration)
- [Azure Kubernetes Service Documentation](https://learn.microsoft.com/en-us/azure/aks/)
