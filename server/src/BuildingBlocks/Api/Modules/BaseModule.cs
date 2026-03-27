using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.Modules;

/// <summary>
/// Base class for module registration. Implements <see cref="IModule"/> and delegates to abstract methods.
/// The host calls <see cref="BuildingBlocksExtensions.AddBuildingBlocks"/> (no args) for pipeline behaviors and infrastructure.
/// Derived modules register their own MediatR handlers and validators in Application, plus Infrastructure and API (controllers).
/// </summary>
public abstract class BaseModule : IModule
{
    /// <inheritdoc />
    public abstract string ModuleName { get; }

    /// <inheritdoc />
    public abstract string SchemaName { get; }

    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterModuleServices(services, configuration);
        return services;
    }

    /// <inheritdoc />
    public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
    {
        return ConfigureModuleMiddleware(app);
    }

    /// <summary>
    /// Register module-specific services (Application: MediatR + validators; Infrastructure: DbContext, repositories, UoW; API: controllers).
    /// </summary>
    protected abstract void RegisterModuleServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Configure module-specific middleware. Default implementation returns <paramref name="app"/> unchanged.
    /// </summary>
    protected virtual IApplicationBuilder ConfigureModuleMiddleware(IApplicationBuilder app) => app;
}
