using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215102000, "Create tenant_application_environments table")]
public class CreateTenantApplicationEnvironmentsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_application_environments")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_application_environments")
            .WithColumn("tenant_application_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("environment_type").AsString(50).NotNullable()
            .WithColumn("application_release_id").AsGuid().Nullable()
            .WithColumn("release_version").AsString(50).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable()
            .WithColumn("deployed_at").AsDateTime().Nullable()
            .WithColumn("deployed_by").AsGuid().Nullable()
            .WithColumn("configuration_json").AsCustom("jsonb").Nullable()
            .WithColumn("database_name").AsString(200).Nullable()
            .WithColumn("connection_string").AsString(2000).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_tenant_application_environments_tenant_application_id")
            .FromTable("tenant_application_environments").InSchema("tenantapplication").ForeignColumn("tenant_application_id")
            .ToTable("tenant_applications").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_tenant_application_environments_tenant_application_id")
            .OnTable("tenant_application_environments")
            .InSchema("tenantapplication")
            .OnColumn("tenant_application_id");
    }

    public override void Down()
    {
        Delete.Table("tenant_application_environments").InSchema("tenantapplication");
    }
}
