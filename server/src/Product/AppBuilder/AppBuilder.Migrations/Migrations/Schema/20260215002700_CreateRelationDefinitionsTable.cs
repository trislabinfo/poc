using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215002700, "Create relation_definitions table")]
public class CreateRelationDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("relation_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_relation_definitions")
            .WithColumn("source_entity_id").AsGuid().NotNullable()
            .WithColumn("target_entity_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("relation_type").AsString(50).NotNullable()
            .WithColumn("cascade_delete").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.ForeignKey("fk_relation_definitions_source_entity_id")
            .FromTable("relation_definitions").InSchema("appbuilder").ForeignColumn("source_entity_id")
            .ToTable("entity_definitions").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("fk_relation_definitions_target_entity_id")
            .FromTable("relation_definitions").InSchema("appbuilder").ForeignColumn("target_entity_id")
            .ToTable("entity_definitions").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.None);

        Create.Index("ix_relation_definitions_source_entity_id")
            .OnTable("relation_definitions")
            .InSchema("appbuilder")
            .OnColumn("source_entity_id");

        Create.Index("ix_relation_definitions_target_entity_id")
            .OnTable("relation_definitions")
            .InSchema("appbuilder")
            .OnColumn("target_entity_id");

        Create.UniqueConstraint("uq_relation_definitions_source_entity_id_name")
            .OnTable("relation_definitions")
            .WithSchema("appbuilder")
            .Columns("source_entity_id", "name");
    }

    public override void Down()
    {
        Delete.Table("relation_definitions").InSchema("appbuilder");
    }
}
