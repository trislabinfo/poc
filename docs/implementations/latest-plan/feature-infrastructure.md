# Feature Module - Migrations Layer

**Status**: ✅ Complete  
**Last Updated**: 2026-02-11  
**Module**: Feature  
**Layer**: Migrations  

---

## Overview

FluentMigrator migrations for the Feature module schema and tables.

---

## Migration 1: Create Schema

**File**: `20260211100000_CreateFeatureSchema.cs`

```csharp
namespace Datarizen.Feature.Migrations;

[Migration(20260211100000)]
public sealed class CreateFeatureSchema : Migration
{
    public override void Up()
    {
        Create.Schema("feature");
    }

    public override void Down()
    {
        Delete.Schema("feature");
    }
}
```

---

## Migration 2: Create Features Table

**File**: `20260211100001_CreateFeaturesTable.cs`

```csharp
namespace Datarizen.Feature.Migrations;

[Migration(20260211100001)]
public sealed class CreateFeaturesTable : Migration
{
    public override void Up()
    {
        Create.Table("features")
            .InSchema("feature")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("code").AsString(100).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("category").AsString(100).NotNullable()
            .WithColumn("is_globally_enabled").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        // Unique index on code
        Create.Index("ix_features_code")
            .OnTable("features")
            .InSchema("feature")
            .OnColumn("code")
            .Ascending()
            .WithOptions()
            .Unique();

        // Index on category
        Create.Index("ix_features_category")
            .OnTable("features")
            .InSchema("feature")
            .OnColumn("category")
            .Ascending();

        // Index on is_globally_enabled
        Create.Index("ix_features_is_globally_enabled")
            .OnTable("features")
            .InSchema("feature")
            .OnColumn("is_globally_enabled")
            .Ascending();
    }

    public override void Down()
    {
        Delete.Index("ix_features_is_globally_enabled").OnTable("features").InSchema("feature");
        Delete.Index("ix_features_category").OnTable("features").InSchema("feature");
        Delete.Index("ix_features_code").OnTable("features").InSchema("feature");
        Delete.Table("features").InSchema("feature");
    }
}
```

---

## Migration 3: Create Feature Flags Table

**File**: `20260211100002_CreateFeatureFlagsTable.cs`

```csharp
namespace Datarizen.Feature.Migrations;

[Migration(20260211100002)]
public sealed class CreateFeatureFlagsTable : Migration
{
    public override void Up()
    {
        Create.Table("feature_flags")
            .InSchema("feature")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("feature_id").AsGuid().NotNullable()
            .WithColumn("tenant_id").AsGuid().Nullable()
            .WithColumn("user_id").AsGuid().Nullable()
            .WithColumn("is_enabled").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("configuration").AsCustom("jsonb").Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        // Foreign key to features
        Create.ForeignKey("fk_feature_flags_feature_id")
            .FromTable("feature_flags").InSchema("feature").ForeignColumn("feature_id")
            .ToTable("features").InSchema("feature").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Index on feature_id
        Create.Index("ix_feature_flags_feature_id")
            .OnTable("feature_flags")
            .InSchema("feature")
            .OnColumn("feature_id")
            .Ascending();

        // Index on tenant_id
        Create.Index("ix_feature_flags_tenant_id")
            .OnTable("feature_flags")
            .InSchema("feature")
            .OnColumn("tenant_id")
            .Ascending();

        // Index on user_id
        Create.Index("ix_feature_flags_user_id")
            .OnTable("feature_flags")
            .InSchema("feature")
            .OnColumn("user_id")
            .Ascending();

        // Unique composite index on feature_id + tenant_id (where tenant_id IS NOT NULL)
        Create.Index("ix_feature_flags_feature_id_tenant_id")
            .OnTable("feature_flags")
            .InSchema("feature")
            .OnColumn("feature_id").Ascending()
            .OnColumn("tenant_id").Ascending()
            .WithOptions()
            .Unique();

        // Unique composite index on feature_id + user_id (where user_id IS NOT NULL)
        Create.Index("ix_feature_flags_feature_id_user_id")
            .OnTable("feature_flags")
            .InSchema("feature")
            .OnColumn("feature_id").Ascending()
            .OnColumn("user_id").Ascending()
            .WithOptions()
            .Unique();

        // Check constraint: must have either tenant_id or user_id, but not both
        Execute.Sql(@"
            ALTER TABLE feature.feature_flags
            ADD CONSTRAINT ck_feature_flags_scope
            CHECK (
                (tenant_id IS NOT NULL AND user_id IS NULL) OR
                (tenant_id IS NULL AND user_id IS NOT NULL)
            );
        ");
    }

    public override void Down()
    {
        Execute.Sql("ALTER TABLE feature.feature_flags DROP CONSTRAINT ck_feature_flags_scope;");
        Delete.Index("ix_feature_flags_feature_id_user_id").OnTable("feature_flags").InSchema("feature");
        Delete.Index("ix_feature_flags_feature_id_tenant_id").OnTable("feature_flags").InSchema("feature");
        Delete.Index("ix_feature_flags_user_id").OnTable("feature_flags").InSchema("feature");
        Delete.Index("ix_feature_flags_tenant_id").OnTable("feature_flags").InSchema("feature");
        Delete.Index("ix_feature_flags_feature_id").OnTable("feature_flags").InSchema("feature");
        Delete.ForeignKey("fk_feature_flags_feature_id").OnTable("feature_flags").InSchema("feature");
        Delete.Table("feature_flags").InSchema("feature");
    }
}
```

