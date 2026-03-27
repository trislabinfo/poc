using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215000000, "Create appbuilder schema")]
public class CreateAppBuilderSchema : Migration
{
    public override void Up()
    {
        Create.Schema("appbuilder");
    }

    public override void Down()
    {
        Delete.Schema("appbuilder");
    }
}
