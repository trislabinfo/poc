using BuildingBlocks.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppBuilder.Infrastructure.Data.Configurations;

public class AppDefinitionConfiguration : IEntityTypeConfiguration<AppDefinition.Domain.Entities.Application.AppDefinition>
{
    public void Configure(EntityTypeBuilder<AppDefinition.Domain.Entities.Application.AppDefinition> builder)
    {
        builder.ToTable("application_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").IsRequired();
        builder.Property(e => e.CurrentVersion).HasColumnName("current_version").HasMaxLength(50);
        builder.Property(e => e.IsPublic).HasColumnName("is_public").IsRequired();
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasConversion<UtcDateTimeConverter>()
            .IsRequired();
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasConversion<UtcNullableDateTimeConverter>()
            .IsRequired(false);

        builder.HasIndex(e => e.Slug).HasDatabaseName("ix_application_definitions_slug").IsUnique();
        builder.Ignore(e => e.DomainEvents);
    }
}
