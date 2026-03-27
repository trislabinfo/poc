# MigrationRunner

Centralized migration orchestration: runs FluentMigrator migrations for all modules in dependency order.

## Purpose

- Discover which modules to run based on **Deployment:Topology** (Monolith, DistributedApp, Microservices).
- Resolve **dependency order** via each module’s `GetMigrationDependencies()` (topological sort).
- Run migrations **per module** in that order against a single PostgreSQL database.

## Configuration

- **appsettings.json**:
  - `ConnectionStrings:DefaultConnection` – PostgreSQL connection string.
  - `Deployment:Topology` – selects which topology’s module list to use.
  - `MigrationRunner:ModulesByTopology` – map of topology name to list of module names.

- **Postgres in Docker**: When Postgres runs in Docker, the host port may differ from 5432 (e.g. `127.0.0.1:51681->5432/tcp`). Either:
  - Use **appsettings.Development.json** with `Port=51681` (or your current port) and run with `DOTNET_ENVIRONMENT=Development`, or
  - Set **PGPORT**: `$env:PGPORT=51681` (PowerShell) / `export PGPORT=51681` (bash) then run the runner. Get the port from `docker ps`.

## Usage

```bash
# Dry run (report only, no DB changes)
dotnet run --project server/src/MigrationRunner -- --dry-run

# Run migrations for current topology
dotnet run --project server/src/MigrationRunner

# Override topology
dotnet run --project server/src/MigrationRunner -- --topology DistributedApp

# Rollback last migration step per module (reverse order)
dotnet run --project server/src/MigrationRunner -- --rollback

# Rollback dry run
dotnet run --project server/src/MigrationRunner -- --rollback --dry-run
```

## Dependencies

- References all **\*.Migrations** and **\*.Module** projects to resolve assemblies and `GetMigrationDependencies()`.
- Uses **FluentMigrator.Runner** and **FluentMigrator.Runner.Postgres**; each Migrations project can add FluentMigrator migrations that are executed when the runner runs.

## Order

Default order from dependencies: **Tenant** → **Identity** / **Feature** → **User**.
