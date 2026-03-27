using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115105000, "Create role_permissions junction table")]
public class CreateRolePermissionsTable : Migration
{
    public override void Up()
    {
        Create.Table("role_permissions")
            .InSchema("identity")
            .WithColumn("role_id").AsGuid().NotNullable()
            .WithColumn("permission_id").AsGuid().NotNullable();

        Create.PrimaryKey("pk_role_permissions")
            .OnTable("role_permissions")
            .WithSchema("identity")
            .Columns("role_id", "permission_id");
    }

    public override void Down()
    {
        Delete.Table("role_permissions").InSchema("identity");
    }
}
