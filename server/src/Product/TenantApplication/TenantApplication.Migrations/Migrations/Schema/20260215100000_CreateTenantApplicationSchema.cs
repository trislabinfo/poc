using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215100000, "Create tenantapplication schema")]
public class CreateTenantApplicationSchema : Migration
{
    public override void Up()
    {
        Create.Schema("tenantapplication");
    }

    public override void Down()
    {
        Delete.Schema("tenantapplication");
    }
}
