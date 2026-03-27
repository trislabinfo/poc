using AppDefinition.Domain.Entities.Lifecycle;
using BuildingBlocks.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppBuilder.Infrastructure.Data.Configurations;

public class ApplicationReleaseConfiguration : IEntityTypeConfiguration<ApplicationRelease>
{
    public void Configure(EntityTypeBuilder<ApplicationRelease> builder)
    {
        builder.ToTable("application_releases");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").IsRequired();
        builder.Property(e => e.AppDefinitionId).HasColumnName("application_id").IsRequired();
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
        builder.Property(e => e.ApprovedAt)
            .HasColumnName("approved_at")
            .HasColumnType("timestamp with time zone")
            .HasConversion<UtcNullableDateTimeConverter>();
        builder.Property(e => e.ApprovedBy).HasColumnName("approved_by");
        builder.Property(e => e.ReleasedAt)
            .HasColumnName("released_at")
            .HasColumnType("timestamp with time zone")
            .HasConversion<UtcDateTimeConverter>()
            .IsRequired();
        builder.Property(e => e.ReleasedBy).HasColumnName("released_by").IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.InitialViewHtml).HasColumnName("initial_view_html").HasColumnType("text");
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasConversion<UtcDateTimeConverter>()
            .IsRequired();

        // ApplicationRelease is immutable; table has no updated_at column
        builder.Ignore(e => e.UpdatedAt);

        builder.HasOne<AppDefinition.Domain.Entities.Application.AppDefinition>()
            .WithMany()
            .HasForeignKey(e => e.AppDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
