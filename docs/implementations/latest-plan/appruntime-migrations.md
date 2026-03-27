# AppRuntime Module - Migrations Layer Implementation Plan

## Overview

The AppRuntime Migrations layer contains FluentMigrator migrations for creating and managing the database schema for runtime instances, runtime versions, and component registrations.

---

## Migration Dependencies

**AppRuntime has NO dependencies** - it's an independent module that doesn't reference other module schemas.

```csharp
public class AppRuntimeMigrationModule : IMigrationModule
{
    public string ModuleName => "AppRuntime";
    
    public List<string> GetMigrationDependencies()
    {
        return new List<string>(); // No dependencies
    }
}
```

---

## Migrations

### Migration 001: Create Schema

**File**: `AppRuntime.Migrations/Migrations/001_CreateSchema.cs`

**Version**: 202501150001

**Purpose**: Create the `appruntime` schema.

```csharp
[Migration(202501150001)]
public class CreateSchema : Migration
{
    public override void Up()
    {
        Create.Schema("appruntime");
    }
    
    public override void Down()
    {
        Delete.Schema("appruntime");
    }
}
```

---

### Migration 002: Create RuntimeVersions Table

**File**: `AppRuntime.Migrations/Migrations/002_CreateRuntimeVersionsTable.cs`

**Version**: 202501150002

**Purpose**: Create the `runtime_versions` table.

```csharp
[Migration(202501150002)]
public class CreateRuntimeVersionsTable : Migration
{
    public override void Up()
    {
        Create.Table("runtime_versions")
            .InSchema("appruntime")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("version").AsString(50).NotNullable()
            .WithColumn("min_application_version").AsString(50).NotNullable()
            .WithColumn("max_application_version").AsString(50).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("is_default").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("supported_component_types").AsCustom("jsonb").NotNullable()
            .WithColumn("release_notes").AsString(2000).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();
        
        // Unique constraint on version
        Create.UniqueConstraint("uq_runtime_versions_version")
            .OnTable("runtime_versions")
            .WithSchema("appruntime")
            .Column("version");
        
        // Index on is_active
        Create.Index("ix_runtime_versions_is_active")
            .OnTable("runtime_versions")
            .InSchema("appruntime")
            .OnColumn("is_active");
        
        // Index on is_default
        Create.Index("ix_runtime_versions_is_default")
            .OnTable("runtime_versions")
            .InSchema("appruntime")
            .OnColumn("is_default");
    }
    
    public override void Down()
    {
        Delete.Table("runtime_versions").InSchema("appruntime");
    }
}
```

---

### Migration 003: Create ComponentRegistrations Table

**File**: `AppRuntime.Migrations/Migrations/003_CreateComponentRegistrationsTable.cs`

**Version**: 202501150003

**Purpose**: Create the `component_registrations` table.

```csharp
[Migration(202501150003)]
public class CreateComponentRegistrationsTable : Migration
{
    public override void Up()
    {
        Create.Table("component_registrations")
            .InSchema("appruntime")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("component_type").AsString(100).NotNullable()
            .WithColumn("loader_type_name").AsString(500).NotNullable()
            .WithColumn("assembly_name").AsString(200).NotNullable()
            .WithColumn("version").AsString(50).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("capabilities").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();
        
        // Unique constraint on component_type
        Create.UniqueConstraint("uq_component_registrations_component_type")
            .OnTable("component_registrations")
            .WithSchema("appruntime")
            .Column("component_type");
        
        // Index on is_active
        Create.Index("ix_component_registrations_is_active")
            .OnTable("component_registrations")
            .InSchema("appruntime")
            .OnColumn("is_active");
    }
    
    public override void Down()
    {
        Delete.Table("component_registrations").InSchema("appruntime");
    }
}
```

---

### Migration 004: Create RuntimeInstances Table

**File**: `AppRuntime.Migrations/Migrations/004_CreateRuntimeInstancesTable.cs`

**Version**: 202501150004

**Purpose**: Create the `runtime_instances` table.

