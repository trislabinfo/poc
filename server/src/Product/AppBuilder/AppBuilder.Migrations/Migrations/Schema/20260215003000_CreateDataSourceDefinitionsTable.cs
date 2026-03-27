using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215003000, "Create datasource_definitions table")]
public class CreateDataSourceDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("datasource_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_datasource_definitions")
            .WithColumn("application_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("type").AsString(50).NotNullable()
            .WithColumn("configuration_json").AsCustom("jsonb").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_datasource_definitions_application_id")
            .FromTable("datasource_definitions").InSchema("appbuilder").ForeignColumn("application_id")
            .ToTable("application_definitions").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_datasource_definitions_application_id")
            .OnTable("datasource_definitions")
            .InSchema("appbuilder")
            .OnColumn("application_id");
    }

    public override void Down()
    {
        Delete.Table("datasource_definitions").InSchema("appbuilder");
    }
}
