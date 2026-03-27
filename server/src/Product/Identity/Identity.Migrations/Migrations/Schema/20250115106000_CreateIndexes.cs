using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115106000, "Create indexes for performance")]
public class CreateIndexes : Migration
{
    public override void Up()
    {
        // Users table
        Create.Index("idx_users_email")
            .OnTable("users").InSchema("identity")
            .OnColumn("email").Ascending();

        Create.Index("idx_users_default_tenant_id")
            .OnTable("users").InSchema("identity")
            .OnColumn("default_tenant_id").Ascending();

        Create.Index("idx_users_is_active")
            .OnTable("users").InSchema("identity")
            .OnColumn("is_active").Ascending();

        // Credentials table
        Create.Index("idx_credentials_user_id")
            .OnTable("credentials").InSchema("identity")
            .OnColumn("user_id").Ascending();

        Create.Index("idx_credentials_user_id_type")
            .OnTable("credentials").InSchema("identity")
            .OnColumn("user_id").Ascending()
            .OnColumn("type").Ascending();

        // User roles
        Create.Index("idx_user_roles_user_id")
            .OnTable("user_roles").InSchema("identity")
            .OnColumn("user_id").Ascending();

        Create.Index("idx_user_roles_role_id")
            .OnTable("user_roles").InSchema("identity")
            .OnColumn("role_id").Ascending();

        // Role permissions
        Create.Index("idx_role_permissions_role_id")
            .OnTable("role_permissions").InSchema("identity")
            .OnColumn("role_id").Ascending();

        Create.Index("idx_role_permissions_permission_id")
            .OnTable("role_permissions").InSchema("identity")
            .OnColumn("permission_id").Ascending();

        // Permissions
        Create.Index("idx_permissions_resource")
            .OnTable("permissions").InSchema("identity")
            .OnColumn("resource").Ascending();
    }

    public override void Down()
    {
        Delete.Index("idx_credentials_user_id_type").OnTable("credentials").InSchema("identity");
        Delete.Index("idx_credentials_user_id").OnTable("credentials").InSchema("identity");
        Delete.Index("idx_permissions_resource").OnTable("permissions").InSchema("identity");
        Delete.Index("idx_role_permissions_permission_id").OnTable("role_permissions").InSchema("identity");
        Delete.Index("idx_role_permissions_role_id").OnTable("role_permissions").InSchema("identity");
        Delete.Index("idx_user_roles_role_id").OnTable("user_roles").InSchema("identity");
        Delete.Index("idx_user_roles_user_id").OnTable("user_roles").InSchema("identity");
        Delete.Index("idx_users_is_active").OnTable("users").InSchema("identity");
        Delete.Index("idx_users_default_tenant_id").OnTable("users").InSchema("identity");
        Delete.Index("idx_users_email").OnTable("users").InSchema("identity");
    }
}
