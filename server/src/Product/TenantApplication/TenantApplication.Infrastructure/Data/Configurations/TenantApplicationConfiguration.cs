using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantApplicationEntity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Infrastructure.Data.Configurations;

public sealed class TenantApplicationConfiguration : IEntityTypeConfiguration<TenantApplicationEntity>
{
    public void Configure(EntityTypeBuilder<TenantApplicationEntity> builder)
    {
        builder.ToTable("tenant_applications");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.ApplicationReleaseId).HasColumnName("application_release_id");
        builder.Property(e => e.ApplicationId).HasColumnName("application_id");
        builder.Property(e => e.Major).HasColumnName("major");
        builder.Property(e => e.Minor).HasColumnName("minor");
        builder.Property(e => e.Patch).HasColumnName("patch");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(e => e.IsCustom).HasColumnName("is_custom").IsRequired();
        builder.Property(e => e.SourceApplicationReleaseId).HasColumnName("source_application_release_id");
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.ConfigurationJson).HasColumnName("configuration").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.InstalledAt).HasColumnName("installed_at");
        builder.Property(e => e.ActivatedAt).HasColumnName("activated_at");
        builder.Property(e => e.DeactivatedAt).HasColumnName("deactivated_at");
        builder.Property(e => e.UninstalledAt).HasColumnName("uninstalled_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(e => e.Environments)
            .WithOne()
            .HasForeignKey(e => e.TenantApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Environments).HasField("_environments");

        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_tenant_applications_tenant_id");
        builder.HasIndex(e => new { e.TenantId, e.Slug }).HasDatabaseName("ix_tenant_applications_tenant_id_slug").IsUnique();
    }
}
