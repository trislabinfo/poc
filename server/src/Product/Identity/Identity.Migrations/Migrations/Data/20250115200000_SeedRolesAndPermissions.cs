using Datarizen.Identity.Migrations.Helpers;
using FluentMigrator;
using System.Reflection;

namespace Datarizen.Identity.Migrations.Migrations.Data;

[Migration(20250115200000, "Seed roles and permissions from JSON")]
public class SeedRolesAndPermissions : Migration
{
    private const string SeedDataResourcePrefix = "Datarizen.Identity.Migrations.SeedData";

    public override void Up()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var permissions = BuildingBlocks.Migrations.SeedDataLoader.Load<PermissionSeedDto>(assembly, SeedDataResourcePrefix, "permissions.json");
        var roles = BuildingBlocks.Migrations.SeedDataLoader.Load<RoleSeedDto>(assembly, SeedDataResourcePrefix, "roles.json");

        foreach (var p in permissions)
        {
            Insert.IntoTable("permissions").InSchema("identity")
                .Row(new { id = p.Id, name = p.Name, resource = p.Resource, action = p.Action });
        }

        foreach (var r in roles)
        {
            Insert.IntoTable("roles").InSchema("identity")
                .Row(new { id = r.Id, name = r.Name, description = r.Description });
        }

        foreach (var r in roles)
        {
            foreach (var permissionId in r.PermissionIds)
            {
                Insert.IntoTable("role_permissions").InSchema("identity")
                    .Row(new { role_id = r.Id, permission_id = permissionId });
            }
        }
    }

    public override void Down()
    {
        Delete.FromTable("role_permissions").InSchema("identity").AllRows();
        Delete.FromTable("roles").InSchema("identity").AllRows();
        Delete.FromTable("permissions").InSchema("identity").AllRows();
    }
}
