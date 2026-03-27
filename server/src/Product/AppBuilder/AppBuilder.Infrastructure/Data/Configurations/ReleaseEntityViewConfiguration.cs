using AppDefinition.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppBuilder.Infrastructure.Data.Configurations;

/// <summary>Maps ReleaseEntityView to appbuilder.release_entity_views.</summary>
public sealed class ReleaseEntityViewConfiguration : IEntityTypeConfiguration<ReleaseEntityView>
{
    public void Configure(EntityTypeBuilder<ReleaseEntityView> builder)
    {
        builder.ToTable("release_entity_views");

        builder.HasKey(e => new { e.ReleaseId, e.EntityId, e.ViewType });

        builder.Property(e => e.ReleaseId).HasColumnName("release_id").IsRequired();
        builder.Property(e => e.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(e => e.ViewType).HasColumnName("view_type").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Html).HasColumnName("html").HasColumnType("text").IsRequired();
    }
}
