using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215002800, "Create navigation_definitions table")]
public class CreateNavigationDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("navigation_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_navigation_definitions")
            .WithColumn("application_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("configuration_json").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_navigation_definitions_application_id")
            .FromTable("navigation_definitions").InSchema("appbuilder").ForeignColumn("application_id")
            .ToTable("application_definitions").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_navigation_definitions_application_id")
            .OnTable("navigation_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id");
    }

    public override void Down()
    {
        Delete.Table("navigation_definitions").InSchema("appbuilder");
    }
}
