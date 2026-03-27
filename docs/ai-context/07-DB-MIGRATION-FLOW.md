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
  --file=lost_users.sql

# Import into production
psql -h prod-db.example.com -U datarizen -d datarizen_prod < lost_users.sql

# Drop temporary database
dropdb datarizen_temp
```

---

## Monitoring and Observability

### Checking Migration Status

**Via MigrationRunner CLI**:

```bash
# Check status for all modules
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status

# Output:
# Module: Identity
#   Current Version: 20250115103000
#   Pending Migrations: 0
#   Last Applied: 2025-01-15 10:30:00
#
# Module: Tenant
#   Current Version: 20250115104500
#   Pending Migrations: 1
#   Last Applied: 2025-01-15 10:45:00
#   Pending: 20250116103000_AddTenantSettings
```

**Via SQL**:

```sql
-- Check all module versions
SELECT 
    'identity' as module,
    version,
    description,
    applied_on
FROM identity.__FluentMigrator_VersionInfo
ORDER BY version DESC
LIMIT 5

UNION ALL

SELECT 
    'tenant' as module,
    version,
    description,
    applied_on
FROM tenant.__FluentMigrator_VersionInfo
ORDER BY version DESC
LIMIT 5;
```

### Logging Migration Execution

**Console logging**:

```
[2025-01-15 10:30:00] INFO: Starting migration orchestrator...
[2025-01-15 10:30:01] INFO: Discovered 5 modules: Identity, Tenant, Product, Notification, Audit
[2025-01-15 10:30:01] INFO: Migration order: Audit, Identity, Notification, Product, Tenant
[2025-01-15 10:30:02] INFO: Running migrations for module: Identity
[2025-01-15 10:30:03] INFO: Applied migration 20250115103000_AddUserPreferencesTable (1.2s)
[2025-01-15 10:30:04] INFO: Running migrations for module: Tenant
[2025-01-15 10:30:05] INFO: No pending migrations for module: Tenant
[2025-01-15 10:30:06] INFO: Migrations completed successfully
[2025-01-15 10:30:06] INFO: Total duration: 6.2s
```

**Structured logging** (JSON):

```json
{
  "timestamp": "2025-01-15T10:30:03Z",
  "level": "Information",
  "message": "Applied migration",
  "module": "Identity",
  "migration": "20250115103000_AddUserPreferencesTable",
  "duration_ms": 1200,
  "success": true
}
```

### Alerting on Migration Failures

**Slack notification**:

```bash
# In CI/CD pipeline
- name: Notify on migration failure
  if: failure()
  run: |
    curl -X POST ${{ secrets.SLACK_WEBHOOK }} \
      -H 'Content-Type: application/json' \
      -d '{
        "text": "🚨 Migration failed in Production",
        "blocks": [
          {
            "type": "section",
            "text": {
              "type": "mrkdwn",
              "text": "*Migration Failure*\n*Environment:* Production\n*Branch:* ${{ github.ref }}\n*Commit:* ${{ github.sha }}"
            }
          }
        ]
      }'
```

**Email notification**:

```csharp
// In MigrationOrchestrator
public async Task<MigrationResult> RunMigrationsAsync(...)
{
    try
    {
        // Run migrations...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Migration failed");
        
        await _emailService.SendAsync(new Email
        {
            To = "ops-team@datarizen.com",
            Subject = "Migration Failure - Production",
            Body = $"Migration failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}"
        });
        
        throw;
    }
}
```

**PagerDuty alert**:

```bash
# Trigger PagerDuty incident
curl -X POST https://events.pagerduty.com/v2/enqueue \
  -H 'Content-Type: application/json' \
  -d '{
    "routing_key": "${{ secrets.PAGERDUTY_ROUTING_KEY }}",
    "event_action": "trigger",
    "payload": {
      "summary": "Database migration failed in Production",
      "severity": "critical",
      "source": "GitHub Actions",
      "custom_details": {
        "environment": "Production",
        "branch": "${{ github.ref }}",
        "commit": "${{ github.sha }}"
      }
    }
  }'
```

### Tracking Migration History

**FluentMigrator version tables**:

Each module has its own version table:

```sql
-- identity.__FluentMigrator_VersionInfo
CREATE TABLE identity.__FluentMigrator_VersionInfo (
    version BIGINT PRIMARY KEY,
    description VARCHAR(255),
    applied_on TIMESTAMP NOT NULL
);

-- tenant.__FluentMigrator_VersionInfo
CREATE TABLE tenant.__FluentMigrator_VersionInfo (
    version BIGINT PRIMARY KEY,
    description VARCHAR(255),
    applied_on TIMESTAMP NOT NULL
);
```

**Query migration history**:

```sql
-- Get all migrations across all modules
SELECT 
    'identity' as module,
    version,
    description,
    applied_on
FROM identity.__FluentMigrator_VersionInfo

UNION ALL

SELECT 
    'tenant' as module,
    version,
    description,
    applied_on
FROM tenant.__FluentMigrator_VersionInfo

UNION ALL

SELECT 
    'product' as module,
    version,
    description,
    applied_on
FROM product.__FluentMigrator_VersionInfo

ORDER BY applied_on DESC;
```

### Dashboard for Migration Status

**Future consideration**: Admin UI to view migration status

**Features**:
- View current version for each module
- View pending migrations
- View migration history
- Trigger migrations (with approval)
- View migration logs

**Example UI**:

```
┌─────────────────────────────────────────────────────────┐
│ Database Migration Status                               │
├─────────────────────────────────────────────────────────┤
│ Module          Current Version    Pending   Last Run   │
├─────────────────────────────────────────────────────────┤
│ Identity        20250115103000     0         10:30 AM   │
│ Tenant          20250115104500     1         10:45 AM   │
│ Product         20250115105000     0         10:50 AM   │
│ Notification    20250115103000     0         10:30 AM   │
│ Audit           20250115103000     0         10:30 AM   │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Recent Migrations                                       │
├─────────────────────────────────────────────────────────┤
│ 2025-01-15 10:50 - Product - 20250115105000 - Success  │
│ 2025-01-15 10:45 - Tenant - 20250115104500 - Success   │
│ 2025-01-15 10:30 - Identity - 20250115103000 - Success │
└─────────────────────────────────────────────────────────┘
```

---

## Multi-Environment Strategy

### Environment Flow

```
Development → Staging → Production
```

**Development**:
- Local developer machines
- Docker Compose PostgreSQL
- Migrations run automatically on startup (optional)
- Frequent schema changes

**Staging**:
- Mirrors production environment
- Automated deployments from `develop` branch
- Automated migration execution
- Automated rollback on failure
- Integration testing

**Production**:
- Manual deployments from `main` branch
- Manual migration approval (or automated with safeguards)
- Manual rollback procedures
- Monitoring and alerting

### Environment-Specific Configuration

**appsettings.Development.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datarizen_dev;Username=datarizen;Password=dev_password"
  },
  "Migration": {
    "AutoMigrate": true,
    "RollbackOnFailure": true
  }
}
```

**appsettings.Staging.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=staging-db.internal;Database=datarizen_staging;Username=datarizen;Password=${STAGING_DB_PASSWORD}"
  },
  "Migration": {
    "AutoMigrate": true,
    "RollbackOnFailure": true,
    "NotifyOnFailure": true
  }
}
```

**appsettings.Production.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db.internal;Database=datarizen_prod;Username=datarizen;Password=${PROD_DB_PASSWORD}"
  },
  "Migration": {
    "AutoMigrate": false,
    "RequireApproval": true,
    "NotifyOnFailure": true,
    "AlertOnFailure": true
  }
}
```

### Connection String Management

**Development** (local):
```bash
# .env file (not committed)
ConnectionStrings__DefaultConnection="Host=localhost;Database=datarizen_dev;Username=datarizen;Password=dev_password"
```

