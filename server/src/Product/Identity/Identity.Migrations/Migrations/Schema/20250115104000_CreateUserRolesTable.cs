using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115104000, "Create user_roles junction table")]
public class CreateUserRolesTable : Migration
{
    public override void Up()
    {
        Create.Table("user_roles")
            .InSchema("identity")
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("role_id").AsGuid().NotNullable();

        Create.PrimaryKey("pk_user_roles")
            .OnTable("user_roles")
            .WithSchema("identity")
            .Columns("user_id", "role_id");
    }

    public override void Down()
    {
        Delete.Table("user_roles").InSchema("identity");
    }
}
