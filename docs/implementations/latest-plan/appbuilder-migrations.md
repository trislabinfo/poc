# AppBuilder Module - Migrations Layer

**Status**: ✅ Updated to align with new domain model  
**Last Updated**: 2026-02-11  
**Module**: AppBuilder  
**Layer**: Migrations  

---

## Overview

FluentMigrator migrations for the AppBuilder module schema.

**Key Changes from Domain Update**:
- ❌ Removed application_schemas table
- ✅ Added entity_definitions table
- ✅ Added property_definitions table
- ✅ Added relation_definitions table
- ✅ Renamed *_components tables to *_definitions
- ✅ Split snapshot_json into navigation_json, page_json, datasource_json, entity_json
- ✅ Added major, minor, patch columns
- ✅ Removed icon_url and created_by columns
- ✅ Removed version string column (computed from major.minor.patch)
- ✅ Removed type column from navigation_definitions (moved to configuration_json)
- ✅ Removed type column from datasource_definitions (moved to configuration_json)
- ✅ Renamed layout_json to configuration_json in page_definitions
- ✅ Added is_active column to application_releases

---

## Migration Files

```
AppBuilder.Migrations/
└── Migrations/
    └── Schema/
        ├── 20260211100000_CreateAppBuilderSchema.cs
        ├── 20260211100100_CreateApplicationDefinitionsTable.cs
        ├── 20260211100200_CreateEntityDefinitionsTable.cs
        ├── 20260211100300_CreatePropertyDefinitionsTable.cs
        ├── 20260211100400_CreateRelationDefinitionsTable.cs
        ├── 20260211100500_CreateNavigationDefinitionsTable.cs
        ├── 20260211100600_CreatePageDefinitionsTable.cs
        ├── 20260211100700_CreateDataSourceDefinitionsTable.cs
        └── 20260211100800_CreateApplicationReleasesTable.cs
```

---

## Migration 1: Create Schema

```csharp
[Migration(20260211100000)]
public class CreateAppBuilderSchema : Migration
{
    public override void Up()
    {
        Create.Schema("appbuilder");
    }

    public override void Down()
    {
        Delete.Schema("appbuilder");
    }
}
```

---

## Migration 2: Create ApplicationDefinitions Table

```csharp
[Migration(20260211100100)]
public class CreateApplicationDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("application_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(1000).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable()
            .WithColumn("status").AsString(50).NotNullable()
            .WithColumn("current_version").AsString(50).Nullable()
            .WithColumn("is_public").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_application_definitions_slug")
            .OnTable("application_definitions")
            .InSchema("appbuilder")
            .OnColumn("slug")
            .Unique();

        Create.Index("ix_application_definitions_status")
            .OnTable("application_definitions")
            .InSchema("appbuilder")
            .OnColumn("status");
    }

    public override void Down()
    {
        Delete.Table("application_definitions").InSchema("appbuilder");
    }
}
```

---

## Migration 3: Create EntityDefinitions Table

```csharp
[Migration(20260211100200)]
public class CreateEntityDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("entity_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("application_id").AsGuid().NotNullable()
                .ForeignKey("fk_entity_definitions_application_id", "appbuilder", "application_definitions", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("display_name").AsString(200).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("attributes").AsCustom("jsonb").NotNullable()
            .WithColumn("primary_key").AsString(100).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("ix_entity_definitions_application_id")
            .OnTable("entity_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id");

        Create.Index("ix_entity_definitions_application_id_name")
            .OnTable("entity_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id")
            .OnColumn("name")
            .Unique();
    }

    public override void Down()
    {
        Delete.Table("entity_definitions").InSchema("appbuilder");
    }
}
```

---

## Migration 4: Create PropertyDefinitions Table

```csharp
[Migration(20260211100300)]
public class CreatePropertyDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("property_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("entity_id").AsGuid().NotNullable()
                .ForeignKey("fk_property_definitions_entity_id", "appbuilder", "entity_definitions", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("display_name").AsString(200).NotNullable()
            .WithColumn("data_type").AsString(50).NotNullable()
            .WithColumn("is_required").AsBoolean().NotNullable()
            .WithColumn("default_value").AsString(500).Nullable()
            .WithColumn("validation_rules").AsCustom("jsonb").NotNullable()
            .WithColumn("order").AsInt32().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("ix_property_definitions_entity_id")
            .OnTable("property_definitions")
            .InSchema("appbuilder")
            .OnColumn("entity_id");

        Create.Index("ix_property_definitions_entity_id_name")
            .OnTable("property_definitions")
            .InSchema("appbuilder")
            .OnColumn("entity_id")
            .OnColumn("name")
            .Unique();

        Create.Index("ix_property_definitions_entity_id_order")
            .OnTable("property_definitions")
            .InSchema("appbuilder")
            .OnColumn("entity_id")
            .OnColumn("order");
    }

    public override void Down()
    {
        Delete.Table("property_definitions").InSchema("appbuilder");
    }
}
```

---

## Migration 5: Create RelationDefinitions Table

