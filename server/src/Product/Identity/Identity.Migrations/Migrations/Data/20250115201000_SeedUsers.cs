using Datarizen.Identity.Migrations.Helpers;
using FluentMigrator;
using System.Reflection;

namespace Datarizen.Identity.Migrations.Migrations.Data;

[Migration(20250115201000, "Seed users from environment-specific JSON")]
public class SeedUsers : Migration
{
    private const string SeedDataResourcePrefix = "Datarizen.Identity.Migrations.SeedData";

    public override void Up()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var users = BuildingBlocks.Migrations.SeedDataLoader.Load<UserSeedDto>(assembly, SeedDataResourcePrefix, "users.json");

        foreach (var u in users)
        {
            Insert.IntoTable("users").InSchema("identity")
                .Row(new
                {
                    id = u.Id,
                    email = u.Email,
                    default_tenant_id = u.DefaultTenantId,
                    display_name = u.DisplayName,
                    is_active = u.IsActive,
                    created_at = u.CreatedAt,
                    updated_at = u.UpdatedAt
                });

            var credentialId = Guid.NewGuid();
            Insert.IntoTable("credentials").InSchema("identity")
                .Row(new
                {
                    id = credentialId,
                    user_id = u.Id,
                    type = "Password",
                    password_hash = u.PasswordHash,
                    is_active = true,
                    created_at = u.CreatedAt,
                    updated_at = (DateTime?)null
                });

            foreach (var roleId in u.RoleIds)
            {
                Insert.IntoTable("user_roles").InSchema("identity")
                    .Row(new { user_id = u.Id, role_id = roleId });
            }
        }
    }

    public override void Down()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var users = BuildingBlocks.Migrations.SeedDataLoader.Load<UserSeedDto>(assembly, SeedDataResourcePrefix, "users.json");

        foreach (var u in users)
        {
            foreach (var roleId in u.RoleIds)
            {
                Delete.FromTable("user_roles").InSchema("identity").Row(new { user_id = u.Id, role_id = roleId });
            }
            Delete.FromTable("credentials").InSchema("identity").Row(new { user_id = u.Id });
            Delete.FromTable("users").InSchema("identity").Row(new { id = u.Id });
        }
    }
}
