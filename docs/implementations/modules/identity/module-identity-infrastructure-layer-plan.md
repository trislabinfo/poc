# Module: Identity - Infrastructure Layer Implementation Plan

**Status**: 🆕 New - Infrastructure Layer Implementation  
**Last Updated**: 2025-01-15  
**Estimated Total Time**: ~18 hours  
**Related Documents**: 
- `docs/implementations/module-identity-domain-layer-plan.md` (Domain model reference)
- `docs/implementations/module-identity-application-layer-plan.md` (Application layer reference)
- `docs/implementations/module-identity-migrations-layer-plan.md` (Database migrations)
- `docs/ai-context/07-DB-MIGRATIONS.md` (Migration technical details)

---

## Overview

This plan implements the **Infrastructure Layer** for the Identity module. The infrastructure layer provides concrete implementations of domain repository interfaces, EF Core configurations, and domain services.

**Philosophy**: 
- ✅ Common infrastructure patterns in `BuildingBlocks.Infrastructure`
- ✅ Module-specific implementations in `Identity.Infrastructure`
- ✅ Zero vendor lock-in (all vendor-specific code in `Capabilities/*`)
- ✅ Repository pattern with Specification support (Ardalis.Specification)

---

## Architecture Overview

```
Identity.Infrastructure/
├── Data/
│   ├── IdentityDbContext.cs                    # EF Core DbContext
│   └── Configurations/                         # Entity configurations
│       ├── UserConfiguration.cs
│       ├── RoleConfiguration.cs
│       ├── PermissionConfiguration.cs
│       ├── UserRoleConfiguration.cs
│       ├── RolePermissionConfiguration.cs
│       ├── CredentialConfiguration.cs
│       └── RefreshTokenConfiguration.cs
├── Repositories/
│   ├── UserRepository.cs                       # Implements IUserRepository
│   ├── RoleRepository.cs                       # Implements IRoleRepository
│   ├── PermissionRepository.cs                 # Implements IPermissionRepository
│   └── RefreshTokenRepository.cs               # Implements IRefreshTokenRepository
├── Services/
│   └── BCryptPasswordHasher.cs                 # Implements IPasswordHasher
└── Extensions/
    └── InfrastructureServiceCollectionExtensions.cs  # DI registration

BuildingBlocks.Infrastructure/ (already exists)
├── Data/
│   ├── Repository.cs                           # Base repository with Specification support
│   ├── UnitOfWork.cs                           # IUnitOfWork implementation
│   └── IRepository.cs                          # Generic repository interface
└── Extensions/
    └── InfrastructureServiceCollectionExtensions.cs  # Registers UnitOfWork, etc.
```

---

## What Goes in BuildingBlocks.Infrastructure (Already Implemented)

These are **generic, reusable** infrastructure components used by ALL modules:

### ✅ Already Implemented in BuildingBlocks.Infrastructure

1. **`IRepository<TEntity, TKey>`** - Generic repository interface with Specification support
2. **`Repository<TEntity, TKey>`** - Base repository implementation using Ardalis.Specification
3. **`IUnitOfWork`** - Unit of Work interface
4. **`UnitOfWork`** - Unit of Work implementation (wraps DbContext.SaveChangesAsync)
5. **`IDateTimeProvider`** - Abstraction for DateTime.UtcNow (testability)
6. **`DateTimeProvider`** - Default implementation
7. **`ISecurityEventLogger`** - Security audit logging abstraction
8. **`DatabaseSecurityEventLogger`** - Database-based security event logger
9. **`IFeatureFlagService`** - Feature flag abstraction
10. **`InMemoryFeatureFlagService`** - In-memory feature flags (appsettings.json)
11. **`IBackgroundJobScheduler`** - Background job abstraction
12. **`NullBackgroundJobScheduler`** - No-op background job scheduler
13. **`IErrorTracker`** - Error tracking abstraction
14. **`NullErrorTracker`** - No-op error tracker

**Reference**: `Ardalis.Specification` and `Ardalis.Specification.EntityFrameworkCore` packages are already added to `Directory.Packages.props`.

