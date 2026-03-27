# Datarizen AppHost - Aspire Orchestration

This project uses **.NET Aspire** to orchestrate local development infrastructure (PostgreSQL, Redis, RabbitMQ) and the Datarizen API.

---

## Quick Start

### 1. Run Setup Script

**macOS/Linux:**
```bash
cd server/src/AppHost
chmod +x scripts/setup-dev-environment.sh
./scripts/setup-dev-environment.sh
```

**Windows (PowerShell):**
```powershell
cd server\src\AppHost
.\scripts\setup-dev-environment.ps1
```

### 2. Start Docker Desktop

Ensure Docker Desktop is running before starting the AppHost.

### 3. Run database migrations

Create the database schemas and tables (tenant, identity, etc.) by running the MigrationRunner once. Use the same connection details as the AppHost (see `server/src/MigrationRunner/appsettings.json`). From the repository root:

```bash
dotnet run --project server/src/MigrationRunner
```

Or from the MigrationRunner directory with environment set for development:

```bash
cd server/src/MigrationRunner
dotnet run
```

If you skip this step, the API will fail with errors such as `relation "identity.users" does not exist` when calling endpoints that use the Identity or Tenant modules.

### 4. Run AppHost

From the AppHost directory, run with the **Development** launch profile so that `appsettings.Development.json` and user secrets are loaded:

```bash
cd server/src/AppHost
dotnet run --launch-profile https
```

Without `--launch-profile https`, the app may run with Production environment and will not load development settings or user secrets. Alternatively, set the environment before running (e.g. `ASPNETCORE_ENVIRONMENT=Development dotnet run` on macOS/Linux, or `$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run` in PowerShell).

**Choosing topology (Monolith, DistributedApp, Microservices):** The default comes from `appsettings.json` or `appsettings.Development.json` (`Deployment:Topology`). To override from the command line, use `--topology` (recommended) or the config key form; the AppHost parses these so they take precedence over config files.

**Important:** You must use `--` (double hyphen) before `--topology` so that the argument is passed to the AppHost and not to `dotnet run`. Without `--`, the topology override is ignored and the default from config files is used.

```bash
# Run with Microservices topology (identity + tenant as separate processes)
dotnet run --launch-profile https -- --topology Microservices
```

**Windows (PowerShell):**
```powershell
dotnet run --launch-profile https -- --topology Microservices
```

Alternative forms (also supported): `--Deployment__Topology=Microservices` after `--`, or environment variable `Deployment__Topology=Microservices` (PowerShell: `$env:Deployment__Topology="Microservices"`). Valid values are `Monolith`, `DistributedApp`, and `Microservices`. With **Microservices** topology, the AppHost starts the Identity and Tenant services as separate processes plus the gateway (no monolith).

**Client apps (Microservices only):** When using **Microservices** topology, the AppHost also starts the three frontend apps (builder, dashboard, runtime) via `Aspire.Hosting.JavaScript`. They run with pnpm from `client/apps/*`, listen on ports **5173** (builder), **5174** (dashboard), **5175** (runtime), and receive `VITE_API_BASE_URL` pointing at the gateway HTTPS endpoint. Open them from the Aspire Dashboard or at http://localhost:5173, http://localhost:5174, http://localhost:5175. Automatic `pnpm install` is disabled to avoid Windows EPERM (exit -4048) in the installer; **you must run `pnpm install` from the repository root** (or from each `client/apps/<builder|dashboard|runtime>`) before starting the AppHost so dependencies are present.

### 5. Access Services

- **Aspire Dashboard**: Open the login URL printed in the console (e.g. `https://localhost:8433/login?t=...`). From the dashboard you can open pgAdmin, Redis Commander, and the monolith API.
- **Monolith API (Monolith topology)**: **https://localhost:8443** or **http://localhost:8080** (fixed ports to avoid proxy bind errors).
- **pgAdmin**: Open via the Aspire Dashboard (click the `dr-development-dbadmin` resource → **Open in browser**), or go to **http://localhost:5050**.
  - **Login**: Email `admin@datarizen.com`, password `dr-development` (no separate “master password” prompt).
  - **Postgres server**: If you ran the setup script, the **Datarizen Dev** server is already in the list and connects without asking for the database password (credentials are in `conf/pgadmin/servers.json` and the `pgpass` file created by the script). If not, add the server manually (Host: `dr-development-db`, Port: `5432`, Username/Password from user secrets).

---

## What the Setup Script Does

The `setup-dev-environment.sh` (or `.ps1`) script:

1. ✅ Initializes .NET user secrets for the AppHost project
2. ✅ Sets PostgreSQL credentials (`datarizen` / `datarizen!`)
3. ✅ Sets RabbitMQ credentials (`datarizen` / `datarizen!`)
4. ✅ Creates the pgAdmin `pgpass` file so pgAdmin can connect to Postgres without prompting
5. ✅ Verifies Docker is installed and running
6. ✅ Displays next steps and service URLs

---

## Aspire Secret Management

### How It Works

