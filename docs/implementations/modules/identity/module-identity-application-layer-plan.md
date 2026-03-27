# Module: Identity - Migrations Layer Implementation Plan

**Status**: 🆕 New - Migration Layer Implementation  
**Last Updated**: 2025-01-15  
**Estimated Total Time**: ~10 hours  
**Related Documents**: 
- `docs/ai-context/07-DB-MIGRATIONS.md` (Technical implementation)
- `docs/ai-context/07-DB-MIGRATION-FLOW.md` (Operational workflow)
- `docs/implementations/module-identity-application-layer-plan.md` (Domain model reference)

---

## Overview

This plan covers the implementation of the **Migrations Layer** for the Identity module using **FluentMigrator**. The Identity module will have its own schema (`identity`) and manage its database migrations independently from other modules.

### Migration Organization

```
Identity.Migrations/
├── Migrations/
│   ├── Schema/                          # DDL migrations (tables, indexes, constraints)
│   │   ├── 20250115100000_CreateIdentitySchema.cs
│   │   ├── 20250115101000_CreateUsersTable.cs
│   │   ├── 20250115102000_CreateRolesTable.cs
│   │   ├── 20250115103000_CreatePermissionsTable.cs
│   │   ├── 20250115104000_CreateUserRolesTable.cs
│   │   ├── 20250115105000_CreateRolePermissionsTable.cs
│   │   └── 20250115106000_CreateIndexes.cs
│   └── Data/                            # DML migrations (seed data)
│       ├── 20250115200000_SeedDefaultRolesAndPermissions.cs
│       └── 20250115201000_SeedDevelopmentUsers.cs
└── README.md
```

### FluentMigrator Version Table

**When is it created?**
- FluentMigrator **automatically creates** the version table (`__FluentMigrator_VersionInfo`) when you run the **first migration**
- It's created in the **same schema** as your migrations (in our case: `identity` schema)
- You **don't need to create it manually**

**What does it track?**
- `Version` - Migration timestamp (e.g., 20250115100000)
- `AppliedOn` - When the migration was applied
- `Description` - Migration description

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│ PostgreSQL Database: datarizen_dev / datarizen_prod         │
├─────────────────────────────────────────────────────────────┤
│ Schema: identity                                            │
│   ├── Tables (match Domain Model exactly):                 │
│   │   ├── users                                             │
│   │   ├── roles                                             │
│   │   ├── permissions                                       │
│   │   ├── user_roles (junction)                            │
│   │   └── role_permissions (junction)                      │
│   ├── Indexes                                               │
│   └── __FluentMigrator_VersionInfo (auto-created)          │
└─────────────────────────────────────────────────────────────┘
```

---

## Phase 0: Setup Migrations Project (1 hour)

### 0.1: Create Migrations Project (30 minutes)

**Create project**:

```bash
cd server/src/Modules/Identity
dotnet new classlib -n Identity.Migrations
cd Identity.Migrations
mkdir -p Migrations/Schema
mkdir -p Migrations/Data
```

**Update `Identity.Migrations.csproj`**:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Datarizen.Identity.Migrations</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentMigrator" />
    <PackageReference Include="FluentMigrator.Runner" />
  </ItemGroup>

</Project>
```

**Tasks**:
- [ ] Create `Identity.Migrations` project
- [ ] Create `Migrations/Schema` folder
- [ ] Create `Migrations/Data` folder
- [ ] Add FluentMigrator NuGet packages
- [ ] Add project to solution
- [ ] Verify project builds

---

### 0.2: Create README (30 minutes)

**File**: `server/src/Modules/Identity/Identity.Migrations/README.md`

```markdown
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
DML migrations that insert/update data:
- Seed data (roles, permissions)
- Reference data
- Development data (with `[Profile("Development")]`)

**Naming**: `YYYYMMDD2HHmmss_DescriptiveName.cs` (note the "2" prefix for data migrations)

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

### All Environments

```bash
# Run all Identity migrations
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module Identity
```

### Development Only (includes seed data)

```bash
# Run with Development profile (includes dev users)
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module Identity \
  --profile Development
```

### Production (no dev seed data)

```bash
# Run without Development profile (excludes dev users)
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --module Identity
```

## Environment-Specific Seed Data

Use `[Profile("Development")]` attribute for development-only migrations:

```csharp
[Migration(20250115201000, "Seed development users")]
[Profile("Development")] // Only runs when --profile Development is specified
public class SeedDevelopmentUsers : Migration
{
    // ...
}
```

## Schema

**Schema Name**: `identity`

**Tables** (match Domain Model):
- `users` - User aggregate root
- `roles` - Role entity
- `permissions` - Permission entity
- `user_roles` - Many-to-many junction
- `role_permissions` - Many-to-many junction
```

**Tasks**:
- [ ] Create README.md
- [ ] Document migration organization
- [ ] Document FluentMigrator version table
- [ ] Document environment-specific migrations

---

## Phase 1: Schema Migrations (4 hours)

### 1.1: Create Identity Schema (30 minutes)

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Schema/20250115100000_CreateIdentitySchema.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115100000, "Create identity schema")]
public class CreateIdentitySchema : Migration
{
    public override void Up()
    {
        Create.Schema("identity");
    }

    public override void Down()
    {
        Delete.Schema("identity");
    }
}
```

**Tasks**:
- [ ] Create migration file in `Migrations/Schema/`
- [ ] Test migration locally
- [ ] Verify schema created: `\dn` in psql
- [ ] Verify version table created: `SELECT * FROM identity.__FluentMigrator_VersionInfo;`

---

### 1.2: Create Users Table (1 hour)

**Reference Domain Model**: `Identity.Domain/Entities/User.cs`

```csharp
public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public DateTime? EmailConfirmedAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    private readonly List<Role> _roles = new();
}
```

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Schema/20250115101000_CreateUsersTable.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115101000, "Create users table")]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey("pk_users")
            .WithColumn("email").AsString(255).NotNullable()
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("first_name").AsString(100).NotNullable()
            .WithColumn("last_name").AsString(100).NotNullable()
            .WithColumn("is_email_confirmed").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("email_confirmed_at").AsDateTime().Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("last_login_at").AsDateTime().Nullable();

        Create.UniqueConstraint("uq_users_email")
            .OnTable("users")
            .WithSchema("identity")
            .Column("email");
    }

    public override void Down()
    {
        Delete.Table("users").InSchema("identity");
    }
}
```

**Tasks**:
- [ ] Create migration file
- [ ] Match domain model exactly (no extra columns)
- [ ] Add unique constraint on email
- [ ] Test migration
- [ ] Verify table: `\d identity.users`

---

### 1.3: Create Roles Table (30 minutes)

**Reference Domain Model**: `Identity.Domain/Entities/Role.cs`

```csharp
public class Role : Entity<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    private readonly List<Permission> _permissions = new();
}
```

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Schema/20250115102000_CreateRolesTable.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115102000, "Create roles table")]
public class CreateRolesTable : Migration
{
    public override void Up()
    {
        Create.Table("roles")
            .InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey("pk_roles")
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("description").AsString(500).NotNullable();

        Create.UniqueConstraint("uq_roles_name")
            .OnTable("roles")
            .WithSchema("identity")
            .Column("name");
    }

    public override void Down()
    {
        Delete.Table("roles").InSchema("identity");
    }
}
```

**Tasks**:
- [ ] Create migration file
- [ ] Match domain model exactly
- [ ] Add unique constraint on name
- [ ] Test migration

---

### 1.4: Create Permissions Table (30 minutes)

**Reference Domain Model**: `Identity.Domain/Entities/Permission.cs`

```csharp
public class Permission : Entity<Guid>
{
    public string Name { get; private set; }
    public string Resource { get; private set; }
    public string Action { get; private set; }
}
```

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Schema/20250115103000_CreatePermissionsTable.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115103000, "Create permissions table")]
public class CreatePermissionsTable : Migration
{
    public override void Up()
    {
        Create.Table("permissions")
            .InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey("pk_permissions")
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("resource").AsString(100).NotNullable()
            .WithColumn("action").AsString(50).NotNullable();

        Create.UniqueConstraint("uq_permissions_resource_action")
            .OnTable("permissions")
            .WithSchema("identity")
            .Columns("resource", "action");
    }

    public override void Down()
    {
        Delete.Table("permissions").InSchema("identity");
    }
}
```

**Tasks**:
- [ ] Create migration file
- [ ] Match domain model exactly
- [ ] Add unique constraint
- [ ] Test migration

---

### 1.5: Create Junction Tables (1 hour)

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Schema/20250115104000_CreateUserRolesTable.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115104000, "Create user_roles junction table")]
public class CreateUserRolesTable : Migration
{
    public override void Up()
    {
        Create.Table("user_roles")
            .InSchema("identity")
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("role_id").AsGuid().NotNullable();

        Create.PrimaryKey("pk_user_roles")
            .OnTable("user_roles")
            .WithSchema("identity")
            .Columns("user_id", "role_id");
    }

    public override void Down()
    {
        Delete.Table("user_roles").InSchema("identity");
    }
}
```

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Schema/20250115105000_CreateRolePermissionsTable.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115105000, "Create role_permissions junction table")]
public class CreateRolePermissionsTable : Migration
{
    public override void Up()
    {
        Create.Table("role_permissions")
            .InSchema("identity")
            .WithColumn("role_id").AsGuid().NotNullable()
            .WithColumn("permission_id").AsGuid().NotNullable();

        Create.PrimaryKey("pk_role_permissions")
            .OnTable("role_permissions")
            .WithSchema("identity")
            .Columns("role_id", "permission_id");
    }

    public override void Down()
    {
        Delete.Table("role_permissions").InSchema("identity");
    }
}
```

**Tasks**:
- [ ] Create both junction table migrations
- [ ] No foreign keys (schema independence)
- [ ] Test migrations

---

### 1.6: Create Indexes (30 minutes)

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Schema/20250115106000_CreateIndexes.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115106000, "Create indexes for performance")]
public class CreateIndexes : Migration
{
    public override void Up()
    {
        // Users table
        Create.Index("idx_users_email")
            .OnTable("users").InSchema("identity")
            .OnColumn("email").Ascending();

        Create.Index("idx_users_is_active")
            .OnTable("users").InSchema("identity")
            .OnColumn("is_active").Ascending();

        // User roles
        Create.Index("idx_user_roles_user_id")
            .OnTable("user_roles").InSchema("identity")
            .OnColumn("user_id").Ascending();

        Create.Index("idx_user_roles_role_id")
            .OnTable("user_roles").InSchema("identity")
            .OnColumn("role_id").Ascending();

        // Role permissions
        Create.Index("idx_role_permissions_role_id")
            .OnTable("role_permissions").InSchema("identity")
            .OnColumn("role_id").Ascending();

        Create.Index("idx_role_permissions_permission_id")
            .OnTable("role_permissions").InSchema("identity")
            .OnColumn("permission_id").Ascending();

        // Permissions
        Create.Index("idx_permissions_resource")
            .OnTable("permissions").InSchema("identity")
            .OnColumn("resource").Ascending();
    }