```csharp
[Migration(20260211100400)]
public class CreateRelationDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("relation_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("source_entity_id").AsGuid().NotNullable()
                .ForeignKey("fk_relation_definitions_source_entity_id", "appbuilder", "entity_definitions", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("target_entity_id").AsGuid().NotNullable()
                .ForeignKey("fk_relation_definitions_target_entity_id", "appbuilder", "entity_definitions", "id")
                .OnDelete(System.Data.Rule.None)
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("relation_type").AsString(50).NotNullable()
            .WithColumn("cascade_delete").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("ix_relation_definitions_source_entity_id")
            .OnTable("relation_definitions")
            .InSchema("appbuilder")
            .OnColumn("source_entity_id");

        Create.Index("ix_relation_definitions_target_entity_id")
            .OnTable("relation_definitions")
            .InSchema("appbuilder")
            .OnColumn("target_entity_id");

        Create.Index("ix_relation_definitions_source_entity_id_name")
            .OnTable("relation_definitions")
            .InSchema("appbuilder")
            .OnColumn("source_entity_id")
            .OnColumn("name")
            .Unique();
    }

    public override void Down()
    {
        Delete.Table("relation_definitions").InSchema("appbuilder");
    }
}
```

---

## Migration 6: Create NavigationDefinitions Table

```csharp
[Migration(20260211100500)]
public class CreateNavigationDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("navigation_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("application_id").AsGuid().NotNullable()
                .ForeignKey("fk_navigation_definitions_application_id", "appbuilder", "application_definitions", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("configuration_json").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_navigation_definitions_application_id")
            .OnTable("navigation_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id");
    }

    public override void Down()
    {
        Delete.Table("navigation_definitions").InSchema("appbuilder");
    }
}
```

---

## Migration 7: Create PageDefinitions Table

```csharp
[Migration(20260211100600)]
public class CreatePageDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("page_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("application_id").AsGuid().NotNullable()
                .ForeignKey("fk_page_definitions_application_id", "appbuilder", "application_definitions", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("route").AsString(500).NotNullable()
            .WithColumn("configuration_json").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_page_definitions_application_id")
            .OnTable("page_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id");

        Create.Index("ix_page_definitions_application_id_route")
            .OnTable("page_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id")
            .OnColumn("route")
            .Unique();
    }

    public override void Down()
    {
        Delete.Table("page_definitions").InSchema("appbuilder");
    }
}
```

---

## Migration 8: Create DataSourceDefinitions Table

```csharp
[Migration(20260211100700)]
public class CreateDataSourceDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("datasource_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("application_id").AsGuid().NotNullable()
                .ForeignKey("fk_datasource_definitions_application_id", "appbuilder", "application_definitions", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("type").AsString(50).NotNullable()
            .WithColumn("configuration_json").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_datasource_definitions_application_id")
            .OnTable("datasource_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id");
    }

    public override void Down()
    {
        Delete.Table("datasource_definitions").InSchema("appbuilder");
    }
}
```

---

## Migration 9: Create ApplicationReleases Table

```csharp
[Migration(20260211100800)]
public class CreateApplicationReleasesTable : Migration
{
    public override void Up()
    {
        Create.Table("application_releases")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("application_id").AsGuid().NotNullable()
                .ForeignKey("fk_application_releases_application_id", "appbuilder", "application_definitions", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("major").AsInt32().NotNullable()
            .WithColumn("minor").AsInt32().NotNullable()
            .WithColumn("patch").AsInt32().NotNullable()
            .WithColumn("release_notes").AsString(5000).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("released_at").AsDateTime().NotNullable()
            .WithColumn("navigation_json").AsCustom("jsonb").NotNullable()
            .WithColumn("page_json").AsCustom("jsonb").NotNullable()
            .WithColumn("datasource_json").AsCustom("jsonb").NotNullable()
            .WithColumn("entity_json").AsCustom("jsonb").NotNullable();

        Create.Index("ix_application_releases_application_id")
            .OnTable("application_releases")
            .InSchema("appbuilder")
            .OnColumn("application_id");

        Create.Index("ix_application_releases_application_id_version")
            .OnTable("application_releases")
            .InSchema("appbuilder")
            .OnColumn("application_id")
            .OnColumn("major")
            .OnColumn("minor")
            .OnColumn("patch")
            .Unique();

        Create.Index("ix_application_releases_application_id_is_active")
            .OnTable("application_releases")
            .InSchema("appbuilder")
            .OnColumn("application_id")
            .OnColumn("is_active");
    }

    public override void Down()
    {
        Delete.Table("application_releases").InSchema("appbuilder");
    }
}
```

---

## Data Migrations

### Seed Sample Applications

**File**: `20260211200000_SeedSampleApplications.cs`

