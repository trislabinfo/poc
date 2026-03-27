using AppDefinition.Domain.Entities.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantApplicationEntity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Infrastructure.Data.Configurations;

/// <summary>Maps shared ApplicationRelease to tenantapplication.tenant_application_releases.</summary>
public sealed class TenantApplicationReleaseConfiguration : IEntityTypeConfiguration<ApplicationRelease>
{
    public void Configure(EntityTypeBuilder<ApplicationRelease> builder)
    {
        builder.ToTable("tenant_application_releases");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").IsRequired();
        builder.Property(e => e.AppDefinitionId).HasColumnName("tenant_application_id").IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Major).HasColumnName("major").IsRequired();
        builder.Property(e => e.Minor).HasColumnName("minor").IsRequired();
        builder.Property(e => e.Patch).HasColumnName("patch").IsRequired();
        builder.Property(e => e.ReleaseNotes).HasColumnName("release_notes").HasMaxLength(5000);
        builder.Property(e => e.NavigationJson).HasColumnName("navigation_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.PageJson).HasColumnName("page_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.DataSourceJson).HasColumnName("datasource_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.EntityJson).HasColumnName("entity_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.SchemaJson).HasColumnName("schema_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.DdlScriptsJson).HasColumnName("ddl_scripts_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.DdlScriptsStatus).HasColumnName("ddl_scripts_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.ApprovedAt).HasColumnName("approved_at");
        builder.Property(e => e.ApprovedBy).HasColumnName("approved_by");
        builder.Property(e => e.ReleasedAt).HasColumnName("released_at").IsRequired();
        builder.Property(e => e.ReleasedBy).HasColumnName("released_by").IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.InitialViewHtml).HasColumnName("initial_view_html").HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        // ApplicationRelease is immutable; table has no updated_at column
        builder.Ignore(e => e.UpdatedAt);

        builder.HasOne<TenantApplicationEntity>()
            .WithMany()
            .HasForeignKey(e => e.AppDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.DomainEvents);
    }
}
