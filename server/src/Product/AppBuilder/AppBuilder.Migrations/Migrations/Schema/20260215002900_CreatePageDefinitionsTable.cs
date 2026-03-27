using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215002900, "Create page_definitions table")]
public class CreatePageDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("page_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_page_definitions")
            .WithColumn("application_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("route").AsString(500).NotNullable()
            .WithColumn("configuration_json").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_page_definitions_application_id")
            .FromTable("page_definitions").InSchema("appbuilder").ForeignColumn("application_id")
            .ToTable("application_definitions").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_page_definitions_application_id")
            .OnTable("page_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id");

        Create.UniqueConstraint("uq_page_definitions_application_id_route")
            .OnTable("page_definitions")
            .WithSchema("appbuilder")
            .Columns("application_id", "route");
    }

    public override void Down()
    {
        Delete.Table("page_definitions").InSchema("appbuilder");
    }
}
