using FluentMigrator;

namespace Datarizen.Tenant.Migrations.Migrations.Schema;

[Migration(20250116000000, "Create tenant schema")]
public class CreateTenantSchema : Migration
{
    public override void Up()
    {
        Create.Schema("tenant");
    }

    public override void Down()
    {
        Delete.Schema("tenant");
    }
}
