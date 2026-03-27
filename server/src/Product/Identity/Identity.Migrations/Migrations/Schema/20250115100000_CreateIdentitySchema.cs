using FluentMigrator;

namespace Datarizen.Identity.Migrations.Migrations.Schema;

[Migration(20250115100000, "Create identity schema")]
public class CreateIdentitySchema : Migration
{
    public override void Up()
    {
        Create.Schema("identity");
    }

    public override void Down()
    {
        Delete.Schema("identity");
    }
}
