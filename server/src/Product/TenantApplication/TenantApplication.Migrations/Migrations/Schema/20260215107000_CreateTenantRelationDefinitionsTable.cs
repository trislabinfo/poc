using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215107000, "Create tenant_relation_definitions table")]
public class CreateTenantRelationDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_relation_definitions")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_relation_definitions")
            .WithColumn("source_entity_id").AsGuid().NotNullable()
            .WithColumn("target_entity_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("relation_type").AsString(50).NotNullable()
            .WithColumn("cascade_delete").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.ForeignKey("fk_tenant_relation_definitions_source_entity_id")
            .FromTable("tenant_relation_definitions").InSchema("tenantapplication").ForeignColumn("source_entity_id")
            .ToTable("tenant_entity_definitions").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("fk_tenant_relation_definitions_target_entity_id")
            .FromTable("tenant_relation_definitions").InSchema("tenantapplication").ForeignColumn("target_entity_id")
            .ToTable("tenant_entity_definitions").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.None);

        Create.Index("ix_tenant_relation_definitions_source_entity_id")
            .OnTable("tenant_relation_definitions")
            .InSchema("tenantapplication")
            .OnColumn("source_entity_id");

        Create.Index("ix_tenant_relation_definitions_target_entity_id")
            .OnTable("tenant_relation_definitions")
            .InSchema("tenantapplication")
            .OnColumn("target_entity_id");
    }

    public override void Down()
    {
        Delete.Table("tenant_relation_definitions").InSchema("tenantapplication");
    }
}
