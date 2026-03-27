using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base DbContext for module databases. Applies schema and common conventions.
/// </summary>
public abstract class BaseModuleDbContext : DbContext
{
    /// <summary>
    /// Schema name for this module (e.g. "tenant", "identity"). Applied in OnModelCreating.
    /// </summary>
    protected abstract string SchemaName { get; }

    /// <summary>
    /// Default max length for string properties when not specified in configuration.
    /// </summary>
    protected virtual int DefaultStringMaxLength => 500;

    /// <inheritdoc />
    protected BaseModuleDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        ConfigureModuleConventions(modelBuilder);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<string>().HaveMaxLength(DefaultStringMaxLength);
    }

    /// <summary>
    /// Override to add module-specific model configuration (e.g. conventions). Default does nothing.
    /// Entity configurations are typically applied via ApplyConfigurationsFromAssembly in the derived DbContext.
    /// </summary>
    protected virtual void ConfigureModuleConventions(ModelBuilder modelBuilder)
    {
    }
}
