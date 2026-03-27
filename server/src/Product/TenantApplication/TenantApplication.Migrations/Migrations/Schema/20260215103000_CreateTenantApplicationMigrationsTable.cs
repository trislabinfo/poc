using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215103000, "Create tenant_application_migrations table")]
public class CreateTenantApplicationMigrationsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_application_migrations")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_application_migrations")
            .WithColumn("tenant_application_environment_id").AsGuid().NotNullable()
            .WithColumn("from_release_id").AsGuid().Nullable()
            .WithColumn("to_release_id").AsGuid().NotNullable()
            .WithColumn("migration_script_json").AsCustom("text").NotNullable()
            .WithColumn("status").AsString(50).NotNullable()
            .WithColumn("executed_at").AsDateTime().Nullable()
            .WithColumn("error_message").AsString(2000).Nullable()
            .WithColumn("approved_at").AsDateTime().Nullable()
            .WithColumn("approved_by").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.ForeignKey("fk_tenant_application_migrations_environment_id")
            .FromTable("tenant_application_migrations").InSchema("tenantapplication").ForeignColumn("tenant_application_environment_id")
            .ToTable("tenant_application_environments").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_tenant_application_migrations_environment_id")
            .OnTable("tenant_application_migrations")
            .InSchema("tenantapplication")
            .OnColumn("tenant_application_environment_id");
    }

    public override void Down()
    {
        Delete.Table("tenant_application_migrations").InSchema("tenantapplication");
    }
}