    public override void Down()
    {
        Delete.Index("idx_permissions_resource").OnTable("permissions").InSchema("identity");
        Delete.Index("idx_role_permissions_permission_id").OnTable("role_permissions").InSchema("identity");
        Delete.Index("idx_role_permissions_role_id").OnTable("role_permissions").InSchema("identity");
        Delete.Index("idx_user_roles_role_id").OnTable("user_roles").InSchema("identity");
        Delete.Index("idx_user_roles_user_id").OnTable("user_roles").InSchema("identity");
        Delete.Index("idx_users_is_active").OnTable("users").InSchema("identity");
        Delete.Index("idx_users_email").OnTable("users").InSchema("identity");
    }
}
```

**Tasks**:
- [ ] Create migration file
- [ ] Add indexes for frequently queried columns
- [ ] Test migration
- [ ] Verify indexes: `\di identity.*`

---

## Phase 2: Data Migrations (3 hours)

### 2.1: Seed Default Roles and Permissions (2 hours)

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Data/20250115200000_SeedDefaultRolesAndPermissions.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Data;

[Migration(20250115200000, "Seed default roles and permissions")]
public class SeedDefaultRolesAndPermissions : Migration
{
    public override void Up()
    {
        // Seed Permissions
        var permissions = new[]
        {
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "users.read", Resource = "users", Action = "read" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "users.create", Resource = "users", Action = "create" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "users.update", Resource = "users", Action = "update" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "users.delete", Resource = "users", Action = "delete" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000011"), Name = "roles.read", Resource = "roles", Action = "read" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000012"), Name = "roles.create", Resource = "roles", Action = "create" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000013"), Name = "roles.update", Resource = "roles", Action = "update" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000014"), Name = "roles.delete", Resource = "roles", Action = "delete" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000021"), Name = "permissions.read", Resource = "permissions", Action = "read" },
            new { Id = Guid.Parse("10000000-0000-0000-0000-000000000022"), Name = "permissions.assign", Resource = "permissions", Action = "assign" },
        };

        foreach (var permission in permissions)
        {
            Insert.IntoTable("permissions").InSchema("identity")
                .Row(new
                {
                    id = permission.Id,
                    name = permission.Name,
                    resource = permission.Resource,
                    action = permission.Action
                });
        }

        // Seed Roles
        var adminRoleId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var userRoleId = Guid.Parse("20000000-0000-0000-0000-000000000002");

        Insert.IntoTable("roles").InSchema("identity")
            .Row(new
            {
                id = adminRoleId,
                name = "Admin",
                description = "System administrator with full access"
            })
            .Row(new
            {
                id = userRoleId,
                name = "User",
                description = "Standard user with basic access"
            });

        // Assign all permissions to Admin role
        foreach (var permission in permissions)
        {
            Insert.IntoTable("role_permissions").InSchema("identity")
                .Row(new
                {
                    role_id = adminRoleId,
                    permission_id = permission.Id
                });
        }

        // Assign read-only permissions to User role
        var userPermissions = new[]
        {
            Guid.Parse("10000000-0000-0000-0000-000000000001"), // users.read
            Guid.Parse("10000000-0000-0000-0000-000000000011"), // roles.read
            Guid.Parse("10000000-0000-0000-0000-000000000021"), // permissions.read
        };

        foreach (var permissionId in userPermissions)
        {
            Insert.IntoTable("role_permissions").InSchema("identity")
                .Row(new
                {
                    role_id = userRoleId,
                    permission_id = permissionId
                });
        }
    }

    public override void Down()
    {
        Delete.FromTable("role_permissions").InSchema("identity").AllRows();
        Delete.FromTable("roles").InSchema("identity").AllRows();
        Delete.FromTable("permissions").InSchema("identity").AllRows();
    }
}
```

**Tasks**:
- [ ] Create migration file in `Migrations/Data/`
- [ ] Seed permissions (users, roles, permissions CRUD)
- [ ] Seed roles (Admin, User)
- [ ] Assign permissions to roles
- [ ] Test migration
- [ ] Verify data: `SELECT * FROM identity.roles;`

---

### 2.2: Seed Development Users (1 hour)

**File**: `server/src/Modules/Identity/Identity.Migrations/Migrations/Data/20250115201000_SeedDevelopmentUsers.cs`

```csharp
using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Data;

[Migration(20250115201000, "Seed development users")]
[Profile("Development")] // ⭐ Only runs when --profile Development is specified
public class SeedDevelopmentUsers : Migration
{
    public override void Up()
    {
        // Admin user
        var adminUserId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var adminRoleId = Guid.Parse("20000000-0000-0000-0000-000000000001");

        Insert.IntoTable("users").InSchema("identity")
            .Row(new
            {
                id = adminUserId,
                email = "admin@dev.local",
                password_hash = "$2a$11$XKV8Z8qF5Z5Z5Z5Z5Z5Z5uO", // BCrypt: "Admin123!"
                first_name = "Admin",
                last_name = "User",
                is_email_confirmed = true,
                email_confirmed_at = DateTime.UtcNow,
                is_active = true,
                created_at = DateTime.UtcNow,
                last_login_at = (DateTime?)null
            });

        Insert.IntoTable("user_roles").InSchema("identity")
            .Row(new { user_id = adminUserId, role_id = adminRoleId });

        // Regular user
        var regularUserId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var userRoleId = Guid.Parse("20000000-0000-0000-0000-000000000002");

        Insert.IntoTable("users").InSchema("identity")
            .Row(new
            {
                id = regularUserId,
                email = "user@dev.local",
                password_hash = "$2a$11$XKV8Z8qF5Z5Z5Z5Z5Z5Z5uO", // BCrypt: "User123!"
                first_name = "Regular",
                last_name = "User",
                is_email_confirmed = true,
                email_confirmed_at = DateTime.UtcNow,
                is_active = true,
                created_at = DateTime.UtcNow,
                last_login_at = (DateTime?)null
            });

        Insert.IntoTable("user_roles").InSchema("identity")
            .Row(new { user_id = regularUserId, role_id = userRoleId });
    }

    public override void Down()
    {
        Delete.FromTable("user_roles").InSchema("identity")
            .Row(new { user_id = Guid.Parse("30000000-0000-0000-0000-000000000001") });
        Delete.FromTable("user_roles").InSchema("identity")
            .Row(new { user_id = Guid.Parse("30000000-0000-0000-0000-000000000002") });
        Delete.FromTable("users").InSchema("identity")
            .Row(new { id = Guid.Parse("30000000-0000-0000-0000-000000000001") });
        Delete.FromTable("users").InSchema("identity")
            .Row(new { id = Guid.Parse("30000000-0000-0000-0000-000000000002") });
    }
}
```

**Tasks**:
- [ ] Create migration file in `Migrations/Data/`
- [ ] Add `[Profile("Development")]` attribute
- [ ] Seed admin user (admin@dev.local)
- [ ] Seed regular user (user@dev.local)
- [ ] Test with `--profile Development`
- [ ] Verify users NOT created without profile flag

---

## Phase 3: Testing and Validation (2 hours)

### 3.1: Local Testing (1.5 hours)

**Run all migrations (without dev users)**:

```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module Identity \
  --verbose
```

**Run with development users**:

```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module Identity \
  --profile Development \
  --verbose
```

**Verify schema**:

```bash
psql -h localhost -U datarizen -d datarizen_dev

-- Verify version table exists
SELECT * FROM identity.__FluentMigrator_VersionInfo ORDER BY version;

-- Verify tables
\dt identity.*

-- Verify seed data
SELECT * FROM identity.roles;
SELECT * FROM identity.permissions;
SELECT COUNT(*) FROM identity.users; -- Should be 0 without --profile Development
```

**Test rollback**:

```bash
# Rollback last migration
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module Identity \
  --rollback 1

# Re-apply
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module Identity \
  --profile Development
```

**Tasks**:
- [ ] Run migrations without profile
- [ ] Verify no dev users created
- [ ] Run migrations with `--profile Development`
- [ ] Verify dev users created
- [ ] Test rollback
- [ ] Verify version table tracks all migrations

---

### 3.2: Documentation (30 minutes)

**Update module README**:

**File**: `server/src/Modules/Identity/README.md`

```markdown
# Identity Module

## Database Schema

**Schema**: `identity`

### Tables (match Domain Model)

- **users** - User aggregate root
- **roles** - Role entity
- **permissions** - Permission entity
- **user_roles** - Many-to-many junction
- **role_permissions** - Many-to-many junction

### Default Roles

- **Admin** - Full system access (all permissions)
- **User** - Basic user access (read-only permissions)

### Default Permissions

- `users.read`, `users.create`, `users.update`, `users.delete`
- `roles.read`, `roles.create`, `roles.update`, `roles.delete`
- `permissions.read`, `permissions.assign`

### Development Users (Development environment only)

- **admin@dev.local** / Admin123! (Admin role)
- **user@dev.local** / User123! (User role)

## Running Migrations

### Production (no dev users)

```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --module Identity
```

### Development (with dev users)

```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module Identity \
  --profile Development
```

## Migration Version Table

FluentMigrator automatically creates `identity.__FluentMigrator_VersionInfo` to track applied migrations.

```sql
SELECT * FROM identity.__FluentMigrator_VersionInfo ORDER BY version;
```


        // Add to response headers
        context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

        // Add to Serilog LogContext (for structured logging)
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Add to Activity (for OpenTelemetry)
            Activity.Current?.SetTag("correlation_id", correlationId);

            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}
```

**Tasks**:
- [ ] Create CorrelationIdMiddleware
- [ ] Register middleware in Program.cs
- [ ] Verify correlation ID in logs
- [ ] Verify correlation ID in response headers
- [ ] Add unit tests

---

#### 0.7.2: Serilog Configuration & Enrichment (30 minutes) - BEHIND ABSTRACTION

**File**: `BuildingBlocks.Web/Extensions/SerilogExtensions.cs`

```csharp
using Serilog;
using Microsoft.AspNetCore.Builder;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "Datarizen")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        });

        return builder;
    }
}
```

**File**: `appsettings.json` (Serilog configuration)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    }
  }
}
```

**Tasks**:
- [ ] Add Serilog packages to BuildingBlocks.Web
- [ ] Create SerilogExtensions.AddStructuredLogging()
- [ ] Create IStructuredLogger abstraction (already done in 0.2.2)
- [ ] Create SerilogStructuredLogger implementation (already done in 0.2.2)
- [ ] Configure Serilog in appsettings.json
- [ ] Update Program.cs to call AddStructuredLogging()
- [ ] Verify structured logging works

**Success Criteria**:
- ✅ All logs are structured (JSON format)
- ✅ Logs include correlation ID
- ✅ Application code NEVER references Serilog directly (uses IStructuredLogger)
- ✅ Can replace Serilog with NLog by changing ONE file

---

#### 0.7.3: Security Audit Logging (1.5 hours)

**File**: `BuildingBlocks.Application/Auditing/SecurityAuditLogger.cs`

```csharp
using Datarizen.BuildingBlocks.Application.Logging;
using Microsoft.AspNetCore.Http;

namespace Datarizen.BuildingBlocks.Application.Auditing;

/// <summary>
/// Logs security-related events (login, logout, permission changes, etc.).
/// Uses IStructuredLogger (our abstraction).
/// </summary>
public sealed class SecurityAuditLogger
{
    private readonly IStructuredLogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityAuditLogger(
        IStructuredLogger logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void LogLoginSuccess(Guid userId, string email)
    {
        _logger.LogInformation(
            "User login successful: {UserId} ({Email}) from {IpAddress}",
            userId,
            email,
            GetIpAddress());
    }

    public void LogLoginFailure(string email, string reason)
    {
        _logger.LogWarning(
            "User login failed: {Email} from {IpAddress}. Reason: {Reason}",
            email,
            GetIpAddress(),
            reason);
    }

    public void LogPermissionChange(Guid userId, string permission, bool granted)
    {
        _logger.LogInformation(
            "Permission {Action} for user {UserId}: {Permission}",
            granted ? "granted" : "revoked",
            userId,
            permission);
    }

    private string? GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
```

**Tasks**:
- [ ] Create SecurityAuditLogger
- [ ] Add security audit logging to login/logout handlers
- [ ] Add security audit logging to permission changes
- [ ] Register in DI container
- [ ] Add unit tests

---

#### 0.7.4: Metrics Behavior (1 hour)

**File**: `BuildingBlocks.Application/Behaviors/MetricsBehavior.cs`

```csharp
using MediatR;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Datarizen.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that records metrics for requests.
/// Uses System.Diagnostics.Metrics (OpenTelemetry standard).
/// NO EXTERNAL LIBRARY NEEDED.
/// </summary>
public sealed class MetricsBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Meter Meter = new("Datarizen.Application");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("requests.total");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("requests.duration");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Record success metrics
            RequestCounter.Add(1, new KeyValuePair<string, object?>("request", requestName), new KeyValuePair<string, object?>("status", "success"));
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("request", requestName));

