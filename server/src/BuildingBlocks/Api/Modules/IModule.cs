using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.Modules;

/// <summary>
/// Defines the contract for module registration and configuration.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Unique module name (e.g., "Tenant", "Identity"). Mandatory: must be non-null and non-empty.
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Database schema name owned by this module (e.g., "tenant", "identity"). Mandatory: must be non-null and non-empty.
    /// </summary>
    string SchemaName { get; }

    /// <summary>
    /// Register module services into the DI container.
    /// </summary>
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Configure module-specific middleware (optional).
    /// </summary>
    IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app);
}