Aspire uses **.NET User Secrets** to store sensitive configuration (passwords, API keys) **outside your source code**.

**User secrets are stored at:**
- **macOS/Linux**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ AppHost/Program.cs                                          │
├─────────────────────────────────────────────────────────────┤
│ var username = builder.AddParameter("postgres-username");  │
│ var password = builder.AddParameter("postgres-password");  │
│                                                             │
│ var postgres = builder.AddPostgres("db", username, password)│
│     .AddDatabase("datarizen-dev-db", "datarizen_dev");     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ User Secrets (secrets.json)                                 │
├─────────────────────────────────────────────────────────────┤
│ {                                                           │
│   "Parameters:postgres-username": "postgres",               │
│   "Parameters:postgres-password": "DevPassword123!",        │
│   "Parameters:rabbitmq-username": "admin",                  │
│   "Parameters:rabbitmq-password": "DevPassword123!"         │
│ }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Docker Containers                                           │
├─────────────────────────────────────────────────────────────┤
│ PostgreSQL:                                                 │
│   POSTGRES_USER=postgres                                    │
│   POSTGRES_PASSWORD=DevPassword123!                         │
│   POSTGRES_DB=datarizen_development                         │
│                                                             │
│ RabbitMQ:                                                   │
│   RABBITMQ_DEFAULT_USER=admin                               │
│   RABBITMQ_DEFAULT_PASS=DevPassword123!                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Monolith.Host (Your API)                                    │
├─────────────────────────────────────────────────────────────┤
│ Connection string automatically injected:                   │
│ "datarizen-dev-db": "Host=localhost;Port=54321;..."        │
└─────────────────────────────────────────────────────────────┘
```

### Parameter Naming Convention

Aspire looks for parameters with the `Parameters:` prefix:

```bash
# ✅ Correct
dotnet user-secrets set "Parameters:postgres-username" "postgres"

# ❌ Wrong (missing Parameters: prefix)
dotnet user-secrets set "postgres-username" "postgres"
```

### Managing Secrets

**View all secrets:**
```bash
cd server/src/AppHost
dotnet user-secrets list
```

**Set a secret:**
```bash
dotnet user-secrets set "Parameters:key-name" "value"
```

**Remove a secret:**
```bash
dotnet user-secrets remove "Parameters:key-name"
```

**Clear all secrets:**
```bash
dotnet user-secrets clear
```

**View secrets file location:**
```bash
# macOS/Linux
cat ~/.microsoft/usersecrets/$(grep UserSecretsId AppHost.csproj | sed 's/.*<UserSecretsId>\(.*\)<\/UserSecretsId>/\1/')/secrets.json

# Windows (PowerShell)
$secretsId = Select-Xml -Path AppHost.csproj -XPath "//UserSecretsId" | Select-Object -ExpandProperty Node | Select-Object -ExpandProperty InnerText
Get-Content "$env:APPDATA\Microsoft\UserSecrets\$secretsId\secrets.json"
```

---

## Infrastructure Services

### PostgreSQL

**Credentials:**
- Username: `postgres`
- Password: `DevPassword123!`
- Database: `datarizen_development`

**Access via pgAdmin:**
1. Run AppHost: `dotnet run --launch-profile https`
2. Open Aspire Dashboard: http://localhost:15000
3. Find `dr-development-db-pgadmin` resource
4. Click the endpoint URL
5. Login with PostgreSQL credentials

**Direct connection:**
```bash
# Find the dynamic port from Aspire Dashboard
psql -h localhost -p <dynamic-port> -U postgres -d datarizen_development
```

### Redis

**Access via Redis Commander:**
1. Open Aspire Dashboard: http://localhost:15000
2. Find `dr-development-cache-commander` resource
3. Click the endpoint URL

### RabbitMQ

**Credentials:**
- Username: `admin`
- Password: `DevPassword123!`

**Access Management UI:**
1. Open Aspire Dashboard: http://localhost:15000
2. Find `dr-development-messaging` resource
3. Click the management endpoint URL
4. Login with RabbitMQ credentials

---

## Container Lifecycle

Aspire uses `ContainerLifetime.Session` for development:

- **Session**: Containers **stop** when AppHost stops (recommended for development)
- **Persistent**: Containers **keep running** after AppHost stops

**Current configuration:**
```csharp
var lifetime = builder.Environment.IsDevelopment() 
    ? ContainerLifetime.Session      // Development: stop with AppHost
    : ContainerLifetime.Persistent;  // Production: keep running
