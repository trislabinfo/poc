using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantApplication.Domain.Entities;

namespace TenantApplication.Infrastructure.Data.Configurations;

public sealed class TenantApplicationMigrationConfiguration : IEntityTypeConfiguration<TenantApplicationMigration>
{
    public void Configure(EntityTypeBuilder<TenantApplicationMigration> builder)
    {
        builder.ToTable("tenant_application_migrations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.TenantApplicationEnvironmentId).HasColumnName("tenant_application_environment_id").IsRequired();
        builder.Property(e => e.FromReleaseId).HasColumnName("from_release_id");
        builder.Property(e => e.ToReleaseId).HasColumnName("to_release_id").IsRequired();
        builder.Property(e => e.MigrationScriptJson).HasColumnName("migration_script_json").HasColumnType("text").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.ExecutedAt).HasColumnName("executed_at");
        builder.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(e => e.ApprovedAt).HasColumnName("approved_at");
        builder.Property(e => e.ApprovedBy).HasColumnName("approved_by");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(e => e.TenantApplicationEnvironmentId).HasDatabaseName("ix_tenant_application_migrations_environment_id");
    }
}