**Staging/Production** (Kubernetes secrets):
```yaml
# k8s/staging/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: database-secrets
  namespace: staging
type: Opaque
stringData:
  connection-string: "Host=staging-db.internal;Database=datarizen_staging;Username=datarizen;Password=STAGING_PASSWORD"
```

**Injected into pods**:
```yaml
# k8s/staging/deployment.yaml
env:
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: database-secrets
        key: connection-string
```

### Environment-Specific Seed Data

**Development seed data**:

```csharp
[Migration(20250115103000, "Seed development data")]
[Profile("Development")]  // Only runs in Development
public class SeedDevelopmentData : Migration
{
    public override void Up()
    {
        Insert.IntoTable("users").InSchema("identity")
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                email = "admin@dev.local",
                password_hash = "hashed_password",
                is_active = true
            })
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                email = "user@dev.local",
                password_hash = "hashed_password",
                is_active = true
            });
    }

    public override void Down()
    {
        Delete.FromTable("users").InSchema("identity")
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000001") })
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000002") });
    }
}
```

**Production seed data**:

```csharp
[Migration(20250115103000, "Seed production reference data")]
[Profile("Production")]  // Only runs in Production
public class SeedProductionReferenceData : Migration
{
    public override void Up()
    {
        // Seed countries, currencies, etc.
        Insert.IntoTable("countries").InSchema("public")
            .Row(new { code = "US", name = "United States" })
            .Row(new { code = "CA", name = "Canada" })
            .Row(new { code = "GB", name = "United Kingdom" });
    }

    public override void Down()
    {
        Delete.FromTable("countries").InSchema("public").AllRows();
    }
}
```

---

## Common Scenarios and Troubleshooting

### Scenario 1: Migration Fails in Production

**Symptoms**:
- Migration runner exits with error
- Application fails to start
- Database in inconsistent state

**Diagnosis**:

```bash
# Check migration logs
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status

# Check database state
psql -h prod-db.example.com -U datarizen -d datarizen_prod

-- Check version tables
SELECT * FROM identity.__FluentMigrator_VersionInfo ORDER BY version DESC LIMIT 5;

-- Check if tables exist
\dt identity.*
```

**Resolution**:

**Option 1: Fix forward**:
```bash
# Create hotfix migration to fix the issue
# Example: Migration created invalid constraint

# New migration to drop invalid constraint
[Migration(20250115110000, "Fix invalid constraint")]
public class FixInvalidConstraint : Migration
{
    public override void Up()
    {
        Delete.ForeignKey("fk_invalid").OnTable("users").InSchema("identity");
    }
}

# Apply hotfix
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --module Identity
```

**Option 2: Rollback**:
```bash
# Rollback failed migration
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --module Identity \
  --rollback 1

# Deploy previous application version
kubectl rollout undo deployment/datarizen-api -n production
```

**Option 3: Manual fix**:
```sql
-- Connect to database
psql -h prod-db.example.com -U datarizen -d datarizen_prod

-- Manually fix the issue
ALTER TABLE identity.users DROP CONSTRAINT fk_invalid;

-- Update version table to mark migration as applied
INSERT INTO identity.__FluentMigrator_VersionInfo (version, description, applied_on)
VALUES (20250115103000, 'Add user preferences table', CURRENT_TIMESTAMP);
```

### Scenario 2: Partial Migration Failure Across Modules

**Symptoms**:
- Some modules migrated successfully
- Other modules failed
- Application partially working

**Example**:
```
✅ Identity - Migrated successfully
✅ Tenant - Migrated successfully
❌ Product - Migration failed
⏸️  Notification - Not attempted
⏸️  Audit - Not attempted
```

**Diagnosis**:

```bash
# Check status of all modules
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status

# Output shows which modules succeeded/failed
```

**Resolution**:

**Option 1: Fix and continue**:
```bash
# Fix the failing migration
# (Create hotfix or fix code)

# Re-run migrations (will skip already-applied migrations)
dotnet run --project server/src/MigrationRunner -- \
  --environment Production
```

**Option 2: Rollback all**:
```bash
# Rollback all modules to previous state
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --rollback 1

# This rolls back the last migration for each module
```

**Prevention**:
- Test migrations thoroughly in staging
- Use transactions where possible
- Implement retry logic for transient failures

### Scenario 3: Migration Conflicts

**Symptoms**:
- Two developers created migrations with same timestamp
- Git merge conflict in migration files
- Migration runner fails with duplicate version error

**Example**:

Developer A:
```csharp
[Migration(20250115103000, "Add user preferences")]
public class AddUserPreferences : Migration { ... }
```

Developer B:
```csharp
[Migration(20250115103000, "Add user roles")]
public class AddUserRoles : Migration { ... }
```

**Resolution**:

**Step 1: Identify conflict**:
```bash
# Git shows conflict
git status
# Unmerged paths:
#   both added: server/src/Modules/UserManagement/UserManagement.Migrations/Migrations/20250115103000_*.cs
```

**Step 2: Resolve conflict**:
```bash
# Developer B renames their migration
mv 20250115103000_AddUserRoles.cs 20250115103100_AddUserRoles.cs

# Update migration attribute
[Migration(20250115103100, "Add user roles")]  # Changed timestamp
```

**Step 3: Commit resolution**:
```bash
git add .
git commit -m "fix: resolve migration timestamp conflict"
```

**Prevention**:
- Use seconds component for uniqueness
- Communicate with team before creating migrations
- Pull latest changes before creating migration

### Scenario 4: Large-Scale Data Migration

**Symptoms**:
- Migration takes hours to complete
- Database locks causing application downtime
- Migration times out

**Example**:
```csharp
// ❌ Bad: Updates 10 million rows in one transaction
[Migration(20250115103000, "Backfill user preferences")]
public class BackfillUserPreferences : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            UPDATE identity.users 
            SET preferences_json = '{""theme"":""light""}'
            WHERE preferences_json IS NULL
        ");
        // This locks the entire table for hours!
    }
}
```

**Resolution**:

**Option 1: Batch processing**:
```csharp
[Migration(20250115103000, "Backfill user preferences")]
public class BackfillUserPreferences : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            DO $$
            DECLARE
                batch_size INT := 10000;
                rows_affected INT;
            BEGIN
                LOOP
                    UPDATE identity.users
                    SET preferences_json = '{""theme"":""light""}'
                    WHERE preferences_json IS NULL
                    AND id IN (
                        SELECT id FROM identity.users 
                        WHERE preferences_json IS NULL 
                        LIMIT batch_size
                    );
                    
                    GET DIAGNOSTICS rows_affected = ROW_COUNT;
                    EXIT WHEN rows_affected = 0;
                    
                    RAISE NOTICE 'Processed % rows', rows_affected;
                    COMMIT;  -- Commit each batch
                    PERFORM pg_sleep(0.1);  -- Throttle
                END LOOP;
            END $$;
        ");
    }
}
```

**Option 2: Background job**:
```csharp
// Migration 1: Add column (nullable)
[Migration(20250115103000, "Add preferences column")]
public class AddPreferencesColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users").InSchema("identity")
            .AddColumn("preferences_json").AsString(int.MaxValue).Nullable();
    }
}

// Deploy application with background job to backfill data

// Migration 2: Make column non-nullable (after backfill complete)
[Migration(20250120103000, "Make preferences non-nullable")]
public class MakePreferencesNonNullable : Migration
{
    public override void Up()
    {
        Alter.Column("preferences_json")
            .OnTable("users").InSchema("identity")
            .AsString(int.MaxValue).NotNullable();
    }
}
```

**Option 3: Online schema change** (PostgreSQL 11+):
```sql
-- Use pg_repack or similar tools for large table modifications
-- Avoids long-running locks
```

### Scenario 5: Recovering from Data Loss

**Symptoms**:
- Migration accidentally deleted data
- Data corruption detected
- Need to restore specific records