---

## What Goes in Identity.Infrastructure (To Be Implemented)

These are **module-specific** implementations:

### 1. EF Core DbContext
- **`IdentityDbContext`** - DbContext for Identity module
- Configures all 7 entities (User, Role, Permission, UserRole, RolePermission, Credential, RefreshToken)
- Uses schema `identity`
- Inherits from `DbContext` (not a shared base class)

### 2. Entity Configurations (EF Core Fluent API)
- **`UserConfiguration`** - Configures User entity, Email value object, indexes
- **`RoleConfiguration`** - Configures Role entity, indexes
- **`PermissionConfiguration`** - Configures Permission entity, indexes
- **`UserRoleConfiguration`** - Configures many-to-many junction table
- **`RolePermissionConfiguration`** - Configures many-to-many junction table
- **`CredentialConfiguration`** - Configures Credential entity, PasswordHash value object
- **`RefreshTokenConfiguration`** - Configures RefreshToken entity

### 3. Repository Implementations
- **`UserRepository`** - Implements `IUserRepository`, inherits from `Repository<User, Guid>`
- **`RoleRepository`** - Implements `IRoleRepository`, inherits from `Repository<Role, Guid>`
- **`PermissionRepository`** - Implements `IPermissionRepository`, inherits from `Repository<Permission, Guid>`
- **`RefreshTokenRepository`** - Implements `IRefreshTokenRepository`, inherits from `Repository<RefreshToken, Guid>`

### 4. Domain Services
- **`BCryptPasswordHasher`** - Implements `IPasswordHasher` using BCrypt.Net-Next

### 5. DI Registration
- **`InfrastructureServiceCollectionExtensions`** - Registers DbContext, repositories, domain services

---

## Phase 1: Project Setup (30 minutes)

### 1.1: Create Project

**Tasks**:
- [ ] Create `Identity.Infrastructure.csproj`
- [ ] Add references:
  - `Identity.Domain`
  - `BuildingBlocks.Kernel`
  - `BuildingBlocks.Infrastructure`
- [ ] Add NuGet packages:
  - `Microsoft.EntityFrameworkCore` (9.x)
  - `Microsoft.EntityFrameworkCore.Relational` (9.x)
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (9.x)
  - `BCrypt.Net-Next` (4.x)
  - `Ardalis.Specification.EntityFrameworkCore` (9.x) - already in Directory.Packages.props
- [ ] Create folder structure:
  - `Data/`
  - `Data/Configurations/`
  - `Repositories/`
  - `Services/`
  - `Extensions/`

**File**: `Identity.Infrastructure/Identity.Infrastructure.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Datarizen.Identity.Infrastructure</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Identity.Domain\Identity.Domain.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Infrastructure\BuildingBlocks.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="BCrypt.Net-Next" />
    <PackageReference Include="Ardalis.Specification.EntityFrameworkCore" />
  </ItemGroup>
</Project>
```

---

## Phase 2: EF Core DbContext (2 hours)

### 2.1: Create IdentityDbContext

**File**: `Identity.Infrastructure/Data/IdentityDbContext.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datarizen.Identity.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for Identity module
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("identity");

        // Apply all configurations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
```

**Tasks**:
- [ ] Create `IdentityDbContext.cs`
- [ ] Add DbSet properties for all 7 entities
- [ ] Set default schema to `identity`
- [ ] Apply configurations from assembly
- [ ] Build project: `dotnet build Identity.Infrastructure.csproj`

---

## Phase 3: Entity Configurations (4 hours)

### 3.1: UserConfiguration

**File**: `Identity.Infrastructure/Data/Configurations/UserConfiguration.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datarizen.Identity.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(u => u.DefaultTenantId)
            .HasColumnName("default_tenant_id")
            .IsRequired();

        // Email value object (owned entity)
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
        });

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(u => u.Email.Value)
            .HasDatabaseName("ix_users_email")
            .IsUnique();

        builder.HasIndex(u => u.DefaultTenantId)
            .HasDatabaseName("ix_users_default_tenant_id");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("ix_users_is_active");

        // Relationships
        builder.HasMany(u => u.UserRoles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Credentials)
            .WithOne()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events (not persisted)
        builder.Ignore(u => u.DomainEvents);
    }
}
```

