# Module: Identity - Infrastructure Layer Implementation Plan (Multi-Topology)

**Status**: 🆕 New - Multi-Topology Infrastructure Implementation  
**Last Updated**: 2025-01-15  
**Estimated Total Time**: ~22 hours  
**Related Documents**: 
- `docs/implementations/module-identity-infrastructure-layer-plan.md` (Original single-topology plan)
- `docs/implementations/module-identity-domain-layer-plan.md` (Domain model reference)
- `docs/implementations/module-identity-application-layer-plan.md` (Application layer reference)
- `docs/ai-context/06-INTER-MODULE-COMMUNICATION.md` (Cross-module communication patterns)

---

## Overview

This plan implements the **Infrastructure Layer** for the Identity module with support for **three deployment topologies**:

1. **Monolith** - Single database, multiple schemas, all modules in one process
2. **MultiApp** - Single database, multiple schemas, modules split across hosts
3. **Microservices** - Multiple databases, one per module, full isolation

**Philosophy**: 
- ✅ Write once, deploy anywhere
- ✅ Same codebase works in all topologies
- ✅ Configuration-driven topology selection
- ✅ Zero vendor lock-in (all vendor-specific code in `Capabilities/*`)

**CRITICAL RULE**: 
- ❌ **NO MODULE CAN ACCESS ANOTHER MODULE'S SCHEMA OR TABLES**
- ✅ **ALL CROSS-MODULE COMMUNICATION MUST GO THROUGH APPLICATION SERVICES OR HTTP/gRPC CLIENTS**
- ✅ **EACH MODULE ONLY ACCESSES ITS OWN SCHEMA**

---

## Architecture Overview

### Database Topology Comparison

```
┌─────────────────────────────────────────────────────────────────────┐
│ MONOLITH TOPOLOGY                                                   │
├─────────────────────────────────────────────────────────────────────┤
│ MonolithHost Process                                                │
│ ├── IdentityDbContext → datarizen DB, identity schema ONLY         │
│ ├── TenantDbContext   → datarizen DB, tenant schema ONLY           │
│ ├── UserDbContext     → datarizen DB, user schema ONLY             │
│ └── FeatureDbContext  → datarizen DB, feature schema ONLY          │
│                                                                     │
│ PostgreSQL: datarizen                                               │
│ ├── identity schema (users, roles, permissions, ...)               │
│ │   └── IdentityDbContext can ONLY access this schema              │
│ ├── tenant schema (tenants, subscriptions, ...)                    │
│ │   └── TenantDbContext can ONLY access this schema                │
│ ├── user schema (...)                                              │
│ │   └── UserDbContext can ONLY access this schema                  │
│ └── feature schema (...)                                           │
│     └── FeatureDbContext can ONLY access this schema               │
│                                                                     │
│ Cross-Module Communication:                                         │
│ └── Via Application Services (ITenantQueryService, etc.)            │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ MULTIAPP TOPOLOGY                                                   │
├─────────────────────────────────────────────────────────────────────┤
│ ControlPanelHost Process                                            │
│ ├── IdentityDbContext → datarizen DB, identity schema ONLY         │
│ └── TenantDbContext   → datarizen DB, tenant schema ONLY           │
│                                                                     │
│ RuntimeHost Process                                                 │
│ └── UserDbContext     → datarizen DB, user schema ONLY             │
│                                                                     │
│ AppBuilderHost Process                                              │
│ └── FeatureDbContext  → datarizen DB, feature schema ONLY          │
│                                                                     │
│ PostgreSQL: datarizen (SHARED DATABASE, ISOLATED SCHEMAS)           │
│ ├── identity schema                                                │
│ │   └── Only IdentityDbContext can access                          │
│ ├── tenant schema                                                  │
│ │   └── Only TenantDbContext can access                            │
│ ├── user schema                                                    │
│ │   └── Only UserDbContext can access                              │
│ └── feature schema                                                 │
│     └── Only FeatureDbContext can access                           │
│                                                                     │
│ Cross-Module Communication:                                         │
│ └── Via Application Services (same as Monolith)                     │
│     └── Even though same database, NO direct schema access          │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ MICROSERVICES TOPOLOGY                                              │
├─────────────────────────────────────────────────────────────────────┤
│ IdentityService Process                                             │
│ └── IdentityDbContext → identity_db, own schema only (e.g. public) │
│                                                                     │
│ TenantService Process                                               │
│ └── TenantDbContext   → tenant_db, own schema only                  │
│                                                                     │
│ UserService Process                                                 │
│ └── UserDbContext     → user_db, own schema only                    │
│                                                                     │
│ FeatureService Process                                              │
│ └── FeatureDbContext  → feature_db, own schema only                │
│                                                                     │
│ PostgreSQL: SEPARATE DATABASES (one per module)                      │
│ ├── identity_db  – Only IdentityService can access                 │
│ ├── tenant_db     – Only TenantService can access                   │
│ ├── user_db      – Only UserService can access                     │
│ └── feature_db    – Only FeatureService can access                  │
│                                                                     │
│ Cross-Module Communication:                                         │
│ └── Via HTTP/gRPC Clients (ITenantClient, etc.)                    │
└─────────────────────────────────────────────────────────────────────┘
```

### Schema Isolation Rules

| Topology | Database | Schema Access | Cross-Module Access |
|----------|----------|---------------|---------------------|
| **Monolith** | Shared (`datarizen`) | Each DbContext → Own schema ONLY | Application Services (in-process) |
| **MultiApp** | Shared (`datarizen`) | Each DbContext → Own schema ONLY | Application Services (in-process) |
| **Microservices** | Isolated (`identity_db`, `tenant_db`, etc.) | Each DbContext → Own database ONLY | HTTP/gRPC Clients (cross-process) |

**Key Point**: Even in Monolith/MultiApp where modules share the same database, **each module can ONLY access its own schema**. Cross-module data access MUST go through application services.

---

## Project Structure

```
Identity.Infrastructure/
├── Data/
│   ├── IdentityDbContext.cs                    # EF Core DbContext (identity schema ONLY)
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
    └── InfrastructureServiceCollectionExtensions.cs  # DI registration (topology-aware)
```

---

## Phase 1: DbContext Implementation (3 hours)

### 1.1: Create IdentityDbContext (1 hour)

**File**: `Identity.Infrastructure/Data/IdentityDbContext.cs`

<augment_code_snippet path="Identity.Infrastructure/Data/IdentityDbContext.cs" mode="EDIT">
```csharp
using Datarizen.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datarizen.Identity.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Identity module.
/// Supports multiple deployment topologies via configuration.
/// 
/// CRITICAL: This DbContext can ONLY access the 'identity' schema (Monolith/MultiApp)
/// or 'identity_db' database (Microservices). It CANNOT access other module schemas/databases.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    // Aggregate Roots (identity schema ONLY)
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    
    // Entities (identity schema ONLY)
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    
    // Junction Tables (identity schema ONLY)
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema is configured via DbContextOptions (topology-specific)
        // - Monolith/MultiApp: "identity" schema
        // - Microservices: "public" schema (default)
        
        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // PostgreSQL naming convention: snake_case
        configurationBuilder.Properties<string>()
            .HaveMaxLength(500); // Default max length for strings
    }
}



