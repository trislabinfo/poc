using FluentMigrator;

namespace Datarizen.Tenant.Migrations.Migrations.Schema;

[Migration(20250116001000, "Create tenants table")]
public class CreateTenantsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenants")
            .InSchema("tenant")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenants")
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.UniqueConstraint("uq_tenants_slug")
            .OnTable("tenants")
            .WithSchema("tenant")
            .Column("slug");
    }

    public override void Down()
    {
        Delete.Table("tenants").InSchema("tenant");
    }
}
