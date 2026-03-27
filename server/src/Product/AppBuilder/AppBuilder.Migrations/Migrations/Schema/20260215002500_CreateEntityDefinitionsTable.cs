using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215002500, "Create entity_definitions table")]
public class CreateEntityDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("entity_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_entity_definitions")
            .WithColumn("application_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("display_name").AsString(200).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("attributes").AsCustom("jsonb").NotNullable()
            .WithColumn("primary_key").AsString(100).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_entity_definitions_application_id")
            .FromTable("entity_definitions").InSchema("appbuilder").ForeignColumn("application_id")
            .ToTable("application_definitions").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_entity_definitions_application_id")
            .OnTable("entity_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id");

        Create.UniqueConstraint("uq_entity_definitions_application_id_name")
            .OnTable("entity_definitions")
            .WithSchema("appbuilder")
            .Columns("application_id", "name");
    }

    public override void Down()
    {
        Delete.Table("entity_definitions").InSchema("appbuilder");
    }
}
