using Identity.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Api;

public static class IdentityApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers Identity API layer (controllers from this assembly).
    /// </summary>
    public static IServiceCollection AddIdentityApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(IdentityController).Assembly);
        return services;
    }
}
