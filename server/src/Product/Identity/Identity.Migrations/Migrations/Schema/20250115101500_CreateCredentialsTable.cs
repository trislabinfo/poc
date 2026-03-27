using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115101500, "Create credentials table")]
public class CreateCredentialsTable : Migration
{
    public override void Up()
    {
        Create.Table("credentials")
            .InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey("pk_credentials")
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("type").AsString(50).NotNullable()
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_credentials_users")
            .FromTable("credentials").InSchema("identity").ForeignColumn("user_id")
            .ToTable("users").InSchema("identity").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        Delete.ForeignKey("fk_credentials_users").OnTable("credentials").InSchema("identity");
        Delete.Table("credentials").InSchema("identity");
    }
}
