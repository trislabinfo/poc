# Datarizen AI Context - Database Migration Runner

## Overview

Datarizen uses **FluentMigrator** for database schema and data migrations. Migrations are version-controlled, automated, and support multiple database providers (PostgreSQL, SQL Server, MySQL, DB2, Oracle).

**Key Principles**:
- ✅ Each module owns its migrations
- ✅ Schema and data migrations supported
- ✅ Environment-specific migrations (Development, Staging, Production)
- ✅ Zero-downtime deployments using Expand-Contract pattern
- ✅ Multi-tenancy: Platform migrations + Tenant app migrations
- ✅ Independent modules - no dependency ordering needed
- ✅ Rollback support with automated backups
- ✅ Long-running migrations via background jobs

---

## Migration Organization

### Project Structure

Each module has a dedicated migrations project:

```
/server/src/Modules/{ModuleName}
  /{ModuleName}.Migrations
    Datarizen.{ModuleName}.Migrations.csproj
    /Migrations
      20250101120000_CreateUserTable.cs
      20250101130000_AddUserEmailIndex.cs
      20250102140000_SeedDefaultRoles.cs
    README.md
```