```csharp
[Migration(202501150004)]
public class CreateRuntimeInstancesTable : Migration
{
    public override void Up()
    {
        Create.Table("runtime_instances")
            .InSchema("appruntime")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("tenant_application_id").AsGuid().NotNullable()
            .WithColumn("application_release_id").AsGuid().NotNullable()
            .WithColumn("environment_id").AsGuid().NotNullable()
            .WithColumn("application_version").AsString(50).NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("started_at").AsDateTime().NotNullable()
            .WithColumn("stopped_at").AsDateTime().Nullable()
            .WithColumn("last_health_check_at").AsDateTime().Nullable()
            .WithColumn("configuration").AsCustom("jsonb").NotNullable()
            .WithColumn("metadata").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();
        
        // Index on tenant_application_id
        Create.Index("ix_runtime_instances_tenant_application_id")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("tenant_application_id");
        
        // Index on environment_id
        Create.Index("ix_runtime_instances_environment_id")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("environment_id");
        
        // Index on status
        Create.Index("ix_runtime_instances_status")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("status");
        
        // Composite index on environment_id + status
        Create.Index("ix_runtime_instances_environment_status")
            .OnTable("runtime_instances")
            .InSchema("appruntime")
            .OnColumn("environment_id")
            .OnColumn("status");
    }
    
    public override void Down()
    {
        Delete.Table("runtime_instances").InSchema("appruntime");
    }
}
```

---

### Migration 005: Seed Default Runtime Version

**File**: `AppRuntime.Migrations/Migrations/005_SeedDefaultRuntimeVersion.cs`

**Version**: 202501150005

**Purpose**: Seed the default runtime version (1.0.0).

```csharp
[Migration(202501150005)]
public class SeedDefaultRuntimeVersion : Migration
{
    public override void Up()
    {
        var defaultVersionId = Guid.NewGuid();
        var supportedComponentTypes = JsonSerializer.Serialize(new[]
        {
            "Navigation",
            "Page",
            "DataSource.PostgreSQL",
            "DataSource.Redis"
        });
        
        Insert.IntoTable("runtime_versions")
            .InSchema("appruntime")
            .Row(new
            {
                id = defaultVersionId,
                version = "1.0.0",
                min_application_version = "1.0.0",
                max_application_version = (string?)null,
                is_active = true,
                is_default = true,
                supported_component_types = supportedComponentTypes,
                release_notes = "Initial runtime version with support for basic components",
                created_at = DateTime.UtcNow,
                updated_at = (DateTime?)null
            });
    }
    
    public override void Down()
    {
        Delete.FromTable("runtime_versions")
            .InSchema("appruntime")
            .Row(new { version = "1.0.0" });
    }
}
```

---

### Migration 006: Seed Component Registrations

**File**: `AppRuntime.Migrations/Migrations/006_SeedComponentRegistrations.cs`

**Version**: 202501150006

**Purpose**: Seed default component loader registrations.

