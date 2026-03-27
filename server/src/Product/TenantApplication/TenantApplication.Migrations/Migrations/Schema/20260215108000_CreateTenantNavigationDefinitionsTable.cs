using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215108000, "Create tenant_navigation_definitions table")]
public class CreateTenantNavigationDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_navigation_definitions")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_navigation_definitions")
            .WithColumn("tenant_application_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("configuration_json").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_tenant_navigation_definitions_tenant_application_id")
            .FromTable("tenant_navigation_definitions").InSchema("tenantapplication").ForeignColumn("tenant_application_id")
            .ToTable("tenant_applications").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_tenant_navigation_definitions_tenant_application_id")
            .OnTable("tenant_navigation_definitions")
            .InSchema("tenantapplication")
            .OnColumn("tenant_application_id");
    }

    public override void Down()
    {
        Delete.Table("tenant_navigation_definitions").InSchema("tenantapplication");
    }
}
