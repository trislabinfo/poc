using FluentMigrator;

namespace Datarizen.TenantApplication.Migrations.Migrations.Schema;

[Migration(20260215104000, "Create tenant_application_releases table")]
public class CreateTenantApplicationReleasesTable : Migration
{
    public override void Up()
    {
        Create.Table("tenant_application_releases")
            .InSchema("tenantapplication")
            .WithColumn("id").AsGuid().PrimaryKey("pk_tenant_application_releases")
            .WithColumn("tenant_application_id").AsGuid().NotNullable()
            .WithColumn("version").AsString(50).NotNullable()
            .WithColumn("major").AsInt32().NotNullable()
            .WithColumn("minor").AsInt32().NotNullable()
            .WithColumn("patch").AsInt32().NotNullable()
            .WithColumn("release_notes").AsString(5000).Nullable()
            .WithColumn("navigation_json").AsCustom("jsonb").NotNullable()
            .WithColumn("page_json").AsCustom("jsonb").NotNullable()
            .WithColumn("datasource_json").AsCustom("jsonb").NotNullable()
            .WithColumn("entity_json").AsCustom("jsonb").NotNullable()
            .WithColumn("released_at").AsDateTime().NotNullable()
            .WithColumn("released_by").AsGuid().NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("schema_json").AsCustom("jsonb").NotNullable().WithDefaultValue("{}")
            .WithColumn("ddl_scripts_json").AsCustom("jsonb").NotNullable().WithDefaultValue("{}")
            .WithColumn("ddl_scripts_status").AsString(50).NotNullable().WithDefaultValue("Pending")
            .WithColumn("approved_at").AsDateTime().Nullable()
            .WithColumn("approved_by").AsGuid().Nullable()
            .WithColumn("initial_view_html").AsCustom("text").Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.ForeignKey("fk_tenant_application_releases_tenant_application_id")
            .FromTable("tenant_application_releases").InSchema("tenantapplication").ForeignColumn("tenant_application_id")
            .ToTable("tenant_applications").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_tenant_application_releases_tenant_application_id")
            .OnTable("tenant_application_releases")
            .InSchema("tenantapplication")
            .OnColumn("tenant_application_id");

        Create.Table("release_entity_views")
            .InSchema("tenantapplication")
            .WithColumn("release_id").AsGuid().NotNullable()
            .WithColumn("entity_id").AsGuid().NotNullable()
            .WithColumn("view_type").AsString(20).NotNullable()
            .WithColumn("html").AsCustom("text").NotNullable();

        Create.PrimaryKey("pk_release_entity_views")
            .OnTable("release_entity_views")
            .WithSchema("tenantapplication")
            .Columns("release_id", "entity_id", "view_type");

        Create.ForeignKey("fk_release_entity_views_release_id")
            .FromTable("release_entity_views").InSchema("tenantapplication").ForeignColumn("release_id")
            .ToTable("tenant_application_releases").InSchema("tenantapplication").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_release_entity_views_release_id")
            .OnTable("release_entity_views")
            .InSchema("tenantapplication")
            .OnColumn("release_id");

        Create.Index("ix_release_entity_views_release_id_entity_id_view_type")
            .OnTable("release_entity_views")
            .InSchema("tenantapplication")
            .OnColumn("release_id").Ascending()
            .OnColumn("entity_id").Ascending()
            .OnColumn("view_type").Ascending();
    }

    public override void Down()
    {
        Delete.Table("release_entity_views").InSchema("tenantapplication");
        Delete.Table("tenant_application_releases").InSchema("tenantapplication");
    }
}
