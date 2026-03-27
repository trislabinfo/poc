using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.ValueConverters;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Identity module. Supports multiple deployment topologies via configuration.
/// CRITICAL: This DbContext can ONLY access the 'identity' schema (Monolith/MultiApp) or its own
/// database (Microservices). It CANNOT access other module schemas or databases.
/// </summary>
public class IdentityDbContext : BaseModuleDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override string SchemaName => "identity";

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations first
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

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