---

## Migration 4: Seed Default Features

**File**: `20260211100003_SeedDefaultFeatures.cs`

```csharp
namespace Datarizen.Feature.Migrations;

[Migration(20260211100003)]
public sealed class SeedDefaultFeatures : Migration
{
    public override void Up()
    {
        // Core features
        Insert.IntoTable("features").InSchema("feature")
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                code = "advanced-analytics",
                name = "Advanced Analytics",
                description = "Access to advanced analytics and reporting features",
                category = "Analytics",
                is_globally_enabled = false,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                code = "custom-branding",
                name = "Custom Branding",
                description = "Ability to customize application branding and themes",
                category = "Customization",
                is_globally_enabled = false,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                code = "api-access",
                name = "API Access",
                description = "Access to REST API endpoints",
                category = "Integration",
                is_globally_enabled = true,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                code = "export-data",
                name = "Data Export",
                description = "Ability to export data in various formats",
                category = "Data",
                is_globally_enabled = true,
                created_at = DateTime.UtcNow
            })
            .Row(new
            {
                id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                code = "multi-language",
                name = "Multi-Language Support",
                description = "Support for multiple languages and localization",
                category = "Localization",
                is_globally_enabled = false,
                created_at = DateTime.UtcNow
            });
    }

    public override void Down()
    {
        Delete.FromTable("features").InSchema("feature")
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000001") })
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000002") })
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000003") })
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000004") })
            .Row(new { id = Guid.Parse("00000000-0000-0000-0000-000000000005") });
    }
}
```

---

## Database Schema

### Tables

#### features

| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PRIMARY KEY |
| code | varchar(100) | NOT NULL, UNIQUE |
| name | varchar(200) | NOT NULL |
| description | varchar(1000) | NULL |
| category | varchar(100) | NOT NULL |
| is_globally_enabled | boolean | NOT NULL, DEFAULT false |
| created_at | timestamp | NOT NULL |
| updated_at | timestamp | NULL |

**Indexes**:
- `ix_features_code` (UNIQUE) on `code`
- `ix_features_category` on `category`
- `ix_features_is_globally_enabled` on `is_globally_enabled`

#### feature_flags

| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PRIMARY KEY |
| feature_id | uuid | NOT NULL, FK → features.id |
| tenant_id | uuid | NULL |
| user_id | uuid | NULL |
| is_enabled | boolean | NOT NULL, DEFAULT false |
| configuration | jsonb | NULL |
| created_at | timestamp | NOT NULL |
| updated_at | timestamp | NULL |

**Indexes**:
- `ix_feature_flags_feature_id` on `feature_id`
- `ix_feature_flags_tenant_id` on `tenant_id`
- `ix_feature_flags_user_id` on `user_id`
- `ix_feature_flags_feature_id_tenant_id` (UNIQUE) on `feature_id, tenant_id`
- `ix_feature_flags_feature_id_user_id` (UNIQUE) on `feature_id, user_id`

**Constraints**:
- `ck_feature_flags_scope`: `(tenant_id IS NOT NULL AND user_id IS NULL) OR (tenant_id IS NULL AND user_id IS NOT NULL)`

---

## Migration Order

1. `20260211100000_CreateFeatureSchema.cs` - Create schema
2. `20260211100001_CreateFeaturesTable.cs` - Create features table
3. `20260211100002_CreateFeatureFlagsTable.cs` - Create feature_flags table
4. `20260211100003_SeedDefaultFeatures.cs` - Seed default features

---

## Success Criteria

- ✅ Schema created
- ✅ Features table with unique code constraint
- ✅ Feature flags table with scope validation
- ✅ Foreign key relationship (cascade delete)
- ✅ Composite unique indexes for tenant/user scopes
- ✅ Check constraint for XOR scope validation
- ✅ Default features seeded
- ✅ JSONB column for configuration