```csharp
[Migration(202501150006)]
public class SeedComponentRegistrations : Migration
{
    public override void Up()
    {
        // Navigation loader
        Insert.IntoTable("component_registrations")
            .InSchema("appruntime")
            .Row(new
            {
                id = Guid.NewGuid(),
                component_type = "Navigation",
                loader_type_name = "Datarizen.AppRuntime.Infrastructure.Loaders.NavigationLoader",
                assembly_name = "Datarizen.AppRuntime.Infrastructure",
                version = "1.0.0",
                is_active = true,
                capabilities = JsonSerializer.Serialize(new { supportsNesting = true }),
                created_at = DateTime.UtcNow,
                updated_at = (DateTime?)null
            });
        
        // Page loader
        Insert.IntoTable("component_registrations")
            .InSchema("appruntime")
            .Row(new
            {
                id = Guid.NewGuid(),
                component_type = "Page",
                loader_type_name = "Datarizen.AppRuntime.Infrastructure.Loaders.PageLoader",
                assembly_name = "Datarizen.AppRuntime.Infrastructure",
                version = "1.0.0",
                is_active = true,
                capabilities = JsonSerializer.Serialize(new { supportsLayouts = true }),
                created_at = DateTime.UtcNow,
                updated_at = (DateTime?)null
            });
        
        // PostgreSQL data source loader
        Insert.IntoTable("component_registrations")
            .InSchema("appruntime")
            .Row(new
            {
                id = Guid.NewGuid(),
                component_type = "DataSource.PostgreSQL",
                loader_type_name = "Datarizen.AppRuntime.Infrastructure.Loaders.PostgreSqlDataSourceLoader",
                assembly_name = "Datarizen.AppRuntime.Infrastructure",
                version = "1.0.0",
                is_active = true,
                capabilities = JsonSerializer.Serialize(new 
                { 
                    supportsTransactions = true,
                    supportsAsync = true,
                    supportsJsonb = true
                }),
                created_at = DateTime.UtcNow,
                updated_at = (DateTime?)null
            });
        
        // Redis data source loader
        Insert.IntoTable("component_registrations")
            .InSchema("appruntime")
            .Row(new
            {
                id = Guid.NewGuid(),
                component_type = "DataSource.Redis",
                loader_type_name = "Datarizen.AppRuntime.Infrastructure.Loaders.RedisDataSourceLoader",
                assembly_name = "Datarizen.AppRuntime.Infrastructure",
                version = "1.0.0",
                is_active = true,
                capabilities = JsonSerializer.Serialize(new 
                { 
                    supportsCaching = true,
                    supportsPubSub = true
                }),
                created_at = DateTime.UtcNow,
                updated_at = (DateTime?)null
            });
    }
    
    public override void Down()
    {
        Delete.FromTable("component_registrations")
            .InSchema("appruntime")
            .AllRows();
    }
}
```

---

## Migration Module Registration

**File**: `AppRuntime.Migrations/AppRuntimeMigrationModule.cs`

```csharp
public class AppRuntimeMigrationModule : IMigrationModule
{
    public string ModuleName => "AppRuntime";
    
    public List<string> GetMigrationDependencies()
    {
        // AppRuntime has no dependencies on other modules
        return new List<string>();
    }
    
    public Assembly GetMigrationAssembly()
    {
        return typeof(AppRuntimeMigrationModule).Assembly;
    }
}
```

---

## Project Configuration

**File**: `AppRuntime.Migrations/AppRuntime.Migrations.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>Datarizen.AppRuntime.Migrations</RootNamespace>
    <AssemblyName>Datarizen.AppRuntime.Migrations</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentMigrator" />
    <PackageReference Include="FluentMigrator.Runner" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\AppRuntime.Domain\AppRuntime.Domain.csproj" />
    <ProjectReference Include="..\..\BuildingBlocks\Migrations\BuildingBlocks.Migrations.csproj" />
  </ItemGroup>
</Project>
```

---

## Update MigrationRunner Configuration

**File**: `server/src/MigrationRunner/appsettings.json`

Update the module list to include AppRuntime:

```json
{
  "MigrationRunner": {
    "ModulesByTopology": {
      "Monolith": [ "Tenant", "Identity", "User", "Feature", "AppBuilder", "TenantApplication", "AppRuntime" ],
      "DistributedApp": [ "Tenant", "Identity", "User", "Feature", "AppBuilder", "TenantApplication", "AppRuntime" ],
      "Microservices": [ "Tenant", "Identity", "User", "Feature", "AppBuilder", "TenantApplication", "AppRuntime" ]
    }
  }
}
```

---

## Implementation Checklist

### Migrations
- [ ] Create `001_CreateSchema.cs`
- [ ] Create `002_CreateRuntimeVersionsTable.cs`
- [ ] Create `003_CreateComponentRegistrationsTable.cs`
- [ ] Create `004_CreateRuntimeInstancesTable.cs`
- [ ] Create `005_SeedDefaultRuntimeVersion.cs`
- [ ] Create `006_SeedComponentRegistrations.cs`

### Module Registration
- [ ] Create `AppRuntimeMigrationModule.cs`
- [ ] Update `MigrationRunner/appsettings.json`

### Testing
- [ ] Run migrations locally
- [ ] Verify schema creation
- [ ] Verify seed data
- [ ] Test rollback

---

## Estimated Time: 4 hours

- Create schema migration: 0.5 hours
- Create table migrations: 2 hours
- Seed data migrations: 1 hour
- Module registration and testing: 0.5 hours