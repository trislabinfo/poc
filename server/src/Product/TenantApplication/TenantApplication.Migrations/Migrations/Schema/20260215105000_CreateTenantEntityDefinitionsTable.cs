using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215105000, "Create tenant_entity_definitions table")]
public class CreateTenantEntityDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_entity_definitions")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_entity_definitions")
            .WithColumn("tenant_application_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("display_name").AsString(200).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("attributes").AsCustom("jsonb").NotNullable()
            .WithColumn("primary_key").AsString(100).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_tenant_entity_definitions_tenant_application_id")
            .FromTable("tenant_entity_definitions").InSchema("tenantapplication").ForeignColumn("tenant_application_id")
            .ToTable("tenant_applications").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_tenant_entity_definitions_tenant_application_id")
            .OnTable("tenant_entity_definitions")
            .InSchema("tenantapplication")
            .OnColumn("tenant_application_id");

        Create.UniqueConstraint("uq_tenant_entity_definitions_tenant_application_id_name")
            .OnTable("tenant_entity_definitions")
            .WithSchema("tenantapplication")
            .Columns("tenant_application_id", "name");
    }

    public override void Down()
    {
        Delete.Table("tenant_entity_definitions").InSchema("tenantapplication");
    }
}