**Tasks**:
- [ ] Create `UserConfiguration.cs`
- [ ] Configure Email value object as owned entity
- [ ] Add indexes (email unique, default_tenant_id, is_active)
- [ ] Configure relationships (UserRoles, Credentials)
- [ ] Ignore DomainEvents collection

---

### 3.2: RoleConfiguration

**File**: `Identity.Infrastructure/Data/Configurations/RoleConfiguration.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datarizen.Identity.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(r => r.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(r => r.IsSystemRole)
            .HasColumnName("is_system_role")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(r => new { r.Name, r.TenantId })
            .HasDatabaseName("ix_roles_name_tenant_id")
            .IsUnique();

        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("ix_roles_tenant_id");

        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("ix_roles_is_system_role");

        // Relationships
        builder.HasMany(r => r.RolePermissions)
            .WithOne()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(r => r.DomainEvents);
    }
}
```

**Tasks**:
- [ ] Create `RoleConfiguration.cs`
- [ ] Add composite unique index (name, tenant_id)
- [ ] Add indexes (tenant_id, is_system_role)
- [ ] Configure RolePermissions relationship

---

### 3.3: PermissionConfiguration

**File**: `Identity.Infrastructure/Data/Configurations/PermissionConfiguration.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datarizen.Identity.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(p => p.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(p => p.Module)
            .HasColumnName("module")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.Code)
            .HasDatabaseName("ix_permissions_code")
            .IsUnique();

        builder.HasIndex(p => p.Module)
            .HasDatabaseName("ix_permissions_module");

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}
```

**Tasks**:
- [ ] Create `PermissionConfiguration.cs`
- [ ] Add unique index on code
- [ ] Add index on module

---

### 3.4: UserRoleConfiguration

**File**: `Identity.Infrastructure/Data/Configurations/UserRoleConfiguration.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datarizen.Identity.Infrastructure.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(ur => ur.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(ur => ur.GrantedAt)
            .HasColumnName("granted_at")
            .IsRequired();

        builder.Property(ur => ur.GrantedBy)
            .HasColumnName("granted_by")
            .IsRequired();

        // Composite unique index (user cannot have same role twice)
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .HasDatabaseName("ix_user_roles_user_id_role_id")
            .IsUnique();

        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("ix_user_roles_role_id");

        // Ignore domain events
        builder.Ignore(ur => ur.DomainEvents);
    }
}
```

**Tasks**:
- [ ] Create `UserRoleConfiguration.cs`
- [ ] Add composite unique index (user_id, role_id)
- [ ] Add index on role_id

---

### 3.5: RolePermissionConfiguration

**File**: `Identity.Infrastructure/Data/Configurations/RolePermissionConfiguration.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datarizen.Identity.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(rp => rp.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(rp => rp.PermissionId)
            .HasColumnName("permission_id")
            .IsRequired();

        builder.Property(rp => rp.GrantedAt)
            .HasColumnName("granted_at")
            .IsRequired();

        // Composite unique index (role cannot have same permission twice)
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .HasDatabaseName("ix_role_permissions_role_id_permission_id")
            .IsUnique();

        builder.HasIndex(rp => rp.PermissionId)
            .HasDatabaseName("ix_role_permissions_permission_id");

        // Ignore domain events
        builder.Ignore(rp => rp.DomainEvents);
    }
}
```

**Tasks**:
- [ ] Create `RolePermissionConfiguration.cs`
- [ ] Add composite unique index (role_id, permission_id)
- [ ] Add index on permission_id

---

### 3.6: CredentialConfiguration

**File**: `Identity.Infrastructure/Data/Configurations/CredentialConfiguration.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datarizen.Identity.Infrastructure.Data.Configurations;

public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("credentials");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // PasswordHash value object (owned entity)
        builder.OwnsOne(c => c.PasswordHash, hash =>
        {
            hash.Property(h => h.Value)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();
        });

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("ix_credentials_user_id");

        builder.HasIndex(c => new { c.UserId, c.Type })
            .HasDatabaseName("ix_credentials_user_id_type");

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}
```