```csharp
using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Data;

[Migration(20260211200000, "Seed sample applications")]
[Profile("Development")]
public class SeedSampleApplications : Migration
{
    public override void Up()
    {
        var sampleAppId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var customerEntityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        
        // Insert sample application
        Insert.IntoTable("application_definitions").InSchema("appbuilder")
            .Row(new
            {
                id = sampleAppId,
                name = "Sample CRM Application",
                description = "A sample CRM application for demonstration",
                slug = "sample-crm",
                status = "Draft",
                created_at = DateTime.UtcNow,
                updated_at = (DateTime?)null
            });

        // Insert sample entity
        Insert.IntoTable("entity_definitions").InSchema("appbuilder")
            .Row(new
            {
                id = customerEntityId,
                application_id = sampleAppId,
                name = "Customer",
                display_name = "Customer",
                description = "Customer entity",
                attributes = "{}",
                primary_key = "id",
                created_at = DateTime.UtcNow
            });

        // Insert sample properties
        Insert.IntoTable("property_definitions").InSchema("appbuilder")
            .Row(new
            {
                id = Guid.NewGuid(),
                entity_id = customerEntityId,
                name = "firstName",
                display_name = "First Name",
                data_type = "String",
                is_required = true,
                default_value = (string?)null,
                validation_rules = "{\"minLength\": 2, \"maxLength\": 100}",
                order = 1,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.NewGuid(),
                entity_id = customerEntityId,
                name = "lastName",
                display_name = "Last Name",
                data_type = "String",
                is_required = true,
                default_value = (string?)null,
                validation_rules = "{\"minLength\": 2, \"maxLength\": 100}",
                order = 2,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.NewGuid(),
                entity_id = customerEntityId,
                name = "email",
                display_name = "Email",
                data_type = "Email",
                is_required = true,
                default_value = (string?)null,
                validation_rules = "{}",
                order = 3,
                created_at = DateTime.UtcNow
            });
    }

    public override void Down()
    {
        var sampleAppId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        Delete.FromTable("application_definitions").InSchema("appbuilder")
            .Row(new { id = sampleAppId });
    }
}
```

---

## README.md

**File**: `AppBuilder.Migrations/README.md`

```markdown
# AppBuilder Module - Database Migrations

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
- Seed data (sample applications)
- Reference data
- Development data (with `[Profile("Development")]`)

**Naming**: `YYYYMMDD2HHmmss_DescriptiveName.cs` (note the "2" prefix for data migrations)

## Running Migrations

Module list comes from `appsettings.json` / `Deployment:Topology`. Set the environment so the correct seed data is loaded:

```bash
# Development (loads sample applications)
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project server/src/MigrationRunner
```

Or use `DOTNET_ENVIRONMENT`:

```bash
set DOTNET_ENVIRONMENT=Development
dotnet run --project server/src/MigrationRunner
```

- **Development** – seeds sample applications
- **Staging** – no seed data
- **Production** – no seed data

## Schema

**Schema Name**: `appbuilder`

**Tables**:
- `application_definitions` - Application definition aggregate root
- `entity_definitions` - Entity definitions (tables)
- `property_definitions` - Property definitions (columns)
- `relation_definitions` - Relation definitions (foreign keys)
- `navigation_definitions` - Navigation components
- `page_definitions` - Page components
- `datasource_definitions` - Data source components
- `application_releases` - Immutable releases with snapshots

## FluentMigrator Version Table

FluentMigrator automatically creates `appbuilder.__FluentMigrator_VersionInfo` when you run the first migration.

**Structure**:
```sql
CREATE TABLE appbuilder."__FluentMigrator_VersionInfo" (
    "Version" BIGINT PRIMARY KEY,
    "AppliedOn" TIMESTAMP,
    "Description" VARCHAR(1024)
);
```

## Dependencies

AppBuilder has no migration dependencies (runs first or in parallel with other modules).

## Next Steps

1. ✅ Create all schema migrations
2. ✅ Create data migrations for Development
3. ✅ Test migrations locally
4. ✅ Update MigrationRunner to include AppBuilder
```

---

## Module Registration

**File**: `AppBuilder.Module/AppBuilderModule.cs`

```csharp
public class AppBuilderModule : IModule
{
    public string ModuleName => "AppBuilder";
    public string SchemaName => "appbuilder";

    public string[] GetMigrationDependencies()
    {
        // AppBuilder has no dependencies
        return Array.Empty<string>();
    }

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAppBuilderDomain();
        services.AddAppBuilderApplication();
        services.AddAppBuilderInfrastructure(configuration);
    }
}
```

---

## Update MigrationRunner

**File**: `server/src/MigrationRunner/appsettings.json`

```json
{
  "MigrationRunner": {
    "ModulesByTopology": {
      "Monolith": [ "Tenant", "Identity", "User", "Feature", "AppBuilder" ],
      "DistributedApp": [ "Tenant", "Identity", "User", "Feature", "AppBuilder" ],
      "Microservices": [ "Tenant", "Identity", "User", "Feature", "AppBuilder" ]
    }
  }
}
```

---

## Success Criteria

- ✅ All schema migrations created
- ✅ Removed application_schemas table
- ✅ Added entity_definitions, property_definitions, relation_definitions tables
- ✅ Renamed *_components to *_definitions
- ✅ Split snapshot_json into 4 separate JSON columns
- ✅ Added major, minor, patch columns
- ✅ Removed icon_url and created_by columns
- ✅ Added is_active column to application_releases
- ✅ All indexes created
- ✅ All foreign keys configured
- ✅ Data migrations created for Development
- ✅ README.md created
- ✅ Module registered
- ✅ MigrationRunner updated

