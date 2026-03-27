using Microsoft.Extensions.DependencyInjection;
using Tenant.Api.Controllers;

namespace Tenant.Api;

public static class TenantApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers Tenant API layer (controllers from this assembly).
    /// </summary>
    public static IServiceCollection AddTenantApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(TenantController).Assembly);
        return services;
    }
}
