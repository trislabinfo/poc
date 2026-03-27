using FluentMigrator;

namespace Datarizen.AppBuilder.Migrations.Migrations.Schema;

[Migration(20260215002000, "Create application_releases table (includes schema_json, ddl_scripts_json, ddl_scripts_status, approved_at, approved_by)")]
public class CreateApplicationReleasesTable : Migration
{
    public override void Up()
    {
        Create.Table("application_releases")
            .InSchema("appbuilder")
            .WithColumn("id").AsGuid().PrimaryKey("pk_application_releases")
            .WithColumn("application_id").AsGuid().NotNullable()
            .WithColumn("version").AsString(50).NotNullable()
            .WithColumn("major").AsInt32().NotNullable()
            .WithColumn("minor").AsInt32().NotNullable()
            .WithColumn("patch").AsInt32().NotNullable()
            .WithColumn("release_notes").AsString(5000).Nullable()
            .WithColumn("released_at").AsDateTime().NotNullable()
            .WithColumn("released_by").AsGuid().NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("navigation_json").AsCustom("jsonb").NotNullable()
            .WithColumn("page_json").AsCustom("jsonb").NotNullable()
            .WithColumn("datasource_json").AsCustom("jsonb").NotNullable()
            .WithColumn("entity_json").AsCustom("jsonb").NotNullable()
            .WithColumn("schema_json").AsCustom("jsonb").NotNullable().WithDefaultValue("{}")
            .WithColumn("ddl_scripts_json").AsCustom("jsonb").NotNullable().WithDefaultValue("{}")
            .WithColumn("ddl_scripts_status").AsString(50).NotNullable().WithDefaultValue("Pending")
            .WithColumn("approved_at").AsDateTime().Nullable()
            .WithColumn("approved_by").AsGuid().Nullable()
            .WithColumn("initial_view_html").AsCustom("text").Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.ForeignKey("fk_application_releases_application_id")
            .FromTable("application_releases").InSchema("appbuilder").ForeignColumn("application_id")
            .ToTable("application_definitions").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_application_releases_application_id")
            .OnTable("application_releases")
            .InSchema("appbuilder")
            .OnColumn("application_id");

        Create.UniqueConstraint("uq_application_releases_application_id_version")
            .OnTable("application_releases")
            .WithSchema("appbuilder")
            .Columns("application_id", "major", "minor", "patch");

        Create.Index("ix_application_releases_application_id_is_active")
            .OnTable("application_releases")
            .InSchema("appbuilder")
            .OnColumn("application_id").Ascending()
            .OnColumn("is_active").Ascending();

        Create.Table("release_entity_views")
            .InSchema("appbuilder")
            .WithColumn("release_id").AsGuid().NotNullable()
            .WithColumn("entity_id").AsGuid().NotNullable()
            .WithColumn("view_type").AsString(20).NotNullable()
            .WithColumn("html").AsCustom("text").NotNullable();

        Create.PrimaryKey("pk_release_entity_views")
            .OnTable("release_entity_views")
            .WithSchema("appbuilder")
            .Columns("release_id", "entity_id", "view_type");

        Create.ForeignKey("fk_release_entity_views_release_id")
            .FromTable("release_entity_views").InSchema("appbuilder").ForeignColumn("release_id")
            .ToTable("application_releases").InSchema("appbuilder").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("ix_release_entity_views_release_id")
            .OnTable("release_entity_views")
            .InSchema("appbuilder")
            .OnColumn("release_id");

        Create.Index("ix_release_entity_views_release_id_entity_id_view_type")
            .OnTable("release_entity_views")
            .InSchema("appbuilder")
            .OnColumn("release_id").Ascending()
            .OnColumn("entity_id").Ascending()
            .OnColumn("view_type").Ascending();
    }

    public override void Down()
    {
        Delete.Table("release_entity_views").InSchema("appbuilder");
        Delete.Table("application_releases").InSchema("appbuilder");
    }
}