**Example**:
```csharp
// ❌ Oops: Deleted all users instead of inactive users
[Migration(20250115103000, "Delete inactive users")]
public class DeleteInactiveUsers : Migration
{
    public override void Up()
    {
        Execute.Sql("DELETE FROM identity.users");  // Missing WHERE clause!
    }
}
```

**Resolution**:

**Step 1: Stop the bleeding**:
```bash
# Immediately stop application
kubectl scale deployment/datarizen-api --replicas=0 -n production

# Prevent further migrations
# (Remove migration runner from deployment pipeline)
```

**Step 2: Assess damage**:
```sql
-- Check how many records were deleted
SELECT COUNT(*) FROM identity.users;  -- Should be 0 if all deleted

-- Check backup
pg_restore --list backup_20250115_100000.dump | grep users
```

**Step 3: Restore data**:
```bash
# Restore to temporary database
createdb datarizen_temp
pg_restore -h prod-db.example.com -U datarizen -d datarizen_temp \
  backup_20250115_100000.dump

# Extract deleted users
pg_dump -h prod-db.example.com -U datarizen -d datarizen_temp \
  --table=identity.users \
  --data-only \
  --file=deleted_users.sql

# Import into production
psql -h prod-db.example.com -U datarizen -d datarizen_prod < deleted_users.sql

# Verify
psql -h prod-db.example.com -U datarizen -d datarizen_prod \
  -c "SELECT COUNT(*) FROM identity.users"

# Drop temporary database
dropdb datarizen_temp
```

**Step 4: Fix migration**:
```csharp
// Corrected migration
[Migration(20250115103000, "Delete inactive users")]
public class DeleteInactiveUsers : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            DELETE FROM identity.users 
            WHERE is_active = false 
            AND last_login_at < NOW() - INTERVAL '1 year'
        ");
    }
}
```

**Step 5: Restart application**:
```bash
kubectl scale deployment/datarizen-api --replicas=3 -n production
```

**Prevention**:
- Always test migrations in staging first
- Use soft deletes instead of hard deletes
- Implement audit logging
- Require code review for data migrations
- Backup before production migrations

---

## Summary

### Key Takeaways

1. **Migration Development**:
   - Use timestamp-based versioning
   - Write both `Up()` and `Down()` methods
   - Test locally before committing

2. **Version Control**:
   - Commit migrations with application code
   - Never modify merged migrations
   - Coordinate with team to avoid conflicts

3. **Schema vs Data**:
   - Separate schema and data migrations
   - Use batching for large data migrations
   - Consider background jobs for very large datasets

4. **Testing**:
   - Test migrations locally
   - Automated testing in CI/CD
   - Verify rollback capability

5. **Deployment**:
   - Backup before production migrations
   - Automated in staging, manual in production
   - Zero-downtime strategies for critical changes

6. **Rollback**:
   - Automated rollback in staging
   - Manual rollback in production
   - Database restore as last resort
git commit -m "feat(user-management): add user preferences migration"

# Push to remote
git push origin feature/user-preferences

# Create pull request
# ... code review ...

# Merge to main
git checkout main
git merge feature/user-preferences
```

**Important Rules**:
- ✅ Always create migrations in feature branches
- ✅ Never modify existing migrations that have been merged to `main`
- ✅ If you need to change a merged migration, create a new migration
- ✅ Coordinate with team if multiple developers are working on same module

### Code Review Process

**Migration Pull Request Checklist**:

Reviewer should verify:
- [ ] Migration timestamp is unique and correct
- [ ] Migration name is descriptive
- [ ] Schema is explicitly specified (`.InSchema("module_name")`)
- [ ] `Up()` and `Down()` methods are both implemented
- [ ] `Down()` correctly reverses `Up()` changes
- [ ] No cross-schema foreign keys
- [ ] Indexes are created for foreign key columns
- [ ] Data migrations are idempotent (can run multiple times)
- [ ] Long-running migrations use batching
- [ ] Migration has been tested locally

**Example PR Description**:

```markdown
## Migration: Add User Preferences Table

**Module**: UserManagement
**Migration**: 20250115103000_AddUserPreferencesTable

### Changes
- Creates `user_preferences` table in `user_management` schema
- Adds index on `user_id` column
- Supports theme, language, and timezone preferences

### Testing
- [x] Tested migration up locally
- [x] Tested migration down (rollback)
- [x] Verified application works with new schema
- [x] Tested against PostgreSQL 16

### Rollback Plan
Migration can be rolled back safely using `Down()` method.
No data loss as this is a new table.
```

### Handling Migration Conflicts

**Scenario**: Two developers create migrations with same timestamp

**Developer A** (created first):
```csharp
[Migration(20250115103000, "Add user preferences table")]
public class AddUserPreferencesTable : Migration { ... }
```

**Developer B** (created later, same timestamp):
```csharp
[Migration(20250115103000, "Add user roles table")]
public class AddUserRolesTable : Migration { ... }
```

**Resolution**:

1. **Developer B** must update their migration timestamp:
```bash
# Rename file
mv 20250115103000_AddUserRolesTable.cs 20250115103100_AddUserRolesTable.cs
```

2. Update migration attribute:
```csharp
[Migration(20250115103100, "Add user roles table")]  // Changed timestamp
public class AddUserRolesTable : Migration { ... }
```

3. Rebase and resolve conflicts:
```bash
git fetch origin
git rebase origin/main
# Resolve any conflicts
git push origin feature/user-roles --force-with-lease
```

**Prevention**:
- Communicate with team when creating migrations
- Use seconds component for uniqueness (103000, 103001, 103002)
- Check `main` branch before creating migration

---

## Schema Migrations vs Data Migrations

### Schema Migrations (DDL)

**Purpose**: Modify database structure (tables, columns, indexes, constraints)

**Characteristics**:
- Fast execution (usually < 1 second)
- Transactional (all-or-nothing)
- Reversible via `Down()` method
- No data processing

**Examples**:

```csharp
// Create table
Create.Table("users").InSchema("identity")
    .WithColumn("id").AsGuid().PrimaryKey()
    .WithColumn("email").AsString(255).NotNullable();

// Add column
Alter.Table("users").InSchema("identity")
    .AddColumn("phone_number").AsString(20).Nullable();

// Create index
Create.Index("idx_users_email")
    .OnTable("users").InSchema("identity")
    .OnColumn("email").Unique();

// Add constraint
Create.UniqueConstraint("uq_users_email")
    .OnTable("users").InSchema("identity")
    .Column("email");
```

### Data Migrations (DML)

**Purpose**: Modify data (insert, update, delete records)

**Characteristics**:
- Can be slow (depends on data volume)
- May require batching for large datasets
- May not be fully reversible
- Requires careful testing

**Examples**:

```csharp
// Seed data
Insert.IntoTable("roles").InSchema("identity")
    .Row(new { id = Guid.NewGuid(), name = "Admin" })
    .Row(new { id = Guid.NewGuid(), name = "User" });

// Update data
Update.Table("users").InSchema("identity")
    .Set(new { is_active = true })
    .Where(new { email_verified = true });

// Delete data
Delete.FromTable("users").InSchema("identity")
    .Row(new { email = "old@example.com" });
```

### Best Practices

**Separate Schema and Data Migrations**:

✅ **Good** (separate migrations):
```csharp
// Migration 1: Schema change
[Migration(20250115103000, "Add is_premium column to users")]
public class AddIsPremiumColumnToUsers : Migration
{
    public override void Up()
    {
        Alter.Table("users").InSchema("identity")
            .AddColumn("is_premium").AsBoolean().NotNullable().WithDefaultValue(false);
    }
    
    public override void Down()
    {
        Delete.Column("is_premium").FromTable("users").InSchema("identity");
    }
}

