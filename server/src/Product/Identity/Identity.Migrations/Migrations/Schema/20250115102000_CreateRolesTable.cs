using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115102000, "Create roles table")]
public class CreateRolesTable : Migration
{
    public override void Up()
    {
        Create.Table("roles")
            .InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey("pk_roles")
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("description").AsString(500).NotNullable();

        Create.UniqueConstraint("uq_roles_name")
            .OnTable("roles")
            .WithSchema("identity")
            .Column("name");
    }

    public override void Down()
    {
        Delete.Table("roles").InSchema("identity");
    }
}
