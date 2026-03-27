using AppDefinition.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TenantApplication.Infrastructure.Data.Configurations;

public sealed class TenantPropertyDefinitionConfiguration : IEntityTypeConfiguration<PropertyDefinition>
{
    public void Configure(EntityTypeBuilder<PropertyDefinition> builder)
    {
        builder.ToTable("tenant_property_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").IsRequired();
        builder.Property(e => e.EntityDefinitionId).HasColumnName("entity_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.DataType).HasColumnName("data_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsRequired).HasColumnName("is_required").IsRequired();
        builder.Property(e => e.DefaultValue).HasColumnName("default_value").HasMaxLength(500);
        builder.Property(e => e.ValidationRulesJson).HasColumnName("validation_rules").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.Order).HasColumnName("order").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<EntityDefinition>()
            .WithMany()
            .HasForeignKey(e => e.EntityDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
