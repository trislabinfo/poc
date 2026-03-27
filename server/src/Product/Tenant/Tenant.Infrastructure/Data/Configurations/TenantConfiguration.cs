using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tenant.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant.Domain.Entities.Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant.Domain.Entities.Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(t => t.Slug)
            .HasDatabaseName("ix_tenants_slug")
            .IsUnique();

        builder.HasMany(t => t.Users)
            .WithOne()
            .HasForeignKey(tu => tu.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Users).HasField("_tenantUsers");

        builder.Ignore(t => t.DomainEvents);
    }
}
