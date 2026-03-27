using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215002600, "Create property_definitions table")]
public class CreatePropertyDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("property_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_property_definitions")
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

        Create.ForeignKey("fk_property_definitions_entity_id")
            .FromTable("property_definitions").InSchema("appbuilder").ForeignColumn("entity_id")
            .ToTable("entity_definitions").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_property_definitions_entity_id")
            .OnTable("property_definitions")
            .InSchema("appbuilder")
            .OnColumn("entity_id");

        Create.UniqueConstraint("uq_property_definitions_entity_id_name")
            .OnTable("property_definitions")
            .WithSchema("appbuilder")
            .Columns("entity_id", "name");

        Create.Index("ix_property_definitions_entity_id_order")
            .OnTable("property_definitions")
            .InSchema("appbuilder")
            .OnColumn("entity_id").Ascending()
            .OnColumn("order").Ascending();
    }

    public override void Down()
    {
        Delete.Table("property_definitions").InSchema("appbuilder");
    }
}
