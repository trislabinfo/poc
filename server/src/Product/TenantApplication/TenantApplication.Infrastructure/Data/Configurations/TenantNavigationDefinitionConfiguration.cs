using AppDefinition.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantApplicationEntity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Infrastructure.Data.Configurations;

public sealed class TenantNavigationDefinitionConfiguration : IEntityTypeConfiguration<NavigationDefinition>
{
    public void Configure(EntityTypeBuilder<NavigationDefinition> builder)
    {
        builder.ToTable("tenant_navigation_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").IsRequired();
        builder.Property(e => e.AppDefinitionId).HasColumnName("tenant_application_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.ConfigurationJson).HasColumnName("configuration_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<TenantApplicationEntity>()
            .WithMany()
            .HasForeignKey(e => e.AppDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
