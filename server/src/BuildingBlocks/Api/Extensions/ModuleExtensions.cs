using BuildingBlocks.Web.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddModule<TModule>(
        this IServiceCollection services,
        IConfiguration configuration) where TModule : IModule, new()
    {
        var module = new TModule();
        EnsureMandatoryModuleProperties(module);
        return module.RegisterServices(services, configuration);
    }

    public static IApplicationBuilder UseModule<TModule>(
        this IApplicationBuilder app) where TModule : IModule, new()
    {
        var module = new TModule();
        EnsureMandatoryModuleProperties(module);
        return module.ConfigureMiddleware(app);
    }

    private static void EnsureMandatoryModuleProperties(IModule module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module.ModuleName, nameof(IModule.ModuleName));
        ArgumentException.ThrowIfNullOrWhiteSpace(module.SchemaName, nameof(IModule.SchemaName));
    }
}
