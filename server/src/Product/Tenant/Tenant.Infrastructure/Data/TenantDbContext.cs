using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;

namespace Tenant.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Tenant module. Schema is defined on the type (tenant).
/// </summary>
public class TenantDbContext : BaseModuleDbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    protected override string SchemaName => "tenant";

    public DbSet<Tenant.Domain.Entities.Tenant> Tenants => Set<Tenant.Domain.Entities.Tenant>();
    public DbSet<Tenant.Domain.Entities.TenantUser> TenantUsers => Set<Tenant.Domain.Entities.TenantUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations first
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly);

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