**Tasks**:
- [ ] Create `CredentialConfiguration.cs`
- [ ] Configure PasswordHash value object as owned entity
- [ ] Configure CredentialType enum as string
- [ ] Add indexes (user_id, composite user_id + type)

---

### 3.7: RefreshTokenConfiguration

**File**: `Identity.Infrastructure/Data/Configurations/RefreshTokenConfiguration.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datarizen.Identity.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(rt => rt.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rt => rt.Token)
            .HasColumnName("token")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(rt => rt.IsRevoked)
            .HasColumnName("is_revoked")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(rt => rt.Token)
            .HasDatabaseName("ix_refresh_tokens_token")
            .IsUnique();

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("ix_refresh_tokens_expires_at");

        // Ignore domain events
        builder.Ignore(rt => rt.DomainEvents);
    }
}
```

**Tasks**:
- [ ] Create `RefreshTokenConfiguration.cs`
- [ ] Add unique index on token
- [ ] Add indexes (user_id, expires_at)

---

## Phase 4: Repository Implementations (3 hours)

### 4.1: UserRepository

**File**: `Identity.Infrastructure/Repositories/UserRepository.cs`

```csharp
using Datarizen.BuildingBlocks.Infrastructure.Data;
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.Repositories;
using Datarizen.Identity.Domain.ValueObjects;
using Datarizen.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Datarizen.Identity.Infrastructure.Repositories;

/// <summary>
/// Repository for User aggregate
/// </summary>
public class UserRepository : Repository<User, Guid>, IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
    }
}
```

**Tasks**:
- [ ] Create `UserRepository.cs`
- [ ] Inherit from `Repository<User, Guid>` (gets GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync, Specification support)
- [ ] Implement `GetByEmailAsync`
- [ ] Implement `EmailExistsAsync`

---

### 4.2: RoleRepository

**File**: `Identity.Infrastructure/Repositories/RoleRepository.cs`

```csharp
using Datarizen.BuildingBlocks.Infrastructure.Data;
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.Repositories;
using Datarizen.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Datarizen.Identity.Infrastructure.Repositories;

public class RoleRepository : Repository<Role, Guid>, IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Role?> GetByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name && r.TenantId == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }
}
```

**Tasks**:
- [ ] Create `RoleRepository.cs`
- [ ] Inherit from `Repository<Role, Guid>`
- [ ] Implement `GetByNameAsync`
- [ ] Implement `GetAllAsync`

---

### 4.3: PermissionRepository

**File**: `Identity.Infrastructure/Repositories/PermissionRepository.cs`

```csharp
using Datarizen.BuildingBlocks.Infrastructure.Data;
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.Repositories;
using Datarizen.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Datarizen.Identity.Infrastructure.Repositories;

public class PermissionRepository : Repository<Permission, Guid>, IPermissionRepository
{
    private readonly IdentityDbContext _context;

    public PermissionRepository(IdentityDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.ToListAsync(cancellationToken);
    }
}
```

**Tasks**:
- [ ] Create `PermissionRepository.cs`
- [ ] Inherit from `Repository<Permission, Guid>`
- [ ] Implement `GetByCodeAsync`
- [ ] Implement `GetAllAsync`

---

### 4.4: RefreshTokenRepository

**File**: `Identity.Infrastructure/Repositories/RefreshTokenRepository.cs`

```csharp
using Datarizen.BuildingBlocks.Infrastructure.Data;
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.Repositories;
using Datarizen.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Datarizen.Identity.Infrastructure.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken, Guid>, IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;

    public RefreshTokenRepository(IdentityDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke();
        }
    }
}
```

**Tasks**:
- [ ] Create `RefreshTokenRepository.cs`
- [ ] Inherit from `Repository<RefreshToken, Guid>`
- [ ] Implement `GetByTokenAsync`
- [ ] Implement `RevokeAllForUserAsync`

