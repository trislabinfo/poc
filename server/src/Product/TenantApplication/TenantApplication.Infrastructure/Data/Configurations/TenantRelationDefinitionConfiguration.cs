using AppDefinition.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TenantApplication.Infrastructure.Data.Configurations;

public sealed class TenantRelationDefinitionConfiguration : IEntityTypeConfiguration<RelationDefinition>
{
    public void Configure(EntityTypeBuilder<RelationDefinition> builder)
    {
        builder.ToTable("tenant_relation_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").IsRequired();
        builder.Property(e => e.SourceEntityId).HasColumnName("source_entity_id").IsRequired();
        builder.Property(e => e.TargetEntityId).HasColumnName("target_entity_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.RelationType).HasColumnName("relation_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.CascadeDelete).HasColumnName("cascade_delete").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        // tenant_relation_definitions table has no updated_at column
        builder.Ignore(e => e.UpdatedAt);

        builder.HasOne<EntityDefinition>()
            .WithMany()
            .HasForeignKey(e => e.SourceEntityId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<EntityDefinition>()
            .WithMany()
            .HasForeignKey(e => e.TargetEntityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
