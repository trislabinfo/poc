using Feature.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Feature.Api;

public static class FeatureApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers Feature API layer (controllers from this assembly).
    /// </summary>
    public static IServiceCollection AddFeatureApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(FeatureController).Assembly);
        return services;
    }
}