            return response;
        }
        catch
        {
            stopwatch.Stop();

            // Record failure metrics
            RequestCounter.Add(1, new KeyValuePair<string, object?>("request", requestName), new KeyValuePair<string, object?>("status", "failure"));
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("request", requestName));

            throw;
        }
    }
}
```

**Tasks**:
- [ ] Create MetricsBehavior
- [ ] Register in DI container
- [ ] Configure OpenTelemetry to export metrics
- [ ] Verify metrics in Aspire dashboard
- [ ] Add unit tests

---

#### 0.7.5: Error Tracking Integration (30 minutes) - SENTRY SELF-HOSTED

**Decision**: Use **Sentry (self-hosted)** for error tracking.

**Why Sentry?**
- ✅ MIT License (100% free)
- ✅ Best-in-class error tracking
- ✅ Easy to self-host with Docker
- ✅ Excellent .NET SDK
- ✅ Behind `IErrorTracker` abstraction (easy to replace)

**Alternatives Considered**:
- **Rollbar**: Similar features, but SaaS pricing
- **Application Insights**: Azure lock-in
- **Elastic APM**: Requires Elasticsearch infrastructure
- **Seq**: Limited error tracking features
- **Exceptionless**: Smaller community, less mature

**Self-Hosting Sentry**:

```yaml
# docker-compose.yml (add to AppHost or separate infrastructure)
version: '3.8'
services:
  sentry:
    image: sentry:latest
    ports:
      - "9000:9000"
    environment:
      SENTRY_SECRET_KEY: "your-secret-key"
      SENTRY_POSTGRES_HOST: postgres
      SENTRY_REDIS_HOST: redis
    depends_on:
      - postgres
      - redis
```

**Configuration**:

```json
{
  "Sentry": {
    "Dsn": "http://your-self-hosted-sentry:9000/1"
  }
}
```

**File**: `BuildingBlocks.Application/ErrorTracking/IErrorTracker.cs` (NEW ABSTRACTION)

```csharp
namespace Datarizen.BuildingBlocks.Application.ErrorTracking;

/// <summary>
/// Abstraction over error tracking service.
/// Allows replacing Sentry with Raygun, Application Insights, etc.
/// </summary>
public interface IErrorTracker
{
    /// <summary>
    /// Capture an exception and send it to the error tracking service.
    /// </summary>
    void CaptureException(Exception exception, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Capture a message (non-exception error).
    /// </summary>
    void CaptureMessage(string message, Dictionary<string, string>? tags = null);
}
```

**File**: `BuildingBlocks.Infrastructure/ErrorTracking/SentryErrorTracker.cs` (IMPLEMENTATION)

```csharp
using Sentry;
using Datarizen.BuildingBlocks.Application.ErrorTracking;

namespace Datarizen.BuildingBlocks.Infrastructure.ErrorTracking;

/// <summary>
/// Sentry implementation of IErrorTracker.
/// Can be replaced with Raygun, Application Insights, etc.
/// </summary>
internal sealed class SentryErrorTracker : IErrorTracker
{
    public void CaptureException(Exception exception, Dictionary<string, string>? tags = null)
    {
        SentrySdk.CaptureException(exception, scope =>
        {
            if (tags is not null)
            {
                foreach (var tag in tags)
                {
                    scope.SetTag(tag.Key, tag.Value);
                }
            }
        });
    }

    public void CaptureMessage(string message, Dictionary<string, string>? tags = null)
    {
        SentrySdk.CaptureMessage(message, scope =>
        {
            if (tags is not null)
            {
                foreach (var tag in tags)
                {
                    scope.SetTag(tag.Key, tag.Value);
                }
            }
        });
    }
}
```

**File**: `BuildingBlocks.Web/Extensions/ErrorTrackingExtensions.cs`

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using Datarizen.BuildingBlocks.Application.ErrorTracking;
using Datarizen.BuildingBlocks.Infrastructure.ErrorTracking;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class ErrorTrackingExtensions
{
    public static WebApplicationBuilder AddErrorTracking(this WebApplicationBuilder builder)
    {
        // Register Sentry (implementation detail)
        builder.WebHost.UseSentry(options =>
        {
            options.Dsn = builder.Configuration["Sentry:Dsn"];
            options.Environment = builder.Environment.EnvironmentName;
            options.TracesSampleRate = 1.0;
            options.AttachStacktrace = true;
            options.SendDefaultPii = false;
        });

        // Register our abstraction
        builder.Services.AddSingleton<IErrorTracker, SentryErrorTracker>();

        return builder;
    }
}
```

**Usage Example**:

```csharp
// Application code ONLY uses IErrorTracker (our abstraction)
public class SomeCommandHandler
{
    private readonly IErrorTracker _errorTracker;

    public async Task<Result> Handle(SomeCommand request, CancellationToken ct)
    {
        try
        {
            // ... business logic
        }
        catch (Exception ex)
        {
            _errorTracker.CaptureException(ex, new Dictionary<string, string>
            {
                ["TenantId"] = request.TenantId.ToString(),
                ["UserId"] = request.UserId.ToString()
            });
            throw;
        }
    }
}
```

**Tasks**:
- [ ] Create IErrorTracker abstraction
- [ ] Create SentryErrorTracker implementation
- [ ] Add Sentry to docker-compose.yml (self-hosted)
- [ ] Configure Sentry DSN in appsettings.json
- [ ] Update Program.cs to call AddErrorTracking()
- [ ] Add unit tests (mock IErrorTracker)

**Success Criteria**:
- ✅ Application code NEVER references Sentry directly
- ✅ Can replace Sentry with Raygun/AppInsights by changing ONE file
- ✅ Errors include correlation ID, tenant ID, user ID
- ✅ Can view errors in self-hosted Sentry dashboard
- ✅ No SaaS costs

---

## Summary: All External Libraries Behind Abstractions

| Library | Our Abstraction | Implementation | Can Replace With |
|---------|----------------|----------------|------------------|
| **Serilog** | `IStructuredLogger` | `SerilogStructuredLogger` | NLog, Microsoft.Extensions.Logging |
| **Hangfire** | `IBackgroundJobScheduler` | `HangfireBackgroundJobScheduler` | Quartz.NET, Azure Functions, AWS Lambda |
| **Sentry** | `IErrorTracker` | `SentryErrorTracker` | Raygun, Application Insights, Rollbar |
| **FluentValidation** | `IValidator<T>` | FluentValidation validators | Custom validators, DataAnnotations |
| **EF Core** | `IUnitOfWork`, `IRepository<T>` | EF Core implementations | Dapper, NHibernate, ADO.NET |

**NO AutoMapper** - We create our own mappers for full control.

---

## Updated Timeline

**Phase 0: BuildingBlocks Enhancement**
- 0.1: ASP.NET Middleware: 3 hours
- 0.2: MediatR Behaviors: 2 hours
- 0.3: Specification Pattern: 1.5 hours
- 0.4: Background Jobs (Hangfire behind abstraction): 30 minutes
- 0.5: Feature Flags (abstraction only): 30 minutes
- 0.6: Health Checks: 30 minutes
- 0.7: Minimum Viable Observability: 3.5 hours

**Total Phase 0: ~11.5 hours (~1.5 days)**

**Phase 1: Identity Application Layer: ~15.5 hours (~2 days)**

**Grand Total (Critical Path): ~27 hours (~3.5 days)**

---

## Key Benefits

1. ✅ **Vendor Independence**: Can replace ANY library by changing ONE file
2. ✅ **Testability**: Easy to mock abstra
}
```

**Tasks**:
- [ ] Create ISpecification interface
- [ ] Create BaseSpecification class
- [ ] Create SpecificationEvaluator
- [ ] Update IRepository to accept ISpecification
- [ ] Add XML documentation
- [ ] Add unit tests

---

### 0.4: Hangfire Background Jobs (30 minutes) - SIMPLIFIED

**Location**: `BuildingBlocks.Web/Extensions/`

**Purpose**: Production-ready background job scheduler using Hangfire.

#### 0.4.1: Hangfire Configuration

**File**: `BuildingBlocks.Web/Extensions/HangfireExtensions.cs`

```csharp
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class HangfireExtensions
{
    public static WebApplicationBuilder AddHangfire(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");

        builder.Services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                });
        });

        builder.Services.AddHangfireServer();

        return builder;
    }

    public static WebApplication UseHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard("/admin/jobs", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });

        return app;
    }
}

internal class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // TODO: Add real authorization (check if user is admin)
        return true;
    }
}
```

**Usage Example**:

```csharp
// Application code ONLY uses IBackgroundJobScheduler (our abstraction)
public class SendWelcomeEmailCommandHandler
{
    private readonly IBackgroundJobScheduler _jobScheduler;

    public async Task<Result> Handle(SendWelcomeEmailCommand request, CancellationToken ct)
    {
        // Enqueue email sending job
        _jobScheduler.Enqueue<IEmailService>(x => x.SendWelcomeEmailAsync(request.UserId));
        
        return Result.Success();
    }
}
```

**Tasks**:
- [ ] Create IBackgroundJobScheduler abstraction
- [ ] Create HangfireBackgroundJobScheduler implementation
- [ ] Create HangfireExtensions.AddBackgroundJobs()
- [ ] Update Program.cs to call AddBackgroundJobs()
- [ ] Configure Hangfire dashboard (/admin/jobs)
- [ ] Add authorization to dashboard
- [ ] Document migration path to Quartz.NET
- [ ] Add unit tests (mock IBackgroundJobScheduler)

**Success Criteria**:
- ✅ Application code NEVER references Hangfire directly
- ✅ Can replace Hangfire with Quartz.NET by changing ONE file
- ✅ Can enqueue, schedule, and create recurring jobs
- ✅ Dashboard is accessible at /admin/jobs
- ✅ Jobs are persisted in PostgreSQL

---

### 0.5: Feature Flags (BuildingBlocks.Application) (30 minutes) - ABSTRACTION ONLY

**Location**: `BuildingBlocks.Application/FeatureFlags/`

**Purpose**: Gradual rollouts, A/B testing, feature toggles.

#### 0.5.1: IFeatureFlagService Abstraction

**File**: `BuildingBlocks.Application/FeatureFlags/IFeatureFlagService.cs`

```csharp
namespace Datarizen.BuildingBlocks.Application.FeatureFlags;

/// <summary>
/// Abstraction over feature flag service.
/// Allows replacing implementation (LaunchDarkly, Azure App Configuration, custom database, etc.).
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Check if a feature is enabled for the current user/tenant.
    /// </summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a feature is enabled for a specific user.
    /// </summary>
    Task<bool> IsEnabledForUserAsync(string featureName, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a feature is enabled for a specific tenant.
    /// </summary>
    Task<bool> IsEnabledForTenantAsync(string featureName, Guid tenantId, CancellationToken cancellationToken = default);
}
```

**File**: `BuildingBlocks.Infrastructure/FeatureFlags/InMemoryFeatureFlagService.cs` (SIMPLE IMPLEMENTATION)

```csharp
using Datarizen.BuildingBlocks.Application.FeatureFlags;
using Microsoft.Extensions.Configuration;

namespace Datarizen.BuildingBlocks.Infrastructure.FeatureFlags;