```

---

## Data Persistence

All services use **Docker volumes** for data persistence:

- `datarizen-postgres-data` - PostgreSQL data
- `datarizen-redis-data` - Redis data
- `datarizen-rabbitmq-data` - RabbitMQ data

**Clean up volumes (deletes all data):**
```bash
docker volume rm datarizen-postgres-data
docker volume rm datarizen-redis-data
docker volume rm datarizen-rabbitmq-data
```

---

## Troubleshooting

### Secrets Not Found

**Check if user secrets are initialized:**
```bash
cd server/src/AppHost
grep UserSecretsId AppHost.csproj
```

**If missing, run setup script:**
```bash
./scripts/setup-dev-environment.sh
```

### PostgreSQL Authentication Failed

**Clean up old container and volume:**
```bash
docker stop $(docker ps -q)
docker rm $(docker ps -aq)
docker volume rm datarizen-postgres-data
dotnet run --launch-profile https
```

### Port Conflicts

**"Address already in use" / "bind: Only one usage of each socket address"**

This usually means another process is already using the port (e.g. a previous AppHost run, a standalone PostgreSQL/Redis/RabbitMQ, or pgAdmin). Ports used by this stack include:

| Resource | Port(s) | Notes |
|----------|---------|--------|
| Monolith (API) | **8443** (HTTPS), **8080** (HTTP) | Fixed; see below |
| PostgreSQL | 5432 | WithHostPort(5432) |
| Redis | 6379 | WithHostPort(6379) |
| RabbitMQ management | 7070 | WithManagementPlugin(7070) |
| pgAdmin | 5050 | WithHttpEndpoint(port: 5050) |
| Aspire Dashboard | 8433 (HTTPS), 21030 (OTLP) | From launch profile |
| Redis Commander | 6060 | WithHostPort(6060) |
| Builder (client) | 5173 | Microservices topology only |
| Dashboard (client) | 5174 | Microservices topology only |
| Runtime (client) | 5175 | Microservices topology only |

**Mitigation:**

1. **Stop other instances:** Close any other AppHost or `dotnet run` for this solution. Stop standalone Postgres/Redis/RabbitMQ/pgAdmin if they use the same ports.
2. **Monolith uses fixed non-ephemeral ports:** The monolith is configured to use **8443** (HTTPS) and **8080** (HTTP), which are below the ephemeral range (49152–65535). This avoids Aspire’s "configured to use a port in the ephemeral range" warning and reduces proxy bind failures. If 8443 or 8080 are in use, kill the process (e.g. `taskkill /F /IM AppHost.exe`) or change `MonolithHost/Properties/launchSettings.json` and the matching `WithHttpsEndpoint`/`WithHttpEndpoint` in `Program.cs`.

For other resources, Aspire may assign dynamic ports; check the Aspire Dashboard for actual URLs.

**If you need fixed ports for Postgres (not recommended for shared dev):**
```csharp
var postgres = builder.AddPostgres("db", username, password)
    .WithHostPort(5432);  // Fixed port
```

### Docker Not Running

**Start Docker Desktop**, then run (from repo root, use the launch profile so development settings load):
```bash
dotnet run --launch-profile https --project server/src/AppHost
```

### "Application Control policy has blocked this file" (Windows)

If a child project (e.g. Monolith.Host) fails with `FileLoadException` and "An Application Control policy has blocked this file" when loading a DLL from `artifacts\aspire\...`, Windows security (WDAC, AppLocker, Controlled Folder Access, or antivirus) is blocking execution from that path. Aspire builds child projects into `artifacts\aspire\<run-id>\...` by design. To fix:

- Add an **exclusion** in Windows Security → Virus & threat protection → Exclusions for `C:\git\dr\poc` or at least `C:\git\dr\poc\server\artifacts`.
- If you use **Controlled Folder Access**, allow your dev tools or disable it for development.
- Child processes are started with `--no-launch-profile` by Aspire on purpose (ports and config are injected by AppHost); the block is due to the artifacts path, not the launch profile.

---

## Production Deployment

For production, use **Azure Key Vault**, **AWS Secrets Manager**, or **environment variables** instead of user secrets.

**Example with Azure Key Vault:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVault(
        new Uri("https://your-keyvault.vault.azure.net/"),
        new DefaultAzureCredential());
}

var postgresUsername = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
```

---

## Project Structure

```
AppHost/
├── Program.cs                          # Aspire orchestration
├── AppHost.csproj                      # Project file (contains UserSecretsId)
├── scripts/
│   ├── setup-dev-environment.sh        # Setup script (macOS/Linux)
│   └── setup-dev-environment.ps1       # Setup script (Windows)
└── README.md                           # This file
```

---

## Common Commands

```bash
# Run setup script
./scripts/setup-dev-environment.sh

# Run database migrations (once, or after schema changes)
dotnet run --project server/src/MigrationRunner

# Start AppHost (use launch profile so appsettings.Development.json and user secrets load)
dotnet run --launch-profile https

# Start AppHost with a specific topology (use -- before --topology so the arg is passed to the app)
dotnet run --launch-profile https -- --topology Microservices

# View secrets
dotnet user-secrets list

# Set a secret
dotnet user-secrets set "Parameters:key" "value"

# Clean up containers
docker stop $(docker ps -q)
docker rm $(docker ps -aq)

# Clean up volumes (deletes data)
docker volume rm datarizen-postgres-data
```

---

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [User Secrets Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [FluentMigrator Documentation](https://fluentmigrator.github.io/)
