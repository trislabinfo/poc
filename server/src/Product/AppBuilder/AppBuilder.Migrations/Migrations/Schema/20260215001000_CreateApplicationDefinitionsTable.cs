using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215001000, "Create application_definitions table with updated_at")]
public class CreateAppDefinitionsTable : Migration
{
    public override void Up()
    {
        Create.Table("application_definitions")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_application_definitions")
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(1000).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("current_version").AsString(50).Nullable()
            .WithColumn("is_public").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.UniqueConstraint("uq_application_definitions_slug")
            .OnTable("application_definitions")
            .WithSchema("appbuilder")
            .Column("slug");

        Create.Index("ix_application_definitions_slug")
            .OnTable("application_definitions")
            .InSchema("appbuilder")
            .OnColumn("slug");
    }

    public override void Down()
    {
        Delete.Table("application_definitions").InSchema("appbuilder");
    }
}
