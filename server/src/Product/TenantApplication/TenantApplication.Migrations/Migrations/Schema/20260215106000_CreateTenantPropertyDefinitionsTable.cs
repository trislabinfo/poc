using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215106000, "Create tenant_property_definitions table")]
public class CreateTenantPropertyDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_property_definitions")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_property_definitions")
            .WithColumn("entity_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("display_name").AsString(200).NotNullable()
            .WithColumn("data_type").AsString(50).NotNullable()
            .WithColumn("is_required").AsBoolean().NotNullable()
            .WithColumn("default_value").AsString(500).Nullable()
            .WithColumn("validation_rules").AsCustom("jsonb").NotNullable()
            .WithColumn("order").AsInt32().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_tenant_property_definitions_entity_id")
            .FromTable("tenant_property_definitions").InSchema("tenantapplication").ForeignColumn("entity_id")
            .ToTable("tenant_entity_definitions").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_tenant_property_definitions_entity_id")
            .OnTable("tenant_property_definitions")
            .InSchema("tenantapplication")
            .OnColumn("entity_id");

        Create.UniqueConstraint("uq_tenant_property_definitions_entity_id_name")
            .OnTable("tenant_property_definitions")
            .WithSchema("tenantapplication")
            .Columns("entity_id", "name");
    }

    public override void Down()
    {
        Delete.Table("tenant_property_definitions").InSchema("tenantapplication");
    }
}
