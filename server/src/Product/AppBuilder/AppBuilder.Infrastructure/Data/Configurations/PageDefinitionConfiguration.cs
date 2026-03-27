using AppDefinition.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppBuilder.Infrastructure.Data.Configurations;

public class PageDefinitionConfiguration : IEntityTypeConfiguration<PageDefinition>
{
    public void Configure(EntityTypeBuilder<PageDefinition> builder)
    {
        builder.ToTable("page_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").IsRequired();
        builder.Property(e => e.AppDefinitionId).HasColumnName("application_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Route).HasColumnName("route").HasMaxLength(500).IsRequired();
        builder.Property(e => e.ConfigurationJson).HasColumnName("configuration_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<AppDefinition.Domain.Entities.Application.AppDefinition>()
            .WithMany()
            .HasForeignKey(e => e.AppDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