---

## Phase 5: Domain Services (1.5 hours)

### 5.1: BCryptPasswordHasher

**File**: `Identity.Infrastructure/Services/BCryptPasswordHasher.cs`

```csharp
using Datarizen.Identity.Domain.Services;

namespace Datarizen.Identity.Infrastructure.Services;

/// <summary>
/// Password hasher using BCrypt algorithm
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // BCrypt work factor (higher = slower but more secure)

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
```

**Tasks**:
- [ ] Create `BCryptPasswordHasher.cs`
- [ ] Implement `Hash` using BCrypt.Net-Next
- [ ] Implement `Verify` using BCrypt.Net-Next
- [ ] Use work factor 12 (recommended for 2025)
- [ ] Add error handling for invalid hashes

**Note**: BCrypt is preferred over PBKDF2 or SHA256 because:
- ✅ Designed for password hashing (not general-purpose hashing)
- ✅ Adaptive (can increase work factor as hardware improves)
- ✅ Resistant to GPU/ASIC attacks
- ✅ Industry standard (used by Django, Rails, Laravel)

---

## Phase 6: DI Registration (1 hour)

### 6.1: InfrastructureServiceCollectionExtensions

**File**: `Identity.Infrastructure/Extensions/InfrastructureServiceCollectionExtensions.cs`

```csharp
using Datarizen.Identity.Domain.Repositories;
using Datarizen.Identity.Domain.Services;
using Datarizen.Identity.Infrastructure.Data;
using Datarizen.Identity.Infrastructure.Repositories;
using Datarizen.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Datarizen.Identity.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Identity Infrastructure services (DbContext, repositories, domain services)
    /// </summary>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<IdentityDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Domain Services
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }
}
```

**Tasks**:
- [ ] Create `InfrastructureServiceCollectionExtensions.cs`
- [ ] Register `IdentityDbContext` with PostgreSQL
- [ ] Set migrations history table to `identity.__EFMigrationsHistory`
- [ ] Enable retry on failure (3 retries)
- [ ] Register all 4 repositories as scoped
- [ ] Register `BCryptPasswordHasher` as singleton (stateless)

---

## Phase 7: Integration with Application Layer (1 hour)

### 7.1: Update Identity.Api Program.cs

**File**: `Identity.Api/Program.cs`

```csharp
using Datarizen.BuildingBlocks.Web.Extensions;
using Datarizen.Identity.Application.Extensions;
using Datarizen.Identity.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add BuildingBlocks (middleware, behaviors, health checks)
builder.Services.AddBuildingBlocks(builder.Configuration);

// Add Identity layers
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Use BuildingBlocks middleware
app.UseBuildingBlocks();

app.MapControllers();

app.Run();
```

**Tasks**:
- [ ] Update `Program.cs` to call `AddIdentityInfrastructure`
- [ ] Ensure connection string is in `appsettings.json`
- [ ] Build and run: `dotnet run --project Identity.Api`
- [ ] Verify no DI errors

---

## Phase 8: Testing (3 hours)

### 8.1: Repository Integration Tests

**File**: `Identity.IntegrationTests/Repositories/UserRepositoryTests.cs`

