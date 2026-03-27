using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115101000, "Create users table")]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey("pk_users")
            .WithColumn("email").AsString(255).NotNullable()
            .WithColumn("default_tenant_id").AsGuid().NotNullable()
            .WithColumn("display_name").AsString(100).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.UniqueConstraint("uq_users_email")
            .OnTable("users")
            .WithSchema("identity")
            .Column("email");
    }

    public override void Down()
    {
        Delete.Table("users").InSchema("identity");
    }
}
