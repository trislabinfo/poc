using AppRuntime.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace AppRuntime.Api;

public static class AppRuntimeApiServiceCollectionExtensions
{
    public static IServiceCollection AddAppRuntimeApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(CompatibilityController).Assembly);
        return services;
    }
}
