using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Entities.Lifecycle;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using TenantApplicationEntity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Infrastructure.Data;

/// <summary>EF Core DbContext for TenantApplication (tenantapplication schema). Uses shared AppDefinition.Domain entities.</summary>
public class TenantApplicationDbContext : BaseModuleDbContext
{
    public TenantApplicationDbContext(DbContextOptions<TenantApplicationDbContext> options)
        : base(options)
    {
    }

    protected override string SchemaName => "tenantapplication";

    public DbSet<TenantApplicationEntity> TenantApplications => Set<TenantApplicationEntity>();
    public DbSet<TenantApplication.Domain.Entities.TenantApplicationEnvironment> TenantApplicationEnvironments => Set<TenantApplication.Domain.Entities.TenantApplicationEnvironment>();
    public DbSet<TenantApplication.Domain.Entities.TenantApplicationMigration> TenantApplicationMigrations => Set<TenantApplication.Domain.Entities.TenantApplicationMigration>();

    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<PropertyDefinition> PropertyDefinitions => Set<PropertyDefinition>();
    public DbSet<RelationDefinition> RelationDefinitions => Set<RelationDefinition>();
    public DbSet<NavigationDefinition> NavigationDefinitions => Set<NavigationDefinition>();
    public DbSet<PageDefinition> PageDefinitions => Set<PageDefinition>();
    public DbSet<DataSourceDefinition> DataSourceDefinitions => Set<DataSourceDefinition>();
    public DbSet<ApplicationRelease> ApplicationReleases => Set<ApplicationRelease>();
    public DbSet<ReleaseEntityView> ReleaseEntityViews => Set<ReleaseEntityView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations first
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantApplicationDbContext).Assembly);

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
