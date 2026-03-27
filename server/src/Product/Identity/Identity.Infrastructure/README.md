# Identity.Infrastructure

## Overview

Infrastructure layer for the Identity module: EF Core DbContext, entity configurations, repository implementations, and domain services (e.g. password hashing). Supports **multi-topology** deployment (Monolith, MultiApp, Microservices) via configuration.

## Components

### DbContext

- **`IdentityDbContext`** – EF Core DbContext extending `BaseModuleDbContext`; schema is fixed as `identity` (via `SchemaName`). For microservice topology, override or configure per deployment if needed.

### Entity configurations

- **`UserConfiguration`** – User, Email value object, indexes.
- **`RoleConfiguration`** – Role, indexes.
- **`PermissionConfiguration`** – Permission, indexes.
- **`UserRoleConfiguration`** – User–role junction table.
- **`RolePermissionConfiguration`** – Role–permission junction table.
- **`CredentialConfiguration`** – Credential, PasswordHash value object.
- **`RefreshTokenConfiguration`** – RefreshToken.

### Repositories

- **`UserRepository`** – `IUserRepository`.
- **`RoleRepository`** – `IRoleRepository`.
- **`PermissionRepository`** – `IPermissionRepository`.
- **`RefreshTokenRepository`** – `IRefreshTokenRepository`.

All repositories inherit from `Repository<TEntity, TKey>` (BuildingBlocks.Infrastructure) and support Ardalis.Specification.

### Domain services

- **`BCryptPasswordHasher`** – `IPasswordHasher` using BCrypt.Net-Next.

## Registration

The **schema name is provided by the module** (e.g. `IdentityModule.SchemaName`). Connection string is always **`ConnectionStrings:DefaultConnection`**.

```csharp
// In IdentityModule.RegisterServices (calls layer extension at project root):
services.AddIdentityInfrastructure(configuration, SchemaName);
```
Extension: `Identity.Infrastructure/IdentityInfrastructureServiceCollectionExtensions.cs`

Migrations are run outside the application (e.g. FluentMigrator); EF is used for CRUD only.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datarizen;Username=postgres;Password=..."
  }
}
```

## Dependencies

- Identity.Domain
- BuildingBlocks.Kernel
- BuildingBlocks.Infrastructure
- Microsoft.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL
- BCrypt.Net-Next
- Ardalis.Specification.EntityFrameworkCore

## Design notes

- **BCrypt**: Used for password hashing (work factor 12); suitable for production.
- **Ardalis.Specification**: Query logic in specifications; repositories stay thin.
- **Repository pattern**: Data access behind interfaces; testable and swappable.
