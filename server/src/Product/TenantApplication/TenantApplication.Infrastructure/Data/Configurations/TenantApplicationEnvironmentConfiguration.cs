using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantApplication.Domain.Entities;

namespace TenantApplication.Infrastructure.Data.Configurations;

public sealed class TenantApplicationEnvironmentConfiguration : IEntityTypeConfiguration<TenantApplicationEnvironment>
{
    public void Configure(EntityTypeBuilder<TenantApplicationEnvironment> builder)
    {
        builder.ToTable("tenant_application_environments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.TenantApplicationId).HasColumnName("tenant_application_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.EnvironmentType).HasColumnName("environment_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.ApplicationReleaseId).HasColumnName("application_release_id");
        builder.Property(e => e.ReleaseVersion).HasColumnName("release_version").HasMaxLength(50);
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.DeployedAt).HasColumnName("deployed_at");
        builder.Property(e => e.DeployedBy).HasColumnName("deployed_by");
        builder.Property(e => e.ConfigurationJson).HasColumnName("configuration_json").HasColumnType("jsonb");
        builder.Property(e => e.DatabaseName).HasColumnName("database_name").HasMaxLength(200);
        builder.Property(e => e.ConnectionString).HasColumnName("connection_string").HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(e => e.TenantApplicationId).HasDatabaseName("ix_tenant_application_environments_tenant_application_id");
    }
}