```csharp
using Datarizen.Identity.Domain.Entities;
using Datarizen.Identity.Domain.Repositories;
using Datarizen.Identity.Domain.ValueObjects;
using Datarizen.Identity.Infrastructure.Data;
using Datarizen.Identity.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Datarizen.Identity.IntegrationTests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly IUserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(Guid.NewGuid(), email, "Test User").Value;
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task EmailExistsAsync_WhenEmailExists_ReturnsTrue()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(Guid.NewGuid(), email, "Test User").Value;
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EmailExistsAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

**Tasks**:
- [ ] Create `UserRepositoryTests.cs`
- [ ] Test `GetByEmailAsync`
- [ ] Test `EmailExistsAsync`
- [ ] Create similar tests for Role, Permission, RefreshToken repositories
- [ ] Use in-memory database for tests
- [ ] Target >80% code coverage

---

### 8.2: BCryptPasswordHasher Tests

**File**: `Identity.UnitTests/Services/BCryptPasswordHasherTests.cs`

```csharp
using Datarizen.Identity.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace Datarizen.Identity.UnitTests.Services;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ValidPassword_ReturnsHash()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify("WrongPassword", hash);

        // Assert
        result.Should().BeFalse();
    }
}
```

**Tasks**:
- [ ] Create `BCryptPasswordHasherTests.cs`
- [ ] Test `Hash` returns valid BCrypt hash
- [ ] Test `Verify` with correct password
- [ ] Test `Verify` with incorrect password
- [ ] Test error handling (empty password, invalid hash)

---

## Phase 9: Documentation (1 hour)

### 9.1: Create Infrastructure README.md

**File**: `Identity.Infrastructure/README.md`

```markdown
# Identity.Infrastructure

## Overview

This project contains the **Infrastructure Layer** for the Identity module. It provides concrete implementations of domain repository interfaces, EF Core configurations, and domain services.

## Components

### DbContext
- **`IdentityDbContext`** - EF Core DbContext for Identity module (schema: `identity`)

### Entity Configurations
- **`UserConfiguration`** - Configures User entity, Email value object, indexes
- **`RoleConfiguration`** - Configures Role entity, indexes
- **`PermissionConfiguration`** - Configures Permission entity, indexes
- **`UserRoleConfiguration`** - Configures many-to-many junction table
- **`RolePermissionConfiguration`** - Configures many-to-many junction table
- **`CredentialConfiguration`** - Configures Credential entity, PasswordHash value object
- **`RefreshTokenConfiguration`** - Configures RefreshToken entity

### Repositories
- **`UserRepository`** - Implements `IUserRepository`
- **`RoleRepository`** - Implements `IRoleRepository`
- **`PermissionRepository`** - Implements `IPermissionRepository`
- **`RefreshTokenRepository`** - Implements `IRefreshTokenRepository`

All repositories inherit from `Repository<TEntity, TKey>` (BuildingBlocks.Infrastructure) which provides:
- `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- Specification support via Ardalis.Specification

### Domain Services
- **`BCryptPasswordHasher`** - Implements `IPasswordHasher` using BCrypt.Net-Next

## Dependencies

- **Identity.Domain** - Domain model (entities, value objects, repository interfaces)
- **BuildingBlocks.Kernel** - Base classes (Entity, AggregateRoot, ValueObject, Result<T>)
- **BuildingBlocks.Infrastructure** - Repository base class, UnitOfWork
- **Microsoft.EntityFrameworkCore** - ORM
- **Npgsql.EntityFrameworkCore.PostgreSQL** - PostgreSQL provider
- **BCrypt.Net-Next** - Password hashing
- **Ardalis.Specification.EntityFrameworkCore** - Specification pattern support

## Usage

### Register Infrastructure Services

```csharp
builder.Services.AddIdentityInfrastructure(builder.Configuration);
```

### Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datarizen;Username=postgres;Password=postgres"
  }
}
```

### Run Migrations

```bash
cd server/src/Modules/Identity/Identity.Migrations
dotnet fm migrate up
```

## Design Decisions

### Why BCrypt for Password Hashing?
- ✅ Designed specifically for password hashing
- ✅ Adaptive (can increase work factor as hardware improves)
- ✅ Resistant to GPU/ASIC attacks
- ✅ Industry standard (Django, Rails, Laravel)

### Why Ardalis.Specification?
- ✅ Encapsulates query logic in reusable specifications
- ✅ Keeps repositories simple (no query methods)
- ✅ Testable (specifications can be unit tested)
- ✅ Composable (And, Or, Not)

### Why Repository Pattern?
- ✅ Abstracts data access (can swap EF Core for Dapper)
- ✅ Testable (can mock repositories)
- ✅ Domain-driven (repositories are part of domain contract)

## Future Enhancements

### Phase 2: Security Hardening
- Add `LoginAttemptRepository` for tracking failed logins
- Add `PasswordHistoryRepository` for password history