/// <summary>
/// Simple in-memory implementation of IFeatureFlagService.
/// Reads feature flags from appsettings.json.
/// Can be replaced with LaunchDarkly, Azure App Configuration, etc.
/// </summary>
internal sealed class InMemoryFeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;

    public InMemoryFeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var isEnabled = _configuration.GetValue<bool>($"FeatureFlags:{featureName}");
        return Task.FromResult(isEnabled);
    }

    public Task<bool> IsEnabledForUserAsync(string featureName, Guid userId, CancellationToken cancellationToken = default)
    {
        // Simple implementation: same as IsEnabledAsync
        // Can be enhanced to check user-specific flags
        return IsEnabledAsync(featureName, cancellationToken);
    }

    public Task<bool> IsEnabledForTenantAsync(string featureName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Simple implementation: same as IsEnabledAsync
        // Can be enhanced to check tenant-specific flags
        return IsEnabledAsync(featureName, cancellationToken);
    }
}
```

**File**: `appsettings.json` (configuration)

```json
{
  "FeatureFlags": {
    "NewDashboard": true,
    "AdvancedReporting": false,
    "BetaFeatures": false
  }
}
```

**Tasks**:
- [ ] Create IFeatureFlagService abstraction
- [ ] Create InMemoryFeatureFlagService implementation
- [ ] Document how to replace with LaunchDarkly
- [ ] Register in DI container
- [ ] Add unit tests

**Future Enhancement**: Replace with LaunchDarkly by creating `LaunchDarklyFeatureFlagService` implementation.

---

### 0.6: Health Checks (BuildingBlocks.Web) (30 minutes)

**Location**: `BuildingBlocks.Web/HealthChecks/`

**Purpose**: Monitor application health (database, cache, message queue).

**NOTE**: Uses built-in `Microsoft.Extensions.Diagnostics.HealthChecks` (no external library needed).

#### 0.6.1: Custom Health Checks

**File**: `BuildingBlocks.Web/HealthChecks/DatabaseHealthCheck.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Datarizen.BuildingBlocks.Web.HealthChecks;

/// <summary>
/// Health check for PostgreSQL database connectivity.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public DatabaseHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable", ex);
        }
    }
}
```

**File**: `BuildingBlocks.Web/Extensions/HealthCheckExtensions.cs`

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Datarizen.BuildingBlocks.Web.HealthChecks;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");

        builder.Services.AddHealthChecks()
            .AddCheck("database", new DatabaseHealthCheck(connectionString))
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"));

        return builder;
    }

    public static WebApplication UseHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        return app;
    }
}
```

**Tasks**:
- [ ] Create DatabaseHealthCheck
- [ ] Create HealthCheckExtensions
- [ ] Register health checks in Program.cs
- [ ] Add /health endpoint
- [ ] Test health check responses

---

### 0.7: Minimum Viable Observability (3.5 hours)

#### 0.7.1: Correlation ID Middleware (45 minutes)

**File**: `BuildingBlocks.Web/Middleware/CorrelationIdMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Datarizen.BuildingBlocks.Web.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID for distributed tracing.
/// Uses System.Diagnostics.Activity (OpenTelemetry standard).
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add to response headers
        context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

        // Add to Serilog LogContext (for structured logging)
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Add to Activity (for OpenTelemetry)
            Activity.Current?.SetTag("correlation_id", correlationId);

            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}
```

**Tasks**:
- [ ] Create CorrelationIdMiddleware
- [ ] Register middleware in Program.cs
- [ ] Verify correlation ID in logs
- [ ] Verify correlation ID in response headers
- [ ] Add unit tests

---

#### 0.7.2: Serilog Configuration & Enrichment (30 minutes) - BEHIND ABSTRACTION

**File**: `BuildingBlocks.Web/Extensions/SerilogExtensions.cs`

```csharp
using Serilog;
using Microsoft.AspNetCore.Builder;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "Datarizen")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        });

        return builder;
    }
}
```

**File**: `appsettings.json` (Serilog configuration)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    }
  }
}
```

**Tasks**:
- [ ] Add Serilog packages to BuildingBlocks.Web
- [ ] Create SerilogExtensions.AddStructuredLogging()
- [ ] Create IStructuredLogger abstraction (already done in 0.2.2)
- [ ] Create SerilogStructuredLogger implementation (already done in 0.2.2)
- [ ] Configure Serilog in appsettings.json
- [ ] Update Program.cs to call AddStructuredLogging()
- [ ] Verify structured logging works

**Success Criteria**:
- ✅ All logs are structured (JSON format)
- ✅ Logs include correlation ID
- ✅ Application code NEVER references Serilog directly (uses IStructuredLogger)
- ✅ Can replace Serilog with NLog by changing ONE file

---

#### 0.7.3: Security Audit Logging (1.5 hours)

**File**: `BuildingBlocks.Application/Auditing/SecurityAuditLogger.cs`

```csharp
using Datarizen.BuildingBlocks.Application.Logging;
using Microsoft.AspNetCore.Http;

namespace Datarizen.BuildingBlocks.Application.Auditing;

/// <summary>
/// Logs security-related events (login, logout, permission changes, etc.).
/// Uses IStructuredLogger (our abstraction).
/// </summary>
public sealed class SecurityAuditLogger
{
    private readonly IStructuredLogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityAuditLogger(
        IStructuredLogger logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void LogLoginSuccess(Guid userId, string email)
    {
        _logger.LogInformation(
            "User login successful: {UserId} ({Email}) from {IpAddress}",
            userId,
            email,
            GetIpAddress());
    }

    public void LogLoginFailure(string email, string reason)
    {
        _logger.LogWarning(
            "User login failed: {Email} from {IpAddress}. Reason: {Reason}",
            email,
            GetIpAddress(),
            reason);
    }

    public void LogPermissionChange(Guid userId, string permission, bool granted)
    {
        _logger.LogInformation(
            "Permission {Action} for user {UserId}: {Permission}",
            granted ? "granted" : "revoked",
            userId,
            permission);
    }

    private string? GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
```

**Tasks**:
- [ ] Create SecurityAuditLogger
- [ ] Add security audit logging to login/logout handlers
- [ ] Add security audit logging to permission changes
- [ ] Register in DI container
- [ ] Add unit tests

---

#### 0.7.4: Metrics Behavior (1 hour)

**File**: `BuildingBlocks.Application/Behaviors/MetricsBehavior.cs`

```csharp
using MediatR;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Datarizen.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that records metrics for requests.
/// Uses System.Diagnostics.Metrics (OpenTelemetry standard).
/// NO EXTERNAL LIBRARY NEEDED.
/// </summary>
public sealed class MetricsBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Meter Meter = new("Datarizen.Application");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("requests.total");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("requests.duration");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Record success metrics
            RequestCounter.Add(1, new KeyValuePair<string, object?>("request", requestName), new KeyValuePair<string, object?>("status", "success"));
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("request", requestName));

            return response;
        }
        catch
        {
            stopwatch.Stop();

            // Record failure metrics
            RequestCounter.Add(1, new KeyValuePair<string, object?>("request", requestName), new KeyValuePair<string, object?>("status", "failure"));
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("request", requestName));

            throw;
        }
    }
}
```

**Tasks**:
- [ ] Create MetricsBehavior
- [ ] Register in DI container
- [ ] Configure OpenTelemetry to export metrics
- [ ] Verify metrics in Aspire dashboard
- [ ] Add unit tests

---

#### 0.7.5: Error Tracking Integration (30 minutes) - SENTRY SELF-HOSTED

**Decision**: Use **Sentry (self-hosted)** for error tracking.

**Why Sentry?**
- ✅ MIT License (100% free)
- ✅ Best-in-class error tracking
- ✅ Easy to self-host with Docker
- ✅ Excellent .NET SDK
- ✅ Behind `IErrorTracker` abstraction (easy to replace)

**Alternatives Considered**:
- **Rollbar**: Similar features, but SaaS pricing
- **Application Insights**: Azure lock-in
- **Elastic APM**: Requires Elasticsearch infrastructure
- **Seq**: Limited error tracking features
- **Exceptionless**: Smaller community, less mature

**Self-Hosting Sentry**:

```yaml
# docker-compose.yml (add to AppHost or separate infrastructure)
version: '3.8'
services:
  sentry:
    image: sentry:latest
    ports:
      - "9000:9000"
    environment:
      SENTRY_SECRET_KEY: "your-secret-key"
      SENTRY_POSTGRES_HOST: postgres
      SENTRY_REDIS_HOST: redis
    depends_on:
      - postgres
      - redis
```

**Configuration**:

```json
{
  "Sentry": {
    "Dsn": "http://your-self-hosted-sentry:9000/1"
  }
}
```

**File**: `BuildingBlocks.Application/ErrorTracking/IErrorTracker.cs` (NEW ABSTRACTION)

```csharp
namespace Datarizen.BuildingBlocks.Application.ErrorTracking;

/// <summary>
/// Abstraction over error tracking service.
/// Allows replacing Sentry with Raygun, Application Insights, etc.
/// </summary>
public interface IErrorTracker
{
    /// <summary>
    /// Capture an exception and send it to the error tracking service.
    /// </summary>
    void CaptureException(Exception exception, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Capture a message (non-exception error).
    /// </summary>
    void CaptureMessage(string message, Dictionary<string, string>? tags = null);
}
```

**File**: `BuildingBlocks.Infrastructure/ErrorTracking/SentryErrorTracker.cs` (IMPLEMENTATION)

```csharp
using Sentry;
using Datarizen.BuildingBlocks.Application.ErrorTracking;

namespace Datarizen.BuildingBlocks.Infrastructure.ErrorTracking;

/// <summary>
/// Sentry implementation of IErrorTracker.
/// Can be replaced with Raygun, Application Insights, etc.
/// </summary>
internal sealed class SentryErrorTracker : IErrorTracker
{
    public void CaptureException(Exception exception, Dictionary<string, string>? tags = null)
    {
        SentrySdk.CaptureException(exception, scope =>
        {
            if (tags is not null)
            {
                foreach (var tag in tags)
                {
                    scope.SetTag(tag.Key, tag.Value);
                }
            }
        });
    }

