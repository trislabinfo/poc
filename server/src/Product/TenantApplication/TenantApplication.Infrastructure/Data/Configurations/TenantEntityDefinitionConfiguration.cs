using AppDefinition.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantApplicationEntity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Infrastructure.Data.Configurations;

public sealed class TenantEntityDefinitionConfiguration : IEntityTypeConfiguration<EntityDefinition>
{
    public void Configure(EntityTypeBuilder<EntityDefinition> builder)
    {
        builder.ToTable("tenant_entity_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").IsRequired();
        builder.Property(e => e.AppDefinitionId).HasColumnName("tenant_application_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(e => e.AttributesJson).HasColumnName("attributes").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.PrimaryKey).HasColumnName("primary_key").HasMaxLength(100);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<TenantApplicationEntity>()
            .WithMany()
            .HasForeignKey(e => e.AppDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
