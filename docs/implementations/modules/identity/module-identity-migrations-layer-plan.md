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