using FluentMigrator;

namespace Datarizen.Tenant.Migrations.Migrations.Schema;

[Migration(20250116002000, "Create tenant_user table")]
public class CreateTenantUserTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_user")
            .InSchema("tenant")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_user")
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("is_tenant_owner").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_tenant_user_tenant_id")
            .FromTable("tenant_user").InSchema("tenant").ForeignColumn("tenant_id")
            .ToTable("tenants").InSchema("tenant").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.UniqueConstraint("uq_tenant_user_tenant_id_user_id")
            .OnTable("tenant_user")
            .WithSchema("tenant")
            .Columns("tenant_id", "user_id");

        Create.Index("ix_tenant_user_tenant_id_user_id")
            .OnTable("tenant_user")
            .InSchema("tenant")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("user_id").Ascending();

        Create.Index("ix_tenant_user_user_id")
            .OnTable("tenant_user")
            .InSchema("tenant")
            .OnColumn("user_id");
    }

    public override void Down()
    {
        Delete.Table("tenant_user").InSchema("tenant");
    }
}