    public void CaptureMessage(string message, Dictionary<string, string>? tags = null)
    {
        SentrySdk.CaptureMessage(message, scope =>
        {
            if (tags is not null)
            {
                foreach (var tag in tags)
                {
                    scope.SetTag(tag.Key, tag.Value);
                }
            }
        });
    }
}
```

**File**: `BuildingBlocks.Web/Extensions/ErrorTrackingExtensions.cs`

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using Datarizen.BuildingBlocks.Application.ErrorTracking;
using Datarizen.BuildingBlocks.Infrastructure.ErrorTracking;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class ErrorTrackingExtensions
{
    public static WebApplicationBuilder AddErrorTracking(this WebApplicationBuilder builder)
    {
        // Register Sentry (implementation detail)
        builder.WebHost.UseSentry(options =>
        {
            options.Dsn = builder.Configuration["Sentry:Dsn"];
            options.Environment = builder.Environment.EnvironmentName;
            options.TracesSampleRate = 1.0;
            options.AttachStacktrace = true;
            options.SendDefaultPii = false;
        });

        // Register our abstraction
        builder.Services.AddSingleton<IErrorTracker, SentryErrorTracker>();

        return builder;
    }
}
```

**Usage Example**:

```csharp
// Application code ONLY uses IErrorTracker (our abstraction)
public class SomeCommandHandler
{
    private readonly IErrorTracker _errorTracker;

    public async Task<Result> Handle(SomeCommand request, CancellationToken ct)
    {
        try
        {
            // ... business logic
        }
        catch (Exception ex)
        {
            _errorTracker.CaptureException(ex, new Dictionary<string, string>
            {
                ["TenantId"] = request.TenantId.ToString(),
                ["UserId"] = request.UserId.ToString()
            });
            throw;
        }
    }
}
```

**Tasks**:
- [ ] Create IErrorTracker abstraction
- [ ] Create SentryErrorTracker implementation
- [ ] Add Sentry to docker-compose.yml (self-hosted)
- [ ] Configure Sentry DSN in appsettings.json
- [ ] Update Program.cs to call AddErrorTracking()
- [ ] Add unit tests (mock IErrorTracker)

**Success Criteria**:
- ✅ Application code NEVER references Sentry directly
- ✅ Can replace Sentry with Raygun/AppInsights by changing ONE file
- ✅ Errors include correlation ID, tenant ID, user ID
- ✅ Can view errors in self-hosted Sentry dashboard
- ✅ No SaaS costs

---

## Summary: All External Libraries Behind Abstractions

| Library | Our Abstraction | Implementation | Can Replace With |
|---------|----------------|----------------|------------------|
| **Serilog** | `IStructuredLogger` | `SerilogStructuredLogger` | NLog, Microsoft.Extensions.Logging |
| **Hangfire** | `IBackgroundJobScheduler` | `HangfireBackgroundJobScheduler` | Quartz.NET, Azure Functions, AWS Lambda |
| **Sentry** | `IErrorTracker` | `SentryErrorTracker` | Raygun, Application Insights, Rollbar |
| **FluentValidation** | `IValidator<T>` | FluentValidation validators | Custom validators, DataAnnotations |
| **EF Core** | `IUnitOfWork`, `IRepository<T>` | EF Core implementations | Dapper, NHibernate, ADO.NET |

**NO AutoMapper** - We create our own mappers for full control.

---

## Updated Timeline

**Phase 0: BuildingBlocks Enhancement**
- 0.1: ASP.NET Middleware: 3 hours
- 0.2: MediatR Behaviors: 2 hours
- 0.3: Specification Pattern: 1.5 hours
- 0.4: Background Jobs (Hangfire behind abstraction): 30 minutes
- 0.5: Feature Flags (abstraction only): 30 minutes
- 0.6: Health Checks: 30 minutes
- 0.7: Minimum Viable Observability: 3.5 hours

**Total Phase 0: ~11.5 hours (~1.5 days)**

**Phase 1: Identity Application Layer: ~15.5 hours (~2 days)**

**Grand Total (Critical Path): ~27 hours (~3.5 days)**

---

## Key Benefits

1. ✅ **Vendor Independence**: Can replace ANY library by changing ONE file
2. ✅ **Testability**: Easy to mock abstra
}
```

**Tasks**:
- [ ] Create ISpecification interface
- [ ] Create BaseSpecification class
- [ ] Create SpecificationEvaluator
- [ ] Update IRepository to accept ISpecification
- [ ] Add XML documentation
- [ ] Add unit tests

---

### 0.4: Hangfire Background Jobs (30 minutes) - SIMPLIFIED

**Location**: `BuildingBlocks.Web/Extensions/`

**Purpose**: Production-ready background job scheduler using Hangfire.

#### 0.4.1: Hangfire Configuration

**File**: `BuildingBlocks.Web/Extensions/HangfireExtensions.cs`

```csharp
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class HangfireExtensions
{
    public static WebApplicationBuilder AddHangfire(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");

        builder.Services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                });
        });

        builder.Services.AddHangfireServer();

        return builder;
    }

    public static WebApplication UseHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard("/admin/jobs", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });

        return app;
    }
}

internal class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // TODO: Add real authorization (check if user is admin)
        return true;
    }
}
```

**Usage Example**:

```csharp
// Application code ONLY uses IBackgroundJobScheduler (our abstraction)
public class SendWelcomeEmailCommandHandler
{
    private readonly IBackgroundJobScheduler _jobScheduler;

    public async Task<Result> Handle(SendWelcomeEmailCommand request, CancellationToken ct)
    {
        // Enqueue email sending job
        _jobScheduler.Enqueue<IEmailService>(x => x.SendWelcomeEmailAsync(request.UserId));
        
        return Result.Success();
    }
}
```

**Tasks**:
- [ ] Create IBackgroundJobScheduler abstraction
- [ ] Create HangfireBackgroundJobScheduler implementation
- [ ] Create HangfireExtensions.AddBackgroundJobs()
- [ ] Update Program.cs to call AddBackgroundJobs()
- [ ] Configure Hangfire dashboard (/admin/jobs)
- [ ] Add authorization to dashboard
- [ ] Document migration path to Quartz.NET
- [ ] Add unit tests (mock IBackgroundJobScheduler)

**Success Criteria**:
- ✅ Application code NEVER references Hangfire directly
- ✅ Can replace Hangfire with Quartz.NET by changing ONE file
- ✅ Can enqueue, schedule, and create recurring jobs
- ✅ Dashboard is accessible at /admin/jobs
- ✅ Jobs are persisted in PostgreSQL

---

### 0.5: Feature Flags (BuildingBlocks.Application) (30 minutes) - ABSTRACTION ONLY

**Location**: `BuildingBlocks.Application/FeatureFlags/`

**Purpose**: Gradual rollouts, A/B testing, feature toggles.

#### 0.5.1: IFeatureFlagService Abstraction

**File**: `BuildingBlocks.Application/FeatureFlags/IFeatureFlagService.cs`

```csharp
namespace Datarizen.BuildingBlocks.Application.FeatureFlags;

/// <summary>
/// Abstraction over feature flag service.
/// Allows replacing implementation (LaunchDarkly, Azure App Configuration, custom database, etc.).
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Check if a feature is enabled for the current user/tenant.
    /// </summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a feature is enabled for a specific user.
    /// </summary>
    Task<bool> IsEnabledForUserAsync(string featureName, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a feature is enabled for a specific tenant.
    /// </summary>
    Task<bool> IsEnabledForTenantAsync(string featureName, Guid tenantId, CancellationToken cancellationToken = default);
}
```

**File**: `BuildingBlocks.Infrastructure/FeatureFlags/InMemoryFeatureFlagService.cs` (SIMPLE IMPLEMENTATION)

```csharp
using Datarizen.BuildingBlocks.Application.FeatureFlags;
using Microsoft.Extensions.Configuration;

namespace Datarizen.BuildingBlocks.Infrastructure.FeatureFlags;

/// <summary>
/// Simple in-memory implementation of IFeatureFlagService.
/// Reads feature flags from appsettings.json.
/// Can be replaced with LaunchDarkly, Azure App Configuration, etc.
/// </summary>
internal sealed class InMemoryFeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;

    public InMemoryFeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var isEnabled = _configuration.GetValue<bool>($"FeatureFlags:{featureName}");
        return Task.FromResult(isEnabled);
    }

    public Task<bool> IsEnabledForUserAsync(string featureName, Guid userId, CancellationToken cancellationToken = default)
    {
        // Simple implementation: same as IsEnabledAsync
        // Can be enhanced to check user-specific flags
        return IsEnabledAsync(featureName, cancellationToken);
    }

    public Task<bool> IsEnabledForTenantAsync(string featureName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Simple implementation: same as IsEnabledAsync
        // Can be enhanced to check tenant-specific flags
        return IsEnabledAsync(featureName, cancellationToken);
    }
}
```

**File**: `appsettings.json` (configuration)

```json
{
  "FeatureFlags": {
    "NewDashboard": true,
    "AdvancedReporting": false,
    "BetaFeatures": false
  }
}
```

**Tasks**:
- [ ] Create IFeatureFlagService abstraction
- [ ] Create InMemoryFeatureFlagService implementation
- [ ] Document how to replace with LaunchDarkly
- [ ] Register in DI container
- [ ] Add unit tests

**Future Enhancement**: Replace with LaunchDarkly by creating `LaunchDarklyFeatureFlagService` implementation.

---

### 0.6: Health Checks (BuildingBlocks.Web) (30 minutes)

**Location**: `BuildingBlocks.Web/HealthChecks/`

**Purpose**: Monitor application health (database, cache, message queue).

**NOTE**: Uses built-in `Microsoft.Extensions.Diagnostics.HealthChecks` (no external library needed).

#### 0.6.1: Custom Health Checks

**File**: `BuildingBlocks.Web/HealthChecks/DatabaseHealthCheck.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Datarizen.BuildingBlocks.Web.HealthChecks;

/// <summary>
/// Health check for PostgreSQL database connectivity.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public DatabaseHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable", ex);
        }
    }
}
```

**File**: `BuildingBlocks.Web/Extensions/HealthCheckExtensions.cs`

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Datarizen.BuildingBlocks.Web.HealthChecks;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");

        builder.Services.AddHealthChecks()
            .AddCheck("database", new DatabaseHealthCheck(connectionString))
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"));

        return builder;
    }

    public static WebApplication UseHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        return app;
    }
}
```

**Tasks**:
- [ ] Create DatabaseHealthCheck
- [ ] Create HealthCheckExtensions
- [ ] Register health checks in Program.cs
- [ ] Add /health endpoint
- [ ] Test health check responses

---

### 0.7: Minimum Viable Observability (3.5 hours)

#### 0.7.1: Correlation ID Middleware (45 minutes)

**File**: `BuildingBlocks.Web/Middleware/CorrelationIdMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Datarizen.BuildingBlocks.Web.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID for distributed tracing.
/// Uses System.Diagnostics.Activity (OpenTelemetry standard).
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add to response headers
        context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

        // Add to Serilog LogContext (for structured logging)
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Add to Activity (for OpenTelemetry)
            Activity.Current?.SetTag("correlation_id", correlationId);

            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}
```

**Tasks**:
- [ ] Create CorrelationIdMiddleware
- [ ] Register middleware in Program.cs
- [ ] Verify correlation ID in logs
- [ ] Verify correlation ID in response headers
- [ ] Add unit tests

---

#### 0.7.2: Serilog Configuration & Enrichment (30 minutes) - BEHIND ABSTRACTION

**File**: `BuildingBlocks.Web/Extensions/SerilogExtensions.cs`

```csharp
using Serilog;
using Microsoft.AspNetCore.Builder;

namespace Datarizen.BuildingBlocks.Web.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "Datarizen")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        });

        return builder;
    }
}
```

**File**: `appsettings.json` (Serilog configuration)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    }
  }
}
```

**Tasks**:
- [ ] Add Serilog packages to BuildingBlocks.Web
- [ ] Create SerilogExtensions.AddStructuredLogging()
- [ ] Create IStructuredLogger abstraction (already done in 0.2.2)
- [ ] Create SerilogStructuredLogger implementation (already done in 0.2.2)
- [ ] Configure Serilog in appsettings.json
- [ ] Update Program.cs to call AddStructuredLogging()
- [ ] Verify structured logging works

**Success Criteria**:
- ✅ All logs are structured (JSON format)
- ✅ Logs include correlation ID
- ✅ Application code NEVER references Serilog directly (uses IStructuredLogger)
- ✅ Can replace Serilog with NLog by changing ONE file

---

#### 0.7.3: Security Audit Logging (1.5 hours)

**File**: `BuildingBlocks.Application/Auditing/SecurityAuditLogger.cs`

```csharp
using Datarizen.BuildingBlocks.Application.Logging;
using Microsoft.AspNetCore.Http;

namespace Datarizen.BuildingBlocks.Application.Auditing;