// Migration 2: Data migration
[Migration(20250115103100, "Set premium status for existing users")]
public class SetPremiumStatusForExistingUsers : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            UPDATE identity.users 
            SET is_premium = true 
            WHERE subscription_tier = 'premium'
        ");
    }
    
    public override void Down()
    {
        Execute.Sql(@"
            UPDATE identity.users 
            SET is_premium = false
        ");
    }
}
```

❌ **Bad** (combined):
```csharp
[Migration(20250115103000, "Add and populate is_premium column")]
public class AddAndPopulateIsPremiumColumn : Migration
{
    public override void Up()
    {
        // Schema change
        Alter.Table("users").InSchema("identity")
            .AddColumn("is_premium").AsBoolean().NotNullable().WithDefaultValue(false);
        
        // Data change (mixed with schema)
        Execute.Sql(@"
            UPDATE identity.users 
            SET is_premium = true 
            WHERE subscription_tier = 'premium'
        ");
    }
    
    // Down() is complex and error-prone
}
```

**When to Combine**:

Only combine schema and data migrations when:
- The data migration is trivial (e.g., setting default values)
- The migration is small and fast
- Rollback is straightforward

### Handling Long-Running Data Migrations

For migrations that process millions of rows:

**Use Batching**:

```csharp
[Migration(20250115103000, "Backfill user preferences")]
public class BackfillUserPreferences : Migration
{
    public override void Up()
    {
        // Add column first
        Alter.Table("users").InSchema("identity")
            .AddColumn("preferences_json").AsString(int.MaxValue).Nullable();

        // Batch update (PostgreSQL)
        Execute.Sql(@"
            DO $$
            DECLARE
                batch_size INT := 1000;
                rows_affected INT;
            BEGIN
                LOOP
                    UPDATE identity.users
                    SET preferences_json = '{""theme"":""light"",""language"":""en""}'
                    WHERE preferences_json IS NULL
                    AND id IN (
                        SELECT id FROM identity.users 
                        WHERE preferences_json IS NULL 
                        LIMIT batch_size
                    );
                    
                    GET DIAGNOSTICS rows_affected = ROW_COUNT;
                    EXIT WHEN rows_affected = 0;
                    
                    RAISE NOTICE 'Processed % rows', rows_affected;
                    PERFORM pg_sleep(0.1); -- Throttle
                END LOOP;
            END $$;
        ");
    }

    public override void Down()
    {
        Delete.Column("preferences_json").FromTable("users").InSchema("identity");
    }
}
```

**Alternative: Background Job**:

For very large datasets, consider:
1. Add column in migration (nullable)
2. Deploy application
3. Run background job to backfill data
4. Create follow-up migration to make column non-nullable

```csharp
// Migration 1: Add nullable column
[Migration(20250115103000, "Add preferences column")]
public class AddPreferencesColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users").InSchema("identity")
            .AddColumn("preferences_json").AsString(int.MaxValue).Nullable();
    }
}

// Background job backfills data over hours/days

// Migration 2: Make column non-nullable (after backfill complete)
[Migration(20250120103000, "Make preferences column non-nullable")]
public class MakePreferencesColumnNonNullable : Migration
{
    public override void Up()
    {
        Alter.Column("preferences_json")
            .OnTable("users").InSchema("identity")
            .AsString(int.MaxValue).NotNullable();
    }
}
```

---

## Local Development Testing

### Setting Up Local Database

**Using Docker Compose**:

```yaml
# docker-compose.dev.yml
version: '3.8'

services:
  postgres:
    image: postgres:16
    container_name: datarizen_postgres_dev
    environment:
      POSTGRES_DB: datarizen_dev
      POSTGRES_USER: datarizen
      POSTGRES_PASSWORD: dev_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_dev_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U datarizen"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_dev_data:
```

**Start database**:
```bash
docker-compose -f docker-compose.dev.yml up -d
```

### Running Migrations Locally

**Run all module migrations**:
```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Development
```

**Run specific module migration**:
```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module UserManagement
```

**Dry-run (preview without applying)**:
```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --dry-run
```

**Rollback last migration**:
```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module UserManagement \
  --rollback 1
```

**Rollback to specific version**:
```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Development \
  --module UserManagement \
  --target-version 20250115103000
```

### Verifying Migration Success

**Check migration history**:
```bash
psql -h localhost -U datarizen -d datarizen_dev

-- View all migrations for a module
SELECT version, description, applied_on 
FROM user_management.__FluentMigrator_VersionInfo 
ORDER BY version DESC;

-- Check table exists
\dt user_management.*

-- Describe table structure
\d user_management.user_preferences
```

**Verify application works**:
```bash
# Start API
dotnet run --project server/src/Datarizen.Api

# Test endpoints
curl http://localhost:5000/api/users/preferences
```

### Testing Against Different Database Providers

**PostgreSQL** (primary):
```bash
# Connection string in appsettings.Development.json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=datarizen_dev;Username=datarizen;Password=dev_password"
}
```

**SQL Server** (optional):
```
# Start SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name datarizen_sqlserver_dev \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Connection string
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=datarizen_dev;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
}
```

**MySQL** (optional):
```bash
# Start MySQL in Docker
docker run --name datarizen_mysql_dev \
  -e MYSQL_ROOT_PASSWORD=dev_password \
  -e MYSQL_DATABASE=datarizen_dev \
  -p 3306:3306 -d mysql:8.0

# Connection string
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=datarizen_dev;User=root;Password=dev_password"
}
```

### Local Testing Checklist

Before committing a migration:

- [ ] Migration builds successfully
- [ ] Migration applies successfully (`Up()`)
- [ ] Database schema is correct (verify with SQL client)
- [ ] Application starts and works with new schema
- [ ] Migration rolls back successfully (`Down()`)
- [ ] Migration can be re-applied after rollback
- [ ] No errors in application logs
- [ ] Unit tests pass (if applicable)
- [ ] Integration tests pass (if applicable)

---

## CI/CD Pipeline Integration

### GitHub Actions Workflow

**`.github/workflows/database-migrations.yml`**:

```yaml
name: Database Migrations

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  test-migrations:
    name: Test Migrations
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_DB: datarizen_test
          POSTGRES_USER: datarizen
          POSTGRES_PASSWORD: test_password
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore server/Datarizen.sln

      - name: Build solution
        run: dotnet build server/Datarizen.sln --configuration Release --no-restore

      - name: Run migrations
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=datarizen_test;Username=datarizen;Password=test_password"
        run: |
          dotnet run --project server/src/MigrationRunner -- \
            --environment Test \
            --verbose

      - name: Verify migrations
        env:
          PGPASSWORD: test_password
        run: |
          # Check all modules have version tables
          psql -h localhost -U datarizen -d datarizen_test -c "\dt *.__FluentMigrator_VersionInfo"
          
          # Verify schemas exist
          psql -h localhost -U datarizen -d datarizen_test -c "\dn"

      - name: Test rollback
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=datarizen_test;Username=datarizen;Password=test_password"
        run: |
          # Rollback last migration for each module
          dotnet run --project server/src/MigrationRunner -- \
            --environment Test \
            --rollback 1 \
            --verbose

      - name: Re-apply migrations
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=datarizen_test;Username=datarizen;Password=test_password"
        run: |
          dotnet run --project server/src/MigrationRunner -- \
            --environment Test \
            --verbose

      - name: Generate migration scripts
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=datarizen_test;Username=datarizen;Password=test_password"
        run: |
          dotnet run --project server/src/MigrationRunner -- \
            --environment Test \
            --generate-script \
            --output migrations.sql

      - name: Upload migration scripts
        uses: actions/upload-artifact@v4
        with:
          name: migration-scripts
          path: migrations.sql
          retention-days: 30

      - name: Notify on failure
        if: failure()
        run: |
          echo "::error::Migration tests failed. Please review the logs."
