using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115103000, "Create permissions table")]
public class CreatePermissionsTable : Migration
{
    public override void Up()
    {
        Create.Table("permissions")
            .InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey("pk_permissions")
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("resource").AsString(100).NotNullable()
            .WithColumn("action").AsString(50).NotNullable();

        Create.UniqueConstraint("uq_permissions_resource_action")
            .OnTable("permissions")
            .WithSchema("identity")
            .Columns("resource", "action");
    }

    public override void Down()
    {
        Delete.Table("permissions").InSchema("identity");
    }
}
