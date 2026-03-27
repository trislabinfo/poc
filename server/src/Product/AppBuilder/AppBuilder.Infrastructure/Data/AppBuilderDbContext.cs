using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Entities.Lifecycle;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the AppBuilder module. Schema is appbuilder.
/// </summary>
public class AppBuilderDbContext : BaseModuleDbContext
{
    public AppBuilderDbContext(DbContextOptions<AppBuilderDbContext> options)
        : base(options)
    {
    }

    protected override string SchemaName => "appbuilder";

    public DbSet<AppDefinition.Domain.Entities.Application.AppDefinition> AppDefinitions => Set<AppDefinition.Domain.Entities.Application.AppDefinition>();
    public DbSet<ApplicationRelease> ApplicationReleases => Set<ApplicationRelease>();
    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<PropertyDefinition> PropertyDefinitions => Set<PropertyDefinition>();
    public DbSet<RelationDefinition> RelationDefinitions => Set<RelationDefinition>();
    public DbSet<NavigationDefinition> NavigationDefinitions => Set<NavigationDefinition>();
    public DbSet<PageDefinition> PageDefinitions => Set<PageDefinition>();
    public DbSet<DataSourceDefinition> DataSourceDefinitions => Set<DataSourceDefinition>();
    public DbSet<ReleaseEntityView> ReleaseEntityViews => Set<ReleaseEntityView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations first
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppBuilderDbContext).Assembly);

        // Apply UTC converters globally for any DateTime properties that don't have explicit configuration
        // This ensures all DateTime values are UTC when saving to PostgreSQL timestamp with time zone columns
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    // Only apply if not already configured (explicit configurations take precedence)
                    if (property.GetValueConverter() == null)
                    {
                        property.SetValueConverter(new UtcDateTimeConverter());
                    }
                    if (property.GetColumnType() == null)
                    {
                        property.SetColumnType("timestamp with time zone");
                    }
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    // Only apply if not already configured (explicit configurations take precedence)
                    if (property.GetValueConverter() == null)
                    {
                        property.SetValueConverter(new UtcNullableDateTimeConverter());
                    }
                    if (property.GetColumnType() == null)
                    {
                        property.SetColumnType("timestamp with time zone");
                    }
                }
            }
        }
    }
}
