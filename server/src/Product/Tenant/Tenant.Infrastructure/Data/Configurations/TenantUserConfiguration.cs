using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenant.Domain.Entities;

namespace Tenant.Infrastructure.Data.Configurations;

public class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("tenant_user");

        builder.HasKey(tu => tu.Id);

        builder.Property(tu => tu.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(tu => tu.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(tu => tu.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(tu => tu.IsTenantOwner)
            .HasColumnName("is_tenant_owner")
            .IsRequired();

        builder.Property(tu => tu.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(tu => tu.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(tu => new { tu.TenantId, tu.UserId })
            .HasDatabaseName("ix_tenant_user_tenant_id_user_id")
            .IsUnique();

        builder.HasIndex(tu => tu.UserId)
            .HasDatabaseName("ix_tenant_user_user_id");

        builder.Ignore(tu => tu.DomainEvents);
    }
}
