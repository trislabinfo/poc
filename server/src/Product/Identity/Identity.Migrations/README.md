# Identity Module - Database Migrations

## Migration Organization

### Schema Migrations (`Migrations/Schema/`)
DDL migrations that create/modify database structure:
- Tables
- Indexes
- Constraints
- Schema changes

**Naming**: `YYYYMMDDHHmmss_DescriptiveName.cs`

### Data Migrations (`Migrations/Data/`)
DML migrations that insert/update data from **JSON files** in `SeedData/`:
- `SeedRolesAndPermissions` – loads `SeedData/Common/roles.json` and `permissions.json`
- `SeedUsers` – loads `SeedData/{Environment}/users.json` (Development, Staging, Production)

**Naming**: `YYYYMMDD2HHmmss_DescriptiveName.cs` (note the "2" prefix for data migrations)

### Seed Data (`SeedData/`)
- **Common/** – `roles.json`, `permissions.json` (shared across all environments)
- **Development/** – `users.json` (dev-only users)
- **Staging/** – `users.json` (optional)
- **Production/** – `users.json` (optional; usually empty)

Seed data is loaded via **BuildingBlocks.Migrations.SeedDataLoader** (shared by all module migration projects).

## FluentMigrator Version Table

FluentMigrator **automatically creates** `identity.__FluentMigrator_VersionInfo` when you run the first migration.

**Structure**:
```sql
CREATE TABLE identity.__FluentMigrator_VersionInfo (
    Version BIGINT PRIMARY KEY,
    AppliedOn TIMESTAMP,
    Description VARCHAR(1024)
);
```

## Running Migrations

Module list comes from `appsettings.json` / `Deployment:Topology`. Set the environment so the correct seed data is loaded:

```bash
# Development (loads SeedData/Development/users.json)
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project server/src/MigrationRunner
```

Or use `DOTNET_ENVIRONMENT`:

```bash
set DOTNET_ENVIRONMENT=Development
dotnet run --project server/src/MigrationRunner
```

- **Development** – seeds dev users from `SeedData/Development/users.json`
- **Staging** – seeds from `SeedData/Staging/users.json` (if any)
- **Production** – seeds from `SeedData/Production/users.json` (typically empty)

## Schema

**Schema Name**: `identity`

**Tables** (match Domain Model):
- `users` - User aggregate root
- `roles` - Role entity
- `permissions` - Permission entity
- `user_roles` - Many-to-many junction
- `role_permissions` - Many-to-many junction