/// <summary>
/// Logs security-related events (login, logout, permission changes, etc.).
/// Uses IStructuredLogger (our abstraction).
/// </summary>
public sealed class SecurityAuditLogger
{
    private readonly IStructuredLogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityAuditLogger(
        IStructuredLogger logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void LogLoginSuccess(Guid userId, string email)
    {
        _logger.LogInformation(
            "User login successful: {UserId} ({Email}) from {IpAddress}",
            userId,
            email,
            GetIpAddress());
    }

    public void LogLoginFailure(string email, string reason)
    {
        _logger.LogWarning(
            "User login failed: {Email} from {IpAddress}. Reason: {Reason}",
            email,
            GetIpAddress(),
            reason);
    }

    public void LogPermissionChange(Guid userId, string permission, bool granted)
    {
        _logger.LogInformation(
            "Permission {Action} for user {UserId}: {Permission}",
            granted ? "granted" : "revoked",
            userId,
            permission);
    }

    private string? GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
```

**Tasks**:
- [ ] Create SecurityAuditLogger
- [ ] Add security audit logging to login/logout handlers
- [ ] Add security audit logging to permission changes
- [ ] Register in DI container
- [ ] Add unit tests

---

#### 0.7.4: Metrics Behavior (1 hour)

**File**: `BuildingBlocks.Application/Behaviors/MetricsBehavior.cs`

```csharp
using MediatR;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Datarizen.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that records metrics for requests.
/// Uses System.Diagnostics.Metrics (OpenTelemetry standard).
/// NO EXTERNAL LIBRARY NEEDED.
/// </summary>
public sealed class MetricsBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Meter Meter = new("Datarizen.Application");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("requests.total");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("requests.duration");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Record success metrics
            RequestCounter.Add(1, new KeyValuePair<string, object?>("request", requestName), new KeyValuePair<string, object?>("status", "success"));
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("request", requestName));

            return response;
        }
        catch
        {
            stopwatch.Stop();

            // Record failure metrics
            RequestCounter.Add(1, new KeyValuePair<string, object?>("request", requestName), new KeyValuePair<string, object?>("status", "failure"));
            RequestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("request", requestName));

            throw;
        }
    }
}
```

**Tasks**:
- [ ] Create MetricsBehavior
- [ ] Register in DI container
- [ ] Configure OpenTelemetry to export metrics
- [ ] Verify metrics in Aspire dashboard
- [ ] Add unit tests

---

#### 0.7.5: Error Tracking Integration (30 minutes) - SENTRY SELF-HOSTED

**Decision**: Use **Sentry (self-hosted)** for error tracking.

**Why Sentry?**
- ✅ MIT License (100% free)
- ✅ Best-in-class error tracking
- ✅ Easy to self-host with Docker
- ✅ Excellent .NET SDK
- ✅ Behind `IErrorTracker` abstraction (easy to replace)

**Alternatives Considered**:
- **Rollbar**: Similar features, but SaaS pricing
- **Application Insights**: Azure lock-in
- **Elastic APM**: Requires Elasticsearch infrastructure
- **Seq**: Limited error tracking features
- **Exceptionless**: Smaller community, less mature

**Self-Hosting Sentry**:

```yaml
# docker-compose.yml (add to AppHost or separate infrastructure)
version: '3.8'
services:
  sentry:
    image: sentry:latest
    ports:
      - "9000:9000"
    environment:
      SENTRY_SECRET_KEY: "your-secret-key"
      SENTRY_POSTGRES_HOST: postgres
      SENTRY_REDIS_HOST: redis
    depends_on:
      - postgres
      - redis
```

**Configuration**:

```json
{
  "Sentry": {
    "Dsn": "http://your-self-hosted-sentry:9000/1"
  }
}
```

**File**: `BuildingBlocks.Application/ErrorTracking/IErrorTracker.cs` (NEW ABSTRACTION)

```csharp
namespace Datarizen.BuildingBlocks.Application.ErrorTracking;

/// <summary>
/// Abstraction over error tracking service.
/// Allows replacing Sentry with Raygun, Application Insights, etc.
/// </summary>
public interface IErrorTracker
{
    /// <summary>
    /// Capture an exception and send it to the error tracking service.
    /// </summary>
    void CaptureException(Exception exception, Dictionary<string, string>? tags =
                    options.UseNpgsqlConnection(connectionString);
                });
        });

        builder.Services.AddHangfireServer();

        return builder;
    }

    public static WebApplication UseHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });

        return app;
    }
}

// Simple authorization filter (replace with real auth in production)
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // TODO: Add real authorization (check if user is admin)
        return true;
    }
}
```

**File**: `BuildingBlocks.Application/BackgroundJobs/IBackgroundJobScheduler.cs` (UPDATED)

```csharp
namespace Datarizen.BuildingBlocks.Application.BackgroundJobs;

/// <summary>
/// Wrapper around Hangfire for scheduling background jobs.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// Enqueue a job to run immediately in the background.
    /// </summary>
    void Enqueue<TJob>(Expression<Action<TJob>> methodCall);

    /// <summary>
    /// Schedule a job to run at a specific time.
    /// </summary>
    void Schedule<TJob>(Expression<Action<TJob>> methodCall, DateTimeOffset runAt);

    /// <summary>
    /// Schedule a recurring job (cron expression).
    /// </summary>
    void Recurring<TJob>(string jobId, Expression<Action<TJob>> methodCall, string cronExpression);
}

/// <summary>
/// Hangfire implementation of IBackgroundJobScheduler.
/// </summary>
public sealed class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    public void Enqueue<TJob>(Expression<Action<TJob>> methodCall)
    {
        BackgroundJob.Enqueue(methodCall);
    }

    public void Schedule<TJob>(Expression<Action<TJob>> methodCall, DateTimeOffset runAt)
    {
        BackgroundJob.Schedule(methodCall, runAt);
    }

    public void Recurring<TJob>(string jobId, Expression<Action<TJob>> methodCall, string cronExpression)
    {
        RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
    }
}
```

**Usage Example**:

```csharp
// In a command handler
public class SendWelcomeEmailCommandHandler
{
    private readonly IBackgroundJobScheduler _jobScheduler;

    public async Task<Result> Handle(SendWelcomeEmailCommand request, CancellationToken ct)
    {
        // Enqueue email sending job
        _jobScheduler.Enqueue<IEmailService>(x => x.SendWelcomeEmailAsync(request.UserId));
        
        return Result.Success();
    }
}

// Recurring job (in Program.cs)
app.Services.GetRequiredService<IBackgroundJobScheduler>()
    .Recurring<IDataCleanupService>(
        "cleanup-old-data",
        x => x.CleanupOldDataAsync(),
        Cron.Daily);
```

**Tasks**:
- [ ] Add Hangfire packages to BuildingBlocks.Web
- [ ] Create HangfireExtensions.AddHangfire()
- [ ] Create HangfireBackgroundJobScheduler implementation
- [ ] Update Program.cs to call AddHangfire()
- [ ] Configure Hangfire dashboard (/hangfire)
- [ ] Add authorization to Hangfire dashboard
- [ ] Test job enqueueing and execution

**Success Criteria**:
- ✅ Can enqueue background jobs
- ✅ Can schedule jobs for future execution
- ✅ Can create recurring jobs (cron)
- ✅ Hangfire dashboard is accessible at /hangfire
- ✅ Jobs are persisted in PostgreSQL
- ✅ Failed jobs are automatically retried

---

### 0.5: Feature Flags (BuildingBlocks.Application) (30 minutes)

**Location**: `BuildingBlocks.Application/FeatureFlags/`

**Purpose**: Gradual rollouts, A/B testing, feature toggles.

#### 0.5.1: IFeatureFlagService

**File**: `BuildingBlocks.Application/FeatureFlags/IFeatureFlagService.cs`

```csharp
namespace Datarizen.BuildingBlocks.Application.FeatureFlags;

/// <summary>
/// Service for checking feature flags.
/// </summary>
/// <remarks>
/// Implementation options:
/// - LaunchDarkly (recommended for production)
/// - Azure App Configuration
/// - Custom implementation (database-backed)
/// </remarks>
public interface IFeatureFlagService
{
    /// <summary>
    /// Check if a feature is enabled for the current user/tenant.
    /// </summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a feature is enabled for a specific user.
    /// </summary>
    Task<bool> IsEnabledForUserAsync(string featureName, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a feature is enabled for a specific tenant.
    /// </summary>
    Task<bool> IsEnabledForTenantAsync(string featureName, Guid tenantId, CancellationToken cancellationToken = default);
}
```

**Tasks**:
- [ ] Create IFeatureFlagService interface
- [ ] Document implementation options (LaunchDarkly, custom)

---

### 0.6: Health Checks (BuildingBlocks.Web) (30 minutes)

**Location**: `BuildingBlocks.Web/HealthChecks/`

**Purpose**: Monitor application health (database, cache, message queue).

#### 0.6.1: Custom Health Checks

**File**: `BuildingBlocks.Web/HealthChecks/DatabaseHealthCheck.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Datarizen.BuildingBlocks.Web.HealthChecks;

/// <summary>
/// Health check for PostgreSQL database connectivity.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public DatabaseHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable", ex);
        }
    }
}
```

**File**: `BuildingBlocks.Web/HealthChecks/RedisHealthCheck.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Datarizen.BuildingBlocks.Web.HealthChecks;

/// <summary>
/// Health check for Redis cache connectivity.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _redis.GetDatabase();
            await database.PingAsync();

            return HealthCheckResult.Healthy("Redis is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable", ex);
        }
    }
}
```

**Tasks**:
- [ ] Create DatabaseHealthCheck
- [ ] Create RedisHealthCheck
- [ ] Create RabbitMqHealthCheck
- [ ] Register in ServiceDefaults

---

### 0.7: Minimum Viable Observability (BuildingBlocks) (4-6 hours) 🚨 CRITICAL PATH

**Location**: `BuildingBlocks.Web/Middleware/`, `BuildingBlocks.Application/Behaviors/`, `BuildingBlocks.Web/ErrorTracking/`

**Purpose**: Production-ready observability for incident response, security auditing, and performance monitoring.

**Foundation**: Builds on existing ServiceDefaults OpenTelemetry configuration (tracing, metrics, logging).

#### 0.7.1: Correlation ID Middleware (45 minutes)

**File**: `BuildingBlocks.Web/Middleware/CorrelationIdMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Datarizen.BuildingBlocks.Web.Middleware;

/// <summary>
/// Middleware that generates or extracts a correlation ID for request tracing.
/// Correlation ID is propagated to all logs, traces, and outgoing HTTP requests.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdKey = "CorrelationId";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        // 1. Try to extract correlation ID from request header
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();

        // 2. Generate new correlation ID if not present
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // 3. Store in HttpContext.Items for access in behaviors/handlers
        context.Items[CorrelationIdKey] = correlationId;

        // 4. Add to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // 5. Add to logging scope (Serilog will include in all logs)
        using (logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdKey] = correlationId
        }))
        {
            // 6. Add to OpenTelemetry activity (distributed tracing)
            var activity = System.Diagnostics.Activity.Current;
            activity?.SetTag("correlation_id", correlationId);

            await _next(context);
        }
    }
}

/// <summary>
/// Extension methods for accessing correlation ID.
/// </summary>
public static class CorrelationIdExtensions
{
    private const string CorrelationIdKey = "CorrelationId";

    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items.TryGetValue(CorrelationIdKey, out var correlationId)
            ? correlationId as string
            : null;
    }
}
```

**Tasks**:
- [ ] Create CorrelationIdMiddleware
- [ ] Generate correlation ID if not present in request
- [ ] Extract correlation ID from `X-Correlation-ID` header
- [ ] Store in HttpContext.Items for access in behaviors
- [ ] Add to response headers
- [ ] Add to logging scope (Serilog)
- [ ] Add to OpenTelemetry activity tags
- [ ] Create CorrelationIdExtensions helper
- [ ] Add unit tests

**Success Criteria**:
- ✅ Every HTTP request has a unique correlation ID
- ✅ Correlation ID appears in all log entries for that request
- ✅ Correlation ID appears in OpenTelemetry traces
- ✅ Correlation ID is returned in response headers
- ✅ Can trace a request end-to-end using correlation ID

---

#### 0.7.2: Logging Context Enricher (1 hour)

**File**: `BuildingBlocks.Application/Logging/LoggingContextEnricher.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Datarizen.BuildingBlocks.Application.Services;
using Serilog.Context;

namespace Datarizen.BuildingBlocks.Application.Logging;