```

### Validation Checks

**Syntax Validation**:
```bash
# Build all migration projects
dotnet build server/src/Modules/*/*.Migrations/*.csproj
```

**Idempotency Check**:
```bash
# Apply migrations twice - should succeed both times
dotnet run --project server/src/MigrationRunner -- --environment Test
dotnet run --project server/src/MigrationRunner -- --environment Test
```

**Rollback Capability**:
```bash
# Apply migrations
dotnet run --project server/src/MigrationRunner -- --environment Test

# Rollback
dotnet run --project server/src/MigrationRunner -- --environment Test --rollback 1

# Re-apply
dotnet run --project server/src/MigrationRunner -- --environment Test
```

### Generating Migration Scripts

For production deployments, generate SQL scripts for DBA review:

```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --generate-script \
  --output production-migrations-$(date +%Y%m%d).sql
```

**Generated script example**:
```sql
-- Migration Script Generated: 2025-01-15 10:30:00
-- Environment: Production
-- Modules: Identity, Tenant, Product, Notification, Audit

-- ============================================
-- Module: Identity
-- ============================================

-- Migration: 20250115103000_AddUserPreferencesTable
CREATE TABLE identity.user_preferences (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    theme VARCHAR(50) NOT NULL DEFAULT 'light',
    language VARCHAR(10) NOT NULL DEFAULT 'en',
    timezone VARCHAR(50) NOT NULL DEFAULT 'UTC',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_user_preferences_user_id ON identity.user_preferences(user_id);

INSERT INTO identity.__FluentMigrator_VersionInfo (version, description, applied_on)
VALUES (20250115103000, 'Add user preferences table', CURRENT_TIMESTAMP);

-- ============================================
-- Module: Tenant
-- ============================================

-- (Additional migrations...)
```

### Failing the Build

**Migration failure fails the build**:

```yaml
- name: Run migrations
  run: |
    dotnet run --project server/src/MigrationRunner -- --environment Test
  # If migrations fail, step fails and build fails
```

**Custom validation**:

```yaml
- name: Validate migrations
  run: |
    # Check for cross-schema foreign keys (not allowed)
    if grep -r "REFERENCES.*\." server/src/Modules/*/Migrations/; then
      echo "::error::Cross-schema foreign keys detected!"
      exit 1
    fi
    
    # Check for missing Down() methods
    if grep -L "public override void Down()" server/src/Modules/*/*.Migrations/Migrations/*.cs; then
      echo "::error::Migrations missing Down() method!"
      exit 1
    fi
```

---

## Production Deployment Process

### Pre-Deployment Checklist

**1. Database Backup**:

```bash
# PostgreSQL backup
pg_dump -h prod-db.example.com -U datarizen -d datarizen_prod \
  --format=custom \
  --file=backup_$(date +%Y%m%d_%H%M%S).dump

# Verify backup
pg_restore --list backup_20250115_103000.dump | head -20
```

**2. Migration Review**:

- [ ] Review generated SQL scripts
- [ ] Verify no cross-schema dependencies
- [ ] Check for long-running migrations
- [ ] Estimate migration duration
- [ ] Plan maintenance window if needed
- [ ] Notify stakeholders of deployment

**3. Rollback Plan**:

- [ ] Database backup completed
- [ ] Rollback procedure documented
- [ ] Team available for rollback if needed
- [ ] Monitoring alerts configured

### Automated Migration Execution

**Recommended for staging environments**:

```yaml
# .github/workflows/deploy-staging.yml
name: Deploy to Staging

on:
  push:
    branches: [develop]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Backup database
        run: |
          pg_dump -h ${{ secrets.STAGING_DB_HOST }} \
            -U ${{ secrets.STAGING_DB_USER }} \
            -d datarizen_staging \
            --format=custom \
            --file=backup_$(date +%Y%m%d_%H%M%S).dump
        env:
          PGPASSWORD: ${{ secrets.STAGING_DB_PASSWORD }}

      - name: Run migrations
        run: |
          dotnet run --project server/src/MigrationRunner -- \
            --environment Staging \
            --verbose
        env:
          ConnectionStrings__DefaultConnection: ${{ secrets.STAGING_CONNECTION_STRING }}

      - name: Deploy application
        run: |
          # Deploy to staging environment
          ./scripts/deploy-staging.sh

      - name: Rollback on failure
        if: failure()
        run: |
          echo "Deployment failed. Rolling back migrations..."
          dotnet run --project server/src/MigrationRunner -- \
            --environment Staging \
            --rollback 1
        env:
          ConnectionStrings__DefaultConnection: ${{ secrets.STAGING_CONNECTION_STRING }}
```

### Manual Migration Execution

**Recommended for production environments**:

**Step 1: Generate migration script**:
```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --generate-script \
  --output production-migrations-20250115.sql
```

**Step 2: DBA reviews script**:
- Check for syntax errors
- Estimate execution time
- Verify rollback procedures
- Approve for execution

**Step 3: Execute during maintenance window**:
```bash
# Connect to production database
psql -h prod-db.example.com -U datarizen -d datarizen_prod

-- Execute migration script
\i production-migrations-20250115.sql

-- Verify migrations
SELECT * FROM identity.__FluentMigrator_VersionInfo ORDER BY version DESC LIMIT 5;
SELECT * FROM tenant.__FluentMigrator_VersionInfo ORDER BY version DESC LIMIT 5;
-- (Check all modules)
```

**Step 4: Deploy application**:
```bash
# Deploy new application version
kubectl apply -f k8s/production/deployment.yaml

# Wait for rollout
kubectl rollout status deployment/datarizen-api -n production
```

### Blue-Green Deployment

For zero-downtime deployments:

**Approach 1: Backward-compatible migrations**:

```csharp
// Phase 1: Add new column (nullable)
[Migration(20250115103000, "Add email_verified column")]
public class AddEmailVerifiedColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users").InSchema("identity")
            .AddColumn("email_verified").AsBoolean().Nullable();
    }
}

// Deploy application v2 (uses email_verified if present, falls back to old logic)

// Phase 2: Backfill data
[Migration(20250116103000, "Backfill email_verified")]
public class BackfillEmailVerified : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            UPDATE identity.users 
            SET email_verified = true 
            WHERE email_confirmation_token IS NULL
        ");
    }
}

// Phase 3: Make column non-nullable
[Migration(20250117103000, "Make email_verified non-nullable")]
public class MakeEmailVerifiedNonNullable : Migration
{
    public override void Up()
    {
        Alter.Column("email_verified")
            .OnTable("users").InSchema("identity")
            .AsBoolean().NotNullable();
    }
}

// Remove old application v1
```

**Approach 2: Expand-contract pattern**:

1. **Expand**: Add new schema alongside old schema
2. **Migrate**: Dual-write to both old and new schema
3. **Contract**: Remove old schema after migration complete

### Zero-Downtime Migration Strategies

**Adding a column**:
```csharp
// ✅ Safe: Add nullable column
Alter.Table("users").InSchema("identity")
    .AddColumn("phone_number").AsString(20).Nullable();

// ❌ Unsafe: Add non-nullable column without default
Alter.Table("users").InSchema("identity")
    .AddColumn("phone_number").AsString(20).NotNullable();
```

**Renaming a column**:
```csharp
// ❌ Unsafe: Direct rename breaks old application
Rename.Column("email").OnTable("users").InSchema("identity").To("email_address");

// ✅ Safe: Expand-contract pattern
// Step 1: Add new column
Alter.Table("users").InSchema("identity")
    .AddColumn("email_address").AsString(255).Nullable();

// Step 2: Backfill data
Execute.Sql("UPDATE identity.users SET email_address = email");

// Step 3: Deploy application v2 (uses email_address)

// Step 4: Remove old column (in next migration)
Delete.Column("email").FromTable("users").InSchema("identity");
```

**Dropping a column**:
```csharp
// ❌ Unsafe: Immediate drop breaks old application
Delete.Column("old_field").FromTable("users").InSchema("identity");

// ✅ Safe: Two-phase approach
// Phase 1: Deploy application v2 (doesn't use old_field)
// Phase 2: Drop column in next migration
[Migration(20250116103000, "Drop old_field column")]
public class DropOldFieldColumn : Migration
{
    public override void Up()
    {
        Delete.Column("old_field").FromTable("users").InSchema("identity");
    }
}
```

### Post-Deployment Verification

**Check migration status**:
```bash
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status
```

**Verify application health**:
```bash
# Health check endpoint
curl https://api.datarizen.com/health

# Check logs
kubectl logs -f deployment/datarizen-api -n production

# Monitor metrics
# (Check Grafana/Prometheus dashboards)
```

**Smoke tests**:
```bash
# Test critical endpoints
curl https://api.datarizen.com/api/users/me
curl https://api.datarizen.com/api/tenants
curl https://api.datarizen.com/api/products
```

---

## Rollback Strategy

### When to Rollback

**Automatic rollback triggers** (staging):
- Migration fails to apply
- Application fails to start after migration
- Health checks fail after deployment

**Manual rollback triggers** (production):
- Critical bug discovered after deployment
- Performance degradation
- Data corruption detected
- Business decision to revert

### Automated Rollback (Staging)

```yaml
# .github/workflows/deploy-staging.yml
- name: Deploy to Staging
  id: deploy
  run: ./scripts/deploy-staging.sh

- name: Health check
  id: health_check
  run: |
    sleep 30  # Wait for app to start
    curl -f https://staging.datarizen.com/health || exit 1

- name: Rollback on failure
  if: failure()
  run: |
    echo "Deployment failed. Rolling back..."
    
    # Rollback migrations
    dotnet run --project server/src/MigrationRunner -- \
      --environment Staging \
      --rollback 1
    
    # Rollback application
    kubectl rollout undo deployment/datarizen-api -n staging
    
    # Notify team
    curl -X POST ${{ secrets.SLACK_WEBHOOK }} \
      -d '{"text":"Staging deployment failed and was rolled back"}'
```

### Manual Rollback (Production)

**Step 1: Assess the situation**:
```bash
# Check migration status
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status

# Check application logs
kubectl logs deployment/datarizen-api -n production --tail=100

# Check database state
psql -h prod-db.example.com -U datarizen -d datarizen_prod
```

**Step 2: Rollback migrations**:

**Option A: Rollback via MigrationRunner**:
```bash
# Rollback last migration for all modules
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --rollback 1

# Or rollback specific module
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --module UserManagement \
  --rollback 1
```

**Option B: Manual SQL rollback**:
```sql
-- Connect to production database
psql -h prod-db.example.com -U datarizen -d datarizen_prod

-- Check migration history
SELECT * FROM identity.__FluentMigrator_VersionInfo ORDER BY version DESC LIMIT 5;

-- Manually execute Down() migration SQL
-- (Generated from migration code)
DROP TABLE identity.user_preferences;

-- Remove migration from version table
DELETE FROM identity.__FluentMigrator_VersionInfo WHERE version = 20250115103000;
```

**Step 3: Rollback application**:
```bash
# Kubernetes rollback
kubectl rollout undo deployment/datarizen-api -n production

# Or deploy previous version
kubectl set image deployment/datarizen-api \
  datarizen-api=datarizen/api:v1.2.0 \
  -n production
```

**Step 4: Verify rollback**:
```bash
# Check application health
curl https://api.datarizen.com/health

# Check migration status
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status

# Smoke tests
curl https://api.datarizen.com/api/users/me
```

### Database Restore (Last Resort)

**When to restore from backup**:
- Migration caused data corruption
- Rollback migration failed
- Multiple migrations need to be rolled back
- Data loss occurred

**Restore procedure**:

```bash
# 1. Stop application
kubectl scale deployment/datarizen-api --replicas=0 -n production

# 2. Restore database
pg_restore -h prod-db.example.com -U datarizen -d datarizen_prod \
  --clean --if-exists \
  backup_20250115_103000.dump

# 3. Verify restore
psql -h prod-db.example.com -U datarizen -d datarizen_prod -c "\dt"

# 4. Deploy previous application version
kubectl set image deployment/datarizen-api \
  datarizen-api=datarizen/api:v1.2.0 \
  -n production

# 5. Scale up application
kubectl scale deployment/datarizen-api --replicas=3 -n production

# 6. Verify application
curl https://api.datarizen.com/health
```

### Handling Data Loss

**Scenario**: Migration deleted data that cannot be recovered

**Prevention**:
- Always backup before production migrations
- Test migrations thoroughly in staging
- Use soft deletes instead of hard deletes
- Implement audit logging

**Recovery**:
1. Restore from backup to temporary database
2. Extract lost data
3. Import data into production database
4. Verify data integrity

```bash
# Restore to temporary database
createdb datarizen_temp
pg_restore -h prod-db.example.com -U datarizen -d datarizen_temp \
  backup_20250115_103000.dump

# Extract lost data
pg_dump -h prod-db.example.com -U datarizen -d datarizen_temp \
  --table=identity.users \
  --data-only \
  --file=lost_users.sql

# Import into production
psql -h prod-db.example.com -U datarizen -d datarizen_prod < lost_users.sql

# Drop temporary database
dropdb datarizen_temp
```

---

## Monitoring and Observability

### Checking Migration Status

**Via MigrationRunner CLI**:

```bash
# Check status for all modules
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status

# Output:
# Module: Identity
#   Current Version: 20250115103000
#   Pending Migrations: 0
#   Last Applied: 2025-01-15 10:30:00
#
# Module: Tenant
#   Current Version: 20250115104500
#   Pending Migrations: 1
#   Last Applied: 2025-01-15 10:45:00
#   Pending: 20250116103000_AddTenantSettings
```

**Via SQL**:

```sql
-- Check all module versions
SELECT 
    'identity' as module,
    version,
    description,
    applied_on
FROM identity.__FluentMigrator_VersionInfo
ORDER BY version DESC
LIMIT 5

UNION ALL

SELECT 
    'tenant' as module,
    version,
    description,
    applied_on
FROM tenant.__FluentMigrator_VersionInfo
ORDER BY version DESC
LIMIT 5;
```

### Logging Migration Execution

**Console logging**:

```
[2025-01-15 10:30:00] INFO: Starting migration orchestrator...
[2025-01-15 10:30:01] INFO: Discovered 5 modules: Identity, Tenant, Product, Notification, Audit
[2025-01-15 10:30:01] INFO: Migration order: Audit, Identity, Notification, Product, Tenant
[2025-01-15 10:30:02] INFO: Running migrations for module: Identity
[2025-01-15 10:30:03] INFO: Applied migration 20250115103000_AddUserPreferencesTable (1.2s)
[2025-01-15 10:30:04] INFO: Running migrations for module: Tenant
[2025-01-15 10:30:05] INFO: No pending migrations for module: Tenant
[2025-01-15 10:30:06] INFO: Migrations completed successfully
[2025-01-15 10:30:06] INFO: Total duration: 6.2s
```

**Structured logging** (JSON):

```json
{
  "timestamp": "2025-01-15T10:30:03Z",
  "level": "Information",
  "message": "Applied migration",
  "module": "Identity",
  "migration": "20250115103000_AddUserPreferencesTable",
  "duration_ms": 1200,
  "success": true
}
```

### Alerting on Migration Failures

**Slack notification**:

```bash
# In CI/CD pipeline
- name: Notify on migration failure
  if: failure()
  run: |
    curl -X POST ${{ secrets.SLACK_WEBHOOK }} \
      -H 'Content-Type: application/json' \
      -d '{
        "text": "🚨 Migration failed in Production",
        "blocks": [
          {
            "type": "section",
            "text": {
              "type": "mrkdwn",
              "text": "*Migration Failure*\n*Environment:* Production\n*Branch:* ${{ github.ref }}\n*Commit:* ${{ github.sha }}"
            }
          }
        ]
      }'
```

**Email notification**:

```csharp
// In MigrationOrchestrator
public async Task<MigrationResult> RunMigrationsAsync(...)
{
    try
    {
        // Run migrations...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Migration failed");
        
        await _emailService.SendAsync(new Email
        {
            To = "ops-team@datarizen.com",
            Subject = "Migration Failure - Production",
            Body = $"Migration failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}"
        });
        
        throw;
    }
}
```

**PagerDuty alert**:

```bash
# Trigger PagerDuty incident
curl -X POST https://events.pagerduty.com/v2/enqueue \
  -H 'Content-Type: application/json' \
  -d '{
    "routing_key": "${{ secrets.PAGERDUTY_ROUTING_KEY }}",
    "event_action": "trigger",
    "payload": {
      "summary": "Database migration failed in Production",
      "severity": "critical",
      "source": "GitHub Actions",
      "custom_details": {
        "environment": "Production",
        "branch": "${{ github.ref }}",
        "commit": "${{ github.sha }}"
      }
    }
  }'
```

### Tracking Migration History

**FluentMigrator version tables**:

Each module has its own version table:

```sql
-- identity.__FluentMigrator_VersionInfo
CREATE TABLE identity.__FluentMigrator_VersionInfo (
    version BIGINT PRIMARY KEY,
    description VARCHAR(255),
    applied_on TIMESTAMP NOT NULL
);

-- tenant.__FluentMigrator_VersionInfo
CREATE TABLE tenant.__FluentMigrator_VersionInfo (
    version BIGINT PRIMARY KEY,
    description VARCHAR(255),
    applied_on TIMESTAMP NOT NULL
);
```

**Query migration history**:

```sql
-- Get all migrations across all modules
SELECT 
    'identity' as module,
    version,
    description,
    applied_on
FROM identity.__FluentMigrator_VersionInfo

UNION ALL

SELECT 
    'tenant' as module,
    version,
    description,
    applied_on
FROM tenant.__FluentMigrator_VersionInfo

UNION ALL

SELECT 
    'product' as module,
    version,
    description,
    applied_on
FROM product.__FluentMigrator_VersionInfo

ORDER BY applied_on DESC;
```

### Dashboard for Migration Status

**Future consideration**: Admin UI to view migration status

**Features**:
- View current version for each module
- View pending migrations
- View migration history
- Trigger migrations (with approval)
- View migration logs

**Example UI**:

```
┌─────────────────────────────────────────────────────────┐
│ Database Migration Status                               │
├─────────────────────────────────────────────────────────┤
│ Module          Current Version    Pending   Last Run   │
├─────────────────────────────────────────────────────────┤
│ Identity        20250115103000     0         10:30 AM   │
│ Tenant          20250115104500     1         10:45 AM   │
│ Product         20250115105000     0         10:50 AM   │
│ Notification    20250115103000     0         10:30 AM   │
│ Audit           20250115103000     0         10:30 AM   │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Recent Migrations                                       │
├─────────────────────────────────────────────────────────┤
│ 2025-01-15 10:50 - Product - 20250115105000 - Success  │
│ 2025-01-15 10:45 - Tenant - 20250115104500 - Success   │
│ 2025-01-15 10:30 - Identity - 20250115103000 - Success │
└─────────────────────────────────────────────────────────┘
```

---

## Multi-Environment Strategy

### Environment Flow

```
Development → Staging → Production
```

**Development**:
- Local developer machines
- Docker Compose PostgreSQL
- Migrations run automatically on startup (optional)
- Frequent schema changes

**Staging**:
- Mirrors production environment
- Automated deployments from `develop` branch
- Automated migration execution
- Automated rollback on failure
- Integration testing

**Production**:
- Manual deployments from `main` branch
- Manual migration approval (or automated with safeguards)
- Manual rollback procedures
- Monitoring and alerting

### Environment-Specific Configuration

**appsettings.Development.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datarizen_dev;Username=datarizen;Password=dev_password"
  },
  "Migration": {
    "AutoMigrate": true,
    "RollbackOnFailure": true
  }
}
```

**appsettings.Staging.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=staging-db.internal;Database=datarizen_staging;Username=datarizen;Password=${STAGING_DB_PASSWORD}"
  },
  "Migration": {
    "AutoMigrate": true,
    "RollbackOnFailure": true,
    "NotifyOnFailure": true
  }
}
```

**appsettings.Production.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db.internal;Database=datarizen_prod;Username=datarizen;Password=${PROD_DB_PASSWORD}"
  },
  "Migration": {
    "AutoMigrate": false,
    "RequireApproval": true,
    "NotifyOnFailure": true,
    "AlertOnFailure": true
  }
}
```

### Connection String Management

**Development** (local):
```bash
# .env file (not committed)
ConnectionStrings__DefaultConnection="Host=localhost;Database=datarizen_dev;Username=datarizen;Password=dev_password"
```

**Staging/Production** (Kubernetes secrets):
```yaml
# k8s/staging/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: database-secrets
  namespace: staging
type: Opaque
stringData:
  connection-string: "Host=staging-db.internal;Database=datarizen_staging;Username=datarizen;Password=STAGING_PASSWORD"
```

**Injected into pods**:
```yaml
# k8s/staging/deployment.yaml
env:
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: database-secrets
        key: connection-string
```

### Environment-Specific Seed Data

**Development seed data**:

```csharp
[Migration(20250115103000, "Seed development data")]
[Profile("Development")]  // Only runs in Development
public class SeedDevelopmentData : Migration
{
    public override void Up()
    {
        Insert.IntoTable("users").InSchema("identity")
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                email = "admin@dev.local",
                password_hash = "hashed_password",
                is_active = true
            })
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                email = "user@dev.local",
                password_hash = "hashed_password",
                is_active = true
            });
    }

    public override void Down()
    {
        Delete.FromTable("users").InSchema("identity")
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000001") })
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000002") });
    }
}
```

**Production seed data**:

```csharp
[Migration(20250115103000, "Seed production reference data")]
[Profile("Production")]  // Only runs in Production
public class SeedProductionReferenceData : Migration
{
    public override void Up()
    {
        // Seed countries, currencies, etc.
        Insert.IntoTable("countries").InSchema("public")
            .Row(new { code = "US", name = "United States" })
            .Row(new { code = "CA", name = "Canada" })
            .Row(new { code = "GB", name = "United Kingdom" });
    }

    public override void Down()
    {
        Delete.FromTable("countries").InSchema("public").AllRows();
    }
}
```

---

## Common Scenarios and Troubleshooting

### Scenario 1: Migration Fails in Production

**Symptoms**:
- Migration runner exits with error
- Application fails to start
- Database in inconsistent state

**Diagnosis**:

```bash
# Check migration logs
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status

# Check database state
psql -h prod-db.example.com -U datarizen -d datarizen_prod

-- Check version tables
SELECT * FROM identity.__FluentMigrator_VersionInfo ORDER BY version DESC LIMIT 5;

-- Check if tables exist
\dt identity.*
```

**Resolution**:

**Option 1: Fix forward**:
```bash
# Create hotfix migration to fix the issue
# Example: Migration created invalid constraint

# New migration to drop invalid constraint
[Migration(20250115110000, "Fix invalid constraint")]
public class FixInvalidConstraint : Migration
{
    public override void Up()
    {
        Delete.ForeignKey("fk_invalid").OnTable("users").InSchema("identity");
    }
}

# Apply hotfix
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --module Identity
```

**Option 2: Rollback**:
```bash
# Rollback failed migration
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --module Identity \
  --rollback 1

# Deploy previous application version
kubectl rollout undo deployment/datarizen-api -n production
```

**Option 3: Manual fix**:
```sql
-- Connect to database
psql -h prod-db.example.com -U datarizen -d datarizen_prod

-- Manually fix the issue
ALTER TABLE identity.users DROP CONSTRAINT fk_invalid;

-- Update version table to mark migration as applied
INSERT INTO identity.__FluentMigrator_VersionInfo (version, description, applied_on)
VALUES (20250115103000, 'Add user preferences table', CURRENT_TIMESTAMP);
```

### Scenario 2: Partial Migration Failure Across Modules

**Symptoms**:
- Some modules migrated successfully
- Other modules failed
- Application partially working

**Example**:
```
✅ Identity - Migrated successfully
✅ Tenant - Migrated successfully
❌ Product - Migration failed
⏸️  Notification - Not attempted
⏸️  Audit - Not attempted
```

**Diagnosis**:

```bash
# Check status of all modules
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --status

# Output shows which modules succeeded/failed
```

**Resolution**:

**Option 1: Fix and continue**:
```bash
# Fix the failing migration
# (Create hotfix or fix code)

# Re-run migrations (will skip already-applied migrations)
dotnet run --project server/src/MigrationRunner -- \
  --environment Production
```

**Option 2: Rollback all**:
```bash
# Rollback all modules to previous state
dotnet run --project server/src/MigrationRunner -- \
  --environment Production \
  --rollback 1

# This rolls back the last migration for each module
```

**Prevention**:
- Test migrations thoroughly in staging
- Use transactions where possible
- Implement retry logic for transient failures

### Scenario 3: Migration Conflicts

**Symptoms**:
- Two developers created migrations with same timestamp
- Git merge conflict in migration files
- Migration runner fails with duplicate version error

**Example**:

Developer A:
```csharp
[Migration(20250115103000, "Add user preferences")]
public class AddUserPreferences : Migration { ... }
```

Developer B:
```csharp
[Migration(20250115103000, "Add user roles")]
public class AddUserRoles : Migration { ... }
```

**Resolution**:

**Step 1: Identify conflict**:
```bash
# Git shows conflict
git status
# Unmerged paths:
#   both added: server/src/Modules/UserManagement/UserManagement.Migrations/Migrations/20250115103000_*.cs
```

**Step 2: Resolve conflict**:
```bash
# Developer B renames their migration
mv 20250115103000_AddUserRoles.cs 20250115103100_AddUserRoles.cs

# Update migration attribute
[Migration(20250115103100, "Add user roles")]  # Changed timestamp
```

**Step 3: Commit resolution**:
```bash
git add .
git commit -m "fix: resolve migration timestamp conflict"
```

**Prevention**:
- Use seconds component for uniqueness
- Communicate with team before creating migrations
- Pull latest changes before creating migration

### Scenario 4: Large-Scale Data Migration

**Symptoms**:
- Migration takes hours to complete
- Database locks causing application downtime
- Migration times out

**Example**:
```csharp
// ❌ Bad: Updates 10 million rows in one transaction
[Migration(20250115103000, "Backfill user preferences")]
public class BackfillUserPreferences : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            UPDATE identity.users 
            SET preferences_json = '{""theme"":""light""}'
            WHERE preferences_json IS NULL
        ");
        // This locks the entire table for hours!
    }
}
```

**Resolution**:

**Option 1: Batch processing**:
```csharp
[Migration(20250115103000, "Backfill user preferences")]
public class BackfillUserPreferences : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            DO $$
            DECLARE
                batch_size INT := 10000;
                rows_affected INT;
            BEGIN
                LOOP
                    UPDATE identity.users
                    SET preferences_json = '{""theme"":""light""}'
                    WHERE preferences_json IS NULL
                    AND id IN (
                        SELECT id FROM identity.users 
                        WHERE preferences_json IS NULL 
                        LIMIT batch_size
                    );
                    
                    GET DIAGNOSTICS rows_affected = ROW_COUNT;
                    EXIT WHEN rows_affected = 0;
                    
                    RAISE NOTICE 'Processed % rows', rows_affected;
                    COMMIT;  -- Commit each batch
                    PERFORM pg_sleep(0.1);  -- Throttle
                END LOOP;
            END $$;
        ");
    }
}
```

**Option 2: Background job**:
```csharp
// Migration 1: Add column (nullable)
[Migration(20250115103000, "Add preferences column")]
public class AddPreferencesColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users").InSchema("identity")
            .AddColumn("preferences_json").AsString(int.MaxValue).Nullable();
    }
}

// Deploy application with background job to backfill data

// Migration 2: Make column non-nullable (after backfill complete)
[Migration(20250120103000, "Make preferences non-nullable")]
public class MakePreferencesNonNullable : Migration
{
    public override void Up()
    {
        Alter.Column("preferences_json")
            .OnTable("users").InSchema("identity")
            .AsString(int.MaxValue).NotNullable();
    }
}
```

**Option 3: Online schema change** (PostgreSQL 11+):
```sql
-- Use pg_repack or similar tools for large table modifications
-- Avoids long-running locks
```

### Scenario 5: Recovering from Data Loss

**Symptoms**:
- Migration accidentally deleted data
- Data corruption detected
- Need to restore specific records

**Example**:
```csharp
// ❌ Oops: Deleted all users instead of inactive users
[Migration(20250115103000, "Delete inactive users")]
public class DeleteInactiveUsers : Migration
{
    public override void Up()
    {
        Execute.Sql("DELETE FROM identity.users");  // Missing WHERE clause!
    }
}
```

**Resolution**:

**Step 1: Stop the bleeding**:
```bash
# Immediately stop application
kubectl scale deployment/datarizen-api --replicas=0 -n production

# Prevent further migrations
# (Remove migration runner from deployment pipeline)
```

**Step 2: Assess damage**:
```sql
-- Check how many records were deleted
SELECT COUNT(*) FROM identity.users;  -- Should be 0 if all deleted

-- Check backup
pg_restore --list backup_20250115_100000.dump | grep users
```

**Step 3: Restore data**:
```bash
# Restore to temporary database
createdb datarizen_temp
pg_restore -h prod-db.example.com -U datarizen -d datarizen_temp \
  backup_20250115_100000.dump

# Extract deleted users
pg_dump -h prod-db.example.com -U datarizen -d datarizen_temp \
  --table=identity.users \
  --data-only \
  --file=deleted_users.sql

# Import into production
psql -h prod-db.example.com -U datarizen -d datarizen_prod < deleted_users.sql

# Verify
psql -h prod-db.example.com -U datarizen -d datarizen_prod \
  -c "SELECT COUNT(*) FROM identity.users"

# Drop temporary database
dropdb datarizen_temp
```

**Step 4: Fix migration**:
```csharp
// Corrected migration
[Migration(20250115103000, "Delete inactive users")]
public class DeleteInactiveUsers : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            DELETE FROM identity.users 
            WHERE is_active = false 
            AND last_login_at < NOW() - INTERVAL '1 year'
        ");
    }
}
```

**Step 5: Restart application**:
```bash
kubectl scale deployment/datarizen-api --replicas=3 -n production
```

**Prevention**:
- Always test migrations in staging first
- Use soft deletes instead of hard deletes
- Implement audit logging
- Require code review for data migrations
- Backup before production migrations

---

## Summary

### Key Takeaways

1. **Migration Development**:
   - Use timestamp-based versioning
   - Write both `Up()` and `Down()` methods
   - Test locally before committing

2. **Version Control**:
   - Commit migrations with application code
   - Never modify merged migrations
   - Coordinate with team to avoid conflicts

3. **Schema vs Data**:
   - Separate schema and data migrations
   - Use batching for large data migrations
   - Consider background jobs for very large datasets

4. **Testing**:
   - Test migrations locally
   - Automated testing in CI/CD
   - Verify rollback capability

5. **Deployment**:
   - Backup before production migrations
   - Automated in staging, manual in production
   - Zero-downtime strategies for critical changes

6. **Rollback**:
   - Automated rollback in staging
   - Manual rollback in production
   - Database restore as last resort

