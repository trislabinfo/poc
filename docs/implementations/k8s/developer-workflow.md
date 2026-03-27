# Developer Workflow: Daily Development Guide

## Overview

This guide explains how developers work with Datarizen on a daily basis, covering:
- **Aspire** - Local development with Visual Studio 2022 or PowerShell (recommended for daily work)
- **minikube** - Local Kubernetes testing (for validating Kubernetes deployment)

Both environments use the same codebase with **no code changes required**. The switch is purely workflow-based.

---

## Daily Development with Aspire

Aspire is the recommended environment for daily development work. It provides fast iteration, easy debugging, and quick startup times.

### Prerequisites

1. **Visual Studio 2022** (or later) with .NET 9 SDK
2. **Docker Desktop** installed and running
3. **.NET Aspire** workload installed (usually included with Visual Studio)

### Initial Setup (One-Time)

#### Step 1: Run Setup Script

**Windows (PowerShell):**
```powershell
cd server\src\AppHost
.\scripts\setup-dev-environment.ps1
```

This script:
- ✅ Initializes .NET user secrets for the AppHost project
- ✅ Sets PostgreSQL credentials (`datarizen` / `datarizen!`)
- ✅ Sets RabbitMQ credentials (`datarizen` / `datarizen!`)
- ✅ Creates pgAdmin configuration files
- ✅ Verifies Docker is installed and running

#### Step 2: Run Database Migrations (One-Time or After Schema Changes)

From the repository root:

```powershell
dotnet run --project server/src/MigrationRunner
```

**Important:** This must be run before starting the API. If skipped, you'll see errors like `relation "identity.users" does not exist`.

---

### Daily Workflow: Visual Studio 2022

#### Starting the Application

1. **Open Solution**
   - Open `Datarizen.sln` in Visual Studio 2022

2. **Set Startup Project**
   - Right-click `server/src/AppHost/AppHost.csproj`
   - Select **"Set as Startup Project"**

3. **Select Launch Profile**
   - In the toolbar, select **"https"** from the launch profile dropdown
   - This ensures `appsettings.Development.json` and user secrets are loaded

4. **Start Debugging**
   - Press **F5** or click the **Start** button
   - Visual Studio will:
     - Build the solution
     - Start Docker containers (PostgreSQL, Redis, RabbitMQ)
     - Launch the AppHost
     - Open the Aspire Dashboard in your browser

#### Accessing Services

After startup, you can access:

- **Aspire Dashboard**: Automatically opens at `https://localhost:8433` (login URL printed in console)
- **Monolith API** (Monolith topology): `https://localhost:8443` or `http://localhost:8080`
- **pgAdmin**: `http://localhost:5050` (or via Aspire Dashboard)
  - Login: `admin@datarizen.com` / `dr-development`

#### Changing Topology

To switch between Monolith, DistributedApp, or Microservices:

1. **Via Configuration File**
   - Edit `server/src/AppHost/appsettings.Development.json`
   - Set `"Deployment:Topology": "Monolith"` (or `"DistributedApp"`, `"Microservices"`)

2. **Via Command-Line Arguments** (when running from Visual Studio)
   - Right-click AppHost project → **Properties** → **Debug** → **General**
   - Add to **Command line arguments**: `--topology Microservices`

#### Debugging

- **Set Breakpoints**: Click in the gutter next to any line of code
- **Attach Debugger**: Breakpoints work automatically for AppHost and all child projects
- **View Logs**: Use the Aspire Dashboard's **Logs** tab for each service
- **Inspect Variables**: Hover over variables or use the **Watch** window

#### Stopping the Application

