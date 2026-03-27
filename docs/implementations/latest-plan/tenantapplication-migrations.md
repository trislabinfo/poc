# TenantApplication Module - Migrations Layer (MVP v2)

**Status**: Ready for implementation (shared ApplicationDefinition)  
**Last Updated**: 2026-02-15  
**Module**: TenantApplication  
**Layer**: Migrations  

---

## Shared ApplicationDefinition (migrations)

- Migrations create and maintain the **tenantapplication** schema and all tables used by TenantApplication + shared definition types.  
- Tables include: `tenant_applications`, `tenant_application_environments`, `tenant_application_migrations`, and the definition tables **tenant_entity_definitions**, **tenant_property_definitions**, **tenant_relation_definitions**, **tenant_navigation_definitions**, **tenant_page_definitions**, **tenant_datasource_definitions**, **tenant_application_releases** (same structure as AppBuilder’s definition/release tables, different schema and table names).  
- Migration order and naming should align with the rest of the TenantApplication migrations.  
- **Review**: Confirm schema and table list before implementation.

---

## Overview

FluentMigrator migrations for TenantApplication module schema.

---

## Migration Organization

```
TenantApplication.Migrations/
├── Migrations/
│   ├── Schema/
│   │   ├── 20260212100000_CreateTenantApplicationSchema.cs
│   │   ├── 20260212101000_CreateTenantApplicationsTable.cs
│   │   ├── 20260212102000_CreateTenantApplicationEnvironmentsTable.cs
│   │   ├── 20260212103000_CreateTenantApplicationMigrationsTable.cs
│   │   ├── 20260212104000_CreateApplicationReleasesTable.cs
│   │   ├── 20260212105000_CreateApplicationSnapshotsTable.cs
│   │   ├── 20260212106000_CreateApplicationSchemasTable.cs
│   │   ├── 20260212107000_CreateApplicationEventsTable.cs
│   │   └── 20260212108000_CreateIndexes.cs
│   └── Data/
│       └── (no seed data for production)
└── README.md
```

---

## Schema Migrations

### 1. Create TenantApplication Schema

```csharp
[Migration(20260212100000, "Create TenantApplication schema")]
public class CreateTenantApplicationSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS tenantapplication;");
    }

    public override void Down()
    {
        Execute.Sql("DROP SCHEMA IF EXISTS tenantapplication CASCADE;");
    }
}
```

---

### 2. Create TenantApplications Table

```csharp
[Migration(20260212101000, "Create tenant_applications table")]
public class CreateTenantApplicationsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_applications").InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("application_release_id").AsGuid().Nullable()
            .WithColumn("application_id").AsGuid().Nullable()
            .WithColumn("major").AsInt32().Nullable()
            .WithColumn("minor").AsInt32().Nullable()
            .WithColumn("patch").AsInt32().Nullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("is_custom").AsBoolean().NotNullable()
            .WithColumn("source_application_release_id").AsGuid().Nullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("configuration").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("installed_at").AsDateTime().Nullable()
            .WithColumn("activated_at").AsDateTime().Nullable()
            .WithColumn("deactivated_at").AsDateTime().Nullable()
            .WithColumn("uninstalled_at").AsDateTime().Nullable();

        Create.Index("ix_tenant_applications_tenant_id_slug")
            .OnTable("tenant_applications").InSchema("tenantapplication")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("slug").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("tenant_applications").InSchema("tenantapplication");
    }
}
```

---

### 3. Create TenantApplicationEnvironments Table

```csharp
[Migration(20260212102000, "Create tenant_application_environments table")]
public class CreateTenantApplicationEnvironmentsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_application_environments").InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_application_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("environment_type").AsString(50).NotNullable()
            .WithColumn("application_release_id").AsGuid().Nullable()
            .WithColumn("release_version").AsString(50).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable()
            .WithColumn("deployed_at").AsDateTime().Nullable()
            .WithColumn("deployed_by").AsGuid().Nullable()
            .WithColumn("configuration").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_tenant_application_environments_tenant_application_id_name")
            .OnTable("tenant_application_environments").InSchema("tenantapplication")
            .OnColumn("tenant_application_id").Ascending()
            .OnColumn("name").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("tenant_application_environments").InSchema("tenantapplication");
    }
}
```

---

### 4. Create TenantApplicationMigrations Table

```csharp
[Migration(20260212103000, "Create tenant_application_migrations table")]
public class CreateTenantApplicationMigrationsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_application_migrations").InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_application_environment_id").AsGuid().NotNullable()
            .WithColumn("from_release_id").AsGuid().Nullable()
            .WithColumn("to_release_id").AsGuid().NotNullable()
            .WithColumn("migration_script_json").AsCustom("jsonb").NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("executed_at").AsDateTime().Nullable()
            .WithColumn("error_message").AsString(int.MaxValue).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("tenant_application_migrations").InSchema("tenantapplication");
    }
}
```

---

### 5-8. Tenant-Level Definition Tables (Optional)

When the AppBuilder feature is enabled for tenants, the following tables may be created in `tenantapplication` schema (mirroring AppBuilder’s current schema, without deprecated tables):

- `tenant_application_releases` – same shape as `appbuilder.application_releases`
- `tenant_entity_definitions`, `tenant_property_definitions`, `tenant_relation_definitions`
- `tenant_navigation_definitions`, `tenant_page_definitions`, `tenant_datasource_definitions`

AppBuilder no longer uses `application_snapshots`, `application_schemas`, or `application_events`; omit or defer tenant-level equivalents unless required for event-sourcing.

---

### 9. Create Indexes

```csharp
[Migration(20260212108000, "Create TenantApplication indexes")]
public class CreateIndexes : Migration
{
    public override void Up()
    {
        Create.Index("ix_tenant_applications_tenant_id")
            .OnTable("tenant_applications").InSchema("tenantapplication")
            .OnColumn("tenant_id").Ascending();

        Create.Index("ix_tenant_application_environments_tenant_application_id")
            .OnTable("tenant_application_environments").InSchema("tenantapplication")
            .OnColumn("tenant_application_id").Ascending();

        Create.Index("ix_tenant_application_environments_environment_type")
            .OnTable("tenant_application_environments").InSchema("tenantapplication")
            .OnColumn("environment_type").Ascending();
    }

    public override void Down()
    {
        Delete.Index("ix_tenant_applications_tenant_id").OnTable("tenant_applications").InSchema("tenantapplication");
        Delete.Index("ix_tenant_application_environments_tenant_application_id").OnTable("tenant_application_environments").InSchema("tenantapplication");
        Delete.Index("ix_tenant_application_environments_environment_type").OnTable("tenant_application_environments").InSchema("tenantapplication");
    }
}
```

**Note**: Additional indexes (e.g. `ix_tenant_applications_application_id`, `ix_tenant_applications_application_id_version`) may be added in the same migration or a later one. The migration snippet above is the minimum set; expand as needed.