### Phase 3: Advanced Features
- Add `SessionRepository` for session management
- Add `UserConsentRepository` for GDPR consent tracking

### Phase 4: Performance Optimization
- Add read-only DbContext for queries
- Add caching layer (Redis) for frequently accessed data
- Add database connection pooling
```

**Tasks**:
- [ ] Create `README.md`
- [ ] Document all components
- [ ] Document dependencies
- [ ] Document design decisions
- [ ] Document future enhancements

---

## Success Criteria

### Phase 1 (Project Setup):
- ✅ `Identity.Infrastructure.csproj` created
- ✅ All references added (Domain, BuildingBlocks.Kernel, BuildingBlocks.Infrastructure)
- ✅ All NuGet packages added (EF Core, Npgsql, BCrypt, Ardalis.Specification)
- ✅ Folder structure created

### Phase 2 (DbContext):
- ✅ `IdentityDbContext` created
- ✅ All 7 DbSet properties added
- ✅ Default schema set to `identity`
- ✅ Configurations applied from assembly

### Phase 3 (Entity Configurations):
- ✅ All 7 entity configurations created
- ✅ Value objects configured as owned entities
- ✅ All indexes created
- ✅ All relationships configured
- ✅ DomainEvents ignored

### Phase 4 (Repositories):
- ✅ All 4 repositories created
- ✅ All inherit from `Repository<TEntity, Guid>`
- ✅ All custom methods implemented
- ✅ All use async/await

### Phase 5 (Domain Services):
- ✅ `BCryptPasswordHasher` created
- ✅ Uses work factor 12
- ✅ Error handling implemented

### Phase 6 (DI Registration):
- ✅ `AddIdentityInfrastructure` extension method created
- ✅ DbContext registered with PostgreSQL
- ✅ All repositories registered as scoped
- ✅ `BCryptPasswordHasher` registered as singleton

### Phase 7 (Integration):
- ✅ `Program.cs` updated to call `AddIdentityInfrastructure`
- ✅ Connection string in `appsettings.json`
- ✅ Application runs without DI errors

### Phase 8 (Testing):
- ✅ Repository integration tests created
- ✅ `BCryptPasswordHasher` unit tests created
- ✅ >80% code coverage
- ✅ All tests passing

### Phase 9 (Documentation):
- ✅ `README.md` created
- ✅ All components documented
- ✅ Design decisions documented

---

## Estimated Timeline

**Phase 1 (Project Setup)**: 30 minutes  
**Phase 2 (DbContext)**: 2 hours  
**Phase 3 (Entity Configurations)**: 4 hours  
**Phase 4 (Repositories)**: 3 hours  
**Phase 5 (Domain Services)**: 1.5 hours  
**Phase 6 (DI Registration)**: 1 hour  
**Phase 7 (Integration)**: 1 hour  
**Phase 8 (Testing)**: 3 hours  
**Phase 9 (Documentation)**: 1 hour  

**Total**: ~18 hours (~2.5 days)

---

## Next Steps After Completion

1. **Run Migrations** (see `module-identity-migrations-layer-plan.md`)
   - Create schema migrations
   - Create seed data migrations
   - Apply migrations to database

2. **Implement Application Layer** (see `module-identity-application-layer-plan.md`)
   - Create commands/queries
   - Create handlers
   - Create validators

3. **Update API Layer**
   - Replace stub endpoints with MediatR calls
   - Add request/response DTOs
   - Add Swagger documentation

4. **Integration Tests**
   - Test full request/response flow
   - Test database interactions
   - Test authentication/authorization

---

## Notes

- All repositories inherit from `Repository<TEntity, Guid>` which provides Specification support
- All entity configurations use snake_case column names (PostgreSQL convention)
- All indexes use `ix_` prefix (e.g., `ix_users_email`)
- All foreign keys use `ON DELETE CASCADE` for junction tables
- BCrypt work factor is 12 (recommended for 2025)
- Connection string uses PostgreSQL (can swap for SQL Server/MySQL by changing provider)
- Migrations history table is in `identity` schema