- Press **Shift+F5** or click the **Stop** button
- This stops the AppHost and all child processes
- Docker containers continue running (they're persistent)

---

### Daily Workflow: PowerShell/Command Line

#### Starting the Application

```powershell
# Navigate to AppHost directory
cd server\src\AppHost

# Start AppHost with Development launch profile
dotnet run --launch-profile https
```

**Important:** Always use `--launch-profile https` to ensure:
- `appsettings.Development.json` is loaded
- User secrets are loaded
- Environment is set to `Development`

#### Changing Topology via Command Line

```powershell
# Run with Monolith topology (default)
dotnet run --launch-profile https

# Run with DistributedApp topology
dotnet run --launch-profile https -- --topology DistributedApp

# Run with Microservices topology
dotnet run --launch-profile https -- --topology Microservices
```

**Note:** The `--` (double hyphen) is required to pass arguments to the AppHost application, not to `dotnet run`.

#### Alternative: Environment Variable

```powershell
# Set topology via environment variable
$env:Deployment__Topology = "Microservices"
dotnet run --launch-profile https

# Or in a single line
$env:Deployment__Topology = "Microservices"; dotnet run --launch-profile https
```

#### Viewing Logs

- **Console Output**: Logs appear in the terminal
- **Aspire Dashboard**: Open `https://localhost:8433` and navigate to **Logs** tab
- **Individual Service Logs**: Click on a service in the Aspire Dashboard → **Logs**

#### Stopping the Application

- Press **Ctrl+C** in the terminal
- This stops the AppHost and all child processes
- Docker containers continue running (persistent)

---

### Common Daily Tasks

#### View User Secrets

```powershell
cd server\src\AppHost
dotnet user-secrets list
```

#### Update a Secret

```powershell
cd server\src\AppHost
dotnet user-secrets set "Parameters:postgres-password" "NewPassword123!"
```

#### Check Docker Containers

```powershell
# List running containers
docker ps

# View container logs
docker logs dr-development-db

# Stop all containers
docker stop $(docker ps -q)
```

#### Clean Up (Reset Everything)

```powershell
# Stop all containers
docker stop $(docker ps -q)

# Remove all containers
docker rm $(docker ps -aq)

# Remove volumes (deletes all data!)
docker volume rm dr-development-db
docker volume rm dr-development-cache
docker volume rm dr-development-messaging
```

---

## Switching to minikube (Kubernetes Testing)

Use minikube when you need to:
- Test Kubernetes deployment locally
- Validate Helm charts before AKS deployment
- Test container images
- Verify Kubernetes-specific configuration

### Prerequisites

1. **minikube** installed
2. **kubectl** installed
3. **Helm** installed (v3+)
4. **Aspire CLI** installed: `dotnet tool install -g Aspire.Cli`

### Initial Setup (One-Time)

#### Step 1: Install minikube

**Windows (PowerShell as Administrator):**
```powershell
choco install minikube
```

#### Step 2: Install Helm

**Windows (PowerShell):**

**Option 1: Using winget (Recommended - works without admin):**
```powershell
winget install --id Helm.Helm -e --accept-source-agreements --accept-package-agreements
```

**Option 2: Using Chocolatey (requires admin):**
```powershell
choco install kubernetes-helm
```

**After installation:** Close and reopen your PowerShell terminal, or refresh PATH:
```powershell
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
```

**Verify installation:**
```powershell
helm version
```

#### Step 3: Install Aspire CLI

```powershell
dotnet tool install -g Aspire.Cli
```

---

### Daily Workflow: minikube

#### Step 1: Start minikube Cluster

```powershell
# Start minikube with sufficient resources
minikube start --driver=docker --memory=4096 --cpus=2

# Verify cluster is running
kubectl get nodes
```

#### Step 2: Configure Docker Environment

**Important:** minikube uses its own Docker daemon. You must configure your PowerShell session to use it:

```powershell
minikube docker-env | Invoke-Expression
```

**Keep this terminal session open** - all `docker build` commands must run in this environment.

#### Step 3: Build Container Images

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

**Or create a build script** (`build-images.ps1`):
```powershell
# build-images.ps1
$images = @(
    @{Name="monolith"; Path="server/src/Hosts/MonolithHost/Dockerfile"},
    @{Name="controlpanel"; Path="server/src/Hosts/MultiAppControlPanelHost/Dockerfile"},
    @{Name="runtime"; Path="server/src/Hosts/MultiAppRuntimeHost/Dockerfile"},
    @{Name="appbuilder"; Path="server/src/Hosts/MultiAppAppBuilderHost/Dockerfile"},
    @{Name="identity"; Path="server/src/Hosts/IdentityServiceHost/Dockerfile"},
    @{Name="tenant"; Path="server/src/Hosts/TenantServiceHost/Dockerfile"},
    @{Name="appbuilder-svc"; Path="server/src/Hosts/AppBuilderServiceHost/Dockerfile"},
    @{Name="tenantapp"; Path="server/src/Hosts/TenantApplicationServiceHost/Dockerfile"},
    @{Name="gateway"; Path="server/src/ApiGateway/Dockerfile"}
)

foreach ($img in $images) {
    Write-Host "Building $($img.Name)..." -ForegroundColor Green
    docker build -t "datarizen-$($img.Name):local" -f $img.Path .
}
```

#### Step 4: Generate Kubernetes Manifests

**Important:** Before publishing, ensure no AppHost processes are running, as they will lock the AppHost.exe file and prevent publishing.

**Option 1: Use the helper script (Recommended)**
```powershell
# From repository root
.\scripts\publish-k8s.ps1
```

The script will:
- Check for and optionally stop running AppHost processes
- Verify Docker connectivity
- Run the publish command with correct parameters

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

#### Step 5: Create Values File

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
  runtime:
    repository: datarizen-runtime
    tag: local
  appbuilder:
    repository: datarizen-appbuilder
    tag: local
  identity:
    repository: datarizen-identity
    tag: local
  tenant:
    repository: datarizen-tenant
    tag: local
  appbuilder-svc:
    repository: datarizen-appbuilder-svc
    tag: local
  tenantapp:
    repository: datarizen-tenantapp
    tag: local
  gateway:
    repository: datarizen-gateway
    tag: local

# Secrets (for local testing)
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

#### Step 6: Deploy to minikube

```powershell
cd k8s-artifacts
helm install datarizen . -f values-local.yaml
```

**Note:** The Helm chart is generated directly in the `k8s-artifacts` directory (not in a `helm/datarizen` subdirectory). Use `.` to reference the current directory as the chart path.

#### Step 7: Verify Deployment

```powershell
# Check pods
kubectl get pods

# Check services
kubectl get services

# View logs
kubectl logs -f deployment/datarizen-monolith

# Check pod status
kubectl describe pod <pod-name>
```

#### Step 8: Access Services

```powershell
# Port forward to access services locally
kubectl port-forward service/datarizen-monolith 8080:8080

# Access API
# http://localhost:8080

# Open Kubernetes dashboard
minikube dashboard
```

#### Stopping minikube Deployment

```powershell
# Uninstall Helm release
helm uninstall datarizen

# Stop minikube cluster (optional)
minikube stop

# Delete minikube cluster (optional - removes everything)
minikube delete
```

---

## Switching Between Aspire and minikube

### From Aspire to minikube

**Step 1: Stop Aspire**
- **Visual Studio**: Press **Shift+F5** or click **Stop**
- **PowerShell**: Press **Ctrl+C** in the terminal

**Step 2: Verify Docker Containers**
```powershell
# Check if containers are still running
docker ps

# Optionally stop Aspire containers (they're persistent)
docker stop dr-development-db dr-development-cache dr-development-messaging
```

**Step 3: Start minikube Workflow**
- Follow the **minikube Daily Workflow** steps above

**Note:** Aspire and minikube can run simultaneously if using different ports, but it's recommended to stop Aspire to avoid port conflicts.

---

### From minikube to Aspire

**Step 1: Stop minikube Deployment**
```powershell
# Uninstall Helm release
helm uninstall datarizen

# Optionally stop minikube cluster
minikube stop
```

**Step 2: Reset Docker Environment** (if needed)
```powershell
# If you configured minikube's Docker environment, you may need to reset
# Close the terminal and open a new one, or:
# Restart Docker Desktop
```

**Step 3: Start Aspire**
- **Visual Studio**: Press **F5** with AppHost as startup project
- **PowerShell**: `dotnet run --launch-profile https` from `server\src\AppHost`

**Step 4: Verify Infrastructure**
```powershell
# Check Docker containers are running
docker ps

# If containers aren't running, start AppHost and they'll start automatically
```

---

## Quick Reference: Command Comparison

| Task | Aspire | minikube |
|------|--------|----------|
| **Start** | `dotnet run --launch-profile https` | `minikube start` → `helm install` |
| **Stop** | `Ctrl+C` or `Shift+F5` | `helm uninstall datarizen` |
| **View Logs** | Aspire Dashboard | `kubectl logs -f <pod>` |
| **Access API** | `https://localhost:8443` | `kubectl port-forward` → `http://localhost:8080` |
| **Debug** | Visual Studio breakpoints | `kubectl exec -it <pod> -- /bin/sh` |
| **Restart Service** | Stop/Start AppHost | `kubectl rollout restart deployment/<name>` |
| **View Infrastructure** | Aspire Dashboard | `kubectl get all` |

---

## Troubleshooting

### Aspire Issues

#### Port Already in Use

**Problem:** `Address already in use` error

**Solution:**
```powershell
# Find process using the port
netstat -ano | findstr :8443

# Kill the process (replace <PID> with actual process ID)
taskkill /F /PID <PID>

# Or stop all dotnet processes
Get-Process dotnet | Stop-Process -Force
```

#### Docker Not Running

**Problem:** `Cannot connect to Docker daemon` or `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`

**Solution:**
1. **Start Docker Desktop**
   - Open Docker Desktop from Start Menu
   - Wait for it to fully start (whale icon in system tray should be steady, not animating)

2. **Verify Docker is running:**
   ```powershell
   docker ps
   ```
   - Should return list of containers (or empty list if no containers running)
   - If error, Docker is not ready yet

3. **Check Docker Desktop status:**
   - Right-click Docker Desktop icon in system tray
   - Select "Troubleshoot" if issues persist
   - Restart Docker Desktop if needed

4. **If using minikube:**
   ```powershell
   # Ensure minikube is running
   minikube status
   
   # Configure Docker environment for minikube
   minikube docker-env | Invoke-Expression
   
   # Verify Docker connection
   docker ps
   ```

5. **Common fixes:**
   - Restart Docker Desktop
   - Restart Windows (if Docker Desktop won't start)
   - Check Windows WSL 2 is enabled (Docker Desktop requires WSL 2)
   - Verify Docker Desktop is not blocked by antivirus/firewall

#### User Secrets Not Found

**Problem:** `Parameters:postgres-password` not found

**Solution:**
```powershell
cd server\src\AppHost
dotnet user-secrets init
dotnet user-secrets set "Parameters:postgres-password" "DevPassword123!"
```

---

### minikube Issues

#### Images Not Found

**Problem:** Pods show `ImagePullBackOff` error

**Solution:**
- Ensure you're using minikube's Docker daemon: `minikube docker-env | Invoke-Expression`
- Rebuild images: `docker build -t datarizen-monolith:local ...`
- Verify images: `minikube image ls`

#### Services Can't Connect

**Problem:** Application can't connect to PostgreSQL/Redis

**Solution:**
```powershell
# Verify services exist
kubectl get services

# Check service DNS
kubectl exec -it <pod-name> -- nslookup postgres-service

# Verify connection strings use Kubernetes service names (not localhost)
kubectl get configmap -o yaml
```

#### Helm Chart Not Found

**Problem:** `Error: path "./helm/datarizen" not found`

**Cause:** The Helm chart is generated directly in the `k8s-artifacts` directory, not in a `helm/datarizen` subdirectory.

**Solution:**
- Ensure you've run `aspire publish -e k8s -o k8s-artifacts` (note the `-e k8s` parameter)
- Verify Helm chart exists: `ls k8s-artifacts/Chart.yaml`
- Use the correct command from the `k8s-artifacts` directory:
  ```powershell
  cd k8s-artifacts
  helm install datarizen . -f values-local.yaml
  ```
  Note: Use `.` (current directory) as the chart path, not `./helm/datarizen`

#### File Locking Error (MSB3021/MSB3027)

**Problem:** `error MSB3021: Unable to copy file ... The process cannot access the file because it is being used by another process. The file is locked by: "AppHost (27404)"`

**Cause:** A previous AppHost process is still running and has locked the `AppHost.exe` file, preventing the build from completing.

**Solution:**
1. **Stop the running AppHost process:**
   ```powershell
   # Find and stop AppHost processes
   Get-Process -Name "AppHost" -ErrorAction SilentlyContinue | Stop-Process -Force
   ```

2. **Or use the helper script** (recommended):
   ```powershell
   .\scripts\publish-k8s.ps1
   ```
   The script will automatically detect and offer to stop running AppHost processes.

3. **Verify no processes are running:**
   ```powershell
   Get-Process -Name "AppHost" -ErrorAction SilentlyContinue
   ```
   If this returns nothing, you're good to proceed.

**Prevention:** Always stop AppHost (Ctrl+C in the terminal where it's running) before publishing to Kubernetes.

#### Missing Environment Parameter

**Problem:** `Bind mounts are not supported by the Kubernetes publisher`

**Cause:** The `aspire publish` command was run without the `-e k8s` parameter, so bind mounts were not skipped.

**Solution:**
- Always use: `aspire publish -e k8s -o k8s-artifacts`
- Or use the helper script: `.\scripts\publish-k8s.ps1`

---

## Best Practices

### Daily Development

1. **Use Aspire for daily work** - Faster iteration, easier debugging
2. **Use minikube before AKS** - Validate Kubernetes deployment locally
3. **Keep environments separate** - Don't mix Aspire and minikube simultaneously
4. **Clean up regularly** - Stop unused containers/clusters to free resources

### Code Changes

1. **No code changes needed** - Application hosts support both environments via fallback configuration
2. **Test in both environments** - Ensure code works in Aspire and Kubernetes
3. **Document environment-specific issues** - Note any differences

### Configuration

1. **Keep configs separate** - Aspire uses `appsettings.Development.json`, minikube uses `values-local.yaml`
2. **Use environment variables** - For secrets and environment-specific values
3. **Version control configs** - Commit `values-local.yaml` but not secrets

---

## Summary

| Aspect | Aspire | minikube |
|--------|--------|----------|
| **Use Case** | Daily development | Kubernetes testing |
| **Startup Time** | ~10 seconds | ~2-5 minutes |
| **Debugging** | Easy (Visual Studio) | Harder (container logs) |
| **Iteration Speed** | Fast (code changes immediate) | Slower (rebuild image) |
| **Kubernetes Testing** | No | Yes |
| **Production Readiness** | No | Yes (validates K8s config) |

**Recommendation:** Use Aspire for daily development, switch to minikube when you need to test Kubernetes deployment before AKS.

---

## Related Documentation

- `current_deployment.md` - Current deployment strategy details
- `k8s_deployment.md` - Kubernetes deployment implementation guide
- `server/src/AppHost/README.md` - Aspire setup and usage
- `docs/implementations/architectura/module-intermodule-communication-impl-plan.md` - Service discovery details
