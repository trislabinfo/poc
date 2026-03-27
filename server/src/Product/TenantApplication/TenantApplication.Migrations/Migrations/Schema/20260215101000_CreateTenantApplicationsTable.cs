using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215101000, "Create tenant_applications table")]
public class CreateTenantApplicationsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_applications")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_applications")
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
            .WithColumn("status").AsString(50).NotNullable()
            .WithColumn("configuration").AsCustom("jsonb").NotNullable()
            .WithColumn("installed_at").AsDateTime().Nullable()
            .WithColumn("activated_at").AsDateTime().Nullable()
            .WithColumn("deactivated_at").AsDateTime().Nullable()
            .WithColumn("uninstalled_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_tenant_applications_tenant_id")
            .OnTable("tenant_applications")
            .InSchema("tenantapplication")
            .OnColumn("tenant_id");

        Create.UniqueConstraint("uq_tenant_applications_tenant_id_slug")
            .OnTable("tenant_applications")
            .WithSchema("tenantapplication")
            .Columns("tenant_id", "slug");
    }

    public override void Down()
    {
        Delete.Table("tenant_applications").InSchema("tenantapplication");
    }
}
