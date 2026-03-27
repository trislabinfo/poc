using FluentMigrator;

namespace AppRuntime.Migrations.Migrations.Schema;

[Migration(20260220000000, "Create appruntime schema")]
public class CreateAppRuntimeSchema : Migration
{
    public override void Up()
    {
        Create.Schema("appruntime");
    }

    public override void Down()
    {
        Delete.Schema("appruntime");
    }
}
