using AppBuilder.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace AppBuilder.Api;

public static class AppBuilderApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers AppBuilder API layer (controllers from this assembly).
    /// </summary>
    public static IServiceCollection AddAppBuilderApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(AppDefinitionsController).Assembly);
        return services;
    }
}