/// <summary>
/// Enriches logs with contextual information (correlation ID, tenant, user, request path).
/// </summary>
public sealed class LoggingContextEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserService _currentUserService;

    public LoggingContextEnricher(
        IHttpContextAccessor httpContextAccessor,
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserService currentUserService)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContextAccessor = tenantContextAccessor;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Enriches the current logging context with correlation ID, tenant, user, and request path.
    /// Call this at the start of command/query handlers.
    /// </summary>
    public IDisposable EnrichContext()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return new NoOpDisposable();
        }

        var properties = new List<IDisposable>();

        // Add correlation ID
        var correlationId = context.GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            properties.Add(LogContext.PushProperty("CorrelationId", correlationId));
        }

        // Add tenant ID
        var tenantId = _tenantContextAccessor.TenantId;
        if (tenantId.HasValue)
        {
            properties.Add(LogContext.PushProperty("TenantId", tenantId.Value));
        }

        // Add user ID
        var userId = _currentUserService.UserId;
        if (userId.HasValue)
        {
            properties.Add(LogContext.PushProperty("UserId", userId.Value));
        }

        // Add request path
        var requestPath = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(requestPath))
        {
            properties.Add(LogContext.PushProperty("RequestPath", requestPath));
        }

        return new CompositeDisposable(properties);
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables;

        public CompositeDisposable(List<IDisposable> disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
```

**File**: `BuildingBlocks.Application/Behaviors/LoggingContextBehavior.cs`

```csharp
using MediatR;
using Datarizen.BuildingBlocks.Application.Logging;

namespace Datarizen.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR behavior that enriches logs with contextual information.
/// Runs FIRST in the pipeline to ensure all subsequent logs have context.
/// </summary>
public sealed class LoggingContextBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly LoggingContextEnricher _enricher;

    public LoggingContextBehavior(LoggingContextEnricher enricher)
    {
        _enricher = enricher;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using (_enricher.EnrichContext())
        {
            return await next();
        }
    }
}
```

**Tasks**:
- [ ] Create LoggingContextEnricher
- [ ] Enrich with CorrelationId (from HttpContext.Items)
- [ ] Enrich with TenantId (from ITenantContextAccessor)
- [ ] Enrich with UserId (from ICurrentUserService)
- [ ] Enrich with RequestPath (from HttpContext.Request.Path)
- [ ] Create LoggingContextBehavior (runs FIRST in pipeline)
- [ ] Configure Serilog to output structured JSON logs
- [ ] Add unit tests

**Success Criteria**:
- ✅ All logs include CorrelationId, TenantId, UserId, RequestPath (when available)
- ✅ Logs are in structured JSON format
- ✅ Can filter logs by tenant or user
- ✅ Can trace all logs for a single request using correlation ID

---

#### 0.7.3: Security Audit Logging (1.5 hours)

**File**: `BuildingBlocks.Web/Middleware/SecurityAuditMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Datarizen.BuildingBlocks.Web.Middleware;

/// <summary>
/// Middleware that logs authentication attempts (success and failure).
/// Logs include: timestamp, user, IP address, user agent, result.
/// </summary>
public sealed class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    public SecurityAuditMiddleware(
        RequestDelegate next,
        ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only audit authentication endpoints
        if (!IsAuthenticationEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        await _next(context);

        // Log authentication attempt
        var statusCode = context.Response.StatusCode;
        var isSuccess = statusCode >= 200 && statusCode < 300;
        var userId = context.User.Identity?.Name;

        if (isSuccess)
        {
            _logger.LogInformation(
                "Authentication succeeded for user {UserId} from {IpAddress} using {UserAgent}",
                userId ?? "unknown",
                ipAddress,
                userAgent);
        }
        else
        {
            _logger.LogWarning(
                "Authentication failed for user {UserId} from {IpAddress} using {UserAgent} with status {StatusCode}",
                userId ?? "unknown",
                ipAddress,
                userAgent,
                statusCode);
        }
    }

    private static bool IsAuthenticationEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/identity/login") ||
               path.StartsWithSegments("/api/identity/register") ||
               path.StartsWithSegments("/api/identity/refresh");
    }
}
```

**File**: `BuildingBlocks.Application/Behaviors/AuthorizationAuditBehavior.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Datarizen.BuildingBlocks.Application.Services;

namespace Datarizen.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR behavior that logs authorization failures.
/// Runs AFTER AuthorizationBehavior to capture failures.
/// </summary>
public sealed class AuthorizationAuditBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<AuthorizationAuditBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public AuthorizationAuditBehavior(
        ILogger<AuthorizationAuditBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        ITenantContextAccessor tenantContextAccessor)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (UnauthorizedAccessException ex)
        {
            // Log authorization failure
            _logger.LogWarning(
                ex,
                "Authorization failed for user {UserId} in tenant {TenantId} attempting {RequestType}",
                _currentUserService.UserId,
                _tenantContextAccessor.TenantId,
                typeof(TRequest).Name);

            throw;
        }
    }
}
```

**Tasks**:
- [ ] Create SecurityAuditMiddleware
- [ ] Log authentication attempts (success/failure)
- [ ] Include IP address, user agent, timestamp
- [ ] Create AuthorizationAuditBehavior
- [ ] Log authorization failures with user/tenant context
- [ ] Consider storing audit logs in dedicated table or SIEM
- [ ] Add unit tests

**Success Criteria**:
- ✅ All authentication attempts are logged
- ✅ All authorization failures are logged
- ✅ Logs include user, tenant, IP address, user agent
- ✅ Can answer: "Who tried to access what resource and when?"
- ✅ Can detect brute force attacks (multiple failed logins)

---

#### 0.7.4: Metrics Behavior (1 hour)

**File**: `BuildingBlocks.Application/Behaviors/MetricsBehavior.cs`

```csharp
using MediatR;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Datarizen.BuildingBlocks.Application.Services;

namespace Datarizen.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR behavior that records OpenTelemetry metrics for commands and queries.
/// Metrics: command/query execution count, duration, success/failure rate.
/// </summary>
public sealed class MetricsBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private static readonly Meter Meter = new("Datarizen.Application", "1.0.0");
    
    private static readonly Counter<long> ExecutionCounter = Meter.CreateCounter<long>(
        "app.commands.executed",
        description: "Number of commands/queries executed");
    
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>(
        "app.commands.duration",
        unit: "ms",
        description: "Duration of command/query execution");

    public MetricsBehavior(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var tenantId = _tenantContextAccessor.TenantId?.ToString() ?? "unknown";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Record success metrics
            var tags = new TagList
            {
                { "request_type", requestType },
                { "tenant_id", tenantId },
                { "result", "success" }
            };

            ExecutionCounter.Add(1, tags);
            DurationHistogram.Record(stopwatch.ElapsedMilliseconds, tags);

            return response;
        }
        catch
        {
            stopwatch.Stop();

            // Record failure metrics
            var tags = new TagList
            {
                { "request_type", requestType },
                { "tenant_id", tenantId },
                { "result", "failure" }
            };

            ExecutionCounter.Add(1, tags);
            DurationHistogram.Record(stopwatch.ElapsedMilliseconds, tags);

            throw;
        }
    }
}
```

**Tasks**:
- [ ] Create MetricsBehavior
- [ ] Record `app.commands.executed` counter (tags: request_type, tenant_id, result)
- [ ] Record `app.commands.duration` histogram (tags: request_type, tenant_id)
- [ ] Integrate with ServiceDefaults OpenTelemetry metrics
- [ ] Verify metrics appear in Prometheus/Grafana
- [ ] Add unit tests

**Success Criteria**:
- ✅ All commands/queries are counted
- ✅ All command/query durations are recorded
- ✅ Metrics include tenant context
- ✅ Can answer: "Which commands are slowest?"
- ✅ Can answer: "What is the error rate per tenant?"
- ✅ Metrics visible in Prometheus/Grafana

---

#### 0.7.5: Error Tracking Integration (45 minutes)

**File**: `BuildingBlocks.Web/ErrorTracking/IErrorTracker.cs`

```csharp
namespace Datarizen.BuildingBlocks.Web.ErrorTracking;

/// <summary>
/// Interface for error tracking services (Sentry, Application Insights, Raygun).
/// </summary>
public interface IErrorTracker
{
    /// <summary>
    /// Capture an exception with contextual information.
    /// </summary>
    Task CaptureExceptionAsync(
        Exception exception,
        ErrorContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contextual information for error tracking.
/// </summary>
public sealed class ErrorContext
{
    public string? CorrelationId { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? UserId { get; init; }
    public string? RequestPath { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public Dictionary<string, object>? AdditionalData { get; init; }
}
```

**File**: `BuildingBlocks.Web/Middleware/ExceptionHandlingMiddleware.cs` (UPDATE)

```csharp
// Add IErrorTracker to constructor
private readonly IErrorTracker? _errorTracker;

public ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IErrorTracker? errorTracker = null) // Optional: may not be configured
{
    _next = next;
    _logger = logger;
    _errorTracker = errorTracker;
}

// Update InvokeAsync to send errors to tracker
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception occurred");

        // Send to error tracking service
        if (_errorTracker is not null)
        {
            var errorContext = new ErrorContext
            {
                CorrelationId = context.GetCorrelationId(),
                TenantId = context.Items["TenantId"] as Guid?,
                UserId = context.Items["UserId"] as Guid?,
                RequestPath = context.Request.Path.Value,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString()
            };

            await _errorTracker.CaptureExceptionAsync(ex, errorContext);
        }

        await HandleExceptionAsync(context, ex);
    }
}
```

**Tasks**:
- [ ] Create IErrorTracker interface
- [ ] Create ErrorContext class
- [ ] Update ExceptionHandlingMiddleware to use IErrorTracker
- [ ] Enrich error context with correlation ID, tenant, user, request path
- [ ] Document implementation options (Sentry, Application Insights, Raygun)
- [ ] Add unit tests

**Success Criteria**:
- ✅ All unhandled exceptions are sent to error tracker
- ✅ Error context includes correlation ID, tenant, user, request path
- ✅ Can answer: "What caused this production error?"
- ✅ Can group errors by fingerprint
- ✅ Can filter errors by tenant or user

---

### 0.8: Advanced Observability (Future Enhancement) ⏭️ NOT CRITICAL PATH

**Location**: `BuildingBlocks.Application/`, `BuildingBlocks.Infrastructure/`

**Purpose**: Proactive monitoring, compliance, and business intelligence.

**Estimated Effort**: ~8-12 hours (deferred based on operational needs)

#### 0.8.1: Business Metrics (2 hours) ⏭️

**Purpose**: Track feature usage, conversion funnels, A/B test results.

**Examples**:
- User registration funnel (started → email confirmed → profile completed)
- Feature adoption rate (% of users using dark mode)
- A/B test results (variant A vs variant B conversion rate)

**Implementation**:
- Custom OpenTelemetry metrics
- Integration with analytics platforms (Mixpanel, Amplitude)

---

#### 0.8.2: Performance Monitoring (3 hours) ⏭️

**Purpose**: Detect N+1 queries, slow queries, memory leaks.

**Examples**:
- N+1 query detection (EF Core interceptor)
- Slow query alerts (queries > 1 second)
- Memory profiling (heap snapshots, GC metrics)

**Implementation**:
- EF Core interceptors
- Custom OpenTelemetry spans
- Integration with APM tools (Application Insights, Datadog)

---

#### 0.8.3: Compliance Logging (2 hours) ⏭️

**Purpose**: GDPR data access logs, data retention enforcement, consent tracking.

**Examples**:
- Log all personal data access (who accessed whose data)
- Enforce data retention policies (delete data after N days)
- Track consent versions (user agreed to terms v1.2 on 2024-01-15)

**Implementation**:
- Dedicated audit log table
- Background jobs for data retention
- Consent tracking in domain model

---

#### 0.8.4: SLA/SLO Tracking (2 hours) ⏭️

**Purpose**: Error budgets, availability metrics, latency percentiles.

**Examples**:
- 99.9% availability SLA (max 43 minutes downtime per month)
- Error budget (max 0.1% error rate)
- Latency SLO (p95 < 500ms, p99 < 1000ms)

**Implementation**:
- Custom OpenTelemetry metrics
- Alerting rules (Prometheus Alertmanager)
- SLO dashboards (Grafana)

---

#### 0.8.5: Custom Tracing (1.5 hours) ⏭️

**Purpose**: OpenTelemetry spans for domain events, background jobs, external API calls.

**Examples**:
- Trace domain event publishing and handling
- Trace background job execution
- Trace external API calls (Stripe, SendGrid)

**Implementation**:
- Custom OpenTelemetry spans
- Activity propagation to background jobs
- Trace context in domain events

---

#### 0.8.6: Alerting Rules (1.5 hours) ⏭️

**Purpose**: Automated alerts for error spikes, performance degradation, security anomalies.

**Examples**:
- Alert if error rate > 1% for 5 minutes
- Alert if p95 latency > 1 second for 5 minutes
- Alert if failed login attempts > 10 in 1 minute (brute force)

**Implementation**:
- Prometheus Alertmanager rules
- Integration with PagerDuty, Slack, email
- Runbooks for common alerts

---

**Decision Framework: When to Implement Phase 0.8**

Implement Phase 0.8 features when:
- ✅ Production traffic > 1000 requests/day
- ✅ Compliance audit required (SOC2, GDPR, HIPAA)
- ✅ Performance issues detected (slow queries, high latency)
- ✅ Security incident occurred (brute force, data breach)
- ✅ Business metrics needed (feature adoption, conversion funnels)

---

## Phase 1: Identity Application Layer (15.5 hours)

**Status**: Done (implementation uses MediatR, Result&lt;T&gt;, Ardalis.Specification; namespace `Identity.Application`).

### 1.1: Project Setup (30 minutes)

**Location**: `server/src/Product/Identity/Identity.Application/`

**Tasks**:
- [x] Create `Identity.Application.csproj`
- [x] Add references:
  - `Identity.Domain`
  - `BuildingBlocks.Kernel`
  - `BuildingBlocks.Application`
- [x] Add NuGet packages:
  - `MediatR` (13.x)
  - `FluentValidation` (11.x)
  - `FluentValidation.DependencyInjectionExtensions` (11.x)
- [x] Create folder structure:
  - `Commands/`
  - `Queries/`
  - `DTOs/`
  - `Mappers/`
  - `Validators/`
  - `Specifications/`
  - `Services/`
  - `Extensions/`

---

### 1.2: Define DTOs (2 hours)

**Location**: `Identity.Application/DTOs/`

**Purpose**: Data Transfer Objects for API responses.

#### 1.2.1: User DTOs

**File**: `DTOs/Users/UserDto.cs`

```csharp
namespace Datarizen.Identity.Application.DTOs.Users;

public sealed record UserDto(
    Guid Id,
    Guid DefaultTenantId,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record UserDetailDto(
    Guid Id,
    Guid DefaultTenantId,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<RoleDto> Roles,
    List<PermissionDto> Permissions);
```

#### 1.2.2: Role DTOs

**File**: `DTOs/Roles/RoleDto.cs`

```csharp
namespace Datarizen.Identity.Application.DTOs.Roles;

public sealed record RoleDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    bool IsSystemRole);

public sealed record RoleDetailDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    bool IsSystemRole,
    List<PermissionDto> Permissions);
```

#### 1.2.3: Permission DTOs

**File**: `DTOs/Permissions/PermissionDto.cs`

```csharp
namespace Datarizen.Identity.Application.DTOs.Permissions;

public sealed record PermissionDto(
    Guid Id,
    string Name,
    string? Description,
    string Resource,
    string Action);
```

**Tasks**:
- [x] Create UserDto, UserDetailDto
- [x] Create RoleDto, RoleDetailDto
- [x] Create PermissionDto
- [ ] Add XML documentation

---

### 1.3: Define Mappers (1.5 hours)

**Location**: `Identity.Application/Mappers/`

**Purpose**: Map domain entities to DTOs.

#### 1.3.1: UserMapper

**File**: `Mappers/UserMapper.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Application.DTOs.Users;

namespace Datarizen.Identity.Application.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.DefaultTenantId,
            user.Email.Value,
            user.DisplayName,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
    }

    public static List<UserDto> ToDto(IEnumerable<User> users)
    {
        return users.Select(ToDto).ToList();
    }

    public static UserDetailDto ToDetailDto(User user)
    {
        return new UserDetailDto(
            user.Id,
            user.DefaultTenantId,
            user.Email.Value,
            user.DisplayName,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            user.Roles.Select(RoleMapper.ToDto).ToList(),
            user.Permissions.Select(PermissionMapper.ToDto).ToList());
    }
}
```

**Tasks**:
- [x] Create UserMapper
- [x] Create RoleMapper
- [x] Create PermissionMapper
- [ ] Add XML documentation

---

### 1.4: Define Specifications (1.5 hours)

**Location**: `Identity.Application/Specifications/`

**Purpose**: Reusable query specifications for complex queries.

#### 1.4.1: UserSpecifications

**File**: `Specifications/Users/UserSpecifications.cs`

```csharp
using Datarizen.BuildingBlocks.Kernel.Specifications;
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.ValueObjects;

namespace Datarizen.Identity.Application.Specifications.Users;

/// <summary>
/// Specifications for User entity queries.
/// </summary>
public static class UserSpecifications
{
    /// <summary>
    /// Specification for active users.
    /// </summary>
    public sealed class ActiveUsersSpec : BaseSpecification<User>
    {
        public ActiveUsersSpec() : base(u => u.IsActive)
        {
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Specification for users by email.
    /// </summary>
    public sealed class UserByEmailSpec : BaseSpecification<User>
    {
        public UserByEmailSpec(Email email) : base(u => u.Email == email)
        {
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Specification for users by tenant with roles and permissions.
    /// </summary>
    public sealed class UsersByTenantWithRolesSpec : BaseSpecification<User>
    {
        public UsersByTenantWithRolesSpec(Guid tenantId) : base(u => u.DefaultTenantId == tenantId)
        {
            AddInclude(u => u.Roles);
            AddInclude(u => u.Permissions);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Specification for paginated active users.
    /// </summary>
    public sealed class PaginatedActiveUsersSpec : BaseSpecification<User>
    {
        public PaginatedActiveUsersSpec(int pageNumber, int pageSize) 
            : base(u => u.IsActive)
        {
            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
            AddOrderBy(u => u.DisplayName);
            ApplyNoTracking();
        }
    }
}
```

#### 1.4.2: RoleSpecifications

**File**: `Specifications/Roles/RoleSpecifications.cs`

```csharp
using Datarizen.BuildingBlocks.Kernel.Specifications;
using Datarizen.Identity.Domain.Entities;

namespace Datarizen.Identity.Application.Specifications.Roles;

public static class RoleSpecifications
{
    public sealed class RolesByTenantSpec : BaseSpecification<Role>
    {
        public RolesByTenantSpec(Guid tenantId) : base(r => r.TenantId == tenantId)
        {
            ApplyNoTracking();
        }
    }

    public sealed class RoleByNameSpec : BaseSpecification<Role>
    {
        public RoleByNameSpec(Guid tenantId, string name) 
            : base(r => r.TenantId == tenantId && r.Name == name)
        {
            ApplyNoTracking();
        }
    }

    public sealed class RoleWithPermissionsSpec : BaseSpecification<Role>
    {
        public RoleWithPermissionsSpec(Guid roleId) : base(r => r.Id == roleId)
        {
            AddInclude(r => r.Permissions);
            ApplyNoTracking();
        }
    }
}
```

**Tasks**:
- [x] Create UserSpecifications
- [x] Create RoleSpecifications
- [x] Create PermissionSpecifications
- [ ] Add XML documentation
- [ ] Add unit tests for specifications

---

### 1.5: Define Validators (2 hours)

**Location**: `Identity.Application/Validators/`

**Purpose**: FluentValidation validators for commands and queries.

#### 1.5.1: CreateUserCommandValidator

**File**: `Validators/Users/CreateUserCommandValidator.cs`

```csharp
using FluentValidation;
using Datarizen.Identity.Application.Commands.Users.CreateUser;

namespace Datarizen.Identity.Application.Validators.Users;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.DefaultTenantId)
            .NotEmpty();
    }
}
```

**Tasks**:
- [x] Create validators for all commands
- [ ] Create validators for queries (if needed)
- [x] Add custom validation rules
- [ ] Add unit tests

---

### 1.6: Define Commands (3 hours)

**Location**: `Identity.Application/Commands/`

**Purpose**: MediatR commands for write operations.

**Changes**: Add `ITransactionalCommand` to all commands that modify data.

**Tasks**:
- [x] CreateUserCommand + handler (IRequest&lt;Result&lt;Guid&gt;&gt;, ITransactionalCommand)
- [x] UpdateUserCommand + handler (IRequest&lt;Result&gt;)
- [ ] DeactivateUserCommand (optional)
- [ ] Add unit tests for command handlers

### 1.7: Define Queries (2 hours)

**Tasks**:
- [x] GetUserByIdQuery + handler (Result&lt;UserDto&gt;)
- [x] ListUsersQuery + handler (Result&lt;IReadOnlyList&lt;UserDto&gt;&gt;, optional DefaultTenantId)
- [ ] GetUserDetailQuery with roles/permissions (Phase 2 when User has navigation)
- [ ] Add unit tests for query handlers

### 1.8: DI Registration & Module (30 minutes)

**Tasks**:
- [x] Register MediatR from Identity.Application assembly in IdentityModule
- [x] Register FluentValidation validators from Identity.Application in IdentityModule
- [x] Pipeline behaviors (Validation, Transaction, Logging) provided by AddBuildingBlocks()

#### 1.6.1: CreateUserCommand

**File**: `Commands/Users/CreateUser/CreateUserCommand.cs`

```csharp
using Datarizen.BuildingBlocks.Application.Messaging;
using Datarizen.BuildingBlocks.Application.Behaviors;

namespace Datarizen.Identity.Application.Commands.Users.CreateUser;

public sealed record CreateUserCommand(
    Guid DefaultTenantId,
    string Email,
    string DisplayName,
    string Password
) : ICommand<Guid>, 
    ITransactionalCommand,  // NEW: Wrap in transaction
    IAuditableCommand       // Audit logging
{
    string IAuditableCommand.EntityType => "User";
    string IAuditableCommand.EntityId => string.Empty;
    string IAuditableCommand.Action => "Create";
}
```

**File**: `Commands/Users/CreateUser/CreateUserCommandHandler.cs`

```csharp
using Datarizen.BuildingBlocks.Application.Messaging;
using Datarizen.BuildingBlocks.Kernel.Results;
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.ValueObjects;
using Datarizen.Identity.Domain.Repositories;
using Datarizen.Identity.Domain.Services;

namespace Datarizen.Identity.Application.Commands.Users.CreateUser;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Create email value object
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result<Guid>.Failure(emailResult.Error);
        }

        // 2. Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(
            emailResult.Value,
            cancellationToken);

        if (existingUser is not null)
        {
            return Result<Guid>.Failure(
                Error.Conflict("User.EmailAlreadyExists", "User with this email already exists"));
        }

        // 3. Create user
        var userResult = User.Create(
            request.DefaultTenantId,
            emailResult.Value,
            request.DisplayName);

        if (userResult.IsFailure)
        {
            return Result<Guid>.Failure(userResult.Error);
        }

        var user = userResult.Value;

        // 4. Hash password and create credential
        var passwordHash = _